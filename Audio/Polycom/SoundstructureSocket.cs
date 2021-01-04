 
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Crestron.SimplSharp;
using UX.Lib2.Sockets;

namespace UX.Lib2.Devices.Audio.Polycom
{
    public class SoundstructureSocket : TCPClientSocketBase
    {
        private readonly byte[] _bytes;
        private int _byteIndex;

        public SoundstructureSocket(string hostAddress)
            : base (hostAddress, 52774, 10000)
        {
            _bytes = new byte[10000];
        }

        internal event TcpSocketReceivedDataHandler ReceivedData;

        protected override void OnConnect()
        {
            _byteIndex = 0;
        }

        protected override void OnDisconnect()
        {

        }

        public new void Send(string str)
        {
            str = str + "\x0d";

#if DEBUG
            CrestronConsole.PrintLine("Soundstructure Tx: {0}", str);
#endif
            base.Send(str);
        }

        protected override void OnReceive(byte[] buffer, int count)
        {
            for (var i = 0; i < count; i++)
            {
                _bytes[_byteIndex] = buffer[i];

                if (_bytes[_byteIndex] == 10)
                {
                    //skip
                }
                if (_bytes[_byteIndex] != 13)
                {
                    _byteIndex++;
                }
                else
                {
                    OnReceivedData(Encoding.UTF8.GetString(_bytes, 0, _byteIndex));
                    _byteIndex = 0;
                }
            }
        }

        protected virtual void OnReceivedData(string data)
        {
            var handler = ReceivedData;
            if (handler != null) handler(data);
        }

        public static List<string> ElementsFromString(string str)
        {
            var elements = new List<string>();

            var r = new Regex("(['\"])((?:\\\\\\1|.)*?)\\1|([^\\s\"']+)");

            foreach (Match m in r.Matches(str))
            {
                if (m.Groups[1].Length > 0)
                    elements.Add(m.Groups[2].Value);
                else
                    elements.Add(m.Groups[3].Value);
            }

            return elements;
        }
    }

    internal delegate void TcpSocketReceivedDataHandler(string data);
}