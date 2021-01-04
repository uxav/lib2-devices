 
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Crestron.SimplSharp.CrestronIO;
using Crestron.SimplSharp.CrestronXml;

namespace UX.Lib2.Devices.Cisco
{
    internal class CodecCommand
    {
        #region Fields

        private readonly string _path;
        private readonly string _command;
        private readonly CodecCommandArgs _args = new CodecCommandArgs();

        #endregion

        #region Constructors

        /// <summary>
        /// The default Constructor.
        /// </summary>
        internal CodecCommand(string path, string command)
        {
            _path = path;
            _command = command;
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events
        #endregion

        #region Delegates
        #endregion

        #region Properties

        public string Command
        {
            get { return _command; }
        }

        public string Path
        {
            get { return _path; }
        }

        public CodecCommandArgs Args
        {
            get
            {
                var argNamesToIndex = new List<string>();
                foreach (var arg in from arg in _args
                    let arg1 = arg
                    where !argNamesToIndex.Contains(arg1.Name) && _args.Count(a => a.Name == arg1.Name) > 1
                    select arg)
                {
                    argNamesToIndex.Add(arg.Name);
                }
                foreach (var name in argNamesToIndex)
                {
                    var count = 1;
                    var name1 = name;
                    foreach (var arg in _args.Where(arg => arg.Name == name1))
                    {
                        arg.Count = count++;
                    }
                }
                return _args;
            }
        }

        public string XmlString
        {
            get
            {
                var pathMatches = Regex.Matches(_path, @"\/?(\w+)");
                var pathNames = from Match match in pathMatches select match.Groups[1].Value;

                var xml = new StringWriterWithEncoding(Encoding.UTF8);
                var settings = new XmlWriterSettings {Encoding = Encoding.UTF8, Indent = true};

                using (var xw = new XmlWriter(xml, settings))
                {
                    xw.WriteStartDocument();
                    xw.WriteStartElement("Command");
                    foreach (var pathName in pathNames)
                    {
                        xw.WriteStartElement(pathName);
                    }
                    xw.WriteStartElement(Command);
                    xw.WriteAttributeString("command", "True");
                    
                    foreach (var arg in Args)
                    {
                        xw.WriteStartElement(arg.Name);
                        if (arg.Count > 0)
                            xw.WriteAttributeString("item", arg.Count.ToString());
                        xw.WriteValue(arg.Value);
                        xw.WriteEndElement();
                    }

                    xw.WriteEndDocument();
                }

                return xml.ToString().Replace("encoding=\"utf-16\"", "encoding=\"utf-8\"");
            }
        }

        #endregion

        #region Methods
        #endregion
    }

    public sealed class StringWriterWithEncoding : StringWriter
    {
        private readonly Encoding _encoding;

        public StringWriterWithEncoding(Encoding encoding)
        {
            _encoding = encoding;
        }

        public override Encoding Encoding
        {
            get { return _encoding; }
        }
    }
}