using System;
using System.Globalization;
using UX.Lib2.Cloud.Logger;
using UX.Lib2.Models;

namespace UX.Lib2.Devices.Displays.Smart
{
    public class SmartBoardVolume : IAudioLevelControl
    {
        private readonly SmartBoard _smartBoard;
        private ushort _level;
        private bool _muted;

        internal SmartBoardVolume(SmartBoard smartBoard)
        {
            _smartBoard = smartBoard;
        }

        public string Name
        {
            get { return _smartBoard.Name + " Volume"; }
        }

        public AudioLevelType ControlType
        {
            get { return AudioLevelType.Source; }
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
                _smartBoard.Send(string.Format("set volume={0}",
                    (ushort) Tools.ScaleRange(value, ushort.MinValue, ushort.MaxValue, 0, 100)));
            }
        }

        public string LevelString
        {
            get { return _level.ToString(CultureInfo.InvariantCulture); }
        }

        public bool SupportsMute
        {
            get { return true; }
        }

        public bool Muted
        {
            get { return _muted; }
            set { _smartBoard.Send("set mute=" + (value ? "on" : "off")); }
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

        public event AudioMuteChangeEventHandler MuteChange;

        internal void OnMuteChange(bool muted)
        {
            _muted = muted;
            var handler = MuteChange;
            if (handler == null) return;
            try
            {
                handler(muted);
            }
            catch (Exception e)
            {
                CloudLog.Exception(e);
            }
        }

        public event AudioLevelChangeEventHandler LevelChange;

        internal void OnLevelChange(ushort level)
        {
            _level = level;
            var handler = LevelChange;
            if (handler == null) return;
            try
            {
                handler(this, Level);
            }
            catch (Exception e)
            {
                CloudLog.Exception(e);
            }
        }
    }
}