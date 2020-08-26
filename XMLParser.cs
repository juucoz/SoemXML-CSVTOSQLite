using System.IO;

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
