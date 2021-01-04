using System;
using System.Text;
using UX.Lib2.Cloud.Logger;
using UX.Lib2.Sockets;

namespace UX.Lib2.Devices.Audio.Shure
{
    public class ShureSocket : TCPClientSocketBase
    {
        private const int BufferSize = 10000;

        public ShureSocket(string address)
            : base(address, 2202, BufferSize)
        {

        }

        protected override void OnConnect()
        {
            
        }

        protected override void OnDisconnect()
        {
            
        }

        public new void Send(string str)
        {
#if DEBUG
            Debug.WriteWarn("Shure Tx", str);
#endif
            base.Send(str);
        }

        protected override void OnReceive(byte[] buffer, int count)
        {
#if DEBUG
            Debug.WriteSuccess("Shure Rx", Tools.GetBytesAsReadableString(buffer, 0, count, true));
#endif
            try
            {
                OnReceivedData(Encoding.ASCII.GetString(buffer, 0, count));
            }
            catch (Exception e)
            {
                CloudLog.Exception(e);
            }
        }

        public event ShureSocketOnReceive ReceivedData;

        protected virtual void OnReceivedData(string data)
        {
            var handler = ReceivedData;
            if (handler != null) handler(data);
        }
    }

    public delegate void ShureSocketOnReceive(string data);
}