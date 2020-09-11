using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;

namespace PiTnProcessor
{
    class DBValues
    {
        public string DbConnectionString { get; set; }
        public Dictionary<string, Dictionary<string, int>> ColumnIndices { get; set; }
        public Dictionary<string, SQLiteCommand> DbInsertCommandCache { get; set; }
        public HashSet<string> ExistingTableNames { get; set; }


        public static DBValues getDBValues(string dbFilePath)
        {
            var dbValues = new DBValues();
            var dbConnectionStringBuilder = new SQLiteConnectionStringBuilder
            {
                DataSource = dbFilePath,
                Pooling = false,
                //DefaultTimeout = 5000,
                SyncMode = SynchronizationModes.Off,
                JournalMode = SQLiteJournalModeEnum.Memory,
                //PageSize = 65536,
                //CacheSize = 16777216,
                //FailIfMissing = false,
                //ReadOnly = false
            };

        dbValues.DbConnectionString = dbConnectionStringBuilder.ConnectionString;
            

            using (SQLiteConnection dbConnection = new SQLiteConnection(dbValues.DbConnectionString))
            {
                dbConnection.Open();

                dbValues.ColumnIndices = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);
                dbValues.DbInsertCommandCache = new Dictionary<string, SQLiteCommand>(StringComparer.OrdinalIgnoreCase);

                dbValues.ExistingTableNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using (SQLiteCommand dbCommand = new SQLiteCommand("SELECT name FROM sqlite_master WHERE type='table'", dbConnection))
                {
                    using (SQLiteDataReader dbReader = dbCommand.ExecuteReader())
                    {
                        while (dbReader.Read())
                        {
                            string tableName = dbReader.GetString(0);
        dbValues.ExistingTableNames.Add(tableName);
                        }
}
                }
                foreach (string tableName in dbValues.ExistingTableNames)
{
    var columnNames = new List<string>();
    using (SQLiteCommand dbCommand = new SQLiteCommand($"pragma table_info([{tableName}])", dbConnection))
    {
        using (SQLiteDataReader dbReader = dbCommand.ExecuteReader())
        {
            while (dbReader.Read())
            {
                string columnName = dbReader.GetString(1);
                columnNames.Add(columnName);
            }
        }
    }
    dbValues.ColumnIndices.Add(tableName, columnNames.Select((s, i) => new { s, i }).ToDictionary(o => o.s, o => o.i, StringComparer.OrdinalIgnoreCase));
    var dbInsertCommand = new SQLiteCommand($"INSERT INTO [{tableName}] VALUES ({string.Join(",", Enumerable.Repeat("?", columnNames.Count))})", dbConnection);
    for (int i = 0; i < columnNames.Count; i++)
        dbInsertCommand.Parameters.Add(new SQLiteParameter());
    dbValues.DbInsertCommandCache.Add(tableName, dbInsertCommand);
}



            }


            return dbValues;

        }


    }
}