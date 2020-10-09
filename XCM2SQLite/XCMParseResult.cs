using System.Collections.Generic;

namespace XCM2SQLite
{
    internal class XCMParseResult

    {
        public List<Dictionary<string, string>> RowValues { get; set; }



        public XCMParseResult()
        {
            RowValues = new List<Dictionary<string, string>>();
        }

    }
}