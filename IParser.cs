using System.Collections.Generic;
using System.IO;

namespace SoemXmlToSQLite
{
    internal interface IParser
    {
        public TextFileParseOutput Parse(Stream input);
        
    }
}