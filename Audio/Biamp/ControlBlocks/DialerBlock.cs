 
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UX.Lib2.Cloud.Logger;

namespace UX.Lib2.Devices.Audio.Biamp.ControlBlocks
{
    public class DialerBlock : TesiraBlockBase, IEnumerable<DialerLine>
    {
        private readonly Dictionary<uint, DialerLine> _channels = new Dictionary<uint, DialerLine>();

        internal DialerBlock(Tesira device, string instanceTag)
            : base(device, instanceTag)
        {
        }

        public override TesiraBlockType Type
        {
            get { return TesiraBlockType.DialerBlock; }
        }

        public int NumberOfLines
        {
            get { return _channels.Count; }
        }

        public DialerLine this[uint line]
        {
            get { return _channels[line]; }
        }

        protected override void ControlShouldInitialize()
        {
            Device.Send(InstanceTag, TesiraCommand.Get, TesiraAttributeCode.NumChannels);
            Device.Send(InstanceTag, TesiraCommand.Get, TesiraAttributeCode.CallState);
            Device.Send(InstanceTag, TesiraCommand.Get, TesiraAttributeCode.DisplayNameLabel);
        }

        protected override void ReceivedResponse(TesiraResponse response)
        {
            if (response.OtherCommandElements.Any())
            {
                try
                {
                    var channel = uint.Parse(response.OtherCommandElements.First());
#if DEBUG
                    Debug.WriteInfo("Response for channel " + channel);
#endif
                    if (_channels.ContainsKey(channel))
                    {
                        _channels[channel].UpdateFromResponse(response);
                        return;
                    }
                }
                catch (Exception e)
                {
                    CloudLog.Exception(e, "Should be response with index");
                }
            }

            if (response.CommandType != TesiraCommand.Get || response.OtherCommandElements.Any()) return;

            var json = response.TryParseResponse();

            if (json == null)
            {
                CloudLog.Error("{0} could not parse {1} value from json message \"{2}\"", GetType().Name,
                    response.AttributeCode, response.Message);
                return;
            }

            try
            {
                switch (response.AttributeCode)
                {
                    case TesiraAttributeCode.NumChannels:
                        var numberOfChannels = json["value"].Value<uint>();
                        for (uint i = 1; i <= numberOfChannels; i++)
                        {
                            if (!_channels.ContainsKey(i))
                            {
                                CloudLog.Debug("Creating Dialer Line {0}, i");
                                _channels[i] = new DialerLine(this, i);
                            }
                        }
                        OnInitialized();
                        break;
                    case TesiraAttributeCode.CallState:
                        var callStates = json["value"]["callState"].ToObject<List<CallState>>();
                        foreach (var line in _channels)
                        {
                            var statesForLine = callStates.Where(c => c.LineId == (line.Key - 1));
                            line.Value.UpdateCallStates(statesForLine);
                        }
                        break;
                }
            }
            catch (Exception e)
            {
                CloudLog.Error("{0} could not parse {1} value from json \"{2}\", {3}", GetType().Name,
                    response.AttributeCode, json.ToString(), e.Message);
            }
        }

        protected override void ReceivedNotification(TesiraAttributeCode attributeCode, JToken data)
        {
            switch (attributeCode)
            {
                case TesiraAttributeCode.CallState:
                {
                    var callStates = data["value"]["callState"].ToObject<List<CallState>>();
                    foreach (var line in _channels)
                    {
                        var statesForLine = callStates.Where(c => c.LineId == (line.Key - 1));
                        line.Value.UpdateCallStates(statesForLine);
                    }
                }
                    break;
                case TesiraAttributeCode.DisplayNameLabel:
                {
                    
                }
                    break;
            }
        }

        public void ForceCallStateUpdate()
        {
            Device.Send(InstanceTag, TesiraCommand.Get, TesiraAttributeCode.CallState);            
        }

        public override void Subscribe()
        {
            Subscribe(TesiraAttributeCode.CallState);
        }

        public IEnumerator<DialerLine> GetEnumerator()
        {
            return _channels.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class CallState
    {
        [JsonProperty(PropertyName = "state")]
        public string State { get; set; }

        [JsonProperty(PropertyName = "lineId")]
        public uint LineId { get; set; }

        [JsonProperty(PropertyName = "callId")]
        public uint CallId { get; set; }

        [JsonProperty(PropertyName = "action")]
        public string Action { get; set; }

        [JsonProperty(PropertyName = "cid")]
        public string Cid { get; set; }

        [JsonProperty(PropertyName = "prompt")]
        public string Prompt { get; set; }
    }
}