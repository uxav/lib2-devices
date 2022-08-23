using System;
using System.Text;
using UX.Lib2.Cloud.Logger;
using UX.Lib2.Sockets;

namespace UX.Lib2.Devices.Extron
{
    public class ExtronSocket : TCPClientSocketBase
    {
        private readonly string _password;
        private readonly byte[] _bytes;
        private int _byteIndex;
        private bool _loggedIn;
        private const int BufferSize = 10000;

        public ExtronSocket(string address, int port, string password)
            : base(address, port, BufferSize)
        {
            _password = password;
            _bytes = new byte[BufferSize];
        }

        public event ReceivedStringEventHandler ReceivedString;

        protected override void OnConnect()
        {
            _byteIndex = 0;
        }

        protected override void OnDisconnect()
        {
            
        }

        public new void Send(string stringToSend)
        {
            //var bytes = Encoding.ASCII.GetBytes(stringToSend);
            //Debug.WriteInfo("Extron Tx", Tools.GetBytesAsReadableString(bytes, 0, bytes.Length, true));
            base.Send(stringToSend);
        }

        protected override void OnReceive(byte[] buffer, int count)
        {
            //Debug.WriteSuccess("Extron Rx", Tools.GetBytesAsReadableString(buffer, 0, count, true));

            var text = Encoding.ASCII.GetString(buffer, 0, count);
            if (text.Contains("Password:"))
            {
                Send(_password + "\r");
                return;
            }

            for (var i = 0; i < count; i++)
            {
                var b = buffer[i];

                if (b != 10 && b != 13)
                {
                    _bytes[_byteIndex] = b;
                    _byteIndex++;
                }

                else if (b == 10)
                {
                    OnReceivedString(Encoding.ASCII.GetString(_bytes, 0, _byteIndex));
                    _byteIndex = 0;
                }
            }
        }

        protected virtual void OnReceivedString(string stringReceived)
        {
            var handler = ReceivedString;
            if (handler == null) return;
            try
            {
                handler(stringReceived);
            }
            catch (Exception e)
            {
                CloudLog.Exception(e);
            }
        }
    }

    public delegate void ReceivedStringEventHandler(string stringReceived);
}