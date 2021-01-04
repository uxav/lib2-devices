namespace UX.Lib2.Devices.Audio.Yamaha
{
    public class Parameter
    {
        private readonly string _address;
        private readonly int _xIndex;
        private readonly int _yIndex;

        internal Parameter(string address, int xIndex, int yIndex, string[] values)
        {
            _address = address;
            _xIndex = xIndex;
            _yIndex = yIndex;
#if DEBUG
            Debug.WriteInfo("Added new parameter", "{0}[{1},{2}]: {2}", address, xIndex, yIndex, string.Join(", ", values));
#endif
        }

        public int XIndex
        {
            get { return _xIndex; }
        }

        public int YIndex
        {
            get { return _yIndex; }
        }
    }
}