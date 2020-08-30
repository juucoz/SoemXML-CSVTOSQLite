using System;
using System.Data.SQLite;
using System.IO;

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
            Console.WriteLine("Enter the directory name that you want to save to SQLite, press ENTER to save all files in the directory.");
            string selectedFolder = Console.ReadLine();
            return selectedFolder;
        }

        public static void CallConverter(string selectedFolder, Options options, SQLiteConnection dbConnection, string dbFilePath)
        {
            if (Directory.Exists(Path.Join(options.InputPath, $"{selectedFolder}")))
            {
                foreach (string filePath in Directory.EnumerateFiles(Path.Join(options.InputPath, $"{selectedFolder}"), options.SourceFileMask, SearchOption.AllDirectories))
                {
                    var values = new DBValues(dbFilePath);
                    string fileName = Path.GetFileName(filePath);
                    var parser = ParserFactory.CreateParser(filePath);

                    if (parser is CSVParser csvparser)
                    {

                        Console.WriteLine(fileName);

                        using (FileStream stream = File.OpenRead(filePath))
                        {
                            var parsedFile = csvparser.Parse(stream);
                            SoemXmlToDbConverter.Convert(
                                parsedFile,
                                dbConnection,
                                values.ColumnIndices
                        );
                        }
                    }

                    if (parser is XMLParser xmlparser)
                    {
                        {
                            Console.WriteLine(fileName);
                            // SOEMDSP1_MINI-LINK_AGC_20191023_001500.xml
                            
                            using (FileStream stream = File.OpenRead(filePath))
                            {
                                var parsedFile = xmlparser.Parse(stream);
                                SoemXmlToDbConverter.Convert(
                                    parsedFile,
                                    dbConnection,
                                    values.ColumnIndices,
                                    values.DbInsertCommandCache);
                            }
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("This directory doesn't exist.");
                return;
            }

        }
    }
}
