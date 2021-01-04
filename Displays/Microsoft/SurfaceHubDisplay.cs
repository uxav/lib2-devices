using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.CrestronThread;
using UX.Lib2.Cloud.Logger;
using UX.Lib2.DeviceSupport;

namespace UX.Lib2.Devices.Displays.Microsoft
{
    public class SurfaceHubDisplay : DisplayDeviceBase
    {
        private readonly IComPortDevice _comPort;
        private readonly CrestronQueue<byte> _rxQueue = new CrestronQueue<byte>(1000);
        private Thread _rxThread;
        private bool _programStopping;
        private DisplayDeviceInput _currentInput;

        private readonly ComPort.ComPortSpec _portSpec = new ComPort.ComPortSpec()
        {
            BaudRate = ComPort.eComBaudRates.ComspecBaudRate115200,
            DataBits = ComPort.eComDataBits.ComspecDataBits8,
            Parity = ComPort.eComParityType.ComspecParityNone,
            StopBits = ComPort.eComStopBits.ComspecStopBits1,
            Protocol = ComPort.eComProtocolType.ComspecProtocolRS232,
            HardwareHandShake = ComPort.eComHardwareHandshakeType.ComspecHardwareHandshakeNone,
            SoftwareHandshake = ComPort.eComSoftwareHandshakeType.ComspecSoftwareHandshakeNone,
            ReportCTSChanges = false
        };

        private int _pollCount;
        private bool _initialized;
        private CTimer _pollTimer;
        private CTimer _commsTimeOutTimer;

        public SurfaceHubDisplay(string name, IComPortDevice comPort) : base(name)
        {
            CrestronEnvironment.ProgramStatusEventHandler += type =>
            {
                _programStopping = type == eProgramStatusEventType.Stopping;
            };

            _comPort = comPort;
            var port = _comPort as ComPort;
            if (port != null)
                port.Register();
            _comPort.SetComPortSpec(_portSpec);
            _comPort.SerialDataReceived += ComPortOnSerialDataReceived;
        }

        public override string ManufacturerName
        {
            get { return "Microsoft"; }
        }

        public override string ModelName
        {
            get { return "Surface Hub"; }
        }

        public override string DeviceAddressString
        {
            get { return _comPort.ToString(); }
        }

        public override string SerialNumber
        {
            get { return "Not Available"; }
        }

        public override string VersionInfo
        {
            get { return "Not Available"; }
        }

        public override DisplayDeviceInput CurrentInput
        {
            get { return _currentInput; }
        }

        public override IEnumerable<DisplayDeviceInput> AvailableInputs
        {
            get
            {
                return new[]
                {
                    DisplayDeviceInput.BuiltIn,
                    DisplayDeviceInput.DisplayPort,
                    DisplayDeviceInput.HDMI1,
                    DisplayDeviceInput.VGA,
                    DisplayDeviceInput.Wireless
                };
            }
        }

        public override bool SupportsDisplayUsage
        {
            get { return false; }
        }

        protected override void SetPowerFeedback(DevicePowerStatus newPowerState)
        {
            if (PowerStatus != DevicePowerStatus.PowerOff && newPowerState == DevicePowerStatus.PowerWarming)
            {
                return;
            }

            PowerStatus = newPowerState;
        }

        protected override void ActionPowerRequest(bool powerRequest)
        {
            ResetPolling();
            Send(powerRequest ? "PowerOn" : "PowerOff");
        }

        public override void SetInput(DisplayDeviceInput input)
        {
            if (AvailableInputs.All(i => i != input))
            {
                throw new Exception("Invalid Input");
            }

            ResetPolling();

            switch (input)
            {
                case DisplayDeviceInput.BuiltIn:
                    Send("Source=0");
                    break;
                case DisplayDeviceInput.DisplayPort:
                    Send("Source=1");
                    break;
                case DisplayDeviceInput.HDMI1:
                    Send("Source=2");
                    break;
                case DisplayDeviceInput.VGA:
                    Send("Source=3");
                    break;
                case DisplayDeviceInput.Wireless:
                    Send("Source=4");
                    break;
            }
        }

        private void Send(string command)
        {
#if DEBUG
            Debug.WriteInfo("Surface Hub Tx", command);
#endif
            _comPort.Send(command + "\x0d");
        }

        private void ComPortOnSerialDataReceived(IComPortDevice device, ComPortSerialDataEventArgs args)
        {
            var bytes = args.SerialData.ToByteArray();
#if DEBUG
            Debug.WriteWarn("Surface Hub Rx");
            Tools.PrintBytes(bytes, 0, bytes.Length, true);
#endif
            foreach (var b in bytes)
                _rxQueue.Enqueue(b);
#if DEBUG
            Debug.WriteWarn("Bytes in queue", _rxQueue.Count.ToString());
#endif

            DeviceCommunicating = true;

            if (_commsTimeOutTimer == null)
            {
                _commsTimeOutTimer = new CTimer(specific =>
                {
                    DeviceCommunicating = false;
                }, 60000);
            }
            else
            {
                _commsTimeOutTimer.Reset(60000);
            }

            if (_rxThread != null && _rxThread.ThreadState == Thread.eThreadStates.ThreadRunning) return;

            _rxThread = new Thread(ReceiveBufferProcess, null)
            {
                Priority = Thread.eThreadPriority.UberPriority,
                Name = string.Format("LG Display ComPort - Rx Handler")
            };
        }

        private object ReceiveBufferProcess(object userspecific)
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
                    if (b == 10)
                    {
                        //ignore
                    }
                    else if (b == 13)
                    {
                        var copiedBytes = new byte[byteIndex];
                        Array.Copy(bytes, copiedBytes, byteIndex);
#if DEBUG
                        CrestronConsole.Print("Surface Hub Processed Response: ");
                        Tools.PrintBytes(copiedBytes, 0, copiedBytes.Length, true);
#endif
                        try
                        {
                            var match = Regex.Match(Encoding.ASCII.GetString(copiedBytes, 0, copiedBytes.Length),
                                @"^\w+=\d+$");
                            if (match.Success)
                            {
                                var paramName = match.Groups[1].Value;
                                if (string.IsNullOrEmpty(paramName))
                                {

                                    var value = 0;
                                    try
                                    {
                                        value = int.Parse(match.Groups[2].Value);
                                    }
                                    catch
                                    {
                                        Debug.WriteWarn(
                                            string.Format("{0}, Error parsing value \"{1}\" for key paramName\"{2}\"",
                                                GetType().Name, match.Groups[2].Value, paramName));
                                    }

                                    switch (paramName)
                                    {
                                        case "power":
                                            switch (value)
                                            {
                                                case 1:
                                                    SetPowerFeedback(DevicePowerStatus.PowerWarming);
                                                    break;
                                                case 5:
                                                    SetPowerFeedback(DevicePowerStatus.PowerOn);
                                                    break;
                                                default:
                                                    SetPowerFeedback(DevicePowerStatus.PowerOff);
                                                    break;
                                            }
                                            break;
                                        case "source":
                                            switch (value)
                                            {
                                                case 0:
                                                    _currentInput = DisplayDeviceInput.BuiltIn;
                                                    break;
                                                case 1:
                                                    _currentInput = DisplayDeviceInput.DisplayPort;
                                                    break;
                                                case 2:
                                                    _currentInput = DisplayDeviceInput.HDMI1;
                                                    break;
                                                case 3:
                                                    _currentInput = DisplayDeviceInput.VGA;
                                                    break;
                                                case 4:
                                                    _currentInput = DisplayDeviceInput.Wireless;
                                                    break;
                                            }
                                            break;
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            CloudLog.Exception(e);
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
                        CrestronConsole.Print("Error in Surface Hub Rx Handler: ");
                        Tools.PrintBytes(bytes, 0, byteIndex);
#endif
                        CloudLog.Exception(string.Format("{0} - Exception in rx thread", GetType().Name), e);
                    }
                }

                CrestronEnvironment.AllowOtherAppsToRun();
                Thread.Sleep(0);
            }
        }

        public override void Initialize()
        {
            if (_initialized) return;

            _initialized = true;

            ResetPolling();
        }

        private void ResetPolling()
        {
            _pollCount = 0;

            if (_pollTimer == null || _pollTimer.Disposed)
            {
                _pollTimer = new CTimer(Poll, null, 10000, 500);
            }
            else
            {
                _pollTimer.Stop();
                _pollTimer.Reset(500, 500);
            }
        }

        private void Poll(object userSpecific)
        {
            _pollCount++;

            switch (_pollCount)
            {
                case 10:
                    Send("Power?");
                    break;
                case 11:
                    if (PowerStatus == DevicePowerStatus.PowerOn)
                    {
                        Send("Source?");
                    }
                    break;
            }

            if ((_pollCount >= 10 && PowerStatus != DevicePowerStatus.PowerOn) || _pollCount == 12)
            {
                _pollCount = 0;
            }
        }
    }
}