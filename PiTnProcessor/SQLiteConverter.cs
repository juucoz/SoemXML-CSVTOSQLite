using CommandLine;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace PiTnProcessor
{
    internal static class SQLiteConverter
    {
        public static void Convert(
            XMLParser parser,
            GZipStream zippedStream,
            FileStream stream,
            string filePath,
            SQLiteConnection dbConnection,
            Dictionary<string, Dictionary<string, int>> columnIndices,
            Dictionary<string, SQLiteCommand> dbInsertCommandCache)
        {
            bool zippedFlag = true;
            var start = StopwatchProxy.Instance.Stopwatch.ElapsedMilliseconds;
            var fileName = Path.GetFileName(filePath);
            var parseTime = new Stopwatch();
            var insertTime = new Stopwatch();
            using (DbTransaction dbTransaction = dbConnection.BeginTransaction())
            {

                XmlReaderSettings xmlReaderSettings = SQLiteConverter.StartXmlReader();

                using (parser.xmlReader)
                {
                    parser.xmlReader.MoveToContent();
                    if (parser.ReadConfig == "row")
                    {
                        while (!parser.xmlReader.EOF)
                        {
                            parser.xmlReader.Read();
                            if (string.Equals(parser.xmlReader.LocalName, parser.ReadConfig, StringComparison.OrdinalIgnoreCase))
                            {
                                break;
                            }
                        }

                    }
                    else
                    {
                        parser.xmlReader.Read();
                    }
                    var headElementName = parser.xmlReader.LocalName;

                    while (parser.xmlReader.LocalName == headElementName && parser.xmlReader.Depth != 0)
                    {
                        parseTime.Start();
                        var parsedRow = zippedFlag ? parser.Parse(zippedStream) : parser.Parse(stream);
                        if (parser.Flag is true)
                        {
                            
                            headElementName = parser.xmlReader.LocalName;
                            parser.xmlReader.Read();
                            parsedRow = zippedFlag ? parser.Parse(zippedStream) : parser.Parse(stream);
                        }
                        parseTime.Stop();



                        Dictionary<string, int> currentObjectColumnIndices;
                        if (!columnIndices.TryGetValue(FileValues.Class, out currentObjectColumnIndices))
                        {
                            currentObjectColumnIndices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                            int columnIndex = 0;

                            foreach (KeyValuePair<string, string> rowValue in parsedRow.RowValues)
                            {
                                string parameterName = rowValue.Key;
                                string parameterValue = rowValue.Value;
                                currentObjectColumnIndices.Add(parameterName, columnIndex);
                                columnIndex++;
                            }
                            columnIndices.Add(FileValues.Class, currentObjectColumnIndices);
                            string dbCommandText = $"CREATE TABLE {FileValues.Class} ( {string.Join(",", parsedRow.RowValues.Select(p => $"[{p.Key}] NUMBER COLLATE NOCASE"))})";
                            using (SQLiteCommand dbCommand = new SQLiteCommand(dbCommandText, dbConnection))
                            {

                                dbCommand.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            foreach (KeyValuePair<string, string> rowValue in parsedRow.RowValues)
                            {
                                string parameterName = rowValue.Key;


                                if (!currentObjectColumnIndices.ContainsKey(parameterName))
                                {
                                    string dbCommandText = $"ALTER TABLE {FileValues.Class} ADD [{parameterName}] NUMBER COLLATE NOCASE";
                                    using (SQLiteCommand dbCommand = new SQLiteCommand(dbCommandText, dbConnection))
                                    {
                                        dbCommand.ExecuteNonQuery();

                                    }
                                    Log.Information($"A new column {parameterName} is added to Table {FileValues.Class}");
                                    currentObjectColumnIndices.Add(parameterName, currentObjectColumnIndices.Count);
                                }

                            }
                        }
                        // 72 ms

                        if (!dbInsertCommandCache.TryGetValue(FileValues.Class, out var dbInsertCommand))
                        {
                            dbInsertCommand = new SQLiteCommand($"INSERT INTO [{FileValues.Class}] VALUES ({string.Join(",", Enumerable.Repeat("?", currentObjectColumnIndices.Count))})", dbConnection);
                            for (int i = 0; i < currentObjectColumnIndices.Count; i++)
                                dbInsertCommand.Parameters.Add(new SQLiteParameter());
                            dbInsertCommandCache.Add(FileValues.Class, dbInsertCommand);
                        }
                        else
                        {
                            //  dbInsertCommand.Connection = dbConnection;
                            if (dbInsertCommand.Parameters.Count != currentObjectColumnIndices.Count)
                            {
                                dbInsertCommand.Dispose();

                                dbInsertCommand = new SQLiteCommand($"INSERT INTO [{FileValues.Class}] VALUES ({string.Join(",", Enumerable.Repeat("?", currentObjectColumnIndices.Count))})", dbConnection);
                                for (int i = 0; i < currentObjectColumnIndices.Count; i++)
                                    dbInsertCommand.Parameters.Add(new SQLiteParameter());
                                dbInsertCommandCache[FileValues.Class] = dbInsertCommand;
                            }

                        }
                        // 72 ms
                        // 30ms
                        for (int i = 0; i < dbInsertCommand.Parameters.Count; i++)
                        {
                            dbInsertCommand.Parameters[i].Value = null;
                        }

                        foreach (KeyValuePair<string, string> rowValue in parsedRow.RowValues)
                        {
                            string parameterName = rowValue.Key;
                            string parameterValue = rowValue.Value;
                            int columnIndex = currentObjectColumnIndices[parameterName];
                            dbInsertCommand.Parameters[columnIndex].Value = parameterValue;
                        }

                        insertTime.Start();
                        dbInsertCommand.ExecuteNonQuery();
                        insertTime.Stop();

                    }

                    // Log.Information("Parse duration for {Full_File_Path} is {Parse_Duration} with {success_failure} for table {target_table}", stream.Name, parseTime.ElapsedMilliseconds, "-", dbConnection.FileName);
                    // Log.Information("SQLiteInsert duration for {Full_File_Path} is {Insert_Duration} with {success_failure} for table {target_table}", stream.Name, insertTime.ElapsedMilliseconds, "-", dbConnection.FileName);

                }


                dbTransaction.Commit();


                Console.WriteLine("Parse Time : {0} Complete Time: {1} Insert Time : {2}", parseTime.ElapsedMilliseconds, StopwatchProxy.Instance.Stopwatch.ElapsedMilliseconds, insertTime.ElapsedMilliseconds);
                var stop = StopwatchProxy.Instance.Stopwatch.ElapsedMilliseconds;
                // var lv = new LogValues(stream.Name, start, stop, parseTime.ElapsedMilliseconds, 0, 0, insertTime.ElapsedMilliseconds, dbConnection.DataSource);
                //SQLiteConverter.LogToTable(lv, columnIndices);
            }
        }


        internal static void Convert(
            CSVParser csvparser,
            GZipStream zippedStream,
            FileStream stream,
            string filePath,
            SQLiteConnection dbConnection,
            Dictionary<string, Dictionary<string, int>> columnIndices)

        {
            bool zippedFlag = true;
            bool statTimeFlag = true;
            var start = StopwatchProxy.Instance.Stopwatch.ElapsedMilliseconds;
            var parseTime = new Stopwatch();
            var insertTime = new Stopwatch();
            Log.Information("SQLiteInsert start time for {Full_File_Path} is {Write_Start_Time} for table {target_table}", FileValues.FilePath, start, dbConnection.FileName);
            Console.WriteLine("Watch for db actions have started");

            using (DbTransaction dbTransaction = dbConnection.BeginTransaction())
            {
                zippedFlag = zippedFlag ? filePath.Contains("csv.gz") : false;
                if (zippedFlag)
                {
                    csvparser.Init(zippedStream);
                }
                else
                {
                    csvparser.Init(stream);
                }
                TextFileParseOutput parsedRow = new TextFileParseOutput();
                Dictionary<string, int> currentObjectColumnIndices;
                while (parsedRow != null)
                {
                    parseTime.Start();
                    if (zippedFlag) 
                    {
                        parsedRow = csvparser.Parse(zippedStream);
                    }
                    else
                    {
                        parsedRow = csvparser.Parse(stream);
                    }
                    
                    parseTime.Stop();

                    if (parsedRow is null)
                    {
                        break;
                    }

                    if (!columnIndices.TryGetValue(FileValues.Ne + " " + FileValues.Class, out currentObjectColumnIndices))
                    {
                        columnIndices.Add(FileValues.Ne + " " + FileValues.Class, currentObjectColumnIndices);
                        string dbCommandText = $"CREATE TABLE [{FileValues.Ne + " " + FileValues.Class}] ({string.Join(",", parsedRow.RowValues.Select(p => $"[{p.Key}] NUMBER COLLATE NOCASE"))})";
                        using (SQLiteCommand dbCommand = new SQLiteCommand(dbCommandText, dbConnection))
                        {
                            dbCommand.ExecuteNonQuery();
                        }
                    }

                    string text = $"INSERT INTO [{FileValues.Ne + " " + FileValues.Class}] VALUES({string.Join(",", Enumerable.Repeat("?", parsedRow.RowValues.Keys.Count))})";
                    SQLiteCommand command = new SQLiteCommand(text, dbConnection);
                    for (int i = 0; i < parsedRow.RowValues.Count; i++)
                    {
                        command.Parameters.Add(new SQLiteParameter());
                    }

                    int counter = 0;
                    foreach (KeyValuePair<string, string> rowValue in parsedRow.RowValues)
                    {
                        command.Parameters[counter].Value = rowValue.Value;
                        counter++;
                    }
                    if (statTimeFlag)
                    {
                        try
                        {
                            long.TryParse(parsedRow.RowValues["statTime"], out var utcDateTime);
                            long.TryParse(parsedRow.RowValues["timeCaptured"], out utcDateTime);
                            command.Parameters[0].Value = DateTimeOffset.FromUnixTimeMilliseconds(utcDateTime).UtcDateTime;
                        }

                        catch (KeyNotFoundException k)
                        {
                            statTimeFlag = false;
                        }
                    }

                    insertTime.Start();
                    command.ExecuteNonQuery();
                    insertTime.Stop();
                }
                //string dbInsertText = $"INSERT INTO [{parsedFile.Ne + " " + parsedFile.Type}] VALUES({string.Join(",", datum.Select(d => $"'{d.Value}'"))})";
                //using (SQLiteCommand dbCommand = new SQLiteCommand(dbInsertText, dbConnection))
                //{
                //    dbCommand.ExecuteNonQuery();
                //}
                dbTransaction.Commit();


            }

            var stop = StopwatchProxy.Instance.Stopwatch.ElapsedMilliseconds;

            //var lv = new LogValues(input.Name, start, stop, parseTime.ElapsedMilliseconds, 0, 0, insertTime.ElapsedMilliseconds, dbConnection.DataSource);
            //SQLiteConverter.LogToTable(lv, columnIndices);
            //lv.Logs["Target_Table"] = dbConnection.DataSource;
            //Log.Information("SQLiteInsert end time for {Full_File_Path} is {Write_End_Time} and the duration is {Insert_Duration} for table {target_table}", parsedFile.FilePath, stop, stop - start, dbConnection.FileName);

            //SQLiteConverter.LogToTable(lv, columnIndices);

        }
        private static void LogToTable(LogValues logValues, Dictionary<string, Dictionary<string, int>> columnIndices)
        {
            var builder = new SQLiteConnectionStringBuilder()
            {
                DataSource = "log.sqlite",
                Pooling = false
            };
            var logConnectionString = builder.ConnectionString;
            // var logDBValues = new DBValues("log.sqlite");
            var logDBValues = DBValues.GetDBValues("log.sqlite");
            using (SQLiteConnection logConnection = new SQLiteConnection(logConnectionString))
            {
                logConnection.Open();

                if (!logDBValues.ColumnIndices.ContainsKey("Log"))
                {
                    columnIndices.Add("Log", new Dictionary<string, int>()); ;
                    string dbCommandText = $"CREATE TABLE Log ({string.Join(",", logValues.Logs.Select(l => $"[{l.Key}] NUMBER COLLATE NOCASE "))})";


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
        private static XmlReaderSettings StartXmlReader()
        {
            XmlReaderSettings xmlReaderSettings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Ignore,
                IgnoreComments = true,
                IgnoreProcessingInstructions = true,
                IgnoreWhitespace = true,
                CheckCharacters = false,
                ValidationFlags = System.Xml.Schema.XmlSchemaValidationFlags.None
            };
            return xmlReaderSettings;
        }
    }
}


