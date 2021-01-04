namespace UX.Lib2.Devices.Audio.Biamp.ControlBlocks
{
    public class MuteChannel : IoChannelBase
    {
        private string _name = string.Empty;

        internal MuteChannel(TesiraBlockBase controlBlock, uint channelNumber)
            : base(controlBlock, channelNumber)
        {
            controlBlock.Device.Send(controlBlock.InstanceTag, TesiraCommand.Get, TesiraAttributeCode.Mute,
                new[] { channelNumber });
        }

        public override string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public override bool SupportsLevel
        {
            get { return false; }
        }

        public override bool SupportsMute
        {
            get { return true; }
        }
    }
}