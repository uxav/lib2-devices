 
namespace UX.Lib2.Devices.Cisco.Audio
{
    public class InputHdmiConnector : CodecApiElement
    {
        [CodecApiNameAttribute("EcReferenceDelay")]
#pragma warning disable 649 // assigned using reflection
        private int _ecReferenceDelay;
#pragma warning restore 649

        [CodecApiNameAttribute("Mute")]
#pragma warning disable 649 // assigned using reflection
        private string _mute;
#pragma warning restore 649

        internal InputHdmiConnector(CodecApiElement parent, string propertyName, int indexer)
            : base(parent, propertyName, indexer)
        {

        }

        public int EcReferenceDelay
        {
            get { return _ecReferenceDelay; }
        }

        public bool Muted
        {
            get { return _mute == "On"; }
        }
    }
}