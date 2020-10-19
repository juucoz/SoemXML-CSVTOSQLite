using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using XCM2SQLite;

namespace XCM2SQLite
{
    class Program
    {
        public class StopwatchProxy
        {
            private Stopwatch _stopwatch;
            private static readonly StopwatchProxy _stopwatchProxy = new StopwatchProxy();

            private StopwatchProxy()
            {
                _stopwatch = new Stopwatch();
            }

            public Stopwatch Stopwatch { get { return _stopwatch; } }

            public static StopwatchProxy Instance
            {
                get { return _stopwatchProxy; }
            }
        }
        static void Main(string[] args)
        {
            var opts = Options.GetOptions(args);
            if (opts is null)
            {
                return;
            }

            var inputPath = opts.InputPath;
            var dbFilePath = opts.DbFilePath;
            
            var dbValues = DBValues.GetDBValues(dbFilePath, inputPath);

        }
    }
}
class DBValues
{
    public SQLiteConnection DbConnection { get; set; }
    public Dictionary<string, Dictionary<string, int>> ColumnIndices { get; set; }
    public Dictionary<string, SQLiteCommand> DbInsertCommandCache { get; set; }
    public HashSet<string> ExistingTableNames { get; set; }

    public static DBValues GetDBValues(string dbFilePath, string inputPath)
    {
        var dbConnectionStringBuilder = new SQLiteConnectionStringBuilder
        {
            DataSource = dbFilePath,
            Pooling = false,
            //DefaultTimeout = 5000,
            //PageSize = 65536,
            //CacheSize = 16777216,
            //FailIfMissing = false,
            //ReadOnly = false
        };
        var fileCheckerdbConnectionStringBuilder = new SQLiteConnectionStringBuilder
        {
            DataSource = Path.GetFileNameWithoutExtension(dbFilePath) + ".files" + ".sqlite",
            Pooling = false,
            //DefaultTimeout = 5000,
            //PageSize = 65536,
            //CacheSize = 16777216,
            //FailIfMissing = false,
            //ReadOnly = false
        };
        using (SQLiteConnection fileCheckerdbConnection = new SQLiteConnection(fileCheckerdbConnectionStringBuilder.ConnectionString))
        {
            using (SQLiteConnection dbConnection = new SQLiteConnection(dbConnectionStringBuilder.ConnectionString))
            {
                fileCheckerdbConnection.Open();
                dbConnection.Open();
                FileValues.CallConverter(inputPath, dbConnection, fileCheckerdbConnection);
                return GetDBValues(dbConnection);
            }
        }
    }
    public static DBValues GetDBValues(SQLiteConnection dbConnection)
    {
        var dbValues = new DBValues();

        dbValues.DbConnection = dbConnection;
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
        return dbValues;
    }
}

