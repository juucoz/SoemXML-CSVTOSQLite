using System;
using System.Data.SQLite;

namespace SoemXmlToSQLite
{
    class Program
    {
        static void Main(string[] args)
        {
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
                FileValues.CallConverter(selectedFolder, dbvalues, opts, dbConnection, dbFilePath);

            }

        }

    }
}

