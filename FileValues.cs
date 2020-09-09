using Serilog;
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
            if (!Directory.Exists(Path.Join(inputPath, $"{selectedFolder}"))){
                Console.WriteLine("-- Selected directory does not exist -- Try Again. ");
                Log.Error(new FileNotFoundException(), "This directory {Full_File_Path} does not exist.", Path.Join(inputPath, $"{selectedFolder}"));
                return GetFileValue(inputPath);
            }
            return selectedFolder;
        }

        public static void CallConverter(string selectedFolder, Options options, SQLiteConnection dbConnection)
        {
            
            if (Directory.Exists(Path.Join(options.InputPath, $"{selectedFolder}")))
            {
                foreach (string filePath in Directory.EnumerateFiles(Path.Join(options.InputPath, $"{selectedFolder}"), options.SourceFileMask, SearchOption.AllDirectories))
                {
                    //var values = new DBValues(dbFilePath);
                    var values = DBValues.getDBValues(options.DbFilePath);
                    string fileName = Path.GetFileName(filePath);
                    var parser = ParserFactory.CreateParser(filePath);

                    var start = StopwatchProxy.Instance.Stopwatch.ElapsedMilliseconds;
                    Log.Information("Parse start time for {Full_File_Path} is {Parse_Start_Time}", filePath, start);

                    if (parser is CSVParser csvparser)
                    {

                        Console.WriteLine(fileName);

                        using (FileStream stream = File.OpenRead(filePath))
                        {
                            var parsedFile = csvparser.Parse(stream);
                            var stop = StopwatchProxy.Instance.Stopwatch.ElapsedMilliseconds;
                            Log.Information("Parse end time for {Full_File_Path} is {Parse_End_Time} and the duration is {Parse_Duration}", filePath, stop, stop - start);
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
                                var stop = StopwatchProxy.Instance.Stopwatch.ElapsedMilliseconds;
                                Log.Information("Parse end time for {Full_File_Path} is {Parse_End_Time} and the duration is {Parse_Duration}", filePath, stop, stop - start);
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
                
                Log.Error(new FileNotFoundException(), "This directory {Full_File_Path} does not exist.", Path.Join(options.InputPath, $"{selectedFolder}"));
                Console.WriteLine("This directory doesn't exist.");
                return;
            }

        }
    }
}
