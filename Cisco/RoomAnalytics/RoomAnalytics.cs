 
namespace UX.Lib2.Devices.Cisco.RoomAnalytics
{
    public class RoomAnalytics : CodecApiElement
    {
        [CodecApiNameAttribute("PeopleCount")]
        private PeopleCount _peopleCount;

        [CodecApiNameAttribute("PeoplePresence")]
#pragma warning disable 649 // assigned using reflection
        private PeoplePresenceStatus _peoplePresence;
#pragma warning restore 649

        internal RoomAnalytics(CiscoTelePresenceCodec codec)
            : base(codec)
        {
            _peopleCount = new PeopleCount(this, "PeopleCount");
        }

        public PeopleCount PeopleCount
        {
            get { return _peopleCount; }
        }

        public PeoplePresenceStatus PeoplePresence
        {
            get { return _peoplePresence; }
        }
    }

    public enum PeoplePresenceStatus
    {
        Unknown,
        No,
        Yes
    }
}