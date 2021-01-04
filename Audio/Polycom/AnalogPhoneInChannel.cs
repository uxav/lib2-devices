 
using System;
using Crestron.SimplSharp;

namespace UX.Lib2.Devices.Audio.Polycom
{
    public class AnalogPhoneInChannel : VirtualChannel
    {
        internal AnalogPhoneInChannel(Soundstructure device, string name, uint[] values)
            : base(device, name, SoundstructureVirtualChannelType.MONO, SoundstructurePhysicalChannelType.PSTN_IN, values)
        {

        }

        public bool IsRinging { get; private set; }

        protected override void OnFeedbackReceived(SoundstructureCommandType commandType, string commandModifier, double value)
        {
            if (commandType == SoundstructureCommandType.PHONE_RING)
            {
#if DEBUG
                CrestronConsole.PrintLine("{0}.OnFeedbackReceived {1} {2}", GetType().Name, commandType, value);
#endif
                IsRinging = Convert.ToBoolean(value);


                if (IsRingingChange != null)
                    IsRingingChange(this);
            }

            base.OnFeedbackReceived(commandType, commandModifier, value);
        }

        public event AnalogPhoneInChannelRingEventHandler IsRingingChange;
    }

    public delegate void AnalogPhoneInChannelRingEventHandler(AnalogPhoneInChannel channel);
}