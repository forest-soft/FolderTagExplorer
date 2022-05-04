using System;
using Microsoft.Data.Sqlite;
using Windows.Storage;
using System.IO;
using System.Collections.Generic;

namespace DataAccessLibrary
{
    public static class DataAccess
    {
        public async static void InitializeDatabase()
        {
            await ApplicationData.Current.LocalFolder.CreateFileAsync("sqliteSample.db", CreationCollisionOption.OpenIfExists);
            string dbpath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "sqliteSample.db");
            using (SqliteConnection db = new SqliteConnection($"Filename={dbpath}"))
            {
                db.Open();

                String tableCommand = "CREATE TABLE IF NOT EXISTS Folder (" +
                                            "id INTEGER PRIMARY KEY, " +
                                            "path NVARCHAR(2048) NULL" +
                                      ")";

                SqliteCommand createTable = new SqliteCommand(tableCommand, db);

                createTable.ExecuteReader();
            }
        }

        public static void AddData(string inputText)
        {
            string dbpath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "sqliteSample.db");
            using (SqliteConnection db = new SqliteConnection($"Filename={dbpath}"))
            {
                db.Open();

                SqliteCommand insertCommand = new SqliteCommand();
                insertCommand.Connection = db;

                // Use parameterized query to prevent SQL injection attacks
                insertCommand.CommandText = "INSERT INTO Folder (id, path) VALUES (NULL, @Entry);";
                insertCommand.Parameters.AddWithValue("@Entry", inputText);

                insertCommand.ExecuteReader();

                db.Close();
            }

        }

        public static List<Dictionary<String, String>> SelectAllData()
        {
            List<Dictionary<String, String>> entries = new List<Dictionary<String, String>>();

            string dbpath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "sqliteSample.db");
            using (SqliteConnection db = new SqliteConnection($"Filename={dbpath}"))
            {
                db.Open();

                SqliteCommand sql_command = new SqliteCommand();
                sql_command.Connection = db;

                // Use parameterized query to prevent SQL injection attacks
                sql_command.CommandText = "SELECT * FROM Folder ORDER BY id ASC;";

                SqliteDataReader query = sql_command.ExecuteReader();

                while (query.Read())
                {
                    Dictionary<String, String> data = new Dictionary<String, String>();
                    data["id"] = query["id"].ToString();
                    data["path"] = query["path"].ToString();
                    // entries.Add(query.GetString(0));
                    entries.Add(data);
                }

                db.Close();
            }

            return entries;
        }


    }
}
