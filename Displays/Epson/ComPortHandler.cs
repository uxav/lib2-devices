using System;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpProInternal;
using UX.Lib2.Cloud.Logger;

namespace UX.Lib2.Devices.Displays.Epson
{
    public class ComPortHandler
    {
        private readonly IComPortDevice _comPort;

        public ComPortHandler(IComPortDevice comPort)
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
                BaudRate = ComPort.eComBaudRates.ComspecBaudRate9600,
                DataBits = ComPort.eComDataBits.ComspecDataBits8,
                StopBits = ComPort.eComStopBits.ComspecStopBits1,
                Parity = ComPort.eComParityType.ComspecParityNone,
                Protocol = ComPort.eComProtocolType.ComspecProtocolRS232,
                ReportCTSChanges = false,
                HardwareHandShake = ComPort.eComHardwareHandshakeType.ComspecHardwareHandshakeNone,
                SoftwareHandshake = ComPort.eComSoftwareHandshakeType.ComspecSoftwareHandshakeNone,
            });

            _comPort.SerialDataReceived += PortOnSerialDataReceived;
        }

        public event ReceivedStringEventHandler ReceivedString;

        protected virtual void OnReceivedString(string receivedstring)
        {
            var handler = ReceivedString;
            if (handler == null) return;

            try
            {
                handler(receivedstring);
            }
            catch (Exception e)
            {
                CloudLog.Exception(e);
            }
        }

        private void PortOnSerialDataReceived(IComPortDevice device, ComPortSerialDataEventArgs args)
        {
            OnReceivedString(args.SerialData);
        }

        public void Send(string stringToSend)
        {
#if DEBUG
            Debug.WriteInfo("Epson Tx", stringToSend);
#endif
            _comPort.Send(stringToSend + "\r");
        }

        public override string ToString()
        {
            return "Epson on ComPort: " + _comPort;
        }
    }

    public delegate void ReceivedStringEventHandler(string receivedString);
}