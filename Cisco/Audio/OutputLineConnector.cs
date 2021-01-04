 
namespace UX.Lib2.Devices.Cisco.Audio
{
    public class OutputLineConnector : CodecApiElement
    {
        [CodecApiNameAttribute("ConnectionStatus")]
#pragma warning disable 649 // assigned using reflection
        private ConnectionState _connectionStatus;
#pragma warning restore 649

        [CodecApiNameAttribute("DelayMs")]
#pragma warning disable 649 // assigned using reflection
        private int _delayMs;
#pragma warning restore 649

        internal OutputLineConnector(CodecApiElement parent, string propertyName, int indexer)
            : base(parent, propertyName, indexer)
        {

        }

        public int DelayMs
        {
            get { return _delayMs; }
        }

        ConnectionState ConnectionStatus
        {
            get { return _connectionStatus; }
        }

        public enum ConnectionState
        {
            Unknown,
            Connected,
            NotConnected
        }
    }
}