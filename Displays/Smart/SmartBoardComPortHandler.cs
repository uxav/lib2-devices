using System;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.CrestronThread;
using Crestron.SimplSharpProInternal;
using UX.Lib2.Cloud.Logger;

namespace UX.Lib2.Devices.Displays.Smart
{
    public class SmartBoardComPortHandler
    {
        private readonly IComPortDevice _comPort;
        private const int BufferLen = 1000;
        private readonly CrestronQueue<byte> _rxQueue = new CrestronQueue<byte>(BufferLen);
        private Thread _rxThread;
        private bool _programStopping;

        public SmartBoardComPortHandler(IComPortDevice comPort)
        {
            CrestronEnvironment.ProgramStatusEventHandler += type =>
            {
                _programStopping = type == eProgramStatusEventType.Stopping;
                if (_rxThread != null && _rxThread.ThreadState == Thread.eThreadStates.ThreadRunning)
                {
                    _rxQueue.Enqueue(0x00);
                }
            };

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
                BaudRate = ComPort.eComBaudRates.ComspecBaudRate19200,
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
            var bytes = args.SerialData.ToByteArray();
#if false
            Debug.WriteWarn("Smart Serial Rx", Tools.PrintBytes(bytes, 0, bytes.Length, true));
#endif
            foreach (var b in bytes)
            {
                _rxQueue.Enqueue(b);
            }

            if (_rxThread != null && _rxThread.ThreadState == Thread.eThreadStates.ThreadRunning) return;

            _rxThread = new Thread(ReceiveBufferProcess, null)
            {
                Priority = Thread.eThreadPriority.UberPriority,
                Name = string.Format("SmartBoard ComPort - Rx Handler")
            };
        }

        public void Send(string stringToSend)
        {
            _comPort.Send(stringToSend);
        }

        object ReceiveBufferProcess(object obj)
        {
            var bytes = new Byte[BufferLen];
            var byteIndex = 0;

            while (true)
            {
                try
                {
                    var b = _rxQueue.Dequeue();

                    if (_programStopping)
                    {
                        CloudLog.Notice("Exiting {0}", Thread.CurrentThread.Name);
                        return null;
                    }

                    if (b == 0x0d)
                    {
                        OnReceivedString(Encoding.ASCII.GetString(bytes, 0, byteIndex));

                        byteIndex = 0;
                    }
                    else if(b != 0x0a)
                    {
                        bytes[byteIndex] = b;
                        byteIndex++;
                    }
                }
                catch (Exception e)
                {
                    if (e.Message != "ThreadAbortException")
                    {
                        CloudLog.Exception(string.Format("{0} - Exception in rx thread", GetType().Name), e);
                    }
                }

                CrestronEnvironment.AllowOtherAppsToRun();
                Thread.Sleep(0);
            }
        }

        public override string ToString()
        {
            return "SmartBoard on ComPort: " + _comPort;
        }
    }

    public delegate void ReceivedStringEventHandler(string receivedString);
}