using System;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.CrestronThread;
using UX.Lib2.Cloud.Logger;

namespace UX.Lib2.Devices.Displays.LG
{
    public class LgComPortHandler
    {
        #region Fields
        
        private readonly IComPortDevice _comPort;
        private readonly CrestronQueue<byte> _rxQueue = new CrestronQueue<byte>(1000);
        private Thread _rxThread;
        private bool _programStopping;

        private readonly ComPort.ComPortSpec _spec = new ComPort.ComPortSpec()
        {
            BaudRate = global::Crestron.SimplSharpPro.ComPort.eComBaudRates.ComspecBaudRate9600,
            DataBits = global::Crestron.SimplSharpPro.ComPort.eComDataBits.ComspecDataBits8,
            Parity = global::Crestron.SimplSharpPro.ComPort.eComParityType.ComspecParityNone,
            StopBits = global::Crestron.SimplSharpPro.ComPort.eComStopBits.ComspecStopBits1,
            Protocol = global::Crestron.SimplSharpPro.ComPort.eComProtocolType.ComspecProtocolRS232,
            HardwareHandShake = global::Crestron.SimplSharpPro.ComPort.eComHardwareHandshakeType.ComspecHardwareHandshakeNone,
            SoftwareHandshake = global::Crestron.SimplSharpPro.ComPort.eComSoftwareHandshakeType.ComspecSoftwareHandshakeNone,
            ReportCTSChanges = false
        };

        #endregion

        #region Constructors

        public LgComPortHandler(IComPortDevice comPort)
        {
            CrestronEnvironment.ProgramStatusEventHandler += type =>
            {
                _programStopping = type == eProgramStatusEventType.Stopping;
            };

            _comPort = comPort;
            var port = _comPort as ComPort;
            if (port != null)
                port.Register();
            _comPort.SetComPortSpec(_spec);
            _comPort.SerialDataReceived += ComPortOnSerialDataReceived;
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events

        public event ReceivedDataEventHandler ReceivedData;

        #endregion

        #region Delegates
        #endregion

        #region Properties

        public IComPortDevice ComPort
        {
            get { return _comPort; }
        }

        #endregion

        #region Methods

        private void ComPortOnSerialDataReceived(IComPortDevice receivingComPort, ComPortSerialDataEventArgs args)
        {
            var bytes = args.SerialData.ToByteArray();
#if DEBUG
            //Debug.WriteWarn("Lg Serial Rx");
            //Tools.PrintBytes(bytes, 0, bytes.Length, true);
#endif
            foreach (var b in bytes)
                _rxQueue.Enqueue(b);
#if DEBUG
            //Debug.WriteWarn("Bytes in queue", _rxQueue.Count.ToString());
#endif
            if (_rxThread != null && _rxThread.ThreadState == Thread.eThreadStates.ThreadRunning) return;

            _rxThread = new Thread(ReceiveBufferProcess, null)
            {
                Priority = Thread.eThreadPriority.UberPriority,
                Name = string.Format("LG Display ComPort - Rx Handler")
            };
        }

        object ReceiveBufferProcess(object obj)
        {
            var bytes = new Byte[1000];
            var byteIndex = 0;

            while (true)
            {
                try
                {
                    var b = _rxQueue.Dequeue();

                    if (_programStopping)
                        return null;

                    if (b == 'x')
                    {
                        var copiedBytes = new byte[byteIndex];
                        Array.Copy(bytes, copiedBytes, byteIndex);
#if DEBUG
                        //CrestronConsole.Print("LG Rx: ");
                        //Tools.PrintBytes(copiedBytes, 0, copiedBytes.Length, true);
#endif
                        if (ReceivedData != null)
                        {
                            try
                            {
                                ReceivedData(copiedBytes);
                            }
                            catch (Exception e)
                            {
                                CloudLog.Exception(e);
                            }
                        }

                        byteIndex = 0;
                    }
                    else
                    {
                        bytes[byteIndex] = b;
                        byteIndex++;
                    }
                }
                catch (Exception e)
                {
                    if (e.Message != "ThreadAbortException")
                    {
#if DEBUG
                        //CrestronConsole.Print("Error in Lg Rx Handler: ");
                        //Tools.PrintBytes(bytes, 0, byteIndex);
#endif
                        CloudLog.Exception(string.Format("{0} - Exception in rx thread", GetType().Name), e);
                    }
                }

                CrestronEnvironment.AllowOtherAppsToRun();
                Thread.Sleep(0);
            }
        }

        public void Send(char cmd1, char cmd2, uint id, byte data)
        {
            var str = string.Format("{0}{1} {2:X2} {3:X2}", cmd1, cmd2, id, data);
#if DEBUG
            //Debug.WriteInfo("LG Tx", str);
#endif
            _comPort.Send(str + "\x0D");
        }

        #endregion
    }

    public delegate void ReceivedDataEventHandler(byte[] receivedData);
}