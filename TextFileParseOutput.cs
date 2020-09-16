using System.Collections.Generic;

namespace PiTnProcessor
{
    public class TextFileParseOutput
    {
        public LogValues logValues;
        
        public Dictionary<string,string> RowValues { get; set; }

        //public TextFileParseOutput(string filePath, string ne, string type, string @class, string timestamp, Dictionary<string,string> rowValues)
        //{
            
        //    FilePath = filePath;
        //    Ne = ne;
        //    Type = type;
        //    Class = @class;
        //    Timestamp = timestamp;
        //    RowValues = rowValues;
        //}

        public TextFileParseOutput()
        {
            RowValues = new Dictionary<string, string>();
        }
    }

}
