namespace UX.Lib2.Devices.Polycom
{
    public class MeetingDiallingNumber
    {
        private readonly string _number;
        private readonly DiallingNumberType _type;

        public MeetingDiallingNumber(string number, DiallingNumberType type)
        {
            _number = number;
            _type = type;
        }

        public string Number
        {
            get { return _number; }
        }

        public DiallingNumberType Type
        {
            get { return _type; }
        }
    }

    public enum DiallingNumberType
    {
        Video,
        Audio
    }
}