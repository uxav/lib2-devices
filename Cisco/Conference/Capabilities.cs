 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace UX.Lib2.Devices.Cisco.Conference
{
    public class Capabilities : CodecApiElement
    {
        [CodecApiNameAttribute("Hold")]
#pragma warning disable 649 // assigned using reflection
        private bool _hold;
#pragma warning restore 649

        [CodecApiNameAttribute("Presentation")]
#pragma warning disable 649 // assigned using reflection
        private bool _presentation;
#pragma warning restore 649

        public Capabilities(CodecApiElement parent, string propertyName) : base(parent, propertyName)
        {

        }

        public bool Hold
        {
            get { return _hold; }
        }

        public bool Presentation
        {
            get { return _presentation; }
        }
    }
}