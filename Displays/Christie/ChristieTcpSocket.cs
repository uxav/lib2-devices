using System.Text;
using UX.Lib2.Sockets;

namespace UX.Lib2.Devices.Displays.Christie
{
    public class ChristieTcpSocket : TCPClientSocketBase
    {
        #region Fields

        private readonly byte[] _bytes;
        private int _byteIndex;

        #endregion

        #region Constructors

        internal ChristieTcpSocket(string address)
            : base(address, 3002, 1000)
        {
            _bytes = new byte[1000];
        }
        
        #endregion

        #region Finalizers
        #endregion

        #region Events

        internal event ReceivedDataHandler ReceivedData;

        #endregion

        #region Delegates
        #endregion

        #region Properties
        #endregion

        #region Methods

        public new void Send(string data)
        {
#if DEBUG
            Debug.WriteWarn("Projector Tx", '(' + data + ')');
#endif
            base.Send('(' + data + ')');
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

                if (_bytes[_byteIndex] != ')')
                {
                    _byteIndex ++;
                }
                else
                {
                    _byteIndex ++;
                    var data = Encoding.ASCII.GetString(_bytes, 0, _byteIndex);
#if DEBUG
                    Debug.WriteSuccess("Projector Rx", data);
#endif
                    OnReceivedData(data);
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
    
    internal delegate void ReceivedDataHandler(string data);
}