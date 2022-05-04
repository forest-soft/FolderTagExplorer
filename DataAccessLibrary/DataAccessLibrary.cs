﻿using System;
using Microsoft.Data.Sqlite;
using Windows.Storage;
using System.IO;
using System.Collections.Generic;

namespace DataAccessLibrary
{
	public static class DataAccess
	{
		private static string default_data_name = "Main";


		/// <summary>
		/// Sqliteへのコネクションを取得する。
		/// </summary>
		/// <param name="data_name">データ名</param>
		/// <returns>コネクションオブジェクト</returns>
		private static SqliteConnection GetConnection(string data_name = null)
		{
			if (data_name == null)
			{
				data_name = default_data_name;
			}
			
			string dbpath = Path.Combine(ApplicationData.Current.LocalFolder.Path, data_name + ".db");
			SqliteConnection connection = new SqliteConnection($"Filename={dbpath}");
			return connection;
		}
		
		/// <summary>
		/// Sqliteファイルの初期化処理を行う。
		/// </summary>
		/// <param name="data_name">データ名</param>
		public async static void InitializeDatabase(string data_name = null)
		{
			if (data_name == null)
			{
				data_name = default_data_name;
			}
			
			await ApplicationData.Current.LocalFolder.CreateFileAsync(data_name + ".db", CreationCollisionOption.OpenIfExists);
			using (SqliteConnection db = GetConnection())
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
		
		public static Dictionary<String, String> GetItemByPath(string path)
		{
			Dictionary<String, String> result = null;
			
			using (SqliteConnection db = GetConnection())
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
		
		public static List<Dictionary<String, String>> SelectAllData()
		{
			List<Dictionary<String, String>> entries = new List<Dictionary<String, String>>();
			
			using (SqliteConnection db = GetConnection())
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
		
		public static void AddData(string inputText)
		{
			using (SqliteConnection db = GetConnection())
			{
				db.Open();
				
				SqliteCommand insertCommand = new SqliteCommand();
				insertCommand.Connection = db;
				
				// Use parameterized query to prevent SQL injection attacks
				insertCommand.CommandText = "INSERT INTO Item (id, path) VALUES (NULL, @Entry);";
				insertCommand.Parameters.AddWithValue("@Entry", inputText);
				
				insertCommand.ExecuteReader();
				
				// db.Close();
			}
			
		}
		
	}
}
