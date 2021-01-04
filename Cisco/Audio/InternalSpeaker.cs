 
namespace UX.Lib2.Devices.Cisco.Audio
{
    public class InternalSpeaker : CodecApiElement
    {
        [CodecApiNameAttribute("Mode")]
#pragma warning disable 649 // assigned using reflection
        private string _mode;
#pragma warning restore 649

        [CodecApiNameAttribute("DelayMs")]
#pragma warning disable 649 // assigned using reflection
        private int _delayMs;
#pragma warning restore 649

        internal InternalSpeaker(CodecApiElement parent, string propertyName, int indexer)
            : base(parent, propertyName, indexer)
        {

        }

        public bool Enabled
        {
            get { return _mode == "On"; }
        }

        public int DelayMs
        {
            get { return _delayMs; }
        }
    }
}