using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Globalization;
using Serilog;
using System.Xml;
using System.IO.Compression;

namespace PiTnProcessor
{

    class CSVParser : IParser
    {
        // public System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
        private StreamReader reader;
        private string[] _headers;
        private char[] separators;
        private TextFileParseOutput _defaultResult;
        int skippedLineCounter = 0;


        [DefaultValue(",")]
        public string SourceSeparator { get; set; } = ",";

        [DefaultValue(1)]
        public int HeaderLine { get; set; }

        [DefaultValue(false)]
        public bool SkipEscape { get; set; }

        [DefaultValue(0)]
        public int[] TypeIndex { get; set; }

        [DefaultValue(0)]
        public int[] NeIndex { get; set; }

        [DefaultValue(0)]
        public int[] DateIndex { get; set; }
        public TextFileParseOutput Init(GZipStream input)
        {
            _defaultResult = new TextFileParseOutput();
            separators = SourceSeparator.ToCharArray();

            string unTrimmedHeaders = "Timestamp" + SourceSeparator + ReadHeaders(input);
            ParseHeaders(unTrimmedHeaders, _defaultResult);

            reader = new StreamReader(input);

            input.Position = 0;
            for (var space = 1; space <= HeaderLine; space++)
            {
                reader.ReadLine();
            }

            return _defaultResult;
        }
        public TextFileParseOutput Parse(GZipStream input)
        {
            var start = StopwatchProxy.Instance.Stopwatch.ElapsedMilliseconds;

            while (!reader.EndOfStream)
            {
                string line = FileValues.Timestamp + SourceSeparator + reader.ReadLine();
                ParseLine(line, _defaultResult, separators);
                return _defaultResult;
            }
            var stop = StopwatchProxy.Instance.Stopwatch.ElapsedMilliseconds;

            //_defaultResult.logValues = new LogValues(input.Name,start,stop,stop-start,_defaultResult.Data.Count - skippedLineCounter,skippedLineCounter,0,""); 
            return null;
        }

        protected string ReadHeaders(GZipStream inp)
        {
            StreamReader rd = new StreamReader(inp, leaveOpen: true);

            for (var space = 1; space < HeaderLine; space++)
            {
                rd.ReadLine();
            }
            var unTrimmedHeaders = rd.ReadLine();
            return unTrimmedHeaders;

        }


        protected void ParseHeaders(string headerLine, TextFileParseOutput _defaultResult)
        {
            string targetLine = headerLine;
            if (SkipEscape)
            {
                targetLine = targetLine.Replace("\"", "");
            }


            char[] separators = SourceSeparator.ToCharArray();

            _headers = targetLine.Split(separators);


            // create parse item


            // _defaultResult.Headers.AddRange(_headers);

        }
        /// <summary>
        ///
        /// </summary>
        /// <param name="line"></param>
        protected void ParseLine(string line, TextFileParseOutput _defaultResult, char[] separators)
        {
            string[] dataRow;
            string target;

            target = line.Trim().Replace("'", "''");
            if (SkipEscape)
            {
                dataRow = Regex.Split(target, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
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

            if (dataRow.Length == _headers.Length)
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
                Log.Error("Invalid value count in row {Errow_Row}", _defaultResult.RowValues.Count + HeaderLine + 1 + skippedLineCounter);
                Console.WriteLine($"Invalid value count in row {_defaultResult.RowValues.Count + HeaderLine + 1 + skippedLineCounter } ");
                skippedLineCounter++;
            }



        }
        //private void SetFileValues(TextFileParseOutput _defaultResult, string filePath)
        //{
        //    string fileName = Path.GetFileName(filePath);
        //    try
        //    {
        //        FileValues.FilePath = filePath;
        //        string fileNameWoutExc = Path.GetFileNameWithoutExtension(filePath);
        //        var values = Regex.Matches(fileNameWoutExc, @"[^_\s][^_]*[^_\s]*");

        //        FileValues.Ne = values[NeIndex].Value;
        //        FileValues.Type = values[TypeIndex].Value;

        //        string date = values[DateIndex - 1].Value + "_" + values[DateIndex].Value;
        //        string dateBackup = values[DateIndex].Value;
        //        string[] formatStrings = { "yyyy-MM-dd_HH'h'mm'm'ss'sZ'", "yyyy-MM-dd_HH-mm-ss", "yyyyMMddHHmmZ" };
        //        ParseDate(date, dateBackup, formatStrings, fileName);

        //    }
        //    catch (ArgumentOutOfRangeException a)
        //    {
        //        Log.Error(a, "Argument Out of Range Exception occured.");
        //        Console.WriteLine(a.Message);
        //    }
        //    catch (FormatException e)
        //    {
        //        Log.Error(e, "File, {File_Name} couldn't be parsed by any DateTime formats.", fileName);
        //        Console.WriteLine(e.Message);
        //    }
        //}
        //private void ParseDate(string date, string dateBackup, string[] formats, string fileName)
        //{
        //    if (DateTime.TryParseExact(date, formats, null, DateTimeStyles.None, out DateTime v))
        //    {
        //        FileValues.Timestamp = v.ToString("u");
        //    }
        //    else if (DateTime.TryParseExact(dateBackup, formats, null, DateTimeStyles.None, out v))
        //    {
        //        FileValues.Timestamp = v.ToString("u");
        //    }
        //    else
        //    {
        //        throw new FormatException("File " + fileName + " couldn't be parsed by any DateTime formats.");
        //    }
        //}
    }
}
