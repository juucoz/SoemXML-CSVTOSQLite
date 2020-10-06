﻿using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;

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
            var dbConnectionStringBuilder = new SQLiteConnectionStringBuilder
            {
                DataSource = args[1],
                Pooling = false,
            };

            string dbConnectionString = dbConnectionStringBuilder.ConnectionString;

            using (SQLiteConnection dbConnection = new SQLiteConnection(dbConnectionString))
            {
                dbConnection.Open();

                var ColumnIndices = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);
                var dbInsertCommandCache = new Dictionary<string, SQLiteCommand>(StringComparer.OrdinalIgnoreCase);

                var ExistingTableNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using (SQLiteCommand dbCommand = new SQLiteCommand("SELECT name FROM sqlite_master WHERE type='table'", dbConnection))
                {
                    using (SQLiteDataReader dbReader = dbCommand.ExecuteReader())
                    {
                        while (dbReader.Read())
                        {
                            string tableName = dbReader.GetString(0);
                            ExistingTableNames.Add(tableName);
                        }
                    }
                }
                foreach (string tableName in ExistingTableNames)
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
                    ColumnIndices.Add(tableName, columnNames.Select((s, i) => new { s, i }).ToDictionary(o => o.s, o => o.i, StringComparer.OrdinalIgnoreCase));
                    var dbInsertCommand = new SQLiteCommand($"INSERT INTO [{tableName}] VALUES ({string.Join(",", Enumerable.Repeat("?", columnNames.Count))})", dbConnection);
                    for (int i = 0; i < columnNames.Count; i++)
                        dbInsertCommand.Parameters.Add(new SQLiteParameter());
                    dbInsertCommandCache.Add(tableName, dbInsertCommand);
                }

                string sourceFileMask = args[0];
                string directoryPath = Path.GetDirectoryName(sourceFileMask);
                string fileMask = Path.GetFileName(sourceFileMask);
                if (Directory.Exists(Path.Join(directoryPath)))
                {
                    
                    foreach (string filePath in Directory.EnumerateFiles(directoryPath, fileMask,searchOption:SearchOption.AllDirectories))
                    {
                        //var values = new DBValues(dbFilePath);
                        string fileName = Path.GetFileName(filePath);

                        //var start = StopwatchProxy.Instance.Stopwatch.ElapsedMilliseconds;

                        if (fileName.Contains("xcm.gz"))
                        {
                            XCMParser xcmParser = new XCMParser();
                            
                            Console.WriteLine(fileName);

                            using (FileStream oldstream = File.OpenRead(filePath))
                            using (GZipStream zippedStream = new GZipStream(oldstream, CompressionMode.Decompress))
                            {
                                if (filePath.Contains(".xcm.gz"))
                                {
                                    xcmParser.setXMLParser(zippedStream);
                                }
                                else
                                {
                                    xcmParser.setXMLParser(oldstream);
                                }
                                //var stop = StopwatchProxy.Instance.Stopwatch.ElapsedMilliseconds;
                                //Log.Information("Parse end time for {Full_File_Path} is {Parse_End_Time} and the duration is {Parse_Duration}", filePath, stop, stop - start);
                                XCMConverter.Convert(
                                        xcmParser,
                                        zippedStream,
                                        oldstream,
                                        filePath,
                                        dbConnection,
                                        ColumnIndices,
                                        dbInsertCommandCache
                                        
                                
                            );
                            }
                        }
                    }
                }
                else
                {
                    directoryPath = ".";
                    //Log.Error(new FileNotFoundException(), "This directory {Full_File_Path} does not exist.", Path.Join(options.InputPath, $"{selectedFolder}"));
                    Console.WriteLine("This directory doesn't exist.");
                    return;
                }


            }
        }
    }
}