 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace UX.Lib2.Devices.Cisco.Conference
{
    public class ConferenceLine : CodecApiElement
    {
        [CodecApiNameAttribute("Mode")]
#pragma warning disable 649 // assigned using reflection
        private ConferenceLineMode _mode;
#pragma warning restore 649

        internal ConferenceLine(CodecApiElement parent, string propertyName, int indexer)
            : base(parent, propertyName, indexer)
        {
            
        }

        public ConferenceLineMode Mode
        {
            get { return _mode; }
        }
    }

    public enum ConferenceLineMode
    {
        Private,
        Shared
    }
}