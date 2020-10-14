using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace PiTnProcessor
{
    static class FileValues
    {
        public static string Ne { get; set; }
        public static string Type { get; set; }
        public static string Class { get; set; }
        public static string Timestamp { get; set; }
        public static string FilePath { get; set; }
        public static long FileSize { get; set; }
        public static string FileCheckerTableName { get; set; }
        public static string SHA_512 { get; set; }

        //public static string GetFileValue(string inputPath)
        //{
        //    string[] dirNames = Directory.GetDirectories(inputPath);
        //    foreach (string dirName in dirNames)
        //    {
        //        Console.WriteLine(Path.GetFileNameWithoutExtension(dirName));
        //    }
        //    Console.WriteLine("Enter the directory name that you want to save to SQLite, press ENTER to save all files in the directory.");
        //    string selectedFolder = Console.ReadLine();
        //    if (!Directory.Exists(Path.Join(inputPath, $"{selectedFolder}"))){
        //        Console.WriteLine("-- Selected directory does not exist -- Try Again. ");
        //        Log.Error(new FileNotFoundException(), "This directory {Full_File_Path} does not exist.", Path.Join(inputPath, $"{selectedFolder}"));
        //        return GetFileValue(inputPath);
        //    }
        //    return selectedFolder;
        //}

        public static void CallConverter(Options options, SQLiteConnection dbConnection, SQLiteConnection fileCheckerdbConnection)
        {
            var inputPath = Path.GetDirectoryName(options.InputPath);
            var sourceFileMask = Path.GetFileName(options.InputPath);

            if (Directory.Exists(Path.Join(inputPath)))
            {
                foreach (string filePath in Directory.EnumerateFiles(inputPath, sourceFileMask, SearchOption.AllDirectories))
                {
                    //var values = new DBValues(dbFilePath);
                    var values = DBValues.GetDBValues(dbConnection);
                    string fileName = Path.GetFileName(filePath);
                    var parser = ParserFactory.CreateParser(filePath);
                    // FileValues.SetFileValues(parser, filePath, fileName);


                    var start = StopwatchProxy.Instance.Stopwatch.ElapsedMilliseconds;

                    if (parser is CSVParser csvparser)
                    {
                        bool zippedFlag = true;
                        Console.WriteLine(fileName);

                        using (FileStream stream = File.OpenRead(filePath))
                        using (GZipStream zippedStream = new GZipStream(stream, CompressionMode.Decompress))
                        {

                            var fileExists = FileExists(filePath, fileCheckerdbConnection);
                            if (!fileExists)
                            {
                                var stop = StopwatchProxy.Instance.Stopwatch.ElapsedMilliseconds;
                                FileValues.SetFileValues(parser, filePath, fileName);

                                Log.Information("Parse end time for {Full_File_Path} is {Parse_End_Time} and the duration is {Parse_Duration}", filePath, stop, stop - start);
                                SQLiteConverter.Convert(
                                    csvparser,
                                    zippedStream,
                                    stream,
                                    filePath,
                                    dbConnection,
                                    fileCheckerdbConnection,
                                    values.ColumnIndices
                            );
                            }
                        }
                    }

                    else if (parser is XMLParser xmlparser)
                    {
                        {
                            Console.WriteLine(fileName);
                            // SOEMDSP1_MINI-LINK_AGC_20191023_001500.xml

                            using (FileStream stream = File.OpenRead(filePath))
                            using (GZipStream zippedStream = new GZipStream(stream, CompressionMode.Decompress))
                            {
                                var fileExists = FileExists(filePath, fileCheckerdbConnection);
                                if (!fileExists)
                                {
                                    
                                        if (filePath.Contains(".xml.gz"))
                                        {
                                            xmlparser.setXMLParser(zippedStream);
                                        }
                                        else
                                        {
                                            xmlparser.setXMLParser(stream);
                                        }
                                        FileValues.SetFileValues(parser, filePath, fileName);
                                        var stop = StopwatchProxy.Instance.Stopwatch.ElapsedMilliseconds;
                                        SQLiteConverter.Convert(
                                            xmlparser,
                                            zippedStream,
                                            stream,
                                            filePath,
                                            dbConnection,
                                            fileCheckerdbConnection,
                                            values.ColumnIndices,
                                            values.DbInsertCommandCache);
                                    
                                }
                            }
                        }
                    }
                    else
                    {

                        //Log.Error(new FileNotFoundException(), "This directory {Full_File_Path} does not exist.", Path.Join(options.InputPath, $"{selectedFolder}"));
                        Console.WriteLine("This file type does not have a parser.");
                    }
                }
            }
            else
            {

                //Log.Error(new FileNotFoundException(), "This directory {Full_File_Path} does not exist.", Path.Join(options.InputPath, $"{selectedFolder}"));
                Console.WriteLine("This directory doesn't exist.");
                return;
            }

        }
        private static void SetFileValues(IParser parser, string filePath, string fileName)
        {
            try
            {
                FileValues.FilePath = filePath;
                string fileNameWoutExc = Path.GetFileNameWithoutExtension(filePath);
                string fileNameWoutAllExt = Path.GetFileNameWithoutExtension(fileNameWoutExc);
                var values = Regex.Matches(fileNameWoutAllExt, @"[^_\s][^_]*[^_\s]*");
                // string date = values[parser.DateIndex - 1].Value + "_" + values[parser.DateIndex].Value;
                // string dateBackup = values[parser.DateIndex].Value;
                string date = string.Join("_", values.Where((item, index) => parser.DateIndex.Contains(index)));
                //foreach (var e in parser.DateIndex) { date = string.Append(values[e]); };

                FileValues.Ne = string.Join("_", values.Where((item, index) => parser.NeIndex.Contains(index)));
                FileValues.Class = string.Join("_", values.Where((item, index) => parser.TypeIndex.Contains(index)))
                                         .Replace("-", "_").ToLowerInvariant();
                FileValues.Class = Regex.Replace(FileValues.Class, @"^(\d*)", "");
                string[] formatStrings = { "yyyy-MM-dd_HH'h'mm'm'ss'sZ'", "yyyy-MM-dd_HH-mm-ss", "yyyyMMddHHmmZ", "yyyyMMdd_HHmm", "yyyyMMdd_HHmmss" };
                //_defaultResult.Timestamp = DateTime.ParseExact(Regex.Match(fileName, @"\d{8}_\d{6}").Value, "yyyyMMdd_HHmmss", null).ToString("s");
                ParseDate(date, formatStrings, fileName);
            }
            catch (NullReferenceException n)
            {
                Log.Error(n, "File {File_Name} doesn't have a correspondant parser.", fileName);
                Console.WriteLine(n.Message);
            }
            catch (ArgumentOutOfRangeException a)
            {
                Console.WriteLine(a.Message);
            }
            catch (FormatException e)
            {
                Log.Error(e, "File {File_Name} couldn't be parsed by any DateTime formats.", fileName);
                Console.WriteLine(e.Message);
            }
        }
        private static void ParseDate(string date, string[] formats, string fileName)
        {
            if (DateTime.TryParseExact(date, formats, null, DateTimeStyles.None, out DateTime v))
            {
                FileValues.Timestamp = v.ToString("u");
            }
            else if (DateTime.TryParseExact(date = Regex.Replace(date, "[^.0-9_]", ""), formats, null, DateTimeStyles.None, out v))
            {
                FileValues.Timestamp = v.ToString("u");
            }
            else
            {
                throw new FormatException("File " + fileName + " couldn't be parsed by any DateTime formats.");
            }
        }
        private static bool FileExists(string filePath, SQLiteConnection fileCheckerdbConnection)
        {
            string cmd;
            SQLiteCommand dbCommand;
            bool zippedFlag = true;
            int index = 0;
            zippedFlag = zippedFlag ? filePath.Contains("xml.gz") : false;
            var fileCheckerdbValues = DBValues.GetDBValues(fileCheckerdbConnection);
            FileValues.FileCheckerTableName = Path.GetFileNameWithoutExtension(fileCheckerdbConnection.DataSource);
            if (!fileCheckerdbValues.ColumnIndices.ContainsKey(FileValues.FileCheckerTableName))
            {
                cmd = $"CREATE TABLE {FileValues.FileCheckerTableName} ('filePath' TEXT COLLATE NOCASE,'fileSize' TEXT COLLATE NOCASE ,'SHA_512' VARCHAR(255) COLLATE NOCASE) ";
                using (dbCommand = new SQLiteCommand(cmd, fileCheckerdbConnection))
                {
                    dbCommand.ExecuteNonQuery();
                    fileCheckerdbValues.ColumnIndices.Add(FileValues.FileCheckerTableName, new Dictionary<string, int>());
                    index++;
                }
            }

            int count = 0;
            FileValues.FileSize = new FileInfo(filePath).Length;
            FileValues.SHA_512 = GetSHA512(zippedFlag, filePath);
            FileValues.FilePath = filePath;
            string checkcmd = $"SELECT COUNT(*) FROM {FileCheckerTableName} WHERE filePath = '{FilePath}' AND fileSize = '{FileSize}' AND SHA_512 = '{SHA_512}'";
            SQLiteCommand checkSQLitecmd = new SQLiteCommand(checkcmd, fileCheckerdbConnection);
            using (checkSQLitecmd)
            {
                count = Convert.ToInt32(checkSQLitecmd.ExecuteScalar());
                if (count != 1)
                {
                    return false;
                }
                else
                    return true;
            }


        }
        public static string GetSHA512(bool zippedFlag, string filePath)
        {
            using (FileStream stream = File.OpenRead(filePath))
            using (GZipStream zippedStream = new GZipStream(stream, CompressionMode.Decompress))
            {
                SHA512 hop = SHA512.Create();
                byte[] sha_512_byte_array;
                if (zippedFlag)
                {
                    sha_512_byte_array = hop.ComputeHash(zippedStream);
                }
                else
                {
                    sha_512_byte_array = hop.ComputeHash(stream);
                }

                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < sha_512_byte_array.Length; i++)
                {
                    builder.Append(sha_512_byte_array[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
