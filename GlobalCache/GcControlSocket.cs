using UX.Lib2.Sockets;

namespace UX.Lib2.Devices.GlobalCache
{
    public class GcControlSocket : TCPClientSocketBase
    {
        private const int BufferSize = 10000;

        internal GcControlSocket(string address)
            : base(address, 4998, BufferSize)
        {

        }

        protected override void OnConnect()
        {
            
        }

        protected override void OnDisconnect()
        {

        }

        protected override void OnReceive(byte[] buffer, int count)
        {
#if DEBUG
            Debug.WriteSuccess(GetType().Name + " Rx", Tools.GetBytesAsReadableString(buffer, 0, count, true));
#endif
        }

        public new void Send(string message)
        {
#if DEBUG
            Debug.WriteInfo(GetType().Name + " Tx", message);
#endif
            base.Send(message + "\r");
        }
    }
}