 
namespace UX.Lib2.Devices.Cisco.Audio
{
    public class Input : CodecApiElement
    {
        [CodecApiNameAttribute("Connectors")]
        private InputConnector _connectors;

        internal Input(CodecApiElement parent, string propertyName)
            : base(parent, propertyName)
        {
            _connectors = new InputConnector(this, "Connectors");
        }

        public InputConnector Connectors
        {
            get { return _connectors; }
        }
    }
}