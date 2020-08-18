using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;

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
            StreamReader reader = new StreamReader(input);

            Console.WriteLine(input.Position);
            _defaultResult = new TextFileParseOutput
            {
                headers = new List<string>(),
                data = new List<Dictionary<string, string>>()
            };
            
            string unTrimmedHeaders = ReadHeaders(input);
            ReadHeaders(unTrimmedHeaders, _defaultResult);
            input.Position = 0;

            for (var i = 1; i <= HeaderLine; i++)
            {
                reader.ReadLine();
            }

            
            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                ReadLine(line,_defaultResult);
            }
            Console.WriteLine(input.Position);

            return _defaultResult;
        }

        protected string ReadHeaders(FileStream inp)
        {
            StreamReader rd = new StreamReader(inp, leaveOpen:true);
            
                for (var i = 1; i < HeaderLine; i++)
                {
                    rd.ReadLine();
                Console.WriteLine(inp.Position); 
            }
                var unTrimmedHeaderssss = rd.ReadLine();
            return unTrimmedHeaderssss;
            
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="headerLine"></param>
        protected void ReadHeaders(string headerLine,TextFileParseOutput _defaultResult)
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
        protected void ReadLine(string line,TextFileParseOutput _defaultResult)
        {
            string[] dataRow;
            char[] separators = SourceSeparator.ToCharArray();
            string target;

            target = line.Trim().Replace("'","''");
            if (SkipEscape)          
            {
                dataRow = Regex.Split(target, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
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
