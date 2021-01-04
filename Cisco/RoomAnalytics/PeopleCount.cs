 
namespace UX.Lib2.Devices.Cisco.RoomAnalytics
{
    public class PeopleCount : CodecApiElement
    {
        [CodecApiNameAttribute("Current")]
#pragma warning disable 649 // assigned using reflection
        private int _current;
#pragma warning restore 649

        internal PeopleCount(CodecApiElement parent, string propertyName)
            : base(parent, propertyName)
        {

        }

        public int Current
        {
            get { return _current; }
        }
    }
}