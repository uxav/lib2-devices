using System.Text;
using UX.Lib2.Sockets;

namespace UX.Lib2.Devices.Lightware
{
    public class LightwareSocket : TCPClientSocketBase
    {
        #region Fields

        private readonly byte[] _bytes;
        private int _byteIndex;

        #endregion

        #region Constructors

        public LightwareSocket(string address)
            : base(address, 10001, 10000)
        {
            _bytes = new byte[10000];
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events

        public event SocketReceivedHandler ReceivedData;

        #endregion

        #region Delegates
        #endregion

        #region Properties
        #endregion

        #region Methods

        public new void Send(string stringToSend)
        {
            base.Send(stringToSend);
        }

        protected override void OnConnect()
        {
            _byteIndex = 0;
        }

        protected override void OnDisconnect()
        {

        }

        protected virtual void OnReceivedData(string data)
        {
            var handler = ReceivedData;
            if (handler != null) handler(data);
        }

        protected override void OnReceive(byte[] buffer, int count)
        {
            //Debug.WriteInfo("Lightware socket count", count.ToString());
            //Tools.PrintBytes(buffer, 0, count, true);

            for (var i = 0; i < count; i++)
            {
                if (buffer[i] != 13 && buffer[i] != 10)
                {
                    _bytes[_byteIndex] = buffer[i];
                    _byteIndex++;
                }

                if (buffer[i] != 10) continue;
                //Debug.WriteInfo("Lightware processed bytes");
                //Tools.PrintBytes(_bytes, 0, _byteIndex, true);
                OnReceivedData(Encoding.ASCII.GetString(_bytes, 0, _byteIndex));
                _byteIndex = 0;
            }
        }

        #endregion
    }

    public delegate void SocketReceivedHandler(string args);
}