using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;
using System.Text;

 namespace SoemXmlToSQLite
{
    class ParserFactory
    {
        public static IParser CreateParser(string fileName)
        {
            if (Path.GetExtension(fileName) == ".csv")
            {
                return new CSVParser();
            }
            return null;
        }
    }
}

