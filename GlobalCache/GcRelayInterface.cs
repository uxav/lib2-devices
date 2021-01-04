using System.Collections.Generic;

namespace UX.Lib2.Devices.GlobalCache
{
    public class GcRelayInterface
    {
        private readonly GcControlSocket _socket;
        private readonly Dictionary<uint, GcRelay> _relays = new Dictionary<uint, GcRelay>(); 

        public GcRelayInterface(string address)
        {
            _socket = new GcControlSocket(address);
            for (var i = 1U; i <= 3; i++)
            {
                _relays[i] = new GcRelay(this, i);
            }
        }

        public GcRelay this[uint relay]
        {
            get { return _relays[relay]; }
        }

        internal void Send(string command)
        {
            _socket.Send(command);
        }

        public void Initialize()
        {
            _socket.Connect();
        }
    }
}