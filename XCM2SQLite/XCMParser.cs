using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace XCM2SQLite
{
    class XCMParser
    {

        public XmlReader xmlReader { get; set; }
        public List<string> tableNameList = new List<string>();
        Dictionary<string, string> idCache = new Dictionary<string, string>();
        bool childFlag = true;

        [DefaultValue(1)]
        public int[] TimestampIndex { get; set; }
        public int[] NeIndex { get; set; }
        public int[] TypeIndex { get; set; }

        public string ReadConfig { get; set; }
        public bool Flag { get; set; }


        private XCMParseResult _defaultResult;

        public XmlReaderSettings xmlReaderSettings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Ignore,
            IgnoreComments = true,
            IgnoreProcessingInstructions = true,
            IgnoreWhitespace = true,
            CheckCharacters = false,
            ValidationFlags = System.Xml.Schema.XmlSchemaValidationFlags.None
        };

        public XCMParser()
        {
            Flag = false;
        }

        public void setXMLParser(Stream input)
        {
            //input.Position = 0;
            xmlReader = XmlReader.Create(input, xmlReaderSettings);
        }
        public XCMParseResult Parse<T>(T input)
        {
            //var start = StopwatchProxy.Instance.Stopwatch.ElapsedMilliseconds;
            _defaultResult = new XCMParseResult();
            //flag = flag ? true : SetFileValues(_defaultResult, filePath, fileName);
            XElement xElement;
            idCache = new Dictionary<string, string>();
            try
            {
                xElement = (XElement)XNode.ReadFrom(xmlReader);
            }
            catch (InvalidOperationException)
            {
                return null;
            }
            var dict = new Dictionary<string, string>();
            var rowParam = new Dictionary<string, List<Dictionary<string, string>>>();
            tableNameList = new List<string>();
            childFlag = true;
            if (childFlag)
            {
                rowParam = ExtractChildren(xElement, xElement.Name.LocalName, tableNameList);

            }

            _defaultResult.RowValues = rowParam;

            //var stop = StopwatchProxy.Instance.Stopwatch.ElapsedMilliseconds;
            //_defaultResult.logValues = new LogValues(input.Name,DateTime.Now.ToString(), start, stop, stop - start, _defaultResult.Data.Count, 0, 0, "");
            return _defaultResult;




        }
        private Dictionary<string, List<Dictionary<string, string>>> ExtractChildren(XElement xElement, string parentName, List<string> tableNameList)
        {
            var rowParam = new Dictionary<string, List<Dictionary<string, string>>>();
            var listDict = new List<Dictionary<string, string>>();
            var dict = new Dictionary<string, string>();

            string tableName = GenerateTableName(parentName);
            tableNameList.Add(tableName);

            //id.Add(xElement.FirstAttribute.Value.ToString());
            int idIndex = 0;
            var parentNames = tableNameList.TakeLast(1).ToString().Split(",");
            var separateNames = tableName.Split(",");
            if (separateNames.Length >= 1)
            {
                foreach (var separateName in separateNames)
                {
                    if (!idCache.ContainsKey(separateName))
                    {
                        idCache.Add(separateName, xElement.FirstAttribute.Value);
                    }
                }
                foreach (var separateName in separateNames)
                {
                    if (tableName.Contains(separateName) && separateName != xElement.Name.LocalName)
                    {
                        dict.Add(separateName + "_id", idCache[separateName]);
                    }
                    idIndex++;
                }
            }
            foreach (var attributeas in xElement.Attributes())
            {
                string headerValue = attributeas.Name.LocalName;
                string dataValue = attributeas.Value;
                dict.Add(headerValue, dataValue);

            }
            foreach (XElement child in xElement.Nodes())
            {
                if (child.Name == "Application4G")
                {
                    Console.WriteLine("hop");
                }
                if (child.Name == "attributes")
                {
                    var attributes = child.Nodes();
                    foreach (XElement attribute in attributes)
                    {
                        string headerValue = attribute.Name.LocalName;
                        string dataValue = attribute.Value;
                        dict.Add(headerValue, dataValue);

                    }
                    listDict.Add(dict);
                    rowParam.Add(tableName, listDict);
                }
                else
                {
                    // rowParam.AddRange(ExtractChildren(child, GenerateTableName(parentName, child.Name.ToString()), tableNameList));
                    foreach (var asd in ExtractChildren(child, GenerateTableName(parentName, child.Name.ToString()), tableNameList))
                    {
                        if (rowParam.ContainsKey(asd.Key))
                        {
                            foreach (var listElem in asd.Value)
                            {
                                rowParam[asd.Key].Add(listElem);
                            }
                        }
                        else
                        {
                            rowParam.Add(asd.Key, asd.Value);
                        }
                    }

                }

            }
            childFlag = false;
            return rowParam;
        }
        private static string GenerateTableName(string parentName, string elementName)
        {
            return parentName + "," + elementName;
        }
        private static string GenerateTableName(string parentName)
        {
            return parentName;
        }
    }
    static class FileValues
    {
        public static string Timestamp { get; set; }
        public static string Ne { get; set; }
        public static string TableName { get; set; }

        public static string FilePath { get; set; }
        public static string FileCheckerTableName { get; set; }
        public static long FileSize { get; set; }
        public static string SHA_512 { get; set; }
        public static void SetFileValues(XCMParser parser, string filePath, string fileName)
        {

            try
            {
                FilePath = filePath;
                string fileNameWoutExt = Path.GetFileNameWithoutExtension(filePath);
                string fileNameWoutAllExt = Path.GetFileNameWithoutExtension(fileNameWoutExt);
                var values = Regex.Matches(fileNameWoutAllExt, @"[^_\s][^_]*[^_\s]*");
                // string date = values[parser.DateIndex - 1].Value + "_" + values[parser.DateIndex].Value;
                // string dateBackup = values[parser.DateIndex].Value;
                string date = string.Join("_", values.Where((item, index) => parser.TimestampIndex.Contains(index)));
                //foreach (var e in parser.DateIndex) { date = string.Append(values[e]); };

                TableName = string.Join("_", values.Where((item, index) => parser.TypeIndex.Contains(index)));
                Ne = string.Join("_", values.Where((item, index) => parser.NeIndex.Contains(index)))
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
        private static void ParseDate(string date, string[] formats, string fileName)
        {
            if (DateTime.TryParseExact(date, formats, null, DateTimeStyles.None, out DateTime v))
            {
                Timestamp = v.ToString("u");
            }
            else if (DateTime.TryParseExact(date = Regex.Replace(date, "[^.0-9_]", ""), formats, null, DateTimeStyles.None, out v))
            {
                Timestamp = v.ToString("u");
            }
            else
            {
                throw new FormatException("File " + fileName + " couldn't be parsed by any DateTime formats.");
            }
        }

        public static void CallConverter(string inputPath, SQLiteConnection dbConnection, SQLiteConnection fileCheckerdbConnection)
        {
            var inputPaths = Path.GetDirectoryName(inputPath);
            var sourceFileMask = Path.GetFileName(inputPath);

            if (Directory.Exists(Path.Join(inputPaths)))
            {

                foreach (string filePath in Directory.EnumerateFiles(inputPaths, sourceFileMask, searchOption: SearchOption.AllDirectories))
                {
                    //var values = new DBValues(dbFilePath);
                    string fileName = Path.GetFileName(filePath);

                    //var start = StopwatchProxy.Instance.Stopwatch.ElapsedMilliseconds;

                    if (fileName.Contains("xcm.gz"))
                    {
                        var values = DBValues.GetDBValues(dbConnection);
                        XCMParser xcmParser = new XCMParser();

                        Console.WriteLine(fileName);



                        using (FileStream oldstream = File.OpenRead(filePath))
                        using (GZipStream zippedStream = new GZipStream(oldstream, CompressionMode.Decompress))
                        {
                            var fileExists = FileValues.FileExists(filePath, fileCheckerdbConnection);

                            if (!fileExists)
                            {
                                if (filePath.Contains(".xcm.gz"))
                                {
                                    xcmParser.setXMLParser(zippedStream);
                                }
                                else
                                {
                                    xcmParser.setXMLParser(oldstream);
                                }
                                //var stop = StopwatchProxy.Instance.Stopwatch.ElapsedMilliseconds;
                                //Log.Information("Parse end time for {Full_File_Path} is {Parse_End_Time} and the duration is {Parse_Duration}", filePath, stop, stop - start);
                                XCMConverter.Convert(
                                        xcmParser,
                                        zippedStream,
                                        oldstream,
                                        filePath,
                                        dbConnection,
                                        fileCheckerdbConnection,
                                        values.ColumnIndices,
                                        values.DbInsertCommandCache


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

        private static bool FileExists(string filePath, SQLiteConnection fileCheckerdbConnection)
        {
            string cmd;
            SQLiteCommand dbCommand;
            bool zippedFlag = true;
            int index = 0;
            zippedFlag = zippedFlag ? filePath.Contains("xcm.gz") : false;
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
