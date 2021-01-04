 
using System.Collections.Generic;
using Crestron.SimplSharp;

namespace UX.Lib2.Devices.Cisco.Audio
{
    public class InputConnector : CodecApiElement
    {
        [CodecApiNameAttribute("Microphone")]
        private Dictionary<int, MicrophoneConnector> _micConnector = new Dictionary<int, MicrophoneConnector>();

        [CodecApiNameAttribute("HDMI")]
        private Dictionary<int, InputHdmiConnector> _hdmiConnectors = new Dictionary<int, InputHdmiConnector>();

        [CodecApiNameAttribute("Line")]
        private Dictionary<int, InputLineConnector> _lineConnectors = new Dictionary<int, InputLineConnector>();

        internal InputConnector(CodecApiElement parent, string propertyName)
            : base(parent, propertyName)
        {

        }

        public ReadOnlyDictionary<int, MicrophoneConnector> Microphones
        {
            get { return new ReadOnlyDictionary<int, MicrophoneConnector>(_micConnector); }
        }

        public Dictionary<int, InputHdmiConnector> HDMI
        {
            get { return _hdmiConnectors; }
        }

        public Dictionary<int, InputLineConnector> Line
        {
            get { return _lineConnectors; }
        }
    }
}