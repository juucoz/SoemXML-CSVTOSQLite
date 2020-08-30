﻿using System.IO;

namespace SoemXmlToSQLite
{
    abstract class ParserFactory
    {
        public static IParser CreateParser(string filePath)
        {
            string fileName = Path.GetFileName(filePath);

            if (Path.GetExtension(filePath) == ".csv")
            {
                CSVParser parser = new CSVParser();

                if (fileName.Contains("PM"))
                {
                    parser.HeaderLine = 2;
                    parser.SkipEscape = true;
                    parser.TypeIndex = 0;
                }
                else if (fileName.Contains("all_300"))
                {
                    parser.HeaderLine = 1;
                    parser.SkipEscape = false;
                    parser.TypeIndex = 3;
                }
                else if (fileName.Contains("Report") || fileName.Contains("Information"))
                {
                    parser.HeaderLine = 10;
                    parser.SkipEscape = true;
                    parser.TypeIndex = 0;
                }
                else
                {
                    parser.HeaderLine = 1;
                    parser.SkipEscape = true;
                    parser.TypeIndex = 3;

                }
                return parser;
            }
            if(Path.GetExtension(filePath) == ".xml")
            {
                XMLParser parser = new XMLParser();
                return parser;
            }
            return null;
        }
    }
}

