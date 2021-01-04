using System.Text;
using UX.Lib2.Sockets;

namespace UX.Lib2.Devices.Polycom
{
    public class PolycomSocketHandler : TCPClientSocketBase
    {
        #region Fields

        private readonly byte[] _bytes;
        private int _byteIndex;

        #endregion

        #region Constructors

        public PolycomSocketHandler(string address, int port)
            : base(address, port, 10000)
        {
            _bytes = new byte[10000];
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
            Debug.WriteSuccess("Codec Tx", str);
#endif
            base.Send(str);
        }

        protected override void OnReceive(byte[] buffer, int count)
        {
            //CrestronConsole.Print("Codec Buffer: ");            
            //Tools.PrintBytes(buffer, 0, count, true);

            for (var i = 0; i < count; i++)
            {
                switch (buffer[i])
                {
                    case 10:
                        break;
                    case 13:
                        OnReceivedData(Encoding.UTF8.GetString(_bytes, 0, _byteIndex));
                        _byteIndex = 0;
                        break;
                    default:
                        _bytes[_byteIndex] = buffer[i];
                        _byteIndex++;
                        break;
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