namespace UX.Lib2.Devices.Lightware3
{
    public abstract class LightwareInputOutput
    {
        private readonly LightwareMatrix _device;
        private readonly uint _number;

        protected LightwareInputOutput(LightwareMatrix device, uint number)
        {
            _device = device;
            _number = number;
        }

        public uint Number
        {
            get { return _number; }
        }

        public abstract IOType Type { get; }

        public abstract string Name { get; internal set; }

        public LightwareMatrix Device
        {
            get { return _device; }
        }

        public override string ToString()
        {
            return string.Format(Type + "_" + Number + " \"" + Name + "\"");
        }
    }

    public enum IOType
    {
        Input,
        Output
    }
}