namespace UX.Lib2.Devices.Audio.Yamaha
{
    public class Snapshot
    {
        private readonly YamahaDesk _desk;
        private readonly string _address;
        private readonly int _index;

        internal Snapshot(YamahaDesk desk, string address, int index, string[] values)
        {
            _desk = desk;
            _address = address;
            _index = index;
            Values = values;
#if DEBUG
            Debug.WriteInfo("Added new snapshot", "{0}[{1}]: {2}", address, index, string.Join(", ", values));
#endif
        }

        internal YamahaDesk Desk
        {
            get { return _desk; }
        }

        public string Address
        {
            get { return _address; }
        }

        public int Index
        {
            get { return _index; }
        }

        public string[] Values { get; internal set; }
    }
}