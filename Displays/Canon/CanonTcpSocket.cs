using System.Text;
using UX.Lib2.Sockets;

namespace UX.Lib2.Devices.Displays.Canon
{
    public class CanonTcpSocket : TCPClientSocketBase
    {
        #region Fields

        private readonly byte[] _bytes;
        private int _byteIndex;

        #endregion

        #region Constructors

        internal CanonTcpSocket(string address)
            : base(address, 33336, 1000)
        {
            _bytes = new byte[1000];
        }
        
        #endregion

        #region Finalizers
        #endregion

        #region Events

        internal event TcpSocketReceivedDataHandler ReceivedData;

        #endregion

        #region Delegates
        #endregion

        #region Properties
        #endregion

        #region Methods

        public new void Send(string data)
        {
#if DEBUG
            //CrestronConsole.PrintLine("Projector Tx: {0}", data);
#endif
            base.Send(data + "\x0d");
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

                if (_bytes[_byteIndex] != 13)
                {
                    _byteIndex ++;
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

        #endregion
    }
    
    internal delegate void TcpSocketReceivedDataHandler(string data);
}