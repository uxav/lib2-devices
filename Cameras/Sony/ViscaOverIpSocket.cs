 
using System;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronSockets;
using Crestron.SimplSharpPro.CrestronThread;
using UX.Lib2.Cloud.Logger;

namespace UX.Lib2.Devices.Cameras.Sony
{
    public class ViscaOverIpSocket : IViscaSocket
    {
        private readonly string _ipAddress;
        private readonly int _port;
        private readonly UDPServer _socket;
        private Thread _thread;
        private bool _programStopping;
        private bool _initialized;
        private int _seq;
        private CTimer _resetTimer;

        public ViscaOverIpSocket(string ipAddress, int port)
        {
            _ipAddress = ipAddress;
            _port = port;
            
            _socket = new UDPServer(IPAddress.Parse(ipAddress), port, 1000);
            CrestronEnvironment.ProgramStatusEventHandler += type =>
            {
                _programStopping = type == eProgramStatusEventType.Stopping;
            };
        }

        public ViscaOverIpSocket(string ipAddress, EthernetAdapterType adapterType)
            : this(ipAddress, adapterType, 52381)
        {
        }

        public ViscaOverIpSocket(string ipAddress, EthernetAdapterType adapterType, int port)
        {
            _ipAddress = ipAddress;
            _port = port;
            _socket = new UDPServer(IPAddress.Parse(ipAddress), port, 1000, adapterType);
            CrestronEnvironment.ProgramStatusEventHandler += type =>
            {
                _programStopping = type == eProgramStatusEventType.Stopping;
            };
        }

        public string IpAddress
        {
            get { return _ipAddress; }
        }

        public bool Initialized
        {
            get { return _initialized; }
        }

        public void Send(byte[] data)
        {
            Send(0x01, data);
            if (_resetTimer == null)
            {
                _resetTimer = new CTimer(specific => SendSeqReset(), null, 60000);
            }
            else
            {
                _resetTimer.Reset(60000);
            }
        }

        private void Send(byte commandType, byte[] data)
        {
            var packet = new byte[data.Length + 8];

            _seq++;

            packet[0] = commandType;
            packet[1] = 0x00;

            var dataLengthBytes = BitConverter.GetBytes((ushort) data.Length);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(dataLengthBytes);

            var seqBytes = BitConverter.GetBytes(_seq);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(seqBytes);

            Array.Copy(dataLengthBytes, 0, packet, 2, 2);
            Array.Copy(seqBytes, 0, packet, 4, 4);
            Array.Copy(data, 0, packet, 8, data.Length);

            Debug.WriteWarn("Visca Tx", "{0}::{1}", _ipAddress, Tools.GetBytesAsReadableString(packet, 0, packet.Length, false));

            _socket.SendData(packet, packet.Length);
        }

        private void SendSeqReset()
        {
            _seq = 0;
            Send(0x02, new byte[]{0x01});
        }

        public void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            _socket.EnableUDPServer();
            _thread = new Thread(ReceiveDataProcessingThread, null, Thread.eThreadStartOptions.CreateSuspended)
            {
                Priority = Thread.eThreadPriority.LowestPriority,
                Name = "Visca Over IP Rx Processing Thread"
            };
            _thread.Start();
        }

        private object ReceiveDataProcessingThread(object userSpecific)
        {
            CloudLog.Info("{0} Started", Thread.CurrentThread.Name);
            while (!_programStopping)
            {
                var count = _socket.ReceiveData();
                if (count > 0)
                {
                    Debug.WriteSuccess("Visca Rx " + _socket.IPAddressLastMessageReceivedFrom,
                        Tools.GetBytesAsReadableString(_socket.IncomingDataBuffer, 0, count, false));
                }
                else if(count < 0)
                {
                    CloudLog.Warn("{0} Received Data Count {1}", Thread.CurrentThread.Name, count);
                }
            }
            CloudLog.Warn("{0} Exiting", Thread.CurrentThread.Name);
            return null;
        }
    }
}