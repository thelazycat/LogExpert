﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Xsl;
using LogExpert.Classes.Log;

namespace LogExpert.Classes.xml
{
    internal class XmlBlockSplitter : LogStreamReaderBase
    {
        #region Fields

        private static readonly string[] _splitStrings = { "\r\n", "\n", "\r" };

        private static readonly char[] _newLineChar = { '\n' };

        private readonly XmlLogReader _reader;

        private readonly XmlParserContext _context;
        private readonly XmlReaderSettings _settings;

        private readonly Queue<string> _lineList = new();
        
        private string _stylesheet;
        private XslCompiledTransform _xslt;

        #endregion

        #region cTor

        public XmlBlockSplitter(XmlLogReader reader, IXmlLogConfiguration xmlLogConfig)
        {
            _reader = reader;
            _reader.StartTag = xmlLogConfig.XmlStartTag;
            _reader.EndTag = xmlLogConfig.XmlEndTag;

            Stylesheet = xmlLogConfig.Stylesheet;

            // Create the XmlNamespaceManager.
            NameTable nt = new();
            XmlNamespaceManager nsmgr = new(nt);
            if (xmlLogConfig.Namespace != null)
            {
                nsmgr.AddNamespace(xmlLogConfig.Namespace[0], xmlLogConfig.Namespace[1]);
            }
            // Create the XmlParserContext.
            _context = new XmlParserContext(nt, nsmgr, null, XmlSpace.None);
            _settings = new XmlReaderSettings();
            _settings.ConformanceLevel = ConformanceLevel.Fragment;
        }

        #endregion

        #region Properties

        public override long Position
        {
            get => _reader.Position;
            set => _reader.Position = value;
        }

        public override Encoding Encoding => _reader.Encoding;

        public override bool IsBufferComplete => _lineList.Count == 0;

        public string Stylesheet
        {
            get => _stylesheet;
            set
            {
                _stylesheet = value;
                if (_stylesheet != null)
                {
                    XmlReader stylesheetReader = XmlReader.Create(new StringReader(_stylesheet));

                    _xslt = new XslCompiledTransform();
                    _xslt.Load(stylesheetReader);
                }
                else
                {
                    _xslt = null;
                }
            }
        }

        #endregion

        #region Private Methods

        private void ParseXmlBlock(string block)
        {
            if (_stylesheet != null)
            {
                XmlReader xmlReader = XmlReader.Create(new StringReader(block), _settings, _context);

                xmlReader.Read();
                xmlReader.MoveToContent();
                //xmlReader.MoveToContent();
                StringWriter textWriter = new();

                _xslt.Transform(xmlReader, null, textWriter);
                string message = textWriter.ToString();
                SplitToLinesList(message);
            }
            else
            {
                SplitToLinesList(block);
                //this.lineList.Add(block);   // TODO: make configurable, if block has to be splitted
            }
        }

        private void SplitToLinesList(string message)
        {
            const int MAX_LEN = 3000;
            string[] lines = message.Split(_splitStrings, StringSplitOptions.None);
            foreach (string theLine in lines)
            {
                string line = theLine.Trim(_newLineChar);
                while (line.Length > MAX_LEN)
                {
                    string part = line.Substring(0, MAX_LEN);
                    line = line.Substring(MAX_LEN);
                    _lineList.Enqueue(part);
                }
                _lineList.Enqueue(line);
            }
        }

        #endregion

        #region Public Methods

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _reader.Dispose();
            }
        }

        public override int ReadChar()
        {
            return _reader.ReadChar();
        }

        public override string ReadLine()
        {
            if (_lineList.Count == 0)
            {
                string block = _reader.ReadLine();
                if (block == null)
                {
                    return null;
                }

                try
                {
                    ParseXmlBlock(block);
                }
                catch (XmlException)
                {
                    _lineList.Enqueue("[XML Parser error] " + block);
                }
            }
            return _lineList.Dequeue();
        }

        #endregion
    }
}