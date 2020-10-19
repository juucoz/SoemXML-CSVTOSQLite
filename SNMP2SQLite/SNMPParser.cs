using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace SNMP2SQLite
{
    class SNMPParser
    {

        // public System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
        private StreamReader reader;
        private List<string> _headers;
        private char[] separators;
        private SNMPParseResult _defaultResult;
        int skippedLineCounter = 0;


        [DefaultValue(",")]
        public string SourceSeparator { get; set; } = "|";

        [DefaultValue(2)]
        public int HeaderLine { get; set; }

        [DefaultValue(false)]
        public bool SkipEscape { get; set; }

        [DefaultValue(0)]
        public int[] TableNameIndex { get; set; }

        [DefaultValue(0)]
        public int[] MonameIndex { get; set; }

        [DefaultValue(0)]
        public int[] TimestampIndex { get; set; }
        public SNMPParseResult Init(GZipStream input)
        {
            TableNameIndex = new int[] { 0 };
            MonameIndex = new int[] { 1, 2 };
            TimestampIndex = new int[] { 3 };
            HeaderLine = 3;
            _defaultResult = new SNMPParseResult();
            _headers = new List<string>();
            separators = SourceSeparator.ToCharArray();

            var returns = ReadHeaders(input);
            string unTrimmedHeaders = returns.Item1;
            reader = returns.Item2;
            ParseHeaders(unTrimmedHeaders, _defaultResult);
            //reader = new StreamReader(input);

            //input.Position = 0;
            //for (var space = 1; space <= HeaderLine; space++)
            //{
            //   string ln = reader.ReadLine();
            //}

            return _defaultResult;
        }
        public SNMPParseResult Parse(GZipStream input)
        {
            // var start = StopwatchProxy.Instance.Stopwatch.ElapsedMilliseconds;
            while (reader.Peek() != -1)
            {
                string line = FileValues.Timestamp + SourceSeparator + FileValues.MoName + SourceSeparator + reader.ReadLine();
                ParseLine(line, _defaultResult, separators);
                return _defaultResult;
            }
            //var stop = StopwatchProxy.Instance.Stopwatch.ElapsedMilliseconds;

            //_defaultResult.logValues = new LogValues(input.Name,start,stop,stop-start,_defaultResult.Data.Count - skippedLineCounter,skippedLineCounter,0,""); 
            return null;
        }

        protected (string, StreamReader) ReadHeaders(GZipStream inp)
        {
            StreamReader rd = new StreamReader(inp);

            for (var space = 1; space < HeaderLine; space++)
            {
                rd.ReadLine();
            }
            var unTrimmedHeaders = rd.ReadLine();
            return (unTrimmedHeaders, rd);

        }


        protected void ParseHeaders(string headerLine, SNMPParseResult _defaultResult)
        {
            string targetLine = headerLine;
            if (SkipEscape)
            {
                targetLine = targetLine.Replace("\"", "");
            }


            char[] separators = SourceSeparator.ToCharArray();
            _headers.Add("Timestamp");
            _headers.Add("Moname");
            _headers.AddRange(targetLine.Split(separators).ToList());


            // create parse item


            // _defaultResult.Headers.AddRange(_headers);

        }
        /// <summary>
        ///
        /// </summary>
        /// <param name="line"></param>
        protected void ParseLine(string line, SNMPParseResult _defaultResult, char[] separators)
        {
            string[] dataRow;
            string target;

            target = line.Trim().Replace("'", "''");
            if (SkipEscape)
            {
                dataRow = Regex.Split(target, "|(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
                for (var i = 0; i < dataRow.Length; i++)
                {
                    if (dataRow[i].StartsWith("\""))
                    {
                        dataRow[i] = dataRow[i].Substring(1, dataRow[i].Length > 2 ? dataRow[i].Length - 2 : dataRow[i].Length - 1);
                    }

                }
            }
            else
            {
                dataRow = target.Split(separators);
            }

            if (dataRow.Length == _headers.Count)
            {
                Dictionary<string, string> dict = new Dictionary<string, string>();
                foreach (var dh in dataRow.Zip(_headers, Tuple.Create))
                {
                    dict.Add(dh.Item2, dh.Item1);
                }
                _defaultResult.RowValues = dict;

            }
            else
            {
                //Log.Error("Invalid value count in row {Errow_Row}", _defaultResult.RowValues.Count + HeaderLine + 1 + skippedLineCounter);
                Console.WriteLine($"Invalid value count in row {_defaultResult.RowValues.Count + HeaderLine + 1 + skippedLineCounter } ");
                skippedLineCounter++;
            }
        }
    }

    static class FileValues
    {
        public static string Timestamp { get; set; }
        public static string MoName { get; set; }
        public static string TableName { get; set; }

        public static string FilePath { get; set; }
        public static string SHA_512 { get; set; }
        public static long FileSize { get; set; }
        public static string FileCheckerTableName { get; set; }
        public static void SetFileValues(SNMPParser parser, string filePath, string fileName)
        {

            try
            {
                FileValues.FilePath = filePath;
                string fileNameWoutExt = Path.GetFileNameWithoutExtension(filePath);
                string fileNameWoutAllExt = Path.GetFileNameWithoutExtension(fileNameWoutExt);
                var values = Regex.Matches(fileNameWoutAllExt, @"[^_\s][^_]*[^_\s]*");
                // string date = values[parser.DateIndex - 1].Value + "_" + values[parser.DateIndex].Value;
                // string dateBackup = values[parser.DateIndex].Value;
                string date = string.Join("_", values.Where((item, index) => parser.TimestampIndex.Contains(index)));
                //foreach (var e in parser.DateIndex) { date = string.Append(values[e]); };

                FileValues.TableName = string.Join("_", values.Where((item, index) => parser.TableNameIndex.Contains(index)));
                FileValues.MoName = string.Join("_", values.Where((item, index) => parser.MonameIndex.Contains(index)))
                                         .Replace("-", "_").ToLowerInvariant();
                string[] formatStrings = { "yyyy-MM-dd_HH'h'mm'm'ss'sZ'", "yyyy-MM-dd_HH-mm-ss", "yyyyMMddHHmmZ", "yyyyMMdd_HHmm", "yyyyMMdd_HHmmss", "yyyyMMddHHmmss" };
                //_defaultResult.Timestamp = DateTime.ParseExact(Regex.Match(fileName, @"\d{8}_\d{6}").Value, "yyyyMMdd_HHmmss", null).ToString("s");
                ParseDate(date, formatStrings, fileName);
            }
            catch (ArgumentOutOfRangeException a)
            {
                Console.WriteLine(a.Message);
            }
            catch (FormatException e)
            {
                //Log.Error(e, "File {File_Name} couldn't be parsed by any DateTime formats.", fileName);
                Console.WriteLine(e.Message);
            }
        }
        public static void CallConverter(string inputPath, SQLiteConnection dbConnection, SQLiteConnection fileCheckerdbConnection)
        {
            var zippedFlag = true;
            string inputPaths = Path.GetDirectoryName(inputPath);
            string sourcefileMask = Path.GetFileName(inputPath);
            if (Directory.Exists(Path.Join(inputPaths)))
            {

                foreach (string filePath in Directory.EnumerateFiles(inputPaths, sourcefileMask,SearchOption.AllDirectories))
                {
                    //var values = new DBValues(dbFilePath);
                    string fileName = Path.GetFileName(filePath);

                    //var start = StopwatchProxy.Instance.Stopwatch.ElapsedMilliseconds;

                    if (fileName.Contains("txt.gz"))
                    {
                        zippedFlag = true;
                        var values = DBValues.GetDBValues(dbConnection);
                        SNMPParser snmpParser = new SNMPParser();
                        Console.WriteLine(fileName);

                        using (FileStream oldstream = File.OpenRead(filePath))
                        using (GZipStream zippedStream = new GZipStream(oldstream, CompressionMode.Decompress))
                        {
                            var fileExists = FileExists(filePath, fileCheckerdbConnection);
                            //var stop = StopwatchProxy.Instance.Stopwatch.ElapsedMilliseconds;
                            //Log.Information("Parse end time for {Full_File_Path} is {Parse_End_Time} and the duration is {Parse_Duration}", filePath, stop, stop - start);
                            if (!fileExists)
                            {
                                SNMPConverter.Convert(
                                    snmpParser,
                                    zippedStream,
                                    filePath,
                                    zippedFlag,
                                    dbConnection,
                                    fileCheckerdbConnection,
                                    values.ColumnIndices
                        );
                            }
                        }
                    }
                }
            }
            else
            {
                inputPath = ".";
                //Log.Error(new FileNotFoundException(), "This directory {Full_File_Path} does not exist.", Path.Join(options.InputPath, $"{selectedFolder}"));
                Console.WriteLine("This directory doesn't exist.");
                return;
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
            zippedFlag = zippedFlag ? filePath.Contains("txt.gz") : false;
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
                if (count != 0)
                {
                    return true;
                }
                else
                    return false;
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
