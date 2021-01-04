 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace UX.Lib2.Devices.Cisco.Video
{
    public class OutputMonitor : CodecApiElement
    {
        [CodecApiNameAttribute("SerialNumber")]
#pragma warning disable 649 // assigned using reflection
        private string _serialNumber;
#pragma warning restore 649

        [CodecApiNameAttribute("ModelName")]
#pragma warning disable 649 // assigned using reflection
        private string _modelName;
#pragma warning restore 649

        [CodecApiNameAttribute("FirmwareVersion")]
#pragma warning disable 649 // assigned using reflection
        private string _firmwareVersion;
#pragma warning restore 649

        internal OutputMonitor(CodecApiElement parent, string propertyName, int indexer)
            : base(parent, propertyName, indexer)
        {
            
        }

        public string SerialNumber
        {
            get { return _serialNumber; }
        }

        public string ModelName
        {
            get { return _modelName; }
        }

        public string FirmwareVersion
        {
            get { return _firmwareVersion; }
        }
    }
}