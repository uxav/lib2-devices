using System.Collections.Generic;
using Crestron.SimplSharp;

namespace UX.Lib2.Devices.Cisco.Video
{
    public class Layout : CodecApiElement
    {

        [CodecApiNameAttribute("CurrentLayouts")]
        private readonly CurrentLayouts _currentLayouts;

        internal Layout(CodecApiElement parent, string propertyName)
            : base(parent, propertyName)
        {
            _currentLayouts = new CurrentLayouts(this, "CurrentLayouts");
        }

        public CurrentLayouts CurrentLayouts
        {
            get { return _currentLayouts; }
        }
    }
}