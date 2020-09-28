using System.Collections.Generic;

namespace SNMP2SQLite
{
    internal class SNMPParseResult

    {
        public Dictionary<string, string> RowValues { get; set; }



        public SNMPParseResult()
        {
            RowValues = new Dictionary<string, string>();
        }

    }
}