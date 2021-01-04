using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using UX.Lib2.Cloud.Logger;

namespace UX.Lib2.Devices.Audio.Biamp.ControlBlocks
{
    public sealed class SourceSelectorBlock : TesiraBlockBase
    {
        private string _label = string.Empty;
        private uint _numberOfInputs;
        private uint _numberOfOutputs;
        private uint _numberOfSources;
        private bool _stereroEnabled;
        private uint _sourceSelection;

        internal SourceSelectorBlock(Tesira device, string instanceTag) : base(device, instanceTag)
        {
            if (device.DeviceCommunicating)
            {
                ControlShouldInitialize();
            }
        }

        public override TesiraBlockType Type
        {
            get { return TesiraBlockType.SourceSelectorBlock; }
        }

        public string Label
        {
            get { return _label; }
        }

        public uint NumberOfInputs
        {
            get { return _numberOfInputs; }
        }

        public uint NumberOfOutputs
        {
            get { return _numberOfOutputs; }
        }

        public uint NumberOfSources
        {
            get { return _numberOfSources; }
        }

        public bool StereroEnabled
        {
            get { return _stereroEnabled; }
        }

        public uint SourceSelection
        {
            get { return _sourceSelection; }
            set { Device.Send(InstanceTag, TesiraCommand.Set, TesiraAttributeCode.SourceSelection, new[] {value}); }
        }

        protected override void ControlShouldInitialize()
        {
            Device.Send(InstanceTag, TesiraCommand.Get, TesiraAttributeCode.Label);
            Device.Send(InstanceTag, TesiraCommand.Get, TesiraAttributeCode.NumInputs);
            Device.Send(InstanceTag, TesiraCommand.Get, TesiraAttributeCode.NumOutputs);
            Device.Send(InstanceTag, TesiraCommand.Get, TesiraAttributeCode.NumSources);
            Device.Send(InstanceTag, TesiraCommand.Get, TesiraAttributeCode.SourceSelection);
            Device.Send(InstanceTag, TesiraCommand.Get, TesiraAttributeCode.StereoEnable);
        }

        protected override void ReceivedResponse(TesiraResponse response)
        {
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
                    case TesiraAttributeCode.Label:
                        _label = response.TryParseResponse()["value"].Value<string>();
                        break;
                    case TesiraAttributeCode.NumInputs:
                        _numberOfInputs = response.TryParseResponse()["value"].Value<uint>();
                        break;
                    case TesiraAttributeCode.NumOutputs:
                        _numberOfOutputs = response.TryParseResponse()["value"].Value<uint>();
                        break;
                    case TesiraAttributeCode.NumSources:
                        _numberOfSources = response.TryParseResponse()["value"].Value<uint>();
                        break;
                    case TesiraAttributeCode.SourceSelection:
                        _sourceSelection = response.TryParseResponse()["value"].Value<uint>();
                        break;
                    case TesiraAttributeCode.StereoEnable:
                        _stereroEnabled = response.TryParseResponse()["value"].Value<bool>();
                        OnInitialized();
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
            if (attributeCode == TesiraAttributeCode.SourceSelection)
            {
                _sourceSelection = data["value"].Value<uint>();
            }
        }

        public override void Subscribe()
        {
            Subscribe(TesiraAttributeCode.SourceSelection);
        }
    }
}