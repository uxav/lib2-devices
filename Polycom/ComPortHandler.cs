using System;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.CrestronThread;
using UX.Lib2.Cloud.Logger;

namespace UX.Lib2.Devices.Polycom
{
    public class ComPortHandler
    {
        private readonly IComPortDevice _port;
        private readonly CrestronQueue<byte[]> _rxQueue;
        private Thread _rxThread;
        private readonly byte[] _bytes;
        private int _byteIndex;

        public ComPortHandler(IComPortDevice comPort)
        {
            _bytes = new byte[10000];
            _port = comPort;
            _rxQueue = new CrestronQueue<byte[]>();

            var cp = _port as ComPort;

            if (cp != null && !cp.Registered)
            {
                var result = cp.Register();
                if (result != eDeviceRegistrationUnRegistrationResponse.Success)
                {
                    CloudLog.Warn("Registering comport for {0} result: {1}", GetType().Name, result);
                }
            }

            try
            {
                var spec = new ComPort.ComPortSpec()
                {
                    BaudRate = ComPort.eComBaudRates.ComspecBaudRate38400,
                    DataBits = ComPort.eComDataBits.ComspecDataBits8,
                    StopBits = ComPort.eComStopBits.ComspecStopBits1,
                    Parity = ComPort.eComParityType.ComspecParityNone,
                    HardwareHandShake = ComPort.eComHardwareHandshakeType.ComspecHardwareHandshakeNone,
                    SoftwareHandshake = ComPort.eComSoftwareHandshakeType.ComspecSoftwareHandshakeNone,
                    Protocol = ComPort.eComProtocolType.ComspecProtocolRS232,
                    ReportCTSChanges = false
                };
                _port.SetComPortSpec(spec);
            }
            catch (Exception e)
            {
                CloudLog.Error("Error could not set comport spec for {0}, {1}", GetType().Name, e.Message);
            }
        }

        public void Initialize()
        {
            if(_rxThread != null) return;
            CrestronEnvironment.ProgramStatusEventHandler += CrestronEnvironment_ProgramStatusEventHandler;
            _rxThread = new Thread(RxHandler, null, Thread.eThreadStartOptions.Running)
            {
                Name = "Polycom Comport Rx Handler",
                Priority = Thread.eThreadPriority.UberPriority
            };
            _port.SerialDataReceived += PortOnSerialDataReceived;
        }

        private void PortOnSerialDataReceived(IComPortDevice device, ComPortSerialDataEventArgs args)
        {
            var bytes = Encoding.ASCII.GetBytes(args.SerialData);
#if DEBUG
            //Debug.WriteSuccess("Codec Rx Enqueue", Tools.GetBytesAsReadableString(bytes, 0, bytes.Length, true));
#endif
            _rxQueue.Enqueue(bytes);            
        }

        object RxHandler(object o)
        {
            while (true)
            {
                try
                {
                    var bytes = _rxQueue.Dequeue();

                    if (bytes == null)
                    {
                        CloudLog.Notice("Bytes returned null. Program stopping? Exiting {0}", Thread.CurrentThread.Name);
                        return null;
                    }

                    for (var i = 0; i < bytes.Length; i++)
                    {
                        switch (bytes[i])
                        {
                            case 10:
                                break;
                            case 13:
                                OnDataReceived(Encoding.ASCII.GetString(_bytes, 0, _byteIndex));
                                _byteIndex = 0;
                                break;
                            default:
                                _bytes[_byteIndex] = bytes[i];
                                _byteIndex++;
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    ErrorLog.Exception("Exception in Codec Rx thread:", ex);
                }

                CrestronEnvironment.AllowOtherAppsToRun();
                Thread.Sleep(0);
            }
        }

        public event ReceivedDataEventHandler DataReceived;

        protected virtual void OnDataReceived(string data)
        {
            var handler = DataReceived;
            if (handler != null)
            {
                try
                {
                    handler(data);
                }
                catch (Exception e)
                {
                    CloudLog.Exception(e);
                }
            }
        }


        private void CrestronEnvironment_ProgramStatusEventHandler(eProgramStatusEventType programEventType)
        {
            if (programEventType == eProgramStatusEventType.Stopping)
                _rxQueue.Enqueue(null);
        }

        public void Send(string str)
        {
#if DEBUG
            Debug.WriteWarn("Codec Tx", str);
#endif
            _port.Send(str + "\r");
        }
    }

    public delegate void ReceivedDataEventHandler(string data);
}