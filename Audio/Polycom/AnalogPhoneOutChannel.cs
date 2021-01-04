 
using System;
using Crestron.SimplSharp;

namespace UX.Lib2.Devices.Audio.Polycom
{
    public class AnalogPhoneOutChannel : VirtualChannel
    {
        public AnalogPhoneOutChannel(Soundstructure device, string name, uint[] values)
            : base(device, name, SoundstructureVirtualChannelType.MONO, SoundstructurePhysicalChannelType.PSTN_OUT, values)
        {
            
        }

        public override void Init()
        {
            base.Init();

            Device.Get(this, SoundstructureCommandType.PHONE_CONNECT);
        }

        protected override void OnFeedbackReceived(SoundstructureCommandType commandType, string commandModifier, double value)
        {
            switch (commandType)
            {
                case SoundstructureCommandType.PHONE_CONNECT:
                    _offHook = Convert.ToBoolean(value);
#if DEBUG
                    CrestronConsole.PrintLine("{0} OffHook = {1}", Name, OffHook);
#endif
                    break;
            }

            base.OnFeedbackReceived(commandType, commandModifier, value);
        }

        bool _offHook;

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

        public void Answer()
        {
            OffHook = true;
        }

        public void Ignore()
        {
            Device.Set(this, SoundstructureCommandType.PHONE_IGNORE);
        }
    }
}