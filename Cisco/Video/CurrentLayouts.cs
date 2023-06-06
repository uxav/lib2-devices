using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace UX.Lib2.Devices.Cisco.Video
{
    public class CurrentLayouts : CodecApiElement
    {
        [CodecApiNameAttribute("AvailableLayouts")]
        private Dictionary<int, AvailableLayout> _layouts = new Dictionary<int, AvailableLayout>();

        [CodecApiNameAttribute("ActiveLayout")]
#pragma warning disable 649 // assigned using reflection
        private string _activeLayout;
#pragma warning restore 649

        internal CurrentLayouts(CodecApiElement parent, string propertyName)
            : base(parent, propertyName)
        {
        }

        public ReadOnlyDictionary<int, AvailableLayout> AvailableLayouts
        {
            get { return new ReadOnlyDictionary<int, AvailableLayout>(_layouts); }
        }

        public string ActiveLayout
        {
            get { return _activeLayout; }
        }
    }
}