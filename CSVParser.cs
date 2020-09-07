using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Globalization;
using Serilog;

namespace SoemXmlToSQLite
{

    class CSVParser : IParser
    {
       // public System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
        private string[] _headers;
        private TextFileParseOutput _defaultResult;
        int skippedLineCounter = 0;
        

        [DefaultValue(",")]
        public string SourceSeparator { get; set; } = ",";

        [DefaultValue(1)]
        public int HeaderLine { get; set; }

        [DefaultValue(false)]
        public bool SkipEscape { get; set; }

        [DefaultValue(0)]
        public int TypeIndex { get; set; }
        public TextFileParseOutput Parse(FileStream input)
        {
            var start = StopwatchProxy.Instance.Stopwatch.ElapsedMilliseconds;
            _defaultResult = new TextFileParseOutput();
            SetFileValues(_defaultResult, input.Name);
            StreamReader reader = new StreamReader(input);

            string unTrimmedHeaders = "Timestamp" + SourceSeparator + ReadHeaders(input);

            ParseHeaders(unTrimmedHeaders, _defaultResult);
            input.Position = 0;

            for (var space = 1; space <= HeaderLine; space++)
            {
                reader.ReadLine();
            }

            while (!reader.EndOfStream)
            {
                string line = _defaultResult.Timestamp + SourceSeparator + reader.ReadLine();


                ParseLine(line, _defaultResult);
            }
            Console.WriteLine(input.Position);
            var stop = StopwatchProxy.Instance.Stopwatch.ElapsedMilliseconds;

            _defaultResult.logValues = new LogValues(input.Name,start,stop,stop-start,_defaultResult.Data.Count - skippedLineCounter,skippedLineCounter,0,""); 
            return _defaultResult;
        }

        protected string ReadHeaders(FileStream inp)
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


            _defaultResult.Headers.AddRange(_headers);

        }
        /// <summary>
        ///
        /// </summary>
        /// <param name="line"></param>
        protected void ParseLine(string line, TextFileParseOutput _defaultResult)
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
                foreach (var dh in dataRow.Zip(_defaultResult.Headers, Tuple.Create))
                {
                    dict.Add(dh.Item2, dh.Item1);
                }

                _defaultResult.Data.Add(dict);
            }
            else
            {
                Log.Error("Invalid value count in row {Errow_Row}", _defaultResult.Data.Count + HeaderLine + 1 + skippedLineCounter);
                Console.WriteLine($"Invalid value count in row {_defaultResult.Data.Count + HeaderLine + 1 + skippedLineCounter } ");
                skippedLineCounter++;
            }
            
            

        }
        private void SetFileValues(TextFileParseOutput _defaultResult, string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            try
            {
                _defaultResult.FilePath = filePath;
                
                _defaultResult.Ne = Regex.Match(fileName, @".+?(?=_)").Value;
                _defaultResult.Type = Regex.Matches(fileName, @"(?<=_)(.*?)(?=_)")[TypeIndex].Value;

                if (DateTime.TryParseExact(Regex.Match(fileName, @"\d{4}-\d{2}-\d{2}_\d{2}h\d{2}m\d{2}sZ").Value, "yyyy-MM-dd_HH'h'mm'm'ss'sZ'", null, DateTimeStyles.None, out DateTime v))
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
            catch (ArgumentOutOfRangeException a)
            {
                Log.Error(a,"Argument Out of Range Exception occured.");
                Console.WriteLine(a.Message);
            }
            catch (FormatException e)
            {
                Log.Error(e,"File, {File_Name} couldn't be parsed by any DateTime formats.", fileName );
                Console.WriteLine(e.Message);
            }
        }
    }
}
