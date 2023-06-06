using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace UX.Lib2.Devices.Cisco.Video
{
    public class AvailableLayout : CodecApiElement
    {
        [CodecApiNameAttribute("LayoutName")]
#pragma warning disable 649 // assigned using reflection
        private string _layoutName;
#pragma warning restore 649

        internal AvailableLayout(CodecApiElement parent, string propertyName, int indexer)
            : base(parent, propertyName, indexer)
        {

        }

        public string LayoutName
        {
            get { return _layoutName; }
        }

        public void Set()
        {
            Codec.Send("xCommand Video Layout SetLayout LayoutName: " + _layoutName);
        }
    }
}