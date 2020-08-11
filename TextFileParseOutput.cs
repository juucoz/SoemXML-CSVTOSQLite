using System;
using System.Collections.Generic;
using System.Text;

namespace SoemXmlToSQLite
{
    public class TextFileParseOutput
    {
        public List<string> headers { get; set; }
        public List<Dictionary<string,string>> data { get; set; }
    }
}
