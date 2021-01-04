using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UX.Lib2.Sockets;

namespace UX.Lib2.Devices.Audio.Shure
{
    public class ShureP300 : IEnumerable<ShureLevel>
    {
        private readonly ShureSocket _socket;
        private readonly Dictionary<uint, ShureLevel> _levels = new Dictionary<uint, ShureLevel>(); 

        public ShureP300(string deviceAddress)
        {
            _socket = new ShureSocket(deviceAddress);
            _socket.StatusChanged += SocketOnStatusChanged;
            _socket.ReceivedData += SocketOnReceivedData;

            for (var channel = 1U; channel <= 22; channel++)
            {
                _levels[channel] = new ShureLevel(this, channel);
            }
        }

        public ShureLevel this[uint channel]
        {
            get { return _levels[channel]; }
        }

        private void SocketOnStatusChanged(TCPClientSocketBase socket, SocketStatusEventType eventType)
        {
            if (eventType != SocketStatusEventType.Connected) return;

            foreach (var level in _levels.Values)
            {
                level.Poll();
            }
        }

        private void SocketOnReceivedData(string data)
        {
            var matches = Regex.Matches(data, @"< (\w+) (\d+) (\w+) (\w+) >");
            foreach (Match match in matches)
            {
                if (!match.Success) return;

                var cmdType = match.Groups[1].Value;
                var channel = uint.Parse(match.Groups[2].Value);
                var type = match.Groups[3].Value;
                var value = match.Groups[4].Value;

                Debug.WriteInfo("Shure received", "{0} {1} {2} {3}", cmdType, channel, type, value);

                if (_levels.ContainsKey(channel))
                {
                    _levels[channel].Update(type, value);
                }
            }
        }

        public void Initialize()
        {
            _socket.Connect();
        }

        internal void Send(string str)
        {
            _socket.Send(str);
        }

        public IEnumerator<ShureLevel> GetEnumerator()
        {
            return _levels.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}