 
namespace UX.Lib2.Devices.Cisco.Audio
{
    public class OutputHdmiConnector : CodecApiElement
    {
        [CodecApiNameAttribute("Mode")]
#pragma warning disable 649 // assigned using reflection
        private string _mode;
#pragma warning restore 649

        internal OutputHdmiConnector(CodecApiElement parent, string propertyName, int indexer)
            : base(parent, propertyName, indexer)
        {

        }

        public string Mode
        {
            get { return _mode; }
        }
    }
}