using System;
using System.Globalization;
using Crestron.SimplSharpPro.CrestronThread;
using Newtonsoft.Json.Linq;
using SSMono.Threading.Tasks;
using UX.Lib2.Cloud.Logger;
using UX.Lib2.Models;

namespace UX.Lib2.Devices.Displays.Sony
{
    public class SonyBraviaVolumeControl : IAudioLevelControl
    {
        #region Fields

        private readonly SonyBravia _display;
        private readonly TargetVolumeDeviceType _type;
        private int _volumeMin;
        private int _volumeMax = 100;
        private int _internalLevel;
        private bool _internalMute;
        private Thread _sendVolumeThread;
        private double _requestedLevelToSend;
        private Thread _sendMuteThread;
        private bool _requestedMuteValue;

        #endregion

        #region Constructors

        internal SonyBraviaVolumeControl(SonyBravia display, TargetVolumeDeviceType type)
        {
            _display = display;
            _type = type;
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

        internal int VolumeMin
        {
            get { return _volumeMin; }
            set { _volumeMin = value; }
        }

        internal int VolumeMax
        {
            get { return _volumeMax; }
            set { _volumeMax = value; }
        }

        public string Name
        {
            get { return string.Format("Sony Display {0} Output", TargetType); }
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
            get
            {
                return (ushort) Tools.ScaleRange(InternalLevel, VolumeMin, VolumeMax, ushort.MinValue, ushort.MaxValue);
            }
            set
            {
                if (!_display.Power)
                {
                    CloudLog.Warn("Cannot set {0}, Display Power is off!", GetType().Name);
                    return;
                }

                _requestedLevelToSend = Tools.ScaleRange(value, ushort.MinValue, ushort.MaxValue, VolumeMin, VolumeMax);

                if (_sendVolumeThread == null || _sendVolumeThread.ThreadState != Thread.eThreadStates.ThreadRunning)
                {
                    _sendVolumeThread = new Thread(specific =>
                    {
                        var response =
                            SonyBraviaHttpClient.Request(_display.DeviceAddressString, _display.Psk, "/sony/audio",
                                "setAudioVolume", "1.2", new JObject
                                {
                                    {"volume", _requestedLevelToSend.ToString("F0")},
                                    {"ui", "off"},
                                    {"target", TargetType.ToString().ToLower()}
                                }).Await();

                        if (response.Type == SonyBraviaResponseType.Success)
                        {
                            InternalLevel = (int) _requestedLevelToSend;
                        }

                        return null;
                    }, null);
                }
            }
        }

        public string LevelString
        {
            get { return InternalLevel.ToString(CultureInfo.InvariantCulture); }
        }

        public bool SupportsMute
        {
            get { return true; }
        }

        public bool Muted
        {
            get { return InternalMute; }
            set
            {
                if (!_display.Power) return;

                _requestedMuteValue = value;

                if (_sendMuteThread == null || _sendMuteThread.ThreadState != Thread.eThreadStates.ThreadRunning)
                {
                    _sendMuteThread = new Thread(specific =>
                    {
                        var response =
                            SonyBraviaHttpClient.Request(_display.DeviceAddressString, _display.Psk, "/sony/audio",
                                "setAudioMute", "1.0", new JObject
                                {
                                    {"status", _requestedMuteValue}
                                }).Await();

                        if (response.Type == SonyBraviaResponseType.Success)
                        {
                            InternalMute = value;
                        }

                        return null;
                    }, null);
                }
            }
        }

        public TargetVolumeDeviceType TargetType
        {
            get { return _type; }
        }

        internal int InternalLevel
        {
            get { return _internalLevel; }
            set
            {
                if (_internalLevel == value) return;
                _internalLevel = value;
                OnLevelChange(this, Level);
            }
        }

        internal bool InternalMute
        {
            get { return _internalMute; }
            set
            {
                if(_internalMute == value) return;
                _internalMute = value;
                OnMuteChange(_internalMute);
            }
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

        public virtual void SetDefaultLevel()
        {
            Level = ushort.MaxValue / 2;
        }

        protected virtual void OnLevelChange(IAudioLevelControl control, ushort level)
        {
            var handler = LevelChange;
            if (handler == null) return;
            try
            {
                handler(control, level);
            }
            catch (Exception e)
            {
                CloudLog.Exception(e);
            }
        }

        protected virtual void OnMuteChange(bool muted)
        {
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

        #endregion
    }

    public enum TargetVolumeDeviceType
    {
        Speaker,
        Headphone
    }
}