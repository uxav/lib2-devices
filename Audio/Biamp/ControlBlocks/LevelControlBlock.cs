 
using Newtonsoft.Json.Linq;

namespace UX.Lib2.Devices.Audio.Biamp.ControlBlocks
{
    public sealed class LevelControlBlock : MultiChannelBlockBase<LevelChannel>
    {
        internal LevelControlBlock(Tesira device, string instanceId) : base(device, instanceId)
        {
            if (device.DeviceCommunicating)
            {
                ControlShouldInitialize();
            }
        }

        public override TesiraBlockType Type
        {
            get { return TesiraBlockType.LevelControlBlock; }
        }

        public bool Ganged { get; private set; }

        protected override void ControlShouldInitialize()
        {
            base.ControlShouldInitialize();
            Device.Send(InstanceTag, TesiraCommand.Get, TesiraAttributeCode.Ganged);
        }

        protected override void UpdateAttribute(TesiraAttributeCode code, JToken data)
        {
            switch (code)
            {
                case TesiraAttributeCode.Ganged:
                    Ganged = data["value"].Value<bool>();
                    break;
            }
        }

        public override void Subscribe()
        {
            Subscribe(TesiraAttributeCode.Levels);
            Subscribe(TesiraAttributeCode.Mutes);
        }

        protected override LevelChannel CreateChannel(uint index)
        {
            return new LevelChannel(this, index);
        }
    }
}