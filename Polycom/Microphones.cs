using System;
using System.Text.RegularExpressions;
using UX.Lib2.Cloud.Logger;
using UX.Lib2.Models;

namespace UX.Lib2.Devices.Polycom
{
    public class Microphones : IAudioLevelControl
    {
        public Microphones(PolycomGroupSeriesCodec codec)
        {
            Codec = codec;
            codec.ReceivedFeedback += (seriesCodec, data) => OnReceive(data);
        }

        PolycomGroupSeriesCodec Codec { get; set; }

        bool _muted;

        void OnReceive(string receivedString)
        {
            var match = Regex.Match(receivedString, @"^mute near (off|on)");
            if(!match.Success) return;
            var muted = match.Groups[1].Value == "on";

            if(_muted == muted) return;

            _muted = muted;

            if (MuteChange == null) return;
            try
            {
                MuteChange(_muted);
            }
            catch (Exception e)
            {
                CloudLog.Exception(e);
            }
        }

        public string Name
        {
            get { return "Privacy Mute"; }
        }

        public AudioLevelType ControlType
        {
            get { return AudioLevelType.Microphone; }
        }

        public bool SupportsLevel
        {
            get { return false; }
        }
        public ushort Level { get; set; }
        public string LevelString { get { return string.Empty; } }

        public bool SupportsMute
        {
            get { return true; }
        }

        public bool Muted
        {
            get { return _muted; }
            set
            {
                Codec.Send(string.Format("mute near {0}", (value) ? "on" : "off"));
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

        public virtual void SetDefaultLevel()
        {
            throw new NotSupportedException("No level control");
        }

        public event AudioMuteChangeEventHandler MuteChange;
        public event AudioLevelChangeEventHandler LevelChange;
    }
}