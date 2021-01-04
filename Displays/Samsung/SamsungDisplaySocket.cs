using System;
using UX.Lib2.Cloud.Logger;
using UX.Lib2.Sockets;

namespace UX.Lib2.Devices.Displays.Samsung
{
    public class SamsungDisplaySocket : TCPClientSocketBase
    {
        private readonly byte[] _buffer;
        private int _byteIndex;

        public SamsungDisplaySocket(string address)
            : base(address, 1515, 10000)
        {
            _buffer = new byte[10000];
            _byteIndex = 0;
        }

        internal event TcpSocketReceivedDataHandler ReceivedData;

        public static byte[] BuildCommand(CommandType command, int id, byte[] data)
        {
            var result = new byte[data.Length + 4];

            result[0] = 0xaa;
            result[1] = (byte) command;
            result[2] = (byte) id;
            result[3] = (byte) data.Length;

            for (var i = 4; i < data.Length + 4; i++)
                result[i] = data[i - 4];

            return result;
        }

        public static byte[] BuildCommand(CommandType command, int id)
        {
            var result = new byte[4];

            result[0] = 0xaa;
            result[1] = (byte) command;
            result[2] = (byte) id;
            result[3] = 0x00;

            return result;
        }

        public void Send(byte[] bytes)
        {
            // Packet must start with correct header
            if (bytes[0] == 0xAA)
            {
                int dLen = bytes[3];
                byte[] packet = new byte[dLen + 5];
                Array.Copy(bytes, packet, bytes.Length);
                var chk = 0;
                for (var i = 1; i < bytes.Length; i++)
                    chk = chk + bytes[i];
                packet[packet.Length - 1] = (byte) chk;
#if DEBUG
                Debug.WriteInfo("Samsung Tx",
                    Debug.AnsiPurple + Tools.GetBytesAsReadableString(bytes, 0, bytes.Length, false) + Debug.AnsiReset);
#endif
                Send(packet, 0, packet.Length);
            }
            else
            {
                throw new FormatException("Packet did not begin with correct value");
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
            var dataLength = 0;
#if DEBUG
            //Debug.WriteInfo("Samsung Rx",
            //    Debug.AnsiBlue + Tools.GetBytesAsReadableString(buffer, 0, count, false) + Debug.AnsiReset);
#endif
            for (var i = 0; i < count; i++)
            {
                var b = buffer[i];

                if (b == 0xAA)
                {
                    _byteIndex = 0;
                    dataLength = 0;
                }
                else
                {
                    _byteIndex ++;
                }

                _buffer[_byteIndex] = b;

                if (_byteIndex == 3)
                {
                    dataLength = _buffer[_byteIndex];
                }

                if (_byteIndex != (dataLength + 4)) continue;

                int chk = _buffer[_byteIndex];

                var test = 0;
                for (var j = 1; j < _byteIndex; j++)
                    test = test + _buffer[j];

                if (chk != (byte) test)
                {
#if DEBUG
                    Debug.WriteError("Samsung Rx Checksum Fail");
#endif
                    continue;
                }

                var copiedBytes = new byte[_byteIndex];
                Array.Copy(_buffer, copiedBytes, _byteIndex);
                if (ReceivedData == null) continue;
                try
                {
                    ReceivedData(this, copiedBytes);
                }
                catch (Exception e)
                {
                    CloudLog.Exception(e);
                }
            }
        }

        internal delegate void TcpSocketReceivedDataHandler(SamsungDisplaySocket socket, byte[] data);
    }
}