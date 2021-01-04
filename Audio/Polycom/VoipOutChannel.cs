 
using System;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharp;

namespace UX.Lib2.Devices.Audio.Polycom
{
    public class VoipOutChannel : VoipChannel
    {
        public VoipOutChannel(Soundstructure device, string name, uint[] values)
            : base(device, name, SoundstructurePhysicalChannelType.VOIP_OUT, values)
        {
            _lines = new List<VoipLine> {new VoipLine(this, 1)};
        }

        private readonly List<VoipLine> _lines;

        public VoipLineCollection Lines
        {
            get
            {
                return new VoipLineCollection(_lines);
            }
        }

        public override void Init()
        {
            base.Init();

            Device.Get(this, SoundstructureCommandType.PHONE_CONNECT);
            Device.Send(string.Format("get voip_board_info \"{0}\"", Name));
            Device.Send(string.Format("get voip_status \"{0}\"", Name));
            Device.Send(string.Format("get voip_line \"{0}\"", Name));
            Device.Send(string.Format("get voip_line_label \"{0}\" 1", Name));
            Device.Send(string.Format("get voip_line_state \"{0}\" 1", Name));
            Device.Send(string.Format("get voip_call_appearance_info \"{0}\" 1 1", Name));
            Device.Send(string.Format("get voip_call_appearance_info \"{0}\" 1 2", Name));
            Device.Send(string.Format("get voip_call_appearance_line \"{0}\" 1", Name));
            Device.Send(string.Format("get voip_call_appearance_state \"{0}\" 1", Name));
        }

        public event VoipOutOffHookChange OffHookChange;

        protected override void OnFeedbackReceived(SoundstructureCommandType commandType, string commandModifier, double value)
        {
            switch (commandType)
            {
                case SoundstructureCommandType.PHONE_CONNECT:
                    var offHook = Convert.ToBoolean(value);

                    if (_offHook != offHook)
                    {
                        _offHook = offHook;
                        if (OffHookChange != null)
                            OffHookChange(this, _offHook);
#if DEBUG
                        CrestronConsole.PrintLine("{0} OffHook = {1}", Name, OffHook);
#endif
                    }
                    break;
            }
            
            base.OnFeedbackReceived(commandType, commandModifier, value);
        }

        protected override void OnVoipInfoReceived(string command, string info)
        {
            switch (command)
            {
                case "voip_board_info":
                    info = info.Split(' ').Last();
                    MacAddress = info.Substring(4, info.Length - 5);
                    break;
                case "voip_status":
                    Status = info;
                    break;
            }
            
            base.OnVoipInfoReceived(command, info);
        }

        public string MacAddress { get; protected set; }

        public string Status { get; protected set; }

        bool _offHook;

        public void VoipSend()
        {
            Device.Set(this, SoundstructureCommandType.VOIP_SEND);
        }

        public bool OffHook
        {
            get
            {
                return _offHook;
            }
            set
            {
                Device.Set(this, SoundstructureCommandType.PHONE_CONNECT, value);
            }
        }

        public void Dial(string number)
        {
            if (!OffHook)
                OffHook = true;
            Device.Set(this, SoundstructureCommandType.PHONE_DIAL, number);
        }

        public void Reject()
        {
            Device.Set(this, SoundstructureCommandType.PHONE_REJECT);
        }

        public void Ignore()
        {
            Device.Set(this, SoundstructureCommandType.PHONE_IGNORE);
        }

        public void Answer()
        {
            OffHook = true;
        }

        public void Reboot()
        {
            Device.Set(this, SoundstructureCommandType.VOIP_REBOOT);
        }
    }

    public delegate void VoipOutOffHookChange(VoipOutChannel channel, bool offHook);
}