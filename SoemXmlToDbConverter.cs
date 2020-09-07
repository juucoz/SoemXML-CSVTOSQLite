using Serilog;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Linq;

namespace SoemXmlToSQLite
{
    internal static class SoemXmlToDbConverter
    {
        public static void Convert(
            TextFileParseOutput parsedFile,
            SQLiteConnection dbConnection,
            Dictionary<string, Dictionary<string, int>> columnIndices,
            Dictionary<string, SQLiteCommand> dbInsertCommandCache)
        {
            using (DbTransaction dbTransaction = dbConnection.BeginTransaction())
            {
                var start = StopwatchProxy.Instance.Stopwatch.ElapsedMilliseconds;
                Log.Information(@"SQLiteInsert start time for {Full_File_Path} is {Write_Start_Time} for table {target_table}", parsedFile.FilePath, start, dbConnection.FileName);
                int counter = 0;
                foreach (var datum in parsedFile.Data)
                {
                    if (datum.Count > 2)
                    {
                        Dictionary<string, int> currentObjectColumnIndices;
                        if (!columnIndices.TryGetValue(parsedFile.Class, out currentObjectColumnIndices))
                        {
                            currentObjectColumnIndices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                            int columnIndex = 0;
                            foreach (KeyValuePair<string, string> rowValues in datum)
                            {
                                string parameterName = rowValues.Key;
                                string parameterValue = rowValues.Value;
                                currentObjectColumnIndices.Add(parameterName, columnIndex);
                                columnIndex++;
                            }
                            columnIndices.Add(parsedFile.Class, currentObjectColumnIndices);
                            string dbCommandText = $"CREATE TABLE {parsedFile.Class} ({string.Join(",", datum.Select(d => $"[{d.Key}] TEXT COLLATE NOCASE "))})";
                            using (SQLiteCommand dbCommand = new SQLiteCommand(dbCommandText, dbConnection))
                            {
                                dbCommand.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            foreach (KeyValuePair<string, string> rowValues in datum)
                            {
                                string parameterName = rowValues.Key;
                                int columnIndex;
                                if (!currentObjectColumnIndices.TryGetValue(parameterName, out columnIndex))
                                {
                                    string dbCommandText = $"ALTER TABLE {parsedFile.Class} ADD [{parameterName}] TEXT COLLATE NOCASE";
                                    using (SQLiteCommand dbCommand = new SQLiteCommand(dbCommandText, dbConnection))
                                    {
                                        dbCommand.ExecuteNonQuery();
                                    }
                                }

                            }
                        }
                        SQLiteCommand dbInsertCommand;
                        if (!dbInsertCommandCache.TryGetValue(parsedFile.Class, out dbInsertCommand))
                        {
                            dbInsertCommand = new SQLiteCommand($"INSERT INTO [{parsedFile.Class}] VALUES ({string.Join(",", Enumerable.Repeat("?", currentObjectColumnIndices.Count))})", dbConnection);
                            for (int i = 0; i < currentObjectColumnIndices.Count; i++)
                                dbInsertCommand.Parameters.Add(new SQLiteParameter());
                            dbInsertCommandCache.Add(parsedFile.Class, dbInsertCommand);
                        }
                        else
                        {
                            if (dbInsertCommand.Parameters.Count != currentObjectColumnIndices.Count)
                            {
                                dbInsertCommand.CommandText = $"INSERT INTO [{parsedFile.Class}] VALUES ({string.Join(",", Enumerable.Repeat("?", currentObjectColumnIndices.Count))})";
                                int numberOfParametersToAdd = currentObjectColumnIndices.Count - dbInsertCommand.Parameters.Count;
                                for (int i = 0; i < numberOfParametersToAdd; i++)
                                {
                                    dbInsertCommand.Parameters.Add(new SQLiteParameter());
                                }
                            }
                        }

                        for (int i = 0; i < dbInsertCommand.Parameters.Count; i++)
                        {
                            dbInsertCommand.Parameters[i].Value = null;
                        }

                        foreach (KeyValuePair<string, string> rowValue in datum)
                        {
                            string parameterName = rowValue.Key;
                            string parameterValue = rowValue.Value;
                            int columnIndex = currentObjectColumnIndices[parameterName];
                            dbInsertCommand.Parameters[columnIndex].Value = parameterValue;
                        }

                        try
                        {
                            dbInsertCommand.ExecuteNonQuery();
                        }
                        catch (ObjectDisposedException o)
                        {
                            counter++;
                            //if (counter == (parsedFile.Data.Count))
                            //{
                            //    Console.WriteLine("This table has already been inserted.");
                            //}
                            return;
                        }
                        
                    }

                }

                var stop = StopwatchProxy.Instance.Stopwatch.ElapsedMilliseconds;

                Log.Information("SQLiteInsert end time for {Full_File_Path} is {Write_End_Time} and the duration is {Insert_Duration} with {success_failure} for table {target_table}", parsedFile.FilePath, stop, stop - start, $"{parsedFile.Data.Count}" + "/" + $"{counter}", dbConnection.FileName);
                dbTransaction.Commit();
            }
            SoemXmlToDbConverter.LogToTable(parsedFile.logValues, columnIndices);
        }



        internal static void Convert(
            TextFileParseOutput parsedFile,
            SQLiteConnection dbConnection,
            Dictionary<string, Dictionary<string, int>> columnIndices)

        {
            var start = StopwatchProxy.Instance.Stopwatch.ElapsedMilliseconds;
            Log.Information("SQLiteInsert start time for {Full_File_Path} is {Write_Start_Time} for table {target_table}", parsedFile.FilePath, start, dbConnection.FileName);
            Console.WriteLine("Watch for db actions have started");

            using (DbTransaction dbTransaction = dbConnection.BeginTransaction())
            {
                Dictionary<string, int> currentObjectColumnIndices;
                if (!columnIndices.TryGetValue(parsedFile.Ne + " " + parsedFile.Type, out currentObjectColumnIndices))
                {
                    string dbCommandText = $"CREATE TABLE [{parsedFile.Ne + " " + parsedFile.Type}] ({string.Join(",", parsedFile.Headers.Select(p => $"[{p}] TEXT COLLATE NOCASE"))})";
                    using (SQLiteCommand dbCommand = new SQLiteCommand(dbCommandText, dbConnection))
                    {
                        dbCommand.ExecuteNonQuery();
                    }
                }

                string text = $"INSERT INTO [{parsedFile.Ne + " " + parsedFile.Type}] VALUES({string.Join(",", Enumerable.Repeat("?", parsedFile.Headers.Count))})";
                SQLiteCommand command = new SQLiteCommand(text, dbConnection);
                for (int i = 0; i < parsedFile.Headers.Count; i++)
                {
                    command.Parameters.Add(new SQLiteParameter());
                }
                foreach (var datum in parsedFile.Data)
                {
                    int counter = 0;
                    foreach (KeyValuePair<string, string> rowValue in datum)
                    {
                        command.Parameters[counter].Value = rowValue.Value;
                        counter++;
                    }
                    command.ExecuteNonQuery();

                    //string dbInsertText = $"INSERT INTO [{parsedFile.Ne + " " + parsedFile.Type}] VALUES({string.Join(",", datum.Select(d => $"'{d.Value}'"))})";
                    //using (SQLiteCommand dbCommand = new SQLiteCommand(dbInsertText, dbConnection))
                    //{
                    //    dbCommand.ExecuteNonQuery();
                    //}
                }
                var stop = StopwatchProxy.Instance.Stopwatch.ElapsedMilliseconds;
                parsedFile.logValues.Logs["Load_Duration"] = (stop - start).ToString();
                parsedFile.logValues.Logs["Target_Table"] = dbConnection.DataSource;
                Log.Information("SQLiteInsert end time for {Full_File_Path} is {Write_End_Time} and the duration is {Insert_Duration} for table {target_table}", parsedFile.FilePath, stop, stop - start, dbConnection.FileName);
                dbTransaction.Commit();
            }
            SoemXmlToDbConverter.LogToTable(parsedFile.logValues, columnIndices);

        }
        private static void LogToTable(LogValues logValues, Dictionary<string, Dictionary<string, int>> columnIndices)
        {
            var builder = new SQLiteConnectionStringBuilder()
            {
                DataSource = "log.sqlite",
                Pooling = false
            };
            var logConnectionString = builder.ConnectionString;
            var logDBValues = new DBValues("log.sqlite");
            using (SQLiteConnection logConnection = new SQLiteConnection(logConnectionString))
            {
                logConnection.Open();

                if (!logDBValues.ColumnIndices.ContainsKey("Log"))
                {
                    columnIndices.Add("Log", new Dictionary<string, int>()); ;
                    string dbCommandText = $"CREATE TABLE Log ({string.Join(",", logValues.Logs.Select(l => $"[{l.Key}] TEXT COLLATE NOCASE "))})";


                    using (SQLiteCommand dbCommand = new SQLiteCommand(dbCommandText, logConnection)) 
                    {
                        dbCommand.ExecuteNonQuery(); 
                    }
                        

                }
                else
                {
                    string dbCommandText = $"INSERT INTO Log VALUES({string.Join(",", logValues.Logs.Select(l => $"'{l.Value}'"))})";
                    using (SQLiteCommand dbCommand = new SQLiteCommand(dbCommandText, logConnection))
                    {
                        dbCommand.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}


