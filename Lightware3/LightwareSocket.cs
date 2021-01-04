using System;
using System.Text;
using UX.Lib2.Cloud.Logger;
using UX.Lib2.Sockets;

namespace UX.Lib2.Devices.Lightware3
{
    internal class LightwareSocket : TCPClientSocketBase
    {
        private readonly byte[] _bytes;
        private int _byteIndex;
        private const int BufferSize = 10000;

        public LightwareSocket(string address)
            : base(address, 6107, BufferSize)
        {
            _bytes = new byte[BufferSize];
            _byteIndex = 0;
        }

        public event SocketReceivedEventHandler ReceivedString;

        public new void Send(string str)
        {
            Debug.WriteWarn("Lightware Tx", str);
            base.Send(str + "\r\n");
        }

        protected override void OnConnect()
        {
            _byteIndex = 0;
        }

        protected override void OnDisconnect()
        {
            
        }

        protected override void OnReceive(byte[] buffer, int count)
        {
            for (var i = 0; i < count; i++)
            {
                _bytes[_byteIndex] = buffer[i];
                
                if (_bytes[_byteIndex] == 0x0a && _byteIndex > 0 && _bytes[_byteIndex - 1] == 0x0d)
                {
                    OnReceiveLine(Encoding.ASCII.GetString(_bytes, 0, _byteIndex - 1));
                    _byteIndex = 0;
                    continue;
                }

                _byteIndex++;
            }
        }

        private void OnReceiveLine(string line)
        {
            if(ReceivedString == null) return;

            try
            {
                ReceivedString(line);
            }
            catch (Exception e)
            {
                CloudLog.Exception(e);
            }
        }
    }

    public delegate void SocketReceivedEventHandler(string receivedString);
}