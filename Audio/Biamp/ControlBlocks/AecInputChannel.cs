using System;

namespace UX.Lib2.Devices.Audio.Biamp.ControlBlocks
{
    public class AecInputChannel : IoChannelBase
    {
        internal AecInputChannel(TesiraBlockBase controlBlock, uint channelNumber)
            : base(controlBlock, channelNumber)
        {
            controlBlock.Device.Send(controlBlock.InstanceTag, TesiraCommand.Get, TesiraAttributeCode.Gain,
                new[] { channelNumber });
        }

        public override bool SupportsLevel
        {
            get { return true; }
        }

        public override bool SupportsMute
        {
            get { return false; }
        }

        public override double DeviceLevel
        {
            get { return base.DeviceLevel; }
            set
            {
                if (!SupportsLevel)
                    throw new NotSupportedException("Control block is " + ControlBlock.Type);
                ControlBlock.Device.Send(ControlBlock.InstanceTag, TesiraCommand.Set, TesiraAttributeCode.Gain,
                    new[] { ChannelNumber }, value);
            }
        }

        public override double MinLevel
        {
            get { return 0; }
        }

        public override double MaxLevel
        {
            get { return 66; }
        }
    }
}