using System.Collections.Generic;

namespace XCM2SQLite
{
    internal class XCMParseResult

    {
        public Dictionary<string,List<Dictionary<string, string>>> RowValues { get; set; }



        public XCMParseResult()
        {
            RowValues = new Dictionary<string, List<Dictionary<string, string>>>();
        }

    }
}