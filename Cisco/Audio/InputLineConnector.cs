 
namespace UX.Lib2.Devices.Cisco.Audio
{
    public class InputLineConnector : CodecApiElement
    {
        [CodecApiNameAttribute("Mute")]
#pragma warning disable 649 // assigned using reflection
        private string _mute;
#pragma warning restore 649

        internal InputLineConnector(CodecApiElement parent, string propertyName, int indexer)
            : base(parent, propertyName, indexer)
        {

        }

        public bool Muted
        {
            get { return _mute == "On"; }
        }
    }
}