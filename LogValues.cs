using System.Collections.Generic;

namespace SoemXmlToSQLite
{
    public class LogValues
    {
        public Dictionary<string, string> Logs;

        public LogValues(string full_File_Path, long parse_Start_Time,long parse_End_Time, long parse_Duration,int success, int failure,long load_Duration, string target_Table)
        {
            Logs = new Dictionary<string, string>() { { "Full_File_Path", full_File_Path },
                { "Parse_Start_Time", parse_Start_Time.ToString() } ,
                { "End_Time", parse_End_Time.ToString() }, 
                { "Parse_Duration", parse_Duration.ToString() }, 
                { "Success", success.ToString() }, 
                { "Failure", failure.ToString() }, 
                { "Load_Duration", load_Duration.ToString() }, 
                { "Target_Table", target_Table } };
        }
    }
    
}

