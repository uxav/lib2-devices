 
using System;
using UX.Lib2.Cloud.Logger;
using UX.Lib2.Models;

namespace UX.Lib2.Devices.Cisco.Audio
{
    public class Volume : IAudioLevelControl
    {
        private readonly Audio _audioElement;
        private bool _muted;
        private uint _level;

        internal Volume(Audio audioElement)
        {
            _audioElement = audioElement;
        }

        public string Name { get; private set; }

        public AudioLevelType ControlType
        {
            get
            {
                return AudioLevelType.Conference;
            }
        }

        public bool SupportsLevel
        {
            get { return true; }
        }

        public ushort Level
        {
            get { return (ushort) Tools.ScaleRange(_level, 0, 100, ushort.MinValue, ushort.MaxValue); }
            set
            {
                var level = (uint) Tools.ScaleRange(value, ushort.MinValue, ushort.MaxValue, 0, 100);
                _audioElement.Codec.Send("xCommand Audio Volume Set Level: {0}", level);
            }
        }

        public string LevelString { get; private set; }
        public bool SupportsMute { get { return true; } }

        public bool Muted
        {
            get { return _muted; }
            set
            {
                if (value)
                {
                    Mute();
                }
                else
                {
                    Unmute();
                }
            }
        }

        public void Mute()
        {
            _audioElement.Codec.Send("xCommand Audio Volume Mute");
        }

        public void Unmute()
        {
            _audioElement.Codec.Send("xCommand Audio Volume Unmute");
        }

        public void SetDefaultLevel()
        {
            _audioElement.Codec.Send("xCommand Audio Volume SetToDefault");
        }

        internal void HandleUpdatedLevel(double value)
        {
            var newValue = (uint) value;

            //Debug.WriteSuccess("Codec Volume", newValue.ToString());
            
            if (_level != newValue)
            {
                _level = newValue;
                if (LevelChange == null) return;

                try
                {
                    LevelChange(this, Level);
                }
                catch (Exception e)
                {
                    CloudLog.Exception(e, "Error calling event handler");
                }
            }
        }

        internal void HandleUpdatedMute(string value)
        {
            var muted = value == "On";
            if (_muted != muted)
            {
                _muted = muted;
                if (MuteChange != null)
                {
                    try
                    {
                        MuteChange(_muted);
                    }
                    catch (Exception e)
                    {
                        CloudLog.Exception(e, "Error calling event handler");
                    }
                }
            }
        }

        public event AudioMuteChangeEventHandler MuteChange;
        public event AudioLevelChangeEventHandler LevelChange;
    }
}