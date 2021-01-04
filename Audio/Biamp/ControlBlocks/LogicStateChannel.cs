using Newtonsoft.Json.Linq;

namespace UX.Lib2.Devices.Audio.Biamp.ControlBlocks
{
    public class LogicStateChannel : TesiraChannelBase
    {
        private bool _state;
        private string _label = string.Empty;

        internal LogicStateChannel(TesiraBlockBase controlBlock, uint channelNumber) : base(controlBlock, channelNumber)
        {
            controlBlock.Device.Send(controlBlock.InstanceTag, TesiraCommand.Get, TesiraAttributeCode.Label,
                new[] { channelNumber });
            controlBlock.Device.Send(controlBlock.InstanceTag, TesiraCommand.Get, TesiraAttributeCode.State,
                new[] { channelNumber });
        }

        public bool State
        {
            get
            {
                return _state;
            }
            set
            {
                _state = value;
                ControlBlock.Device.Send(ControlBlock.InstanceTag, TesiraCommand.Set, TesiraAttributeCode.State,
                    new[] { ChannelNumber }, _state);
            }
        }

        public string Label
        {
            get { return _label; }
        }

        internal override void UpdateFromResponse(TesiraResponse response)
        {
#if DEBUG
            Debug.WriteSuccess(ControlBlock.InstanceTag + " Channel " + ChannelNumber,
                "Received {0} response for {1}: {2}", response.CommandType, response.AttributeCode,
                response.TryParseResponse().ToString());
#endif
            if (response.CommandType == TesiraCommand.Get)
            {
                switch (response.AttributeCode)
                {
                    case TesiraAttributeCode.Label:
                        _label = response.TryParseResponse()["value"].Value<string>();
                        break;
                    case TesiraAttributeCode.State:
                        _state = response.TryParseResponse()["value"].Value<bool>();
                        break;
                }
            }
        }

        internal override void UpdateValue(TesiraAttributeCode attributeCode, JToken value)
        {
            
        }
    }
}