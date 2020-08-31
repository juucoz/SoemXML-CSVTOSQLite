using System.IO;

namespace SoemXmlToSQLite
{
    internal interface IParser
    {
        public TextFileParseOutput Parse(FileStream input);

    }
}