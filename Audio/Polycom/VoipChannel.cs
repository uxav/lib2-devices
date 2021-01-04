 
using Crestron.SimplSharp;

namespace UX.Lib2.Devices.Audio.Polycom
{
    public class VoipChannel : VirtualChannel
    {
        public VoipChannel(Soundstructure device, string name, SoundstructurePhysicalChannelType pcType, uint[] values)
            : base(device, name, SoundstructureVirtualChannelType.MONO, pcType, values)
        {
            Device.VoipInfoReceived += Device_VoipInfoReceived;
        }

        private bool _initialised;
        public new bool Initialised
        {
            get
            {
                return base.Initialised && _initialised;
            }
        }

        protected virtual void OnVoipInfoReceived(string command, string info)
        {
#if DEBUG
            CrestronConsole.PrintLine("{0}, {1} = {2}", Name, command, info);
            CrestronConsole.Print("  elements =");
            foreach (var element in SoundstructureSocket.ElementsFromString(info))
            {
                CrestronConsole.Print(" {0}", element);
            }
            CrestronConsole.PrintLine("");
#endif
            _initialised = true;
        }

        void Device_VoipInfoReceived(ISoundstructureItem item, SoundstructureVoipInfoReceivedEventArgs args)
        {
            if (item == this)
            {
                OnVoipInfoReceived(args.Command, args.Info);
            }
        }
    }
}