using System;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.CrestronThread;
using Crestron.SimplSharpProInternal;
using UX.Lib2.Cloud.Logger;
using UX.Lib2.DeviceSupport;

namespace UX.Lib2.Devices.Displays.Samsung
{
    public class SamsungDisplayComPortHandler
    {
        private readonly IComPortDevice _comPort;
        private readonly CrestronQueue<byte[]> _txQueue;
        private readonly CrestronQueue<byte> _rxQueue;
        private Thread _txThread;
        private Thread _rxThread;
        bool _programStopping = false;
        private bool _initialized;

        public SamsungDisplayComPortHandler(IComPortDevice comPort)
        {
            _comPort = comPort;
            _rxQueue = new CrestronQueue<byte>(1000);

            CrestronEnvironment.ProgramStatusEventHandler += type =>
            {
                if (type == eProgramStatusEventType.Stopping)
                {
                    _programStopping = true;

                    if (_txThread != null && _txThread.ThreadState == Thread.eThreadStates.ThreadRunning)
                    {
                        _txQueue.Enqueue(null);
                    }

                    if (_rxThread != null && _rxThread.ThreadState == Thread.eThreadStates.ThreadRunning)
                    {
                        _rxQueue.Enqueue(0x00);
                    }
                }
            };

            _txQueue = new CrestronQueue<byte[]>(50);
        }

        public string Name
        {
            get { return _comPort.ToString(); }
        }

        public void Initialize()
        {
            if (_initialized) return;

            var port = _comPort as PortDevice;

            if (port != null && !port.Registered)
            {
                var result = port.Register();
                if (result != eDeviceRegistrationUnRegistrationResponse.Success)
                {
                    CloudLog.Error("Could not register {0} with ID {1}, {2}", port.GetType().Name, port.ID, result);
                }
            }

            var spec = new ComPort.ComPortSpec()
            {
                BaudRate = ComPort.eComBaudRates.ComspecBaudRate9600,
                DataBits = ComPort.eComDataBits.ComspecDataBits8,
                Parity = ComPort.eComParityType.ComspecParityNone,
                StopBits = ComPort.eComStopBits.ComspecStopBits1,
                Protocol = ComPort.eComProtocolType.ComspecProtocolRS232,
                HardwareHandShake = ComPort.eComHardwareHandshakeType.ComspecHardwareHandshakeNone,
                SoftwareHandshake = ComPort.eComSoftwareHandshakeType.ComspecSoftwareHandshakeNone,
                ReportCTSChanges = false
            };

            _comPort.SetComPortSpec(spec);

            _comPort.SerialDataReceived += (device, args) =>
            {
                var bytes = args.SerialData.ToByteArray();
#if DEBUG
                Debug.WriteSuccess("Samsung Rx",
                    Debug.AnsiBlue + Tools.GetBytesAsReadableString(bytes, 0, bytes.Length, false) +
                    Debug.AnsiReset);
#endif
                foreach (var b in bytes)
                    _rxQueue.Enqueue(b);
      
                if (_rxThread != null && _rxThread.ThreadState == Thread.eThreadStates.ThreadRunning) return;
                _rxThread = new Thread(ReceiveBufferProcess, null, Thread.eThreadStartOptions.CreateSuspended)
                {
                    Priority = Thread.eThreadPriority.UberPriority,
                    Name = string.Format("Samsung Display ComPort - Rx Handler")
                };
                _rxThread.Start();
            };
            _initialized = true;
        }

        public event SamsungDisplayComPortReceivedDataEventHandler ReceivedPacket;

        private object SendBufferProcess(object o)
        {
            while (true)
            {
                try
                {
                    var bytes = _txQueue.Dequeue();
                    if (bytes == null)
                    {
                        CloudLog.Notice("Exiting {0}", Thread.CurrentThread.Name);
                        return null;
                    }
#if DEBUG
                    Debug.WriteInfo("Samsung Tx",
                        Debug.AnsiPurple + Tools.GetBytesAsReadableString(bytes, 0, bytes.Length, false) +
                        Debug.AnsiReset);
#endif
                    _comPort.Send(bytes, bytes.Length);
                    CrestronEnvironment.AllowOtherAppsToRun();
                    Thread.Sleep(10);
                }
                catch (Exception e)
                {
                    if (e.Message != "ThreadAbortException")
                    {
                        CloudLog.Exception(string.Format("{0} - Exception in tx buffer thread", GetType().Name), e);
                    }
                }
            }
        }

        private object ReceiveBufferProcess(object obj)
        {
            var bytes = new Byte[1000];
            var byteIndex = 0;
            var dataLength = 0;

            CloudLog.Info("Started {0}", Thread.CurrentThread.Name);

            while (true)
            {
                try
                {
                    var b = _rxQueue.Dequeue();

                    if (_programStopping)
                        return null;

                    if (b == 0xAA)
                    {
                        byteIndex = 0;
                        dataLength = 0;
                    }
                    else
                        byteIndex++;

                    bytes[byteIndex] = b;
                    if (byteIndex == 3)
                        dataLength = bytes[byteIndex];

                    if (byteIndex == (dataLength + 4))
                    {
                        int chk = bytes[byteIndex];

                        var test = 0;
                        for (var i = 1; i < byteIndex; i++)
                            test = test + bytes[i];

                        if (chk == (byte) test)
                        {
                            var copiedBytes = new byte[byteIndex];
                            Array.Copy(bytes, copiedBytes, byteIndex);
                            if (ReceivedPacket != null)
                                ReceivedPacket(this, copiedBytes);
                        }
                    }
                }
                catch (Exception e)
                {
                    if (e.Message != "ThreadAbortException")
                    {
#if DEBUG
                        CrestronConsole.Print("Error in Samsung Rx: {0}",
                            Tools.GetBytesAsReadableString(bytes, 0, byteIndex, false));
#endif
                        CloudLog.Error("Error in {0} TX Thread", GetType().Name);
                    }
                }

                CrestronEnvironment.AllowOtherAppsToRun();
            }
        }

        public void Send(byte[] bytes)
        {
            // Packet must start with correct header
            if (bytes[0] == 0xAA)
            {
                int dLen = bytes[3];
                var packet = new byte[dLen + 5];
                Array.Copy(bytes, packet, bytes.Length);
                var chk = 0;
                for (var i = 1; i < bytes.Length; i++)
                    chk = chk + bytes[i];
                packet[packet.Length - 1] = (byte)chk;

                if (!_txQueue.TryToEnqueue(packet))
                {
                    CloudLog.Error("Error in {0}, could not Enqueue packet to send", this.GetType().Name);
                }

                if (_txThread != null && _txThread.ThreadState == Thread.eThreadStates.ThreadRunning) return;
                _txThread = new Thread(SendBufferProcess, null, Thread.eThreadStartOptions.CreateSuspended)
                {
                    Priority = Thread.eThreadPriority.HighPriority,
                    Name = string.Format("Samsung Display ComPort - Tx Handler")
                };
                _txThread.Start();
            }
            else
            {
                throw new FormatException("Packet did not begin with correct value");
            }
        }
    }

    public delegate void SamsungDisplayComPortReceivedDataEventHandler(
        SamsungDisplayComPortHandler handler, byte[] receivedData);
}