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

				String tableCommand = "CREATE TABLE IF NOT EXISTS Item (" +
											"id INTEGER PRIMARY KEY, " +
											"path NVARCHAR(2048) NULL" +
									  ")";

				SqliteCommand createTable = new SqliteCommand(tableCommand, db);

				createTable.ExecuteReader();
			}
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
				sql_command.CommandText = "DELETE FROM Item WHERE id = @id;";
				sql_command.Parameters.AddWithValue("@id", id);
				sql_command.ExecuteReader();

				// db.Close();
			}
		}

	}
}
