using System;
using UX.Lib2.Sockets;

namespace UX.Lib2.Devices.Cameras.Sony
{
    public class ViscaTcpClient : TCPClientSocketBase, IViscaSocket
    {
        private readonly string _address;

        public ViscaTcpClient(string address, int port, int bufferSize)
            : base(address, port, bufferSize)
        {
            _address = address;
        }

        protected override void OnConnect()
        {
            
        }

        protected override void OnDisconnect()
        {
            
        }

        protected override void OnReceive(byte[] buffer, int count)
        {
            //Debug.WriteInfo("Cam Rx", "{0}, {1}", HostAddress, Tools.GetBytesAsReadableString(buffer, 0, count, false));
        }

        public bool Initialized { get; private set; }

        public void Send(byte[] data)
        {
            //Debug.WriteWarn("Visca Tx", "{0}, {1}", HostAddress, Tools.GetBytesAsReadableString(data, 0, data.Length, false));

            Send(data, 0, data.Length);
        }

        public string IpAddress
        {
            get { return _address; }
        }

        public void Initialize()
        {
            Connect();
        }
    }
}