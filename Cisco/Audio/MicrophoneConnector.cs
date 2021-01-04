 
namespace UX.Lib2.Devices.Cisco.Audio
{
    public class MicrophoneConnector : CodecApiElement
    {
        [CodecApiNameAttribute("ConnectionStatus")]
#pragma warning disable 649 // assigned using reflection
        private ConnectionState _connectionStatus;
#pragma warning restore 649

        [CodecApiNameAttribute("EcReferenceDelay")]
#pragma warning disable 649 // assigned using reflection
        private int _ecReferenceDelay;
#pragma warning restore 649

        [CodecApiNameAttribute("Mute")]
#pragma warning disable 649 // assigned using reflection
        private string _mute;
#pragma warning restore 649

        internal MicrophoneConnector(CodecApiElement parent, string propertyName, int indexer)
            : base(parent, propertyName, indexer)
        {

        }

        ConnectionState ConnectionStatus
        {
            get { return _connectionStatus; }
        }

        public int EcReferenceDelay
        {
            get { return _ecReferenceDelay; }
        }

        public bool Muted
        {
            get { return _mute == "On"; }
        }

        public enum ConnectionState
        {
            Unknown,
            Connected,
            NotConnected
        }
    }
}