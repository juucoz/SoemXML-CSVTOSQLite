﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Globalization;

namespace SoemXmlToSQLite
{
    class CSVParser : IParser
    {
        private string[] _headers;
        public TextFileParseOutput _defaultResult;
        int skippedLineCounter = 0;

        [DefaultValue(",")]
        public string SourceSeparator { get; set; } = ",";

        [DefaultValue(1)]
        public int HeaderLine { get; set; }

        [DefaultValue(false)]
        public bool SkipEscape { get; set; }

        public TextFileParseOutput Parse(FileStream input)
        {
            DateTime v;
            _defaultResult = new TextFileParseOutput();
            string fileName = Path.GetFileName(input.Name);
            _defaultResult.Ne = Regex.Match(fileName, @".+?(?=_)").Value;
            string @class = Regex.Matches(fileName, @"(?<=_)(.*?)(?=_)")[3].Value;
            try
            {
                if (DateTime.TryParseExact(Regex.Match(fileName, @"\d{4}-\d{2}-\d{2}_\d{2}h\d{2}m\d{2}sZ").Value, "yyyy-MM-dd_HH'h'mm'm'ss'sZ'", null, DateTimeStyles.None, out v))
                {
                    _defaultResult.Timestamp = v.ToString("u");
                }

                else if (DateTime.TryParseExact(Regex.Match(fileName, @"\d{4}-\d{2}-\d{2}_\d{2}-\d{2}-\d{2}").Value, "yyyy-MM-dd_HH-mm-ss", null, DateTimeStyles.None, out v))
                {
                    _defaultResult.Timestamp = v.ToString("u");
                }
                else if (DateTime.TryParseExact(Regex.Match(fileName, @"\d{4}\d{2}\d{2}\d{2}\d{2}").Value, "yyyyMMddHHmm", null, DateTimeStyles.None, out v))
                {
                    _defaultResult.Timestamp = v.ToString("u");
                }
                else
                {
                    throw new FormatException("File " + fileName + " couldn't be parsed by any DateTime formats.");
                }
            }
            catch(FormatException e)
            {
                Console.WriteLine(e.Message);
            }
            StreamReader reader = new StreamReader(input);

            string unTrimmedHeaders = "Timestamp" + SourceSeparator + ReadHeaders(input);

            ReadHeaders(unTrimmedHeaders, _defaultResult);
            input.Position = 0;

            for (var i = 1; i <= HeaderLine; i++)
            {
                reader.ReadLine();
            }


            while (!reader.EndOfStream)
            {
                string line = _defaultResult.Timestamp + SourceSeparator + reader.ReadLine();

                ReadLine(line, _defaultResult);
            }
            Console.WriteLine(input.Position);

            return _defaultResult;
        }

        protected string ReadHeaders(FileStream inp)
        {
            StreamReader rd = new StreamReader(inp, leaveOpen: true);

            for (var i = 1; i < HeaderLine; i++)
            {
                rd.ReadLine();
            }
            var unTrimmedHeaderssss = rd.ReadLine();
            return unTrimmedHeaderssss;

        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="headerLine"></param>
        protected void ReadHeaders(string headerLine, TextFileParseOutput _defaultResult)
        {
            string targetLine = headerLine;
            if (SkipEscape)
            {
                targetLine = targetLine.Replace("\"", "");
            }


            char[] separators = SourceSeparator.ToCharArray();

            _headers = targetLine.Split(separators);


            // create parse item


            _defaultResult.headers.AddRange(_headers);

        }
        /// <summary>
        ///
        /// </summary>
        /// <param name="line"></param>
        protected void ReadLine(string line, TextFileParseOutput _defaultResult)
        {
            string[] dataRow;
            char[] separators = SourceSeparator.ToCharArray();
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
                foreach (var dh in dataRow.Zip(_defaultResult.headers, Tuple.Create))
                {
                    dict.Add(dh.Item2, dh.Item1);
                }

                _defaultResult.data.Add(dict);
            }
            else
            {
                Console.WriteLine($"Problem in row {_defaultResult.data.Count + HeaderLine + 1 + skippedLineCounter } ");
                skippedLineCounter++;
            }



        }
    }
}
