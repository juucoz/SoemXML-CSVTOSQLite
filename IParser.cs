using System.IO;

namespace PiTnProcessor
{
    internal interface IParser
    {
        public int[] DateIndex { get; set; }
        public int[] TypeIndex { get; set; }
        public int[] NeIndex { get; set; }
        public TextFileParseOutput Parse(FileStream input);

    }
}