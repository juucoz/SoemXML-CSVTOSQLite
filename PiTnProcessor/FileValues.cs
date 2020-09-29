﻿using Serilog;
using System;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
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

        public static void CallConverter(Options options, SQLiteConnection dbConnection)
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

                        Console.WriteLine(fileName);

                        using (FileStream stream = File.OpenRead(filePath))
                        using (GZipStream zippedStream = new GZipStream(stream, CompressionMode.Decompress))
                        {
                            var stop = StopwatchProxy.Instance.Stopwatch.ElapsedMilliseconds;
                            FileValues.SetFileValues(parser, filePath, fileName);
                            Log.Information("Parse end time for {Full_File_Path} is {Parse_End_Time} and the duration is {Parse_Duration}", filePath, stop, stop - start);
                            SQLiteConverter.Convert(
                                csvparser,
                                zippedStream,
                                filePath,
                                dbConnection,
                                values.ColumnIndices
                        );
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
                                {
                                    if (sourceFileMask.Contains(".xml.gz"))
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
                                        values.ColumnIndices,
                                        values.DbInsertCommandCache);
                                }
                               
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
    }
}