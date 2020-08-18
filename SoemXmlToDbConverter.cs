using CommandLine;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace SoemXmlToSQLite
{
    internal static class SoemXmlToDbConverter
    {
        public static void Convert(
            Stream xmlStream,
            string ne,
            string @class,
            string timestamp,
            SQLiteConnection dbConnection,
            Dictionary<string, Dictionary<string, int>> columnIndices,
            Dictionary<string, SQLiteCommand> dbInsertCommandCache)
        {
            using (SQLiteTransaction dbTransaction = dbConnection.BeginTransaction())
            {      
                
                /* XmlReaderSettings xmlReaderSettings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Ignore,
                    IgnoreComments = true,
                    IgnoreProcessingInstructions = true,
                    IgnoreWhitespace = true,
                    CheckCharacters = false,
                    ValidationFlags = System.Xml.Schema.XmlSchemaValidationFlags.None
                };
                using (XmlReader xmlReader = XmlReader.Create(xmlStream, xmlReaderSettings))
                {
                    xmlReader.MoveToContent();
                    while (!xmlReader.EOF)
                    {
                        xmlReader.Read();
                        if (string.Equals(xmlReader.LocalName, "row", StringComparison.Ordinal))
                        {
                            break;
                        }
                    }

                    while (string.Equals(xmlReader.LocalName, "row", StringComparison.Ordinal))
                    {
                        XElement xObject = (XElement)XNode.ReadFrom(xmlReader);
                        Dictionary<string, string> parameters =
                            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                            {
                                { "NE", ne },
                                { "TIMESTAMP", timestamp },
                            };
                        foreach (var xAttribute in xObject.Attributes())
                        {
                            string parameterName = xAttribute.Name.LocalName;
                            string parameterValue = xAttribute.Value;
                            parameters.Add(parameterName, parameterValue);
                        }
                        if (parameters.Count > 2) // > 1 because NE is always there
                        {
                            Dictionary<string, int> currentObjectColumnIndices;
                            if (!columnIndices.TryGetValue(@class, out currentObjectColumnIndices))
                            {
                                currentObjectColumnIndices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                                int columnIndex = 0;
                                foreach (KeyValuePair<string, string> parameter in parameters)
                                {
                                    string parameterName = parameter.Key;
                                    string parameterValue = parameter.Value;
                                    currentObjectColumnIndices.Add(parameterName, columnIndex);
                                    columnIndex++;
                                }
                                columnIndices.Add(@class, currentObjectColumnIndices);

                                string dbCommandText = $"CREATE TABLE [{@class}] ({string.Join(",", parameters.Select(p => $"[{p.Key}] TEXT COLLATE NOCASE"))})";
                                using (SQLiteCommand dbCommand = new SQLiteCommand(dbCommandText, dbConnection))
                                {
                                    dbCommand.ExecuteNonQuery();
                                }
                            }
                            else
                            {
                                foreach (KeyValuePair<string, string> parameter in parameters)
                                {
                                    string parameterName = parameter.Key;
                                    int columnIndex;
                                    if (!currentObjectColumnIndices.TryGetValue(parameterName, out columnIndex))
                                    {
                                        columnIndex = currentObjectColumnIndices.Count;
                                        currentObjectColumnIndices.Add(parameterName, columnIndex);
                                        string dbCommandText = $"ALTER TABLE [{@class}] ADD [{parameterName}] TEXT COLLATE NOCASE";
                                        using (SQLiteCommand dbCommand = new SQLiteCommand(dbCommandText, dbConnection))
                                        {
                                            dbCommand.ExecuteNonQuery();
                                        }
                                    }
                                }
                            }

                            SQLiteCommand dbInsertCommand;
                            if (!dbInsertCommandCache.TryGetValue(@class, out dbInsertCommand))
                            {
                                dbInsertCommand = new SQLiteCommand($"INSERT INTO [{@class}] VALUES ({string.Join(",", Enumerable.Repeat("?", currentObjectColumnIndices.Count))})", dbConnection);
                                for (int i = 0; i < currentObjectColumnIndices.Count; i++)
                                    dbInsertCommand.Parameters.Add(new SQLiteParameter());
                                dbInsertCommandCache.Add(@class, dbInsertCommand);
                            }
                            else
                            {
                                if (dbInsertCommand.Parameters.Count != currentObjectColumnIndices.Count)
                                {
                                    dbInsertCommand.CommandText = $"INSERT INTO [{@class}] VALUES ({string.Join(",", Enumerable.Repeat("?", currentObjectColumnIndices.Count))})";
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

                            foreach (KeyValuePair<string, string> parameter in parameters)
                            {
                                string parameterName = parameter.Key;
                                string parameterValue = parameter.Value;
                                int columnIndex = currentObjectColumnIndices[parameterName];
                                dbInsertCommand.Parameters[columnIndex].Value = parameterValue;
                            }
                            dbInsertCommand.ExecuteNonQuery();
                        }
                        xmlReader.Skip();
                    }
                
                }
                */
                dbTransaction.Commit();
            }
        }

        internal static void ConvertTest(TextFileParseOutput parsedFile, FileStream stream, string ne, string fileNameWithoutExt, SQLiteConnection dbConnection, Dictionary<string, Dictionary<string, int>> columnIndices, Dictionary<string, SQLiteCommand> dbInsertCommandCache)
     
        {
            
            using (DbTransaction dbTransaction = dbConnection.BeginTransaction())
            {
                    string dbCommandText = $"CREATE TABLE [{fileNameWithoutExt}] ({string.Join(",", parsedFile.headers.Select(p => $"[{p}] TEXT COLLATE NOCASE"))})";
                    using (SQLiteCommand dbCommand = new SQLiteCommand(dbCommandText,dbConnection))
                    {
                        dbCommand.ExecuteNonQuery();
                    }
                foreach (var datum in parsedFile.data)
                {
                    string dbInsertText = $"INSERT INTO [{fileNameWithoutExt}] VALUES({string.Join(",", datum.Select(d => $"'{d.Value}'"))})";
                    using (SQLiteCommand dbCommand = new SQLiteCommand(dbInsertText, dbConnection))
                    {
                        dbCommand.ExecuteNonQuery();
                    }
                }
              
                dbTransaction.Commit();
            }
            
        }
    }
}