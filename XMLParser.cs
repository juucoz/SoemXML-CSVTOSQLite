using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace PiTnProcessor
{
    class XMLParser : IParser
    {
        public XmlReader xmlReader { get; set; }

        [DefaultValue(1)]
        public int DateIndex { get; set; }
        public string ReadConfig { get; set; }
        public bool flag = false;

        
        private TextFileParseOutput _defaultResult;

        public XmlReaderSettings xmlReaderSettings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Ignore,
            IgnoreComments = true,
            IgnoreProcessingInstructions = true,
            IgnoreWhitespace = true,
            CheckCharacters = false,
            ValidationFlags = System.Xml.Schema.XmlSchemaValidationFlags.None
        };

        public XMLParser()
        {
            flag = false;
        }

        public void setXMLParser(FileStream input)
        {
            xmlReader = XmlReader.Create(input, xmlReaderSettings);
        }
        public TextFileParseOutput Parse(FileStream input)
        {
            //var start = StopwatchProxy.Instance.Stopwatch.ElapsedMilliseconds;
            _defaultResult = new TextFileParseOutput();
            string fileName = Path.GetFileName(input.Name);
            string filePath = input.Name;

            //flag = flag ? true : SetFileValues(_defaultResult, filePath, fileName);



            XElement xObject = (XElement)XNode.ReadFrom(xmlReader);
            var rowParam =
                new Dictionary<string, string>()
                {
                                {"NE", FileValues.Ne },
                                {"Timestamp", FileValues.Timestamp }
                };
            if (ReadConfig == "row")
                foreach (var xAttribute in xObject.Attributes())
                {
                    string headerValue = xAttribute.Name.ToString();
                    string dataValue = xAttribute.Value;
                    rowParam.Add(headerValue, dataValue);
                }
            else
            {
                foreach (var element in xObject.Elements())
                {
                    string headerValue = element.Name.LocalName.ToString();
                    string dataValue = element.Value;
                    rowParam.Add(headerValue, dataValue);
                }
            }

            _defaultResult.RowValues = rowParam;

            //var stop = StopwatchProxy.Instance.Stopwatch.ElapsedMilliseconds;
            //_defaultResult.logValues = new LogValues(input.Name, start, stop, stop - start, _defaultResult.Data.Count, 0, 0, "");
            return _defaultResult;




        }
        //private bool SetFileValues(TextFileParseOutput _defaultResult, string filePath, string fileName)
        //{
        //    try
        //    {
        //        _defaultResult.FilePath = filePath;
        //        string fileNameWoutExc = Path.GetFileNameWithoutExtension(filePath);
        //        var values = Regex.Matches(fileNameWoutExc, @"[^_\s][^_]*[^_\s]*");
        //        string date = values[DateIndex - 1].Value + "_" + values[DateIndex].Value;
        //        string dateBackup = values[DateIndex].Value;

        //        _defaultResult.Ne = Regex.Match(fileName, @".+?(?=_)").Value;
        //        _defaultResult.Class = "SMsaskdla"; //Regex.Match(fileName, @"(?<=^.+?_).+(?=_\d{8})").Value;
        //        string[] formatStrings = { "yyyy-MM-dd_HH'h'mm'm'ss'sZ'", "yyyy-MM-dd_HH-mm-ss", "yyyyMMddHHmmZ", "yyyyMMdd_HHmm" };
        //        //_defaultResult.Timestamp = DateTime.ParseExact(Regex.Match(fileName, @"\d{8}_\d{6}").Value, "yyyyMMdd_HHmmss", null).ToString("s");
        //        ParseDate(date, dateBackup, formatStrings, fileName);
        //        return true;
        //    }
        //    catch (ArgumentOutOfRangeException a)
        //    {
        //        Console.WriteLine(a.Message);
        //        return false;
        //    }
        //    catch (FormatException e)
        //    {
        //        Log.Error(e, "File {File_Name} couldn't be parsed by any DateTime formats.", fileName);
        //        Console.WriteLine(e.Message);
        //        return false;
        //    }
        //}
        //private void ParseDate(string date, string dateBackup, string[] formats, string fileName)
        //{
        //    if (DateTime.TryParseExact(date, formats, null, DateTimeStyles.None, out DateTime v))
        //    {
        //        _defaultResult.Timestamp = v.ToString("u");
        //    }
        //    else if (DateTime.TryParseExact(dateBackup, formats, null, DateTimeStyles.None, out v))
        //    {
        //        _defaultResult.Timestamp = v.ToString("u");
        //    }
        //    else
        //    {
        //        throw new FormatException("File " + fileName + " couldn't be parsed by any DateTime formats.");
        //    }
        //}
    }
}


