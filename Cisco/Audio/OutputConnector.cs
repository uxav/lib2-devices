 
using System.Collections.Generic;

namespace UX.Lib2.Devices.Cisco.Audio
{
    public class OutputConnector : CodecApiElement
    {
        [CodecApiNameAttribute("InternalSpeaker")]
        private Dictionary<int, InternalSpeaker> _internalSpeakers = new Dictionary<int, InternalSpeaker>();

        [CodecApiNameAttribute("HDMI")]
        private Dictionary<int, OutputHdmiConnector> _hdmiConnectors = new Dictionary<int, OutputHdmiConnector>();

        [CodecApiNameAttribute("Line")]
        private Dictionary<int, OutputLineConnector> _lineConnectors = new Dictionary<int, OutputLineConnector>();

        internal OutputConnector(CodecApiElement parent, string propertyName)
            : base(parent, propertyName)
        {

        }

        public Dictionary<int, OutputHdmiConnector> HDMI
        {
            get { return _hdmiConnectors; }
        }

        public Dictionary<int, OutputLineConnector> Line
        {
            get { return _lineConnectors; }
        }

        public Dictionary<int, InternalSpeaker> InternalSpeaker
        {
            get { return _internalSpeakers; }
        }
    }
}