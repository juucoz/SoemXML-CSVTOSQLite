using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace PiTnProcessor
{
    class XMLParser : IParser
    {
        public TextFileParseOutput Parse(FileStream input)
        {
            var start = StopwatchProxy.Instance.Stopwatch.ElapsedMilliseconds;
            TextFileParseOutput _defaultResult = new TextFileParseOutput();
            string fileName = Path.GetFileName(input.Name);
            SetFileValues(_defaultResult, fileName);

            XmlReaderSettings xmlReaderSettings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Ignore,
                IgnoreComments = true,
                IgnoreProcessingInstructions = true,
                IgnoreWhitespace = true,
                CheckCharacters = false,
                ValidationFlags = System.Xml.Schema.XmlSchemaValidationFlags.None
            };
            using (XmlReader xmlReader = XmlReader.Create(input, xmlReaderSettings))
            {
                xmlReader.MoveToContent();
                while (!xmlReader.EOF)
                {
                    xmlReader.Read();
                    if (string.Equals(xmlReader.LocalName, "row", StringComparison.Ordinal))
                    {
                        break;
                    }
                }

                while (string.Equals(xmlReader.LocalName, "row", StringComparison.Ordinal))
                {
                    XElement xObject = (XElement)XNode.ReadFrom(xmlReader);
                    Dictionary<string, string> parameters =
                        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                                { "NE", _defaultResult.Ne },
                                { "TIMESTAMP", _defaultResult.Timestamp },
                        };
                    foreach (var xAttribute in xObject.Attributes())
                    {
                        string parameterName = xAttribute.Name.LocalName;
                        string parameterValue = xAttribute.Value;
                        parameters.Add(parameterName, parameterValue);
                    }

                    _defaultResult.Data.Add(parameters);
                    xmlReader.Skip();
                    
                }
                var stop = StopwatchProxy.Instance.Stopwatch.ElapsedMilliseconds;
                _defaultResult.logValues = new LogValues(input.Name, start, stop, stop - start, _defaultResult.Data.Count, 0, 0, "");

                return _defaultResult;
            }
        }
        private void SetFileValues(TextFileParseOutput _defaultResult, string fileName)
        {
            try
            {
                _defaultResult.FilePath = fileName;
                _defaultResult.Ne = Regex.Match(fileName, @".+?(?=_)").Value;
                _defaultResult.Class = Regex.Match(fileName, @"(?<=^.+?_).+(?=_\d{8})").Value;
                _defaultResult.Timestamp = DateTime.ParseExact(Regex.Match(fileName, @"\d{8}_\d{6}").Value, "yyyyMMdd_HHmmss", null).ToString("s");
            }
            catch (ArgumentOutOfRangeException a)
            {
                Console.WriteLine(a.Message);
            }
            catch (FormatException e)
            {
                Log.Error(e,"File {File_Name} couldn't be parsed by any DateTime formats.",fileName);
                Console.WriteLine(e.Message);
            }
        }
    }
}


