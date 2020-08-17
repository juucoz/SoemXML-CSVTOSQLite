using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SoemXmlToSQLite
{
    class XMLParser : IParser
    {
        public TextFileParseOutput Parse(FileStream input)
        {
            TextFileParseOutput output = new TextFileParseOutput();
            return output;
        }
    }
}
