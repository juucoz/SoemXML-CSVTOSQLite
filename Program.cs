using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SoemXmlToSQLite
{
    class Program
    {
        static void Main(string[] args)
        {
            // var opts = Options.GetOptions(args);

            string inputPath = @"C:\Users\ata.akcay\Desktop\inputFile";
            string sourceFileMask = "*.csv";
            string dbFilePath = "soem6.sqlite";

            var opts = new Options();
            opts.InputPath = inputPath;
            opts.SourceFileMask = sourceFileMask;
            opts.DbFilePath = dbFilePath;

            // string inputPath = opts.InputPath;
            // string sourceFileMask = opts.SourceFileMask;
            // string dbFilePath = opts.DbFilePath;

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

