using System.IO;

namespace PiTnProcessor
{
    internal interface IParser
    {
        public int DateIndex { get; set; }
        public TextFileParseOutput Parse(FileStream input);

    }
}