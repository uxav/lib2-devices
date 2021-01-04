 
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Crestron.SimplSharp;
using UX.Lib2.Models;

namespace UX.Lib2.Devices.Audio.Polycom
{
    public class VirtualChannel : ISoundstructureItem
    {
        internal VirtualChannel(Soundstructure device, string name, SoundstructureVirtualChannelType vcType, SoundstructurePhysicalChannelType pcType, uint[] values)
        {
            Device = device;
            Device.ValueChange += Device_ValueChange;
            Name = name;
            VirtualChannelType = vcType;
            PhysicalChannelType = pcType;
            _physicalChannelIndex = new List<uint>(values);

#if DEBUG
            CrestronConsole.Print("Received {0} with name: {1}, Virtual Type: {2}, Physical Type: {3} Values:",
                        GetType().ToString().Split('.').Last(), Name, VirtualChannelType.ToString(), PhysicalChannelType.ToString());

            foreach (var value in PhysicalChannelIndex)
            {
                CrestronConsole.Print(" {0}", value);
            }

            CrestronConsole.PrintLine("");
#endif
        }

        public Soundstructure Device { get; protected set; }
        public string Name { get; protected set; }

        public SoundstructureVirtualChannelType VirtualChannelType { get; protected set; }
        public SoundstructurePhysicalChannelType PhysicalChannelType { get; protected set; }

        readonly List<uint> _physicalChannelIndex;
        public ReadOnlyCollection<uint> PhysicalChannelIndex
        {
            get
            {
                return _physicalChannelIndex.AsReadOnly();
            }
        }

        public virtual void Init()
        {
            if (SupportsFader)
                Device.Get(this, SoundstructureCommandType.FADER);
            if (SupportsMute)
                Device.Get(this, SoundstructureCommandType.MUTE);
        }

        public bool Initialised
        {
            get
            {
                if (SupportsFader && SupportsMute)
                {
                    if (_faderValueInit && _muteValueInit)
                        return true;
                }
                else if (SupportsMute && _muteValueInit)
                    return true;
                else if (SupportsFader && _faderValueInit)
                    return true;
                else if (!SupportsFader && !SupportsMute)
                    return true;

                return false;
            }
        }

        public bool SupportsFader
        {
            get
            {
                switch (PhysicalChannelType)
                {
                    case SoundstructurePhysicalChannelType.SR_MIC_IN:
                    case SoundstructurePhysicalChannelType.CR_MIC_IN:
                    case SoundstructurePhysicalChannelType.CR_LINE_OUT:
                    case SoundstructurePhysicalChannelType.SR_LINE_OUT:
                    case SoundstructurePhysicalChannelType.PSTN_IN:
                    case SoundstructurePhysicalChannelType.PSTN_OUT:
                    case SoundstructurePhysicalChannelType.VOIP_IN:
                    case SoundstructurePhysicalChannelType.VOIP_OUT:
                    case SoundstructurePhysicalChannelType.CLINK_OUT:
                    case SoundstructurePhysicalChannelType.CLINK_IN:
                    case SoundstructurePhysicalChannelType.SUBMIX:
                        return true;
                }
                return false;
            }
        }

        bool _faderValueInit;
        double _fader;
        public double Fader
        {
            get
            {
                return _fader;
            }
            set
            {
                if (!Device.Set(this, SoundstructureCommandType.FADER, value)) return;
                if (value <= FaderMax && value >= FaderMin)
                    _fader = value;
            }
        }

        public double FaderMin { get; protected set; }
        public double FaderMax { get; protected set; }

        bool _muteValueInit;
        bool _mute;

        public bool IsMic
        {
            get
            {
                switch (PhysicalChannelType)
                {
                    case SoundstructurePhysicalChannelType.SR_MIC_IN:
                    case SoundstructurePhysicalChannelType.CR_MIC_IN:
                        return true;
                }
                return false;
            }
        }

        public bool IsVoip
        {
            get
            {
                switch (PhysicalChannelType)
                {
                    case SoundstructurePhysicalChannelType.VOIP_IN:
                    case SoundstructurePhysicalChannelType.VOIP_OUT:
                        return true;
                }
                return false;
            }
        }

        public bool IsPSTN
        {
            get
            {
                switch (PhysicalChannelType)
                {
                    case SoundstructurePhysicalChannelType.PSTN_IN:
                    case SoundstructurePhysicalChannelType.PSTN_OUT:
                        return true;
                }
                return false;
            }
        }

        protected virtual void OnFeedbackReceived(SoundstructureCommandType commandType, string commandModifier, double value)
        {
            switch (commandType)
            {
                case SoundstructureCommandType.MUTE:
                    _mute = Convert.ToBoolean(value);
                    _muteValueInit = true;
                    OnMuteChange(Muted);
                    break;
                case SoundstructureCommandType.FADER:
                    switch (commandModifier)
                    {
                        case "min":
                            FaderMin = value;
                            break;
                        case "max":
                            FaderMax = value;
                            break;
                        default:
                            _fader = value;
                            _faderValueInit = true;
                            break;
                    }
                    OnLevelChange(this, Level);
                    break;
            }
        }

        void Device_ValueChange(ISoundstructureItem item, SoundstructureValueChangeEventArgs args)
        {
            try
            {
                if (item == this)
                {
                    OnFeedbackReceived(args.CommandType, args.CommandModifier, args.Value);
                }
            }
            catch (Exception e)
            {
                ErrorLog.Error("{0} Error in Device_ValueChange(): {1}", GetType().ToString().Split('.').Last(), e.Message);
            }
        }

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
            get
            {
                switch (PhysicalChannelType)
                {
                    case SoundstructurePhysicalChannelType.SR_MIC_IN:
                    case SoundstructurePhysicalChannelType.CR_MIC_IN:
                    case SoundstructurePhysicalChannelType.CR_LINE_OUT:
                    case SoundstructurePhysicalChannelType.SR_LINE_OUT:
                    case SoundstructurePhysicalChannelType.PSTN_IN:
                    case SoundstructurePhysicalChannelType.PSTN_OUT:
                    case SoundstructurePhysicalChannelType.VOIP_IN:
                    case SoundstructurePhysicalChannelType.VOIP_OUT:
                    case SoundstructurePhysicalChannelType.CLINK_OUT:
                    case SoundstructurePhysicalChannelType.CLINK_IN:
                    case SoundstructurePhysicalChannelType.SUBMIX:
                    case SoundstructurePhysicalChannelType.SIG_GEN:
                        return true;
                }
                return false;
            }
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