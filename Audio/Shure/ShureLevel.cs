using System;
using UX.Lib2.Cloud.Logger;
using UX.Lib2.Models;

namespace UX.Lib2.Devices.Audio.Shure
{
    public class ShureLevel : IAudioLevelControl
    {
        private readonly ShureP300 _device;
        private readonly uint _channel;
        private bool _muted;
        private ushort _level;

        internal ShureLevel(ShureP300 device, uint channel)
        {
            _device = device;
            _channel = channel;
        }

        public string Name { get; private set; }

        public AudioLevelType ControlType
        {
            get { return AudioLevelType.Microphone; }
        }

        public uint Channel
        {
            get { return _channel; }
        }

        public bool SupportsLevel
        {
            get { return true; }
        }

        public ushort Level
        {
            get { return (ushort) Tools.ScaleRange(_level, 0, 1400, ushort.MinValue, ushort.MaxValue); }
            set
            {
                _device.Send(string.Format("< SET {0:00} AUDIO_GAIN_HI_RES {1:0000} >", _channel,
                    Tools.ScaleRange(value, ushort.MinValue, ushort.MaxValue, 0, 1400)));
            }
        }

        public string LevelString
        {
            get
            {
                var scaledLevel = Tools.ScaleRange(_level, 0, 1400, -110, 30);
                return string.Format("{0:0.0} dB", scaledLevel);
            }
        }

        public bool SupportsMute
        {
            get { return true; }
        }

        public bool Muted
        {
            get { return _muted; }
            set { _device.Send(string.Format("< SET {0:D2} AUDIO_MUTE {1} >", _channel, value ? "ON" : "OFF")); }
        }

        public void Mute()
        {
            Muted = true;
        }

        public void Unmute()
        {
            Muted = false;
        }

        public void Poll()
        {
            _device.Send(string.Format("< GET {0:D2} ALL >", _channel));
        }

        public void SetDefaultLevel()
        {
            Level = ushort.MaxValue/2;
        }

        public event AudioMuteChangeEventHandler MuteChange;
        public event AudioLevelChangeEventHandler LevelChange;

        internal void Update(string type, string valueString)
        {
            switch (type)
            {
                case "AUDIO_GAIN_HI_RES":
                    _level = ushort.Parse(valueString);
                    try
                    {
                        if(LevelChange == null) return;

                        LevelChange(this, (ushort) Tools.ScaleRange(_level, 0, 1400, ushort.MinValue, ushort.MaxValue));
                    }
                    catch (Exception e)
                    {
                        CloudLog.Exception(e);
                    }
                    break;
                case "AUDIO_MUTE":
                    _muted = valueString == "ON";
                    try
                    {
                        if (MuteChange == null) return;

                        MuteChange(_muted);
                    }
                    catch (Exception e)
                    {
                        CloudLog.Exception(e);
                    }
                    break;
            }
        }
    }
}