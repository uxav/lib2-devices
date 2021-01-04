using System;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.CrestronThread;
using UX.Lib2.Sockets;

namespace UX.Lib2.Devices.Displays.Avocor
{
    public class AvocorSocket : TCPClientSocketBase
    {
        #region Fields

        private readonly byte[] _bytes;
        private int _byteIndex;
        private readonly CrestronQueue<byte[]> _txQueue = new CrestronQueue<byte[]>(100);
        private Thread _txThread;

        #endregion

        #region Constructors

        internal AvocorSocket(string address)
            : base(address, 23, 1000)
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

        public void Send(int id, MessageType messageType, byte[] bytes)
        {
            var message = new byte[bytes.Length + 5];

            message[0] = 0x07;
            message[1] = (byte)id;
            message[2] = (byte)messageType;
            Array.Copy(bytes, 0, message, 3, bytes.Length);
            message[bytes.Length + 3] = 0x08;
            message[bytes.Length + 4] = 0x0d;

#if DEBUG
            //CrestronConsole.Print("VT Board Tx: ");
            //Tools.PrintBytes(message, 0, message.Length, true);
#endif
            _txQueue.Enqueue(message);

            if (_txThread == null || _txThread.ThreadState != Thread.eThreadStates.ThreadRunning)
            {
                _txThread = new Thread(specific =>
                {
                    while (!_txQueue.IsEmpty)
                    {
                        var m = _txQueue.Dequeue();
                        Send(m, 0, m.Length);
                        Thread.Sleep(200);
                    }
                    return null;
                }, null)
                {
                    Name = string.Format("AvocorSocket Tx Handler"),
                    Priority = Thread.eThreadPriority.MediumPriority
                };
            }
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
            //CrestronConsole.Print("{0} received: ", GetType());
            //Tools.PrintBytes(buffer, 0, count, true);
#endif

            for (var i = 0; i < count; i++)
            {
                _bytes[_byteIndex] = buffer[i];

                if (_bytes[_byteIndex] != 0x08)
                {
                    _byteIndex++;
                }
                else
                {
                    _byteIndex++;
                    var bytes = new byte[_byteIndex];
                    Array.Copy(_bytes, bytes, _byteIndex);
                    OnReceivedData(bytes);
                    _byteIndex = 0;
                }
            }
        }

        protected virtual void OnReceivedData(byte[] bytes)
        {
            var handler = ReceivedData;
            if (handler != null) handler(bytes);
        }

        #endregion
    }

    internal delegate void TcpSocketReceivedDataHandler(byte[] bytes);

    public enum MessageType : byte
    {
        Reply = 0x00,
        Read = 0x01,
        Write = 0x02
    }
}