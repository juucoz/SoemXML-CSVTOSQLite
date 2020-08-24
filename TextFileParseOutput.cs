using System;
using System.Collections.Generic;
using System.Text;

namespace SoemXmlToSQLite
{
    public class TextFileParseOutput
    {
        public string Ne { get; set; }
        public string Timestamp { get; set; }
        public List<string> headers { get; set; }
        public List<Dictionary<string,string>> data { get; set; }

        public TextFileParseOutput(string ne, string timestamp, List<string> headers, List<Dictionary<string, string>> data)
        {
            Ne = ne;
            Timestamp = timestamp;
            this.headers = headers;
            this.data = data;
        }

        public TextFileParseOutput()
        {
            headers = new List<string>();
            data = new List<Dictionary<string, string>>();
        }
    }

}
