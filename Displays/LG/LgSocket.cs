using System;
using System.Text;
using UX.Lib2.Sockets;

namespace UX.Lib2.Devices.Displays.LG
{
    public class LgSocket : TCPClientSocketBase
    {
        #region Fields

        private readonly byte[] _bytes;
        private int _byteIndex;

        #endregion

        #region Constructors

        internal LgSocket(string address, int port)
            : base(address, port, 1000)
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

        public void Send(char cmd1, char cmd2, uint id, byte data)
        {
            var str = string.Format("{0}{1} {2:X2} {3:X2}", cmd1, cmd2, id, data);
#if DEBUG
            //Debug.WriteInfo("LG Tx", str);
#endif
            Send(str + "\x0D");
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
#if DEBUG
            Debug.WriteSuccess("LG Rx", Tools.GetBytesAsReadableString(buffer, 0, count, true));
#endif
            for (var i = 0; i < count; i++)
            {
                _bytes[_byteIndex] = buffer[i];

                if (_bytes[_byteIndex] != 'x')
                {
                    _byteIndex ++;
                }
                else
                {
                    var bytes = new byte[_byteIndex];
                    Array.Copy(_bytes, bytes, _byteIndex);
                    OnReceivedData(bytes);
                    _byteIndex = 0;
                }
            }
        }

        protected virtual void OnReceivedData(byte[] data)
        {
            var handler = ReceivedData;
            if (handler != null) handler(data);
        }

        #endregion
    }
    
    internal delegate void TcpSocketReceivedDataHandler(byte[] data);
}