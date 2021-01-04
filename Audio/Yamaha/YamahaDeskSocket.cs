 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.CrestronThread;
using UX.Lib2.Cloud.Logger;
using UX.Lib2.Sockets;

namespace UX.Lib2.Devices.Audio.Yamaha
{
    public class YamahaDeskSocket : TCPClientSocketBase
    {
        private readonly byte[] _bytes;
        private int _byteIndex;
        private CrestronQueue<string> _sendQueue = new CrestronQueue<string>(1000);
        private Thread _sendThread;

        internal YamahaDeskSocket(string address)
            : base(address, 49280, 10000)
        {
            _bytes = new byte[10000];
        }

        internal event TcpSocketReceivedDataHandler ReceivedData;

        public new void Send(string data)
        {
            _sendQueue.Enqueue(data);

            if (_sendThread == null || _sendThread.ThreadState != Thread.eThreadStates.ThreadRunning)
            {
                _sendThread = new Thread(SendProcess, null) {Priority = Thread.eThreadPriority.HighPriority};
            }
        }

        private object SendProcess(object userSpecific)
        {
            while (_sendQueue.Count > 0)
            {
                var message = _sendQueue.Dequeue();
#if DEBUG
                CrestronConsole.PrintLine(Debug.AnsiGreen + "Yamaha Tx: {0}" + Debug.AnsiReset, message);
#endif
                base.Send(message + "\x0d");

                CrestronEnvironment.AllowOtherAppsToRun();
            }

            return null;
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
            CrestronConsole.Print(Debug.AnsiBlue + "Yamaha Rx: ");
            Tools.PrintBytes(buffer, 0, count, true);
            CrestronConsole.Print(Debug.AnsiReset);

            for (var i = 0; i < count; i++)
            {
                _bytes[_byteIndex] = buffer[i];

                if (_bytes[_byteIndex] != 10)
                {
                    _byteIndex++;
                }
                else
                {
                    OnReceivedData(Encoding.ASCII.GetString(_bytes, 0, _byteIndex));
                    _byteIndex = 0;
                }
            }
        }

        protected virtual void OnReceivedData(string data)
        {
            var handler = ReceivedData;

            try
            {
                if (handler != null) handler(data);
            }
            catch (Exception e)
            {
                CloudLog.Exception(e);
            }
        }
    }

    internal delegate void TcpSocketReceivedDataHandler(string data);
}