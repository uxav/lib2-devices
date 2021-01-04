using System;
using System.Text;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpProInternal;
using UX.Lib2.Cloud.Logger;

namespace UX.Lib2.Devices.Displays.Christie
{
    public class ChristieComPortHandler
    {
        private readonly IComPortDevice _comPort;
        private readonly byte[] _bytes;
        private int _byteIndex;

        internal ChristieComPortHandler(IComPortDevice comPort)
        {
            _comPort = comPort;

            var port = _comPort as CrestronDevice;

            if (port != null && !port.Registered)
            {
                var result = port.Register();
                if (result != eDeviceRegistrationUnRegistrationResponse.Success)
                {
                    CloudLog.Error("Could not register {0}, {1}", port, result);
                }
            }

            _comPort.SetComPortSpec(new ComPort.ComPortSpec()
            {
                BaudRate = ComPort.eComBaudRates.ComspecBaudRate115200,
                DataBits = ComPort.eComDataBits.ComspecDataBits8,
                StopBits = ComPort.eComStopBits.ComspecStopBits1,
                Parity = ComPort.eComParityType.ComspecParityNone,
                Protocol = ComPort.eComProtocolType.ComspecProtocolRS232,
                ReportCTSChanges = false,
                HardwareHandShake = ComPort.eComHardwareHandshakeType.ComspecHardwareHandshakeNone,
                SoftwareHandshake = ComPort.eComSoftwareHandshakeType.ComspecSoftwareHandshakeNone,
            });

            _bytes = new byte[1000];
            _byteIndex = 0;

            _comPort.SerialDataReceived += ComPortOnSerialDataReceived;
        }

        private void ComPortOnSerialDataReceived(IComPortDevice device, ComPortSerialDataEventArgs args)
        {
            var bytes = Encoding.ASCII.GetBytes(args.SerialData);

            foreach (var b in bytes)
            {
                _bytes[_byteIndex] = b;

                if (_bytes[_byteIndex] != ')')
                {
                    _byteIndex++;
                }
                else
                {
                    _byteIndex++;
                    var data = Encoding.ASCII.GetString(_bytes, 0, _byteIndex);
#if DEBUG
                    Debug.WriteSuccess("Projector Rx", data);
#endif
                    OnReceivedData(data);
                    _byteIndex = 0;
                }
            }
        }

        internal event ReceivedDataHandler ReceivedData;

        protected virtual void OnReceivedData(string data)
        {
            var handler = ReceivedData;
            if (handler != null) handler(data);
        }

        public void Send(string data)
        {
#if DEBUG
            Debug.WriteWarn("Projector Tx", '(' + data + ')');
#endif
            _comPort.Send('(' + data + ')');
        }
    }
}