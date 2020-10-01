using System;
using System.IO;

namespace PiTnProcessor
{
    abstract class ParserFactory
    {
        public static IParser CreateParser(string filePath)
        {
            string fileName = Path.GetFileName(filePath);

            if (Path.GetExtension(filePath) == ".csv" || Path.GetFileName(filePath).Contains(".csv.gz"))
            {
                CSVParser parser = new CSVParser();

                parser.DateIndex = new int[] { 2,3 };
                if (fileName.Contains("PM"))
                {
                    parser.HeaderLine = 2;
                    parser.SkipEscape = true;
                    parser.TypeIndex = new int[] { 1 };
                    parser.NeIndex = new int[] { 0 };
                    parser.DateIndex = new int[]{ 3 };
                }
                else if (fileName.Contains("all_300"))
                {
                    parser.HeaderLine = 1;
                    parser.SkipEscape = false;
                    parser.TypeIndex = new int[] { 4 };
                    parser.NeIndex = new int[] { 0 };
                }
                else if (fileName.Contains("Report") || fileName.Contains("Information"))
                {
                    parser.HeaderLine = 10;
                    parser.SkipEscape = true;
                    parser.TypeIndex = new int[] { 1 };
                    parser.NeIndex = new int[] { 0 };
                }
                else
                {
                    parser.HeaderLine = 1;
                    parser.SkipEscape = true;
                    parser.TypeIndex = new int[] { 4 };
                    parser.NeIndex = new int[] { 0 };
                }
                return parser;
            }
            if (Path.GetExtension(filePath) == ".xml" || Path.GetFileName(filePath).Contains(".xml.gz"))
            {
                XMLParser parser = new XMLParser();
                parser.DateIndex = new int[] {3, 4};
                parser.NeIndex = new int[] { 0 };
                parser.TypeIndex = new int[] {1, 2};
                parser.ReadConfig = "row";

                if (fileName.Contains("TRAF_"))
                {
                    parser.DateIndex = new int[] { 0, 1 };
                    parser.NeIndex = new int[] { 4 };
                    parser.TypeIndex = new int[] { 1, 2, 3 };
                    parser.ReadConfig = "-";
                    if (fileName.Contains("TRAF_MEDIA"))
                    {
                        parser.ReadConfig = "equipment.MediaIndependentStatsLogRecord";

                    }
                    else if (fileName.Contains("TRAF_PORT"))
                    {
                        parser.ReadConfig = "equipment.PortNetEgressStatsLogRecord";
                    }
                    return parser;
                }
                if (fileName.Contains("SYS")|| fileName.Contains("RRL"))
                {
                    parser.DateIndex = new int[] { 0, 1 };
                    parser.NeIndex = new int[] { 4 };
                    parser.TypeIndex = new int[] { 1, 2, 3 };
                    parser.ReadConfig = "-";
                }
                
                if (filePath.Contains("YTC-5620SAM") || filePath.Contains("other-xml"))
                {
                    parser.DateIndex = new int[] { 0, 1 };
                    parser.NeIndex = new int[] { 1 };
                    parser.TypeIndex = new int[] { 1 };
                    parser.ReadConfig = "-";
                }
                if (filePath.Contains("TN_WAN"))
                {
                    parser.DateIndex = new int[] { 6, 7 };
                    parser.NeIndex = new int[] { 0 };
                    parser.TypeIndex = new int[] { 1, 2, 3, 4, 5 };
                    parser.ReadConfig = "row";
                } 
                return parser;
            }
            return null;
        }
    }
}

