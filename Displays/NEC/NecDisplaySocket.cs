using System;
using System.Linq;
using Crestron.SimplSharp;
using UX.Lib2.Sockets;

namespace UX.Lib2.Devices.Displays.NEC
{
    internal class NecDisplaySocket : TCPClientSocketBase
    {
        #region Fields

        private readonly byte[] _bytes;
        private int _byteIndex;

        #endregion

        #region Constructors

        internal NecDisplaySocket(string address)
            : base(address, 7142, 10000)
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

        public static byte[] ValueToBytes(int value)
        {
            var str = value.ToString("X2");
            var result = new byte[2];
            result[0] = unchecked((byte)str[0]);
            result[1] = unchecked((byte)str[1]);
            return result;
        }

        public static byte[] CreateHeader(int address, MessageType messageType, int messageLength)
        {
            var result = new byte[7];

            result[0] = 0x01;
            result[1] = 0x30;
            result[2] = (byte)(address + 64);
            result[3] = 0x30;
            result[4] = (byte)messageType;
            Array.Copy(ValueToBytes(messageLength), 0, result, 5, 2);
            return result;
        }

        public void SendCommand(int address, string message)
        {
            var str = "\x02" + message + "\x03";
            this.Send(address, MessageType.Command, str);
        }

        public void SetParameter(int address, string message)
        {
            var str = "\x02" + message + "\x03";
            this.Send(address, MessageType.SetParameter, str);
        }

        public void GetParameter(int address, string message)
        {
            var str = "\x02" + message + "\x03";
            this.Send(address, MessageType.GetParameter, str);
        }

        public void Send(int address, MessageType messageType, string message)
        {
            var messageBytes = new byte[message.Length];
            for (var i = 0; i < message.Length; i++)
            {
                messageBytes[i] = unchecked((byte)message[i]);
            }
            this.Send(address, messageType, messageBytes);
        }

        public void Send(int address, MessageType messageType, byte[] message)
        {
#if DEBUG
            //CrestronConsole.Print("NEC Send display {0}, MessageType.{1}, ", address, messageType.ToString());
            //Tools.PrintBytes(message, message.Length);
#endif
            var header = CreateHeader(address, messageType, message.Length);
            var packet = new byte[7 + message.Length];
            Array.Copy(header, packet, header.Length);
            Array.Copy(message, 0, packet, header.Length, message.Length);

            var chk = 0;
            for (var i = 1; i < packet.Length; i++)
            {
                chk = chk ^ packet[i];
            }

            var finalPacket = new byte[packet.Length + 2];
            Array.Copy(packet, finalPacket, packet.Length);
            finalPacket[packet.Length] = (byte)chk;
            finalPacket[packet.Length + 1] = 0x0D;
#if DEBUG
            CrestronConsole.Print("NEC Tx: ");
            Tools.PrintBytes(finalPacket, 0, finalPacket.Length, true);
#endif
            Send(finalPacket, 0, finalPacket.Length);
        }

        protected override void OnReceive(byte[] buffer, int count)
        {
            for (var i = 0; i < count; i++)
            {
                var b = buffer[i];
                byte nextByte = 0;
                if (i < buffer.Length)
                {
                    nextByte = buffer[i + 1];
                }

                if (b == 13)
                {
                    // check the next byte may also be 13 in which this one maybe the checksum
                    if (nextByte == 13)
                    {
                        b = nextByte;
                        _bytes[_byteIndex] = b;
                        _byteIndex++;
                    }

                    // Copy bytes to new array with length of packet and ignoring the CR.
                    var copiedBytes = new Byte[_byteIndex];
                    Array.Copy(_bytes, copiedBytes, _byteIndex);

                    _byteIndex = 0;

                    var chk = 0;

                    for (var i1 = 1; i1 < (copiedBytes.Length - 1); i1++)
                        chk = chk ^ copiedBytes[i1];
#if DEBUG
                    CrestronConsole.Print("NEC Rx: ");
                    Tools.PrintBytes(copiedBytes, 0, copiedBytes.Length, false);
#endif
                    if (copiedBytes.Length > 0 && chk == copiedBytes.Last())
                    {
                        if (ReceivedData != null)
                            ReceivedData(copiedBytes);
                    }
                    else if (copiedBytes.Length > 0)
                    {
                        ErrorLog.Warn("NEC Display Rx: \"{0}\"", Tools.GetBytesAsReadableString(copiedBytes, 0, copiedBytes.Length, true));
                        ErrorLog.Warn("NEC Display Rx - Checksum Error, chk = 0x{0}, byteIndex = {1}, copiedBytes.Length = {2}",
                            chk.ToString("X2"), _byteIndex, copiedBytes.Length);
#if DEBUG
                        CrestronConsole.PrintLine("NEC Display Rx - Checksum Error, chk = 0x{0}, byteIndex = {1}, copiedBytes.Length = {2}",
                            chk.ToString("X2"), _byteIndex, copiedBytes.Length);
#endif
                    }
                }
                else
                {
                    _bytes[_byteIndex] = b;
                    _byteIndex++;
                }
            }
        }

        #endregion
    }

    internal delegate void TcpSocketReceivedDataHandler(byte[] data);

    public enum MessageType : byte
    {
        Command = 0x41,
        CommandReply = 0x42,
        GetParameter = 0x43,
        GetParameterReply = 0x44,
        SetParameter = 0x45,
        SetParameterReply = 0x46
    }
}