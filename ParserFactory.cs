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
                CSVParser parser = new CSVParser();

                if (fileName.Contains("PM"))
                {
                    parser.HeaderLine = 2;
                    parser.SkipEscape = true;
                }
                else if (fileName.Contains("all_300"))
                {
                    parser.HeaderLine = 1;
                    parser.SkipEscape = false;
                }
                else if (fileName.Contains("Report") || fileName.Contains("Information"))
                {
                    parser.HeaderLine = 10;
                    parser.SkipEscape = true;
                }
                else
                {
                    parser.HeaderLine = 1;
                    parser.SkipEscape = true;

                }

                
                
               /* Console.WriteLine($"Which line is the header in {fileName}?");
                string HeaderLine = Console.ReadLine();
                int number;
                if(!Int32.TryParse(HeaderLine, out number))
                {
                    Console.WriteLine("Enter a valid number.");
                    return CreateParser(fileName);
                }
                parser.HeaderLine = number;
               */

                return parser;
            }
            return null;
        }
    }
}

