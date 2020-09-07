using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;

namespace SoemXmlToSQLite
{
    public class StopwatchProxy
    {
        private Stopwatch _stopwatch;
        private static readonly StopwatchProxy _stopwatchProxy = new StopwatchProxy();

        private StopwatchProxy()
        {
            _stopwatch = new Stopwatch();
        }

        public Stopwatch Stopwatch { get { return _stopwatch; } }

        public static StopwatchProxy Instance
        {
            get { return _stopwatchProxy; }
        }
    }
    class Program
    {
        static void Main(string[] args)
        {


            Log.Logger = new LoggerConfiguration()
            .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();


            var opts = Options.GetOptions(args);

            //string inputPath = @"C:\Users\ata.akcay\Desktop\inputFile";
            //string sourceFileMask = "*.csv";
            //string dbFilePath = "soem6.sqlite";

            //var opts = new Options
            //{
            //    InputPath = inputPath,
            //    SourceFileMask = sourceFileMask,
            //    DbFilePath = dbFilePath
            //};

            string inputPath = opts.InputPath;
            string sourceFileMask = opts.SourceFileMask;
            string dbFilePath = opts.DbFilePath;

            Console.WriteLine(inputPath + sourceFileMask + dbFilePath);

            DBValues dbvalues = new DBValues(dbFilePath);

            using (SQLiteConnection dbConnection = new SQLiteConnection(dbvalues.DbConnectionString))
            {
                dbConnection.Open();
                var selectedFolder = FileValues.GetFileValue(inputPath);
                StopwatchProxy.Instance.Stopwatch.Start();
                FileValues.CallConverter(selectedFolder, opts, dbConnection, dbFilePath);

            }
            Log.CloseAndFlush();
        }
        



    }
}

