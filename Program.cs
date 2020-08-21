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
                FileValues.CallConverter(selectedFolder, dbvalues, opts, dbConnection);
            }
            /*  var dbConnectionStringBuilder = new SQLiteConnectionStringBuilder
              {
                  DataSource = dbFilePath,
                  Pooling = false
              };

              string dbConnectionString = dbConnectionStringBuilder.ConnectionString;


              using (SQLiteConnection dbConnection = new SQLiteConnection(dbConnectionString))
              {
                  dbConnection.Open();

                  var columnIndices = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);
                  var dbInsertCommandCache = new Dictionary<string, SQLiteCommand>(StringComparer.OrdinalIgnoreCase);

                  var existingTableNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                  using (SQLiteCommand dbCommand = new SQLiteCommand("SELECT name FROM sqlite_master WHERE type='table'", dbConnection))
                  {
                      using (SQLiteDataReader dbReader = dbCommand.ExecuteReader())
                      {
                          while (dbReader.Read())
                          {
                              string tableName = dbReader.GetString(0);
                              existingTableNames.Add(tableName);
                          }
                      }
                  }
                  foreach (string tableName in existingTableNames)
                  {
                      var columnNames = new List<string>();
                      using (SQLiteCommand dbCommand = new SQLiteCommand($"pragma table_info([{tableName}])", dbConnection))
                      {
                          using (SQLiteDataReader dbReader = dbCommand.ExecuteReader())
                          {
                              while (dbReader.Read())
                              {
                                  string columnName = dbReader.GetString(1);
                                  columnNames.Add(columnName);
                              }
                          }
                      }
                      columnIndices.Add(tableName, columnNames.Select((s, i) => new { s, i }).ToDictionary(o => o.s, o => o.i, StringComparer.OrdinalIgnoreCase));
                      var dbInsertCommand = new SQLiteCommand($"INSERT INTO [{tableName}] VALUES ({string.Join(",", Enumerable.Repeat("?", columnNames.Count))})", dbConnection);
                      for (int i = 0; i < columnNames.Count; i++)
                          dbInsertCommand.Parameters.Add(new SQLiteParameter());
                      dbInsertCommandCache.Add(tableName, dbInsertCommand);
                  }


                  string[] dirNames = Directory.GetDirectories(inputPath);
                  foreach (string dirName in dirNames)
                  {
                      Console.WriteLine(Path.GetFileNameWithoutExtension(dirName));
                  }
                  Console.WriteLine("Enter the directory name that you want to save to SQLite, enter ALL to save all files in the directory.");
                  string selectedFolder = Console.ReadLine();

                  if (selectedFolder.Equals("ALL"))
                  {
                      foreach (string filePath in Directory.EnumerateFiles(inputPath, sourceFileMask, SearchOption.AllDirectories))
                      {

                          string fileName = Path.GetFileName(filePath);
                          Console.WriteLine(fileName);
                          // SOEMDSP1_MINI-LINK_AGC_20191023_001500.xml
                          string ne = Regex.Match(fileName, @".+?(?=_)").Value;
                          string @class = Regex.Match(fileName, @"(?<=^.+?_).+(?=_\d{8})").Value;
                          string timestamp = DateTime.ParseExact(Regex.Match(fileName, @"\d{8}_\d{6}").Value, "yyyyMMdd_HHmmss", null).ToString("s");
                          using (FileStream stream = File.OpenRead(filePath))
                          {
                              SoemXmlToDbConverter.Convert(
                                  stream,
                                  ne,
                                  @class,
                                  timestamp,
                                  dbConnection,
                                  columnIndices,
                                  dbInsertCommandCache);
                          }
                      }
                  }

                  /*   else if (Directory.Exists(inputPath + $"\\{selectedFolder}"))
                     {
                         foreach (string filePath in Directory.EnumerateFiles(inputPath + $"\\{selectedFolder}", sourceFileMask, SearchOption.AllDirectories))
                         {
                             string fileName = Path.GetFileName(filePath);
                             Console.WriteLine(fileName);
                             // SOEMDSP1_MINI-LINK_AGC_20191023_001500.xml
                             string ne = Regex.Match(fileName, @".+?(?=_)").Value;
                             string @class = Regex.Match(fileName, @"(?<=^.+?_).+(?=_\d{8})").Value;
                             string timestamp = DateTime.ParseExact(Regex.Match(fileName, @"\d{8}_\d{6}").Value, "yyyyMMdd_HHmmss", null).ToString("s");
                             using (FileStream stream = File.OpenRead(filePath))
                             {
                                 SoemXmlToDbConverter.Convert(
                                     stream,
                                     ne,
                                     @class,
                                     timestamp,
                                     dbConnection,
                                     columnIndices,
                                     dbInsertCommandCache);
                             }
                         }
                     } 

                  else if (Directory.Exists(inputPath + $"\\{selectedFolder}"))
                  {
                      foreach (string filePath in Directory.EnumerateFiles(inputPath + $"\\{selectedFolder}", sourceFileMask, SearchOption.AllDirectories))
                      {
                          var parser = ParserFactory.CreateParser(filePath);

                          string fileName = Path.GetFileName(filePath);
                          Console.WriteLine(fileName);
                          // SOEMDSP1_MINI-LINK_AGC_20191023_001500.xml
                          string fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                          string ne = Regex.Match(fileName, @".+?(?=_)").Value;


                          using (FileStream stream = File.OpenRead(filePath))
                          {
                              SoemXmlToDbConverter.ConvertTest(
                                  parser,
                                  stream,
                                  ne,
                                  fileNameWithoutExt,
                                  dbConnection,
                                  columnIndices,
                                  dbInsertCommandCache);
                          }
                      }
                  }
                  else
                  {
                      Console.WriteLine("This folder does not exist in the input file.");
                      return;
                  }


              }
          */
        }
    }
}

