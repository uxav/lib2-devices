namespace UX.Lib2.Devices.Audio.Biamp.ControlBlocks
{
    public class InputChannel : IoChannelBase
    {
        internal InputChannel(TesiraBlockBase controlBlock, uint channelNumber)
            : base(controlBlock, channelNumber)
        {
            controlBlock.Device.Send(controlBlock.InstanceTag, TesiraCommand.Get, TesiraAttributeCode.Mute,
                new[] { channelNumber });
            controlBlock.Device.Send(controlBlock.InstanceTag, TesiraCommand.Get, TesiraAttributeCode.MinLevel,
                new[] { channelNumber });
            controlBlock.Device.Send(controlBlock.InstanceTag, TesiraCommand.Get, TesiraAttributeCode.MaxLevel,
                new[] { channelNumber });
            controlBlock.Device.Send(controlBlock.InstanceTag, TesiraCommand.Get, TesiraAttributeCode.Level,
                new[] { channelNumber });
        }

        public override bool SupportsLevel
        {
            get { return true; }
        }

        public override bool SupportsMute
        {
            get { return true; }
        }
    }
}