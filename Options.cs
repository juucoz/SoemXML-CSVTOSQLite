using CommandLine;
using System;
using System.Linq;

namespace PiTnProcessor
{
    public class Options
    {
        [Option('i',"input-path", HelpText = "Path of the file which the contexts of will be saved to SQLite", Required = true)]
        public string InputPath { get; set; }


        [Option('d', "dbFilePath", HelpText = "Path of the db file.", Required = true)]
        public string DbFilePath { get; set; }


        public static Options GetOptions(string[] args)
        {
            var opts = new Options();
            var result = Parser.Default.ParseArguments<Options>(args)
            .WithParsed<Options>(p => opts = p)
            .WithNotParsed<Options>(e =>
            {
                var exitCode = -2;
                Console.WriteLine("errors {0}", e.Count());
                if (e.Any(x => x is HelpRequestedError || x is VersionRequestedError))
                    exitCode = -1;
                Console.WriteLine("Exit code {0}", exitCode);
            });

            if (result.Tag == ParserResultType.NotParsed)
            {
                Console.WriteLine("Usage: SoemXmlToSQLite -i <inputPath> -m *.xml -d <target.sqlite>");
                return null;
            }
            return opts;
        }
        
    }

}
