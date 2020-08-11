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
        private TextFileParseOutput _defaultResult;

        [DefaultValue(",")]
        public string SourceSeparator { get; set; } = ",";

        public TextFileParseOutput Parse(Stream input)
        {
            TextFileParseOutput _defaultResult = new TextFileParseOutput();

            StreamReader reader = new StreamReader(input);
            if (reader.EndOfStream)
                throw new ApplicationException("File is empty!");

            string unTrimmedHeaders = reader.ReadLine();
            ReadHeaders(unTrimmedHeaders);

            

            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                ReadLine(line);
            }

            return _defaultResult;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="headerLine"></param>
        protected void ReadHeaders(string headerLine)
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
        protected void ReadLine(string line)
        {           
            string target;

            target = line.Trim();
            
            string[] dataRow;
            char[] separators = SourceSeparator.ToCharArray();
            dataRow = target.Split(separators);
            

            if (dataRow.Length == _headers.Length)
            {
                Dictionary<string, string> dict = new Dictionary<string, string>();
               // _defaultResult.data.AddRange(_headers.ToList<string>, dataRow.ToList<string>);
            }
            else
            {
               
                    throw new ApplicationException($"Mismatch on field count between headers and data line: {Environment.NewLine}{line}");
                
            }
        }
    }
}
