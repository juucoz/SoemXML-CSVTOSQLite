using System.IO;

namespace PiTnProcessor
{
    internal interface IParser
    {
        public TextFileParseOutput Parse(FileStream input);

    }
}