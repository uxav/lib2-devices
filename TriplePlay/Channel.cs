using Newtonsoft.Json.Linq;

namespace UX.Lib2.Devices.TriplePlay
{
    public class Channel
    {
        private readonly uint _id;
        private readonly uint _number;
        private readonly string _name;
        private readonly string _iconPath;
        private readonly bool _isWatchable;

        internal Channel(JToken channelInfo)
        {
            _id = channelInfo["id"].Value<uint>();
            _number = channelInfo["channelNumber"].Value<uint>();
            _name = channelInfo["name"].Value<string>();
            _iconPath = channelInfo["iconPath"].Value<string>();
            _isWatchable = channelInfo["isWatchable"].Value<bool>();
        }

        public uint Id
        {
            get { return _id; }
        }

        public uint Number
        {
            get { return _number; }
        }

        public string Name
        {
            get { return _name; }
        }

        public string IconPath
        {
            get { return _iconPath; }
        }

        public bool IsWatchable
        {
            get { return _isWatchable; }
        }
    }
}