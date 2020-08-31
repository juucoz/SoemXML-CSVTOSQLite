﻿using System.Collections.Generic;

namespace SoemXmlToSQLite
{
    public class TextFileParseOutput
    {
        public string Ne { get; set; }
        public string Type { get; set; }
        public string Class { get; set; }
        public string Timestamp { get; set; }
        public List<string> Headers { get; set; }
        public List<Dictionary<string, string>> Data { get; set; }

        public TextFileParseOutput(string ne, string type, string @class, string timestamp, List<string> headers, List<Dictionary<string, string>> data)
        {
            Ne = ne;
            Type = type;
            Class = @class;
            Timestamp = timestamp;
            this.Headers = headers;
            this.Data = data;
        }

        public TextFileParseOutput()
        {
            Headers = new List<string>();
            Data = new List<Dictionary<string, string>>();
        }
    }

}
