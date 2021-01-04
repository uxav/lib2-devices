using Newtonsoft.Json.Linq;

namespace UX.Lib2.Devices.Audio.Biamp.ControlBlocks
{
    public class InputBlock : MultiChannelBlockBase<InputChannel>
    {
        internal InputBlock(Tesira device, string instanceTag)
            : base(device, instanceTag)
        {
        }

        public override TesiraBlockType Type
        {
            get { return TesiraBlockType.InputBlock; }
        }

        protected override void ControlShouldInitialize()
        {
            Device.Send(InstanceTag, TesiraCommand.Get, TesiraAttributeCode.NumChannels);
        }

        protected override void UpdateAttribute(TesiraAttributeCode code, JToken data)
        {

        }

        public override void Subscribe()
        {
            Subscribe(TesiraAttributeCode.Levels);
            Subscribe(TesiraAttributeCode.Mutes);
        }

        protected override InputChannel CreateChannel(uint index)
        {
            return new InputChannel(this, index);
        }
    }
}