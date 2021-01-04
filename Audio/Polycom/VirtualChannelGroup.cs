 
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharp;
using UX.Lib2.Models;

namespace UX.Lib2.Devices.Audio.Polycom
{
    public class VirtualChannelGroup : ISoundstructureItem, IEnumerable<ISoundstructureItem>
    {
        internal VirtualChannelGroup(Soundstructure device, string name, List<ISoundstructureItem> fromChannels)
        {
            Device = device;
            Device.ValueChange += Device_ValueChange;
            Name = name;
            VirtualChannels = new SoundstructureItemCollection(fromChannels);

#if DEBUG
            CrestronConsole.PrintLine("Received group \x22{0}\x22 with {1} channels",
                        Name, Count());
#endif
        }

        public Soundstructure Device { get; protected set; }
        public string Name { get; protected set; }
        private SoundstructureItemCollection VirtualChannels { get; set; }

        public ISoundstructureItem this[string channelName]
        {
            get
            {
                return VirtualChannels[channelName];
            }
        }

        public void Init()
        {
            Device.Get(this, SoundstructureCommandType.FADER);
            Device.Get(this, SoundstructureCommandType.MUTE);
        }

        public bool Initialised
        {
            get
            {
                return _faderValueInit && _muteValueInit;
            }
        }

        public int Count()
        {
            return VirtualChannels.Count();
        }

        public bool ContainsMics
        {
            get { return VirtualChannels.OfType<VirtualChannel>().Count(c => c.IsMic) > 0; }
        }

        public bool SupportsFader
        {
            get
            {
                return true;
            }
        }

        private bool _faderValueInit;
        private double _fader;
        public double Fader
        {
            get
            {
                return _fader;
            }
            set
            {
                if (Device.Set(this, SoundstructureCommandType.FADER, value))
                    _fader = value;
            }
        }

        public double FaderMin { get; protected set; }
        public double FaderMax { get; protected set; }

        bool _muteValueInit;
        bool _mute;

        void Device_ValueChange(ISoundstructureItem item, SoundstructureValueChangeEventArgs args)
        {
            if (item != this) return;
            switch (args.CommandType)
            {
                case SoundstructureCommandType.MUTE:
                    _mute = Convert.ToBoolean(args.Value);
                    _muteValueInit = true;
                    OnMuteChange(Muted);
                    break;
                case SoundstructureCommandType.FADER:
                    switch (args.CommandModifier)
                    {
                        case "min":
                            FaderMin = args.Value;
                            break;
                        case "max":
                            FaderMax = args.Value;
                            break;
                        default:
                            _fader = args.Value;
                            _faderValueInit = true;
                            break;
                    }
                    OnLevelChange(this, Level);
                    break;
            }
        }

        #region IEnumerable<ISoundstructureItem> Members

        public IEnumerator<ISoundstructureItem> GetEnumerator()
        {
            return VirtualChannels.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region IVolumeDevice Members

        public ushort VolumeLevel
        {
            get
            {
                return (ushort)Soundstructure.ScaleRange(Fader, FaderMin, FaderMax, ushort.MinValue, ushort.MaxValue);
            }
            set
            {
                Fader = Soundstructure.ScaleRange(value, ushort.MinValue, ushort.MaxValue, FaderMin, FaderMax);
            }
        }

        public bool VolumeMute
        {
            get
            {
                return _mute;
            }
            set
            {
                if (Device.Set(this, SoundstructureCommandType.MUTE, value))
                    _mute = value;
            }
        }

        public bool SupportsVolumeMute
        {
            get
            {
                return true;
            }
        }

        public bool SupportsVolumeLevel
        {
            get
            {
                return SupportsFader;
            }
        }

        #endregion

        #region IAudioLevelControl Members


        public AudioLevelType ControlType { get; set; }

        public bool SupportsLevel
        {
            get { return SupportsFader; }
        }

        public ushort Level
        {
            get
            {
                return (ushort)Soundstructure.ScaleRange(Fader, FaderMin, FaderMax, ushort.MinValue, ushort.MaxValue);
            }
            set
            {
                Fader = Soundstructure.ScaleRange(value, ushort.MinValue, ushort.MaxValue, FaderMin, FaderMax);
            }
        }

        public string LevelString
        {
            get { throw new NotImplementedException(); }
        }

        public bool SupportsMute
        {
            get { return true; }
        }

        public bool Muted
        {
            get
            {
                return _mute;
            }
            set
            {
                if (Device.Set(this, SoundstructureCommandType.MUTE, value))
                    _mute = value;
            }
        }

        public void Mute()
        {
            if (Device.Set(this, SoundstructureCommandType.MUTE, true))
                _mute = true;
        }

        public void Unmute()
        {
            if (Device.Set(this, SoundstructureCommandType.MUTE, false))
                _mute = false;
        }

        public virtual void SetDefaultLevel()
        {
            Level = ushort.MaxValue / 2;
        }

        public event AudioMuteChangeEventHandler MuteChange;

        protected virtual void OnMuteChange(bool muted)
        {
            var handler = MuteChange;
            if (handler != null) handler(muted);
        }

        public event AudioLevelChangeEventHandler LevelChange;

        protected virtual void OnLevelChange(IAudioLevelControl control, ushort level)
        {
            var handler = LevelChange;
            if (handler != null) handler(control, level);
        }

        #endregion
    }
}