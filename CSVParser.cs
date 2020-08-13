using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Linq;

namespace SoemXmlToSQLite
{
    class CSVParser : IParser
    {
        private string[] _headers;
        public TextFileParseOutput _defaultResult;

        [DefaultValue(",")]
        public string SourceSeparator { get; set; } = ",";

        public TextFileParseOutput Parse(Stream input)
        {
            _defaultResult = new TextFileParseOutput();
            _defaultResult.headers = new List<string>();
            _defaultResult.data = new List<Dictionary<string, string>>();

            StreamReader reader = new StreamReader(input);
            if (reader.EndOfStream)
                throw new ApplicationException("File is empty!");

            string unTrimmedHeaders = reader.ReadLine();
            ReadHeaders(unTrimmedHeaders,_defaultResult);

            

            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                ReadLine(line,_defaultResult);
            }

            return _defaultResult;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="headerLine"></param>
        protected void ReadHeaders(string headerLine,TextFileParseOutput _defaultResult)
        {
            string targetLine = headerLine;
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
            string target;

            target = line.Trim();
            
            string[] dataRow;
            char[] separators = SourceSeparator.ToCharArray();
            dataRow = target.Split(separators);
            

            if (dataRow.Length == _headers.Length)
            {
                Dictionary<string, string> dict = new Dictionary<string, string>();
                foreach (var dh in dataRow.Zip(_defaultResult.headers, Tuple.Create))
                {
                    dict.Add(dh.Item2, dh.Item1);
                }
                // _defaultResult.data.AddRange(_headers.ToList<string>, dataRow.ToList<string>);
                
                _defaultResult.data.Add(dict);
            }
            else
            {
               
                    throw new ApplicationException($"Mismatch on field count between headers and data line: {Environment.NewLine}{line}");
                
            }
        }
    }
}
