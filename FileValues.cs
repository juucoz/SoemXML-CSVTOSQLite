﻿using System;
using System.Data.SQLite;
using System.IO;
using System.Text.RegularExpressions;

namespace SoemXmlToSQLite
{
    class FileValues
    {
        public static string GetFileValue(string inputPath)
        {
            string[] dirNames = Directory.GetDirectories(inputPath);
            foreach (string dirName in dirNames)
            {
                Console.WriteLine(Path.GetFileNameWithoutExtension(dirName));
            }
            Console.WriteLine("Enter the directory name that you want to save to SQLite, enter ALL to save all files in the directory.");
            string selectedFolder = Console.ReadLine();
            return selectedFolder;
        }

        public static void CallConverter(string selectedFolder, DBValues values, Options options, SQLiteConnection dbConnection, string dbFilePath)
        {
            if (selectedFolder.Equals("ALL"))
            {
                foreach (string filePath in Directory.EnumerateFiles(options.InputPath, options.SourceFileMask, SearchOption.AllDirectories))
                {

                    values = new DBValues(dbFilePath);
                    string fileName = Path.GetFileName(filePath);
                    if (fileName.Contains(".xml"))
                    {
                        Console.WriteLine(fileName);
                        // SOEMDSP1_MINI-LINK_AGC_20191023_001500.xml
                        string ne = Regex.Match(fileName, @".+?(?=_)").Value;
                        string @class = Regex.Match(fileName, @"(?<=^.+?_).+(?=_\d{8})").Value;
                        string timestamp = DateTime.ParseExact(Regex.Match(fileName, @"\d{8}_\d{6}").Value, "yyyyMMdd_HHmmss", null).ToString("s");
                        using (FileStream stream = File.OpenRead(filePath))
                        {
                            SoemXmlToDbConverter.Convert(
                                stream,
                                ne,
                                @class,
                                timestamp,
                                dbConnection,
                                values.ColumnIndices,
                                values.DbInsertCommandCache);
                        }
                    }
                }
            }
            else if (Directory.Exists(Path.Join(options.InputPath, $"{selectedFolder}")))
            {
                foreach (string filePath in Directory.EnumerateFiles(Path.Join(options.InputPath, $"{selectedFolder}"), options.SourceFileMask, SearchOption.AllDirectories))
                {
                    values = new DBValues(dbFilePath);
                    string fileName = Path.GetFileName(filePath);
                    var parser = ParserFactory.CreateParser(filePath);

                    if (parser is CSVParser csvparser)
                    {

                        Console.WriteLine(fileName);
                        // SOEMDSP1_MINI-LINK_AGC_20191023_001500.xml
                        string fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);



                        using (FileStream stream = File.OpenRead(filePath))
                        {
                            var parsedFile = csvparser.Parse(stream);
                            SoemXmlToDbConverter.Convert(
                                parsedFile,
                                stream,
                                fileNameWithoutExt,
                                dbConnection,
                                values.ColumnIndices,
                                values.DbInsertCommandCache);
                        }
                    }

                    if (fileName.Contains(".xml"))
                    {
                        {
                            Console.WriteLine(fileName);
                            // SOEMDSP1_MINI-LINK_AGC_20191023_001500.xml
                            string ne = Regex.Match(fileName, @".+?(?=_)").Value;
                            string @class = Regex.Match(fileName, @"(?<=^.+?_).+(?=_\d{8})").Value;
                            string timestamp = DateTime.ParseExact(Regex.Match(fileName, @"\d{8}_\d{6}").Value, "yyyyMMdd_HHmmss", null).ToString("s");
                            using (FileStream stream = File.OpenRead(filePath))
                            {
                                SoemXmlToDbConverter.Convert(
                                    stream,
                                    ne,
                                    @class,
                                    timestamp,
                                    dbConnection,
                                    values.ColumnIndices,
                                    values.DbInsertCommandCache);
                            }
                        }
                    }
                }
            }

        }
    }
}
