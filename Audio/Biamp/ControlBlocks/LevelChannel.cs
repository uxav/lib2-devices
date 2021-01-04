 
using Newtonsoft.Json.Linq;

namespace UX.Lib2.Devices.Audio.Biamp.ControlBlocks
{
    public class LevelChannel : IoChannelBase
    {
        private string _label;
        private string _name;

        internal LevelChannel(TesiraBlockBase controlBlock, uint channelNumber)
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
            _label = string.Format("{0} Level {1}", controlBlock.InstanceTag, channelNumber);
            controlBlock.Device.Send(controlBlock.InstanceTag, TesiraCommand.Get, TesiraAttributeCode.Label,
                new[] { channelNumber });
        }

        internal override void UpdateFromResponse(TesiraResponse response)
        {
            base.UpdateFromResponse(response);

            if (response.CommandType == TesiraCommand.Get)
            {
                switch (response.AttributeCode)
                {
                    
                    case TesiraAttributeCode.Label:
                        _label = response.TryParseResponse()["value"].Value<string>();
                        break;
                }
            }
        }

        public override string Name
        {
            get
            {
                return string.IsNullOrEmpty(_name) ? Label : _name;
            }
            set { _name = value; }
        }

        public string Label
        {
            get { return _label; }
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