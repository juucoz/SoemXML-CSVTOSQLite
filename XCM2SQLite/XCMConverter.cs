using Serilog;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml;
using static XCM2SQLite.Program;

namespace XCM2SQLite
{
    class XCMConverter
    {
        internal static void Convert(
            XCMParser xcmParser,
            GZipStream zippedstream,
            FileStream stream,
            string filePath,
            SQLiteConnection dbConnection,
            SQLiteConnection fileCheckerdbConnection,
            Dictionary<string, Dictionary<string, int>> columnIndices,
            Dictionary<string, SQLiteCommand> dbInsertCommandCache
            )
        {
            bool zippedFlag = true;
            var start = StopwatchProxy.Instance.Stopwatch.ElapsedMilliseconds;
            var fileName = Path.GetFileName(filePath);
            var parseTime = new Stopwatch();
            var insertTime = new Stopwatch();
            using (DbTransaction dbTransaction = dbConnection.BeginTransaction())
            {

                XmlReaderSettings xmlReaderSettings = XCMConverter.StartXmlReader();
                using (xcmParser.xmlReader)
                {
                    string headElementName;
                    xcmParser.xmlReader.MoveToContent();
                    if (xcmParser.ReadConfig == "row")
                    {
                        while (!xcmParser.xmlReader.EOF)
                        {
                            xcmParser.xmlReader.Read();
                            if (string.Equals(xcmParser.xmlReader.LocalName, xcmParser.ReadConfig, StringComparison.OrdinalIgnoreCase))
                            {
                                break;
                            }
                        }
                        headElementName = xcmParser.xmlReader.LocalName;
                    }
                    else
                    {
                        xcmParser.xmlReader.Read();
                        headElementName = xcmParser.xmlReader.LocalName;
                        if (headElementName == "findToFileResponse" && xcmParser.xmlReader.NodeType.ToString() == "EndElement")
                        {
                            return;
                        }
                        var innerReader = xcmParser.xmlReader.ReadSubtree();

                        headElementName = xcmParser.xmlReader.LocalName;


                    }


                    while (xcmParser.xmlReader.EOF != true)
                    {
                        parseTime.Start();
                        var parsedElement = zippedFlag ? xcmParser.Parse(zippedstream) : xcmParser.Parse(stream);
                        if (parsedElement is null)
                        {
                            break;
                        }
                        if (xcmParser.Flag is true)
                        {

                            headElementName = xcmParser.xmlReader.LocalName;
                            xcmParser.xmlReader.Read();
                            parsedElement = zippedFlag ? xcmParser.Parse(zippedstream) : xcmParser.Parse(stream);
                        }
                        parseTime.Stop();

                        var tableNameIndex = 0;
                        foreach (var elementValues in parsedElement.RowValues)
                        {
                            var currentElementName = elementValues.Key;
                            foreach (var rowValues in elementValues.Value)
                            {

                                Dictionary<string, int> currentObjectColumnIndices;
                                if (!columnIndices.TryGetValue(currentElementName, out currentObjectColumnIndices))
                                {
                                    currentObjectColumnIndices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                                    int columnIndex = 0;

                                    foreach (KeyValuePair<string, string> rowValue in rowValues)
                                    {
                                        string parameterName = rowValue.Key;
                                        string parameterValue = rowValue.Value;
                                        currentObjectColumnIndices.Add(parameterName, columnIndex);
                                        columnIndex++;
                                    }
                                    columnIndices.Add(currentElementName, currentObjectColumnIndices);
                                    string dbCommandText = $"CREATE TABLE \"{currentElementName}\" ( {string.Join(",", rowValues.Select(p => $"[{p.Key}] NUMBER COLLATE NOCASE"))})";
                                    using (SQLiteCommand dbCommand = new SQLiteCommand(dbCommandText, dbConnection))
                                    {

                                        dbCommand.ExecuteNonQuery();
                                    }
                                }
                                else
                                {
                                    foreach (KeyValuePair<string, string> rowValue in rowValues)
                                    {
                                        string parameterName = rowValue.Key;


                                        if (!currentObjectColumnIndices.ContainsKey(parameterName))
                                        {
                                            string dbCommandText = $"ALTER TABLE \"{currentElementName}\" ADD [{parameterName}] NUMBER COLLATE NOCASE";
                                            using (SQLiteCommand dbCommand = new SQLiteCommand(dbCommandText, dbConnection))
                                            {
                                                dbCommand.ExecuteNonQuery();

                                            }
                                            Log.Information($"A new column {parameterName} is added to Table {currentElementName}");
                                            currentObjectColumnIndices.Add(parameterName, currentObjectColumnIndices.Count);
                                        }

                                    }
                                }
                                // 72 ms

                                if (!dbInsertCommandCache.TryGetValue(currentElementName, out var dbInsertCommand))
                                {
                                    dbInsertCommand = new SQLiteCommand($"INSERT INTO [{currentElementName}] VALUES ({string.Join(",", Enumerable.Repeat("?", currentObjectColumnIndices.Count))})", dbConnection);
                                    for (int i = 0; i < currentObjectColumnIndices.Count; i++)
                                        dbInsertCommand.Parameters.Add(new SQLiteParameter());
                                    dbInsertCommandCache.Add(currentElementName, dbInsertCommand);
                                }
                                else
                                {
                                    //  dbInsertCommand.Connection = dbConnection;
                                    if (dbInsertCommand.Parameters.Count != currentObjectColumnIndices.Count)
                                    {
                                        dbInsertCommand.Dispose();

                                        dbInsertCommand = new SQLiteCommand($"INSERT INTO [{currentElementName}] VALUES ({string.Join(",", Enumerable.Repeat("?", currentObjectColumnIndices.Count))})", dbConnection);
                                        for (int i = 0; i < currentObjectColumnIndices.Count; i++)
                                            dbInsertCommand.Parameters.Add(new SQLiteParameter());
                                        dbInsertCommandCache[currentElementName] = dbInsertCommand;
                                    }

                                }
                                // 72 ms
                                // 30ms
                                for (int i = 0; i < dbInsertCommand.Parameters.Count; i++)
                                {
                                    dbInsertCommand.Parameters[i].Value = null;
                                }

                                foreach (KeyValuePair<string, string> rowValue in rowValues)
                                {
                                    string parameterName = rowValue.Key;
                                    string parameterValue = rowValue.Value;
                                    int columnIndex = currentObjectColumnIndices[parameterName];
                                    dbInsertCommand.Parameters[columnIndex].Value = parameterValue;
                                }

                                insertTime.Start();
                                dbInsertCommand.ExecuteNonQuery();
                                insertTime.Stop();

                                tableNameIndex++;

                            }
                        }
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
            InsertIntoFileChecker(fileCheckerdbConnection, zippedFlag, filePath);
        }

        private static void InsertIntoFileChecker(SQLiteConnection fileCheckerdbConnection, bool zippedFlag, string filePath)
        {
            var sha_512 = FileValues.GetSHA512(zippedFlag, filePath);

            string command = $"INSERT INTO {FileValues.FileCheckerTableName} VALUES('{FileValues.FilePath}','{FileValues.FileSize}','{sha_512}')";
            using (SQLiteCommand fileCheckerInsert = new SQLiteCommand(command, fileCheckerdbConnection))
            {
                fileCheckerInsert.ExecuteNonQuery();
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
