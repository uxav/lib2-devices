using System.Globalization;
using UX.Lib2.DeviceSupport;
using UX.Lib2.Models;

namespace UX.Lib2.Devices.Displays.LG
{
    public class LgDisplayVolumeLevel : IAudioLevelControl
    {
        #region Fields
        
        private readonly LgDisplay _display;
        private bool _muted;
        private ushort _level;
        private ushort _requestedLevel = ushort.MaxValue/2;

        #endregion

        #region Constructors

        /// <summary>
        /// The default Constructor.
        /// </summary>
        internal LgDisplayVolumeLevel(LgDisplay display)
        {
            _display = display;
            _display.PowerStatusChange += (device, args) =>
            {
                if (args.NewPowerStatus == DevicePowerStatus.PowerOn)
                {
                    Level = _requestedLevel;
                }
            };
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events

        public event AudioMuteChangeEventHandler MuteChange;
        public event AudioLevelChangeEventHandler LevelChange;

        #endregion

        #region Delegates
        #endregion

        #region Properties

        public string Name { get; internal set; }
        public AudioLevelType ControlType { get; internal set; }

        public bool SupportsLevel
        {
            get { return true; }
        }

        public ushort Level
        {
            get { return _level; }
            set
            {
                _requestedLevel = value;
                _display.Send('k', 'f', (byte) Tools.ScaleRange(value, ushort.MinValue, ushort.MaxValue, 0x00, 0x64));
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
            set { _display.Send('k', 'e', (byte) (value ? 0x00 : 0x01)); }
        }

        public virtual void SetDefaultLevel()
        {
            Level = ushort.MaxValue / 2;
        }

        #endregion

        #region Methods

        public void Mute()
        {
            Muted = true;
        }

        public void Unmute()
        {
            Muted = false;
        }

        internal void UpdateFromFeedback(bool volumeMute)
        {
            if (_muted == volumeMute) return;
                _muted = volumeMute;
            if (MuteChange != null)
                MuteChange(_muted);
        }

        internal void UpdateFromFeedback(ushort value)
        {
            if (_level == value) return;
            _level = value;
            if (LevelChange != null)
                LevelChange(this, _level);
        }

        #endregion
    }
}