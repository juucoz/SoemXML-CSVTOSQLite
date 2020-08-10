using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace SoemXmlToSQLite
{
    class Options
    {
        [Option('i', "input-path", HelpText = "Path of the file which the contexts of will be saved to SQLite",Required = true)]
        public string InputPath { get; set;}

        [Option('m', "sourceFileMask", HelpText = "Files that will be saved to SQLite.", Required = true)]
        public string SourceFileMask { get; set; }

        [Option('d', "dbFilePath", HelpText = "Path of the db file.",Required = true)]
        public string DbFilePath { get; set; }

       
    }
}
