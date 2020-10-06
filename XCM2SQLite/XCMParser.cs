using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace XCM2SQLite
{
    class XCMParser
    {

        public XmlReader xmlReader { get; set; }

        [DefaultValue(1)]
        public int[] TimestampIndex { get; set; }
        public int[] NeIndex { get; set; }
        public int[] TypeIndex { get; set; }

        public string ReadConfig { get; set; }
        public bool Flag { get; set; }


        private XCMParseResult _defaultResult;

        public XmlReaderSettings xmlReaderSettings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Ignore,
            IgnoreComments = true,
            IgnoreProcessingInstructions = true,
            IgnoreWhitespace = true,
            CheckCharacters = false,
            ValidationFlags = System.Xml.Schema.XmlSchemaValidationFlags.None
        };

        public XCMParser()
        {
            Flag = false;
        }

        public void setXMLParser(Stream input)
        {
            //input.Position = 0;
            xmlReader = XmlReader.Create(input, xmlReaderSettings);
        }
        public XCMParseResult Parse<T>(T input)
        {
            //var start = StopwatchProxy.Instance.Stopwatch.ElapsedMilliseconds;
            _defaultResult = new XCMParseResult();

            //flag = flag ? true : SetFileValues(_defaultResult, filePath, fileName);
            var rowParam = new Dictionary<string, string>();
            while (xmlReader.NodeType == XmlNodeType.EndElement)
            {
                xmlReader.Read();
            }
            while (xmlReader.MoveToNextAttribute())
            {
                string headerValue = xmlReader.Name.ToString();
                string dataValue = xmlReader.Value;
                rowParam.Add(headerValue, dataValue);
            }
            XElement xAttributes;
            xmlReader.ReadToFollowing("attributes");
            if(xmlReader.ReadState != ReadState.EndOfFile)
            {
               xAttributes = (XElement)XNode.ReadFrom(xmlReader);
                xAttributes.Ancestors();
            }
            else
            {
                return null;
            }
            

            foreach (var element in xAttributes.Elements())
            {
                string headerValue = element.Name.ToString();
                string dataValue = element.Value;
                rowParam.Add(headerValue, dataValue);
                
            }
        

        _defaultResult.RowValues = rowParam;

            //var stop = StopwatchProxy.Instance.Stopwatch.ElapsedMilliseconds;
            //_defaultResult.logValues = new LogValues(input.Name,DateTime.Now.ToString(), start, stop, stop - start, _defaultResult.Data.Count, 0, 0, "");
            return _defaultResult;




        }
}
static class FileValues
{
    public static string Timestamp { get; set; }
    public static string Ne { get; set; }
    public static string TableName { get; set; }

    public static string FilePath { get; set; }
    public static void SetFileValues(XCMParser parser, string filePath, string fileName)
    {

        try
        {
            FilePath = filePath;
            string fileNameWoutExt = Path.GetFileNameWithoutExtension(filePath);
            string fileNameWoutAllExt = Path.GetFileNameWithoutExtension(fileNameWoutExt);
            var values = Regex.Matches(fileNameWoutAllExt, @"[^_\s][^_]*[^_\s]*");
            // string date = values[parser.DateIndex - 1].Value + "_" + values[parser.DateIndex].Value;
            // string dateBackup = values[parser.DateIndex].Value;
            string date = string.Join("_", values.Where((item, index) => parser.TimestampIndex.Contains(index)));
            //foreach (var e in parser.DateIndex) { date = string.Append(values[e]); };

            TableName = string.Join("_", values.Where((item, index) => parser.TypeIndex.Contains(index)));
            Ne = string.Join("_", values.Where((item, index) => parser.NeIndex.Contains(index)))
                                     .Replace("-", "_").ToLowerInvariant();
            string[] formatStrings = { "yyyy-MM-dd_HH'h'mm'm'ss'sZ'", "yyyy-MM-dd_HH-mm-ss", "yyyyMMddHHmmZ", "yyyyMMdd_HHmm", "yyyyMMdd_HHmmss", "yyyyMMddHHmmss" };
            //_defaultResult.Timestamp = DateTime.ParseExact(Regex.Match(fileName, @"\d{8}_\d{6}").Value, "yyyyMMdd_HHmmss", null).ToString("s");
            ParseDate(date, formatStrings, fileName);
        }
        catch (ArgumentOutOfRangeException a)
        {
            Console.WriteLine(a.Message);
        }
        catch (FormatException e)
        {
            //Log.Error(e, "File {File_Name} couldn't be parsed by any DateTime formats.", fileName);
            Console.WriteLine(e.Message);
        }
    }
    private static void ParseDate(string date, string[] formats, string fileName)
    {
        if (DateTime.TryParseExact(date, formats, null, DateTimeStyles.None, out DateTime v))
        {
            Timestamp = v.ToString("u");
        }
        else if (DateTime.TryParseExact(date = Regex.Replace(date, "[^.0-9_]", ""), formats, null, DateTimeStyles.None, out v))
        {
            Timestamp = v.ToString("u");
        }
        else
        {
            throw new FormatException("File " + fileName + " couldn't be parsed by any DateTime formats.");
        }
    }
}
}
