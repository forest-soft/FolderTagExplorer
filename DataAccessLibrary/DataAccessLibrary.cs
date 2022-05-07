using System;
using Microsoft.Data.Sqlite;
using Windows.Storage;
using System.IO;
using System.Collections.Generic;

namespace DataAccessLibrary
{
	public static class DataAccess
	{
		/// <summary>
		/// Sqliteへのコネクションを取得する。
		/// </summary>
		/// <param name="data_name">データ名</param>
		/// <returns>コネクションオブジェクト</returns>
		private static SqliteConnection GetConnection(string data_name)
		{
			string dbpath = Path.Combine(ApplicationData.Current.LocalFolder.Path, data_name + ".db");
			SqliteConnection connection = new SqliteConnection($"Filename={dbpath}");
			return connection;
		}

		/// <summary>
		/// Sqliteファイルの初期化処理を行う。
		/// </summary>
		/// <param name="data_name">データ名</param>
		public async static void InitializeDatabase(string data_name)
		{
			await ApplicationData.Current.LocalFolder.CreateFileAsync(data_name + ".db", CreationCollisionOption.OpenIfExists);
			using (SqliteConnection db = GetConnection(data_name))
			{
				db.Open();

				SqliteCommand command = new SqliteCommand();
				command.Connection = db;

				String sql = "";

				// Itemテーブル
				sql = "SELECT count(*) FROM sqlite_master WHERE type = 'table' AND name = 'Item'";
				command.CommandText = sql;
				if (command.ExecuteScalar().ToString() == "0")
				{
					sql = "CREATE TABLE Item(" +
								"id INTEGER NOT NULL PRIMARY KEY" +
								", path NVARCHAR(2048) NOT NULL" +
							")";
					command.CommandText = sql;
					command.ExecuteNonQuery();
				}


				// Tagテーブル
				sql = "SELECT count(*) FROM sqlite_master WHERE type = 'table' AND name = 'Tag'";
				command.CommandText = sql;
				if (command.ExecuteScalar().ToString() == "0")
				{
					sql = "CREATE TABLE Tag(" +
								"id INTEGER NOT NULL PRIMARY KEY" +
								", name NVARCHAR(2048) NOT NULL" +
							")";
					command.CommandText = sql;
					command.ExecuteNonQuery();
				}

				// Tag×Itemのリレーションテーブル
				sql = "SELECT count(*) FROM sqlite_master WHERE type = 'table' AND name = 'R_TAG_FOR_ITEM'";
				command.CommandText = sql;
				if (command.ExecuteScalar().ToString() == "0")
				{
					sql = "CREATE TABLE R_TAG_FOR_ITEM(" +
							"tag_id INTEGER NOT NULL" +
							", item_id INTEGER NOT NULL" +
							", FOREIGN KEY (tag_id) REFERENCES Tag(id)" +
							", FOREIGN KEY (item_id) REFERENCES Item(id)" +
							", UNIQUE (tag_id, item_id)" +
						");";
					command.CommandText = sql;
					command.ExecuteNonQuery();
				}
			}
		}

		public static Dictionary<String, Object> GetItem(string data_name, string id)
		{
			Dictionary<String, Object> result = null;

			using (SqliteConnection db = GetConnection(data_name))
			{
				db.Open();

				SqliteCommand sql_command = new SqliteCommand();
				sql_command.Connection = db;

				sql_command.CommandText = "SELECT * FROM Item WHERE id = @id";
				sql_command.Parameters.AddWithValue("@id", id);

				SqliteDataReader query = sql_command.ExecuteReader();

				while (query.Read())
				{
					result = new Dictionary<String, Object>();
					result["id"] = query["id"].ToString();
					result["path"] = query["path"].ToString();

					SqliteCommand sub_sql_command = new SqliteCommand();
					sub_sql_command.Connection = db;

					sub_sql_command.CommandText = "SELECT tag_id " +
													"FROM R_TAG_FOR_ITEM " +
													"WHERE item_id = @item_id " +
													"ORDER BY tag_id ASC";
					sub_sql_command.Parameters.AddWithValue("@item_id", result["id"]);
					SqliteDataReader sub_query = sub_sql_command.ExecuteReader();
					Dictionary<String, string> tag_id_list = new Dictionary<String, string>();
					while (sub_query.Read())
					{
						string tag_id = sub_query["tag_id"].ToString();
						tag_id_list[tag_id] = tag_id;
					}
					result["tag_id_list"] = tag_id_list;

					break;
				}
			}

			return result;
		}

		public static Dictionary<String, String> GetItemByPath(string data_name, string path)
		{
			Dictionary<String, String> result = null;

			using (SqliteConnection db = GetConnection(data_name))
			{
				db.Open();

				SqliteCommand sql_command = new SqliteCommand();
				sql_command.Connection = db;

				sql_command.CommandText = "SELECT * FROM Item WHERE path = @path;";
				sql_command.Parameters.AddWithValue("@path", path);

				SqliteDataReader query = sql_command.ExecuteReader();

				while (query.Read())
				{
					result = new Dictionary<String, String>();
					result["id"] = query["id"].ToString();
					result["path"] = query["path"].ToString();

					break;
				}
			}

			return result;
		}

		public static List<Dictionary<String, String>> SelectAllData(string data_name)
		{
			List<Dictionary<String, String>> entries = new List<Dictionary<String, String>>();

			using (SqliteConnection db = GetConnection(data_name))
			{
				db.Open();

				SqliteCommand sql_command = new SqliteCommand();
				sql_command.Connection = db;

				sql_command.CommandText = "SELECT * FROM Item ORDER BY id DESC;";

				SqliteDataReader query = sql_command.ExecuteReader();

				while (query.Read())
				{
					Dictionary<String, String> data = new Dictionary<String, String>();
					data["id"] = query["id"].ToString();
					data["path"] = query["path"].ToString();
					// entries.Add(query.GetString(0));
					entries.Add(data);
				}

				// db.Close();
			}

			return entries;
		}

		public static string AddData(string data_name, string inputText)
		{
			string id = null;
			using (SqliteConnection db = GetConnection(data_name))
			{
				db.Open();

				SqliteCommand insertCommand = new SqliteCommand();
				insertCommand.Connection = db;

				// Use parameterized query to prevent SQL injection attacks
				insertCommand.CommandText = "INSERT INTO Item (id, path) VALUES (NULL, @Entry);";
				insertCommand.Parameters.AddWithValue("@Entry", inputText);

				insertCommand.ExecuteReader();

				SqliteCommand SelectCommand = new SqliteCommand();
				SelectCommand.Connection = db;
				SelectCommand.CommandText = "SELECT last_insert_rowid();";
				id = SelectCommand.ExecuteScalar().ToString();

				// db.Close();
			}

			return id;
		}

		public static void DeleteItemData(string data_name, string id)
		{
			using (SqliteConnection db = GetConnection(data_name))
			{
				db.Open();

				SqliteCommand sql_command = new SqliteCommand();
				sql_command.Connection = db;

				sql_command.CommandText = "DELETE FROM R_TAG_FOR_ITEM WHERE item_id = @item_id;";
				sql_command.Parameters.AddWithValue("@item_id", id);
				sql_command.ExecuteNonQuery();
				sql_command.Parameters.Clear();

				sql_command.CommandText = "DELETE FROM Item WHERE id = @id;";
				sql_command.Parameters.AddWithValue("@id", id);
				sql_command.ExecuteNonQuery();
				sql_command.Parameters.Clear();

			}
		}


		public static string AddTagData(string data_name, string name)
		{
			string id = null;
			using (SqliteConnection db = GetConnection(data_name))
			{
				db.Open();

				SqliteCommand command = new SqliteCommand();
				command.Connection = db;

				command.CommandText = "INSERT INTO Tag (id, name) VALUES (NULL, @name);";
				command.Parameters.AddWithValue("@name", name);
				command.ExecuteNonQuery();

				command.CommandText = "SELECT last_insert_rowid();";
				id = command.ExecuteScalar().ToString();
			}

			return id;
		}

		public static Dictionary<string, Dictionary<String, String>> GetTagList(string data_name)
		{
			Dictionary<string, Dictionary<String, String>> result = new Dictionary<string, Dictionary<String, String>>();

			using (SqliteConnection db = GetConnection(data_name))
			{
				db.Open();

				SqliteCommand sql_command = new SqliteCommand();
				sql_command.Connection = db;
				sql_command.CommandText = "SELECT * FROM Tag ORDER BY name ASC;";

				SqliteDataReader query = sql_command.ExecuteReader();

				while (query.Read())
				{
					Dictionary<String, String> data = new Dictionary<String, String>();
					data["id"] = query["id"].ToString();
					data["name"] = query["name"].ToString();

					result[data["id"]] = data;
				}
			}

			return result;
		}

		public static Dictionary<String, String> GetTag(string data_name, string id)
		{
			Dictionary<String, String> result = null;

			using (SqliteConnection db = GetConnection(data_name))
			{
				db.Open();

				SqliteCommand sql_command = new SqliteCommand();
				sql_command.Connection = db;

				sql_command.CommandText = "SELECT * FROM Tag WHERE id = @id ";
				sql_command.Parameters.AddWithValue("@id", id);

				SqliteDataReader query = sql_command.ExecuteReader();

				while (query.Read())
				{
					result = new Dictionary<String, String>();
					result["id"] = query["id"].ToString();
					result["name"] = query["name"].ToString();

					break;
				}
			}

			return result;
		}


		public static Dictionary<String, String> GetTagByName(string data_name, string name, string ignore_id = null)
		{
			Dictionary<String, String> result = null;

			using (SqliteConnection db = GetConnection(data_name))
			{
				db.Open();

				SqliteCommand sql_command = new SqliteCommand();
				sql_command.Connection = db;

				sql_command.CommandText = "SELECT * FROM Tag WHERE name = @name ";
				sql_command.Parameters.AddWithValue("@name", name);

				if (ignore_id != null)
				{
					sql_command.CommandText += "AND id <> @ignore_id";
					sql_command.Parameters.AddWithValue("@ignore_id", ignore_id);
				}

				SqliteDataReader query = sql_command.ExecuteReader();

				while (query.Read())
				{
					result = new Dictionary<String, String>();
					result["id"] = query["id"].ToString();
					result["name"] = query["name"].ToString();

					break;
				}
			}

			return result;
		}

		public static string SaveTagRelationForItem(string data_name, string item_id, List<Dictionary<String, String>> tag_id_list)
		{
			string id = null;
			using (SqliteConnection db = GetConnection(data_name))
			{
				db.Open();

				SqliteCommand command = new SqliteCommand();
				command.Connection = db;

				command.CommandText = "DELETE FROM R_TAG_FOR_ITEM WHERE item_id = @item_id;";
				command.Parameters.AddWithValue("@item_id", item_id);
				command.ExecuteNonQuery();
				command.Parameters.Clear();

				foreach (var v in tag_id_list)
				{
					command.CommandText = "INSERT INTO R_TAG_FOR_ITEM (tag_id, item_id) VALUES (@tag_id, @item_id);";
					command.Parameters.AddWithValue("@tag_id", v["id"]);
					command.Parameters.AddWithValue("@item_id", item_id);
					command.ExecuteNonQuery();
					command.Parameters.Clear();
				}
			}

			return id;
		}

		public static void DeleteTagData(string data_name, string id)
		{
			using (SqliteConnection db = GetConnection(data_name))
			{
				db.Open();

				SqliteCommand sql_command = new SqliteCommand();
				sql_command.Connection = db;

				sql_command.CommandText = "DELETE FROM R_TAG_FOR_ITEM WHERE tag_id = @tag_id;";
				sql_command.Parameters.AddWithValue("@tag_id", id);
				sql_command.ExecuteNonQuery();
				sql_command.Parameters.Clear();

				sql_command.CommandText = "DELETE FROM Tag WHERE id = @id;";
				sql_command.Parameters.AddWithValue("@id", id);
				sql_command.ExecuteNonQuery();
				sql_command.Parameters.Clear();

			}
		}


	}
}
