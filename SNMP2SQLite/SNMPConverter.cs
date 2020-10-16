using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml;

namespace SNMP2SQLite
{
    class SNMPConverter
    {
        internal static void Convert(
            SNMPParser snmpParser,
            GZipStream input,
            string filePath,
            bool zippedFlag,
            SQLiteConnection dbConnection,
            SQLiteConnection fileCheckerdbConnection,
            Dictionary<string, Dictionary<string, int>> columnIndices)

        {
            //var start = StopwatchProxy.Instance.Stopwatch.ElapsedMilliseconds;
            //var parseTime = new Stopwatch();
            //var insertTime = new Stopwatch();
            //Log.Information("SQLiteInsert start time for {Full_File_Path} is {Write_Start_Time} for table {target_table}", FileValues.FilePath, start, dbConnection.FileName);
            Console.WriteLine("Watch for db actions have started");

            using (DbTransaction dbTransaction = dbConnection.BeginTransaction())
            {
                snmpParser.Init(input);
                FileValues.SetFileValues(snmpParser, filePath, Path.GetFileName(filePath));
                SNMPParseResult parsedRow = new SNMPParseResult();
                Dictionary<string, int> currentObjectColumnIndices;
                while (parsedRow != null)
                {
                    //parseTime.Start();
                    parsedRow = snmpParser.Parse(input);
                    //parseTime.Stop();

                    if (parsedRow is null)
                    {
                        break;
                    }

                    if (!columnIndices.TryGetValue(FileValues.TableName, out currentObjectColumnIndices))
                    {
                        columnIndices.Add(FileValues.TableName, currentObjectColumnIndices);
                        string dbCommandText = $"CREATE TABLE [{FileValues.TableName}] ({string.Join(",", parsedRow.RowValues.Select(p => $"[{p.Key}] NUMBER COLLATE NOCASE"))})";
                        using (SQLiteCommand dbCommand = new SQLiteCommand(dbCommandText, dbConnection))
                        {
                            dbCommand.ExecuteNonQuery();
                        }
                    }

                    string text = $"INSERT INTO [{FileValues.TableName}] VALUES({string.Join(",", Enumerable.Repeat("?", parsedRow.RowValues.Keys.Count))})";
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
                    //insertTime.Start();
                    command.ExecuteNonQuery();
                    //insertTime.Stop();
                }
                //string dbInsertText = $"INSERT INTO [{parsedFile.Ne + " " + parsedFile.Type}] VALUES({string.Join(",", datum.Select(d => $"'{d.Value}'"))})";
                //using (SQLiteCommand dbCommand = new SQLiteCommand(dbInsertText, dbConnection))
                //{
                //    dbCommand.ExecuteNonQuery();
                //}
                dbTransaction.Commit();


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

    }
}
