using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;

namespace PiTnProcessor
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
    public class Program
    {
        public static Options opts;
        static void Main(string[] args)
        {


            Log.Logger = new LoggerConfiguration()
            .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();


            opts = Options.GetOptions(args);
            if (opts is null)
            {
                return;
            }
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
            string dbFilePath = opts.DbFilePath;


            //DBValues dbvalues = new DBValues(dbFilePath);
            var dbvalues = DBValues.GetDBValues(dbFilePath);

            //using (SQLiteConnection dbConnection = dbvalues.DbConnection)
            //{
            //    dbConnection.Open();
            //   //var selectedFolder = FileValues.GetFileValue(opts.InputPath);
            //    StopwatchProxy.Instance.Stopwatch.Start();
            //    FileValues.CallConverter(opts, dbConnection);

            //}
            Log.CloseAndFlush();
        }
        



    }
}

