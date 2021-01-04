using System;
using Newtonsoft.Json.Linq;
using UX.Lib2.Cloud.Logger;
using UX.Lib2.Models;

namespace UX.Lib2.Devices.Audio.Biamp
{
    public abstract class IoChannelBase : TesiraChannelBase, IAudioLevelControl
    {
        private double _level;
        private bool _mute;
        private string _name = string.Empty;

        protected IoChannelBase(TesiraBlockBase controlBlock, uint channelNumber)
            : base(controlBlock, channelNumber)
        {
            
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
                    case TesiraAttributeCode.MinLevel:
                        MinLevel = response.TryParseResponse()["value"].Value<double>();
                        break;
                    case TesiraAttributeCode.MaxLevel:
                        MaxLevel = response.TryParseResponse()["value"].Value<double>();
                        break;
                    case TesiraAttributeCode.Mute:
                        _mute = response.TryParseResponse()["value"].Value<bool>();
                        break;
                    case TesiraAttributeCode.Level:
                        _level = response.TryParseResponse()["value"].Value<double>();
                        LevelString = _level.ToString("F1") + " dB";
                        break;
                    case TesiraAttributeCode.Gain:
                        _level = response.TryParseResponse()["value"].Value<double>();
                        LevelString = _level.ToString("F1") + " dB";
                        break;
                }
            }
        }

        internal override void UpdateValue(TesiraAttributeCode attributeCode, JToken value)
        {
            switch (attributeCode)
            {
                case TesiraAttributeCode.Gain:
                    var gain = value.ToObject<double>();
#if DEBUG
                    Debug.WriteSuccess(Name + " new level value = " + _level.ToString("F1"));
#endif
                    if (Math.Abs(_level - gain) >= 0.1)
                    {
                        _level = gain;
                        LevelString = _level.ToString("F1") + " dB";
                        OnLevelChange(this, Level);
                    }
                    break;
                case TesiraAttributeCode.Level:
                    var level = value.ToObject<double>();
#if DEBUG
                    Debug.WriteSuccess(Name + " new level value = " + _level.ToString("F1"));
#endif
                    if (Math.Abs(_level - level) >= 0.1)
                    {
                        _level = level;
                        LevelString = _level.ToString("F1") + " dB";
                        OnLevelChange(this, Level);
                    }
                    break;
                case TesiraAttributeCode.Mute:
                    var mute = value.ToObject<bool>();
#if DEBUG
                    Debug.WriteSuccess(Name + " " + (mute ? "Muted" : "Unmuted"));
#endif
                    if (mute != _mute)
                    {
                        _mute = mute;
                        OnMuteChange(_mute);
                    }
                    break;
            }
        }

        public override string Name
        {
            get
            {
                return _name;
            }
            set { _name = value; }
        }

        public AudioLevelType ControlType { get; set; }

        public abstract bool SupportsLevel { get; }

        public virtual double DeviceLevel
        {
            get { return _level; }
            set
            {
                if (!SupportsLevel)
                    throw new NotSupportedException("Control block is " + ControlBlock.Type);
                ControlBlock.Device.Send(ControlBlock.InstanceTag, TesiraCommand.Set, TesiraAttributeCode.Level,
                    new[] { ChannelNumber }, value);
            }
        }

        public ushort Level
        {
            get { return (ushort) Tools.ScaleRange(_level, MinLevel, MaxLevel, ushort.MinValue, ushort.MaxValue); }
            set
            {
                DeviceLevel = Tools.ScaleRange(value, ushort.MinValue, ushort.MaxValue, MinLevel, MaxLevel);
            }
        }

        public string LevelString { get; private set; }

        public abstract bool SupportsMute { get; }

        public bool Muted
        {
            get
            {
                return _mute;
            }
            set
            {
                ControlBlock.Device.Send(ControlBlock.InstanceTag, TesiraCommand.Set, TesiraAttributeCode.Mute,
                    new[] {ChannelNumber}, value);
            }
        }

        public void Mute()
        {
            Muted = true;
        }

        public void Unmute()
        {
            Muted = false;
        }

        public void SetDefaultLevel()
        {
            Level = ushort.MaxValue/2;
        }

        public virtual double MinLevel { get; private set; }

        public virtual double MaxLevel { get; private set; }

        public event AudioMuteChangeEventHandler MuteChange;

        protected virtual void OnMuteChange(bool muted)
        {
            var handler = MuteChange;
            if (handler != null)
            {
                try
                {
                    handler(muted);
                }
                catch (Exception e)
                {
                    CloudLog.Exception(e);
                }
            }
        }

        public event AudioLevelChangeEventHandler LevelChange;

        protected virtual void OnLevelChange(IAudioLevelControl control, ushort level)
        {
            var handler = LevelChange;
            if (handler != null) handler(control, level);
        }
    }
}