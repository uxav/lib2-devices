 
namespace UX.Lib2.Devices.Cisco.Audio
{
    public class Output : CodecApiElement
    {
        [CodecApiNameAttribute("Connectors")]
        private OutputConnector _connectors;

        internal Output(CodecApiElement parent, string propertyName)
            : base(parent, propertyName)
        {
            _connectors = new OutputConnector(this, "Connectors");
        }

        public OutputConnector Connectors
        {
            get { return _connectors; }
        }
    }
}