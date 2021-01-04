using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using Crestron.SimplSharp;
using UX.Lib2.Cloud.Logger;
using UX.Lib2.DeviceSupport;
using UX.Lib2.Sockets;

namespace UX.Lib2.Devices.Displays.Christie
{
    public class ChristieGS800 : DisplayDeviceBase
    {
        #region Fields

        private readonly ChristieTcpSocket _socket;
        private CTimer _pollTimer;
        private int _pollCount;
        private bool _firstConnect;
        private readonly ReadOnlyCollection<DisplayDeviceInput> _availableInputs;
        private int _requestedInputValue;
        private DisplayDeviceInput _currentInput;
        private string _modelName;
        private string _serialNumber = "Unknown";
        private ChristieComPortHandler _serialPort;

        #endregion

        #region Constructors

        public ChristieGS800(string name, string address)
            : base(name)
        {
            _socket = new ChristieTcpSocket(address);
            _socket.StatusChanged += SocketOnStatusChanged;
            _socket.ReceivedData += OnReceivedData;
            _availableInputs = new ReadOnlyCollection<DisplayDeviceInput>(
                new List<DisplayDeviceInput>
                {
                    DisplayDeviceInput.HDMI1,
                    DisplayDeviceInput.HDMI2,
                    DisplayDeviceInput.DVI,
                    DisplayDeviceInput.SDI,
                    DisplayDeviceInput.VGA,
                    DisplayDeviceInput.HDBaseT
                });
        }

        public ChristieGS800(string name, IComPortDevice comPort)
            : base(name)
        {
            _serialPort = new ChristieComPortHandler(comPort);
            _serialPort.ReceivedData += OnReceivedData;
            _availableInputs = new ReadOnlyCollection<DisplayDeviceInput>(
                new List<DisplayDeviceInput>
                {
                    DisplayDeviceInput.HDMI1,
                    DisplayDeviceInput.HDMI2,
                    DisplayDeviceInput.DVI,
                    DisplayDeviceInput.SDI,
                    DisplayDeviceInput.VGA,
                    DisplayDeviceInput.HDBaseT
                });
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events
        #endregion

        #region Delegates
        #endregion

        #region Properties

        public override string ManufacturerName
        {
            get { return @"Christie"; }
        }

        public override string ModelName
        {
            get
            {
                return string.IsNullOrEmpty(_modelName) ? @"GS 800 Series" : _modelName;
            }
        }

        public override DisplayDeviceInput CurrentInput
        {
            get { return _currentInput; }
        }

        public override IEnumerable<DisplayDeviceInput> AvailableInputs
        {
            get { return _availableInputs; }
        }

        public override bool SupportsDisplayUsage
        {
            get { return true; }
        }

        public override string SerialNumber
        {
            get { return _serialNumber; }
        }

        public override string DeviceAddressString
        {
            get
            {
                return _serialPort != null ? _serialPort.ToString() : _socket.HostAddress;
            }
        }

        public override string VersionInfo
        {
            get { return "Unknown"; }
        }

        #endregion

        #region Methods

        protected void Send(string data)
        {
            if (_socket != null)
            {
                _socket.Send(data);
            }
            else
            {
                _serialPort.Send(data);
            }
        }

        protected override void SetPowerFeedback(DevicePowerStatus newPowerState)
        {
            PowerStatus = newPowerState;

            if (!_firstConnect)
            {
                _firstConnect = true;
                Power = Power;
                return;
            }

            if ((PowerStatus == DevicePowerStatus.PowerOff || PowerStatus == DevicePowerStatus.PowerOn) && Power != RequestedPower)
            {
                Debug.WriteWarn(GetType().Name + " Power Not as Requested", "PowerStatus = {0}, RequestedPower = {1}, Power = {2}",
                    PowerStatus, RequestedPower, Power);
                ActionPowerRequest(RequestedPower);
            }
        }

        protected override void ActionPowerRequest(bool powerRequest)
        {
            if (powerRequest && PowerStatus == DevicePowerStatus.PowerOff)
            {
                PowerStatus = DevicePowerStatus.PowerWarming;
            }
            if (!powerRequest && PowerStatus == DevicePowerStatus.PowerOn)
            {
                PowerStatus = DevicePowerStatus.PowerCooling;
            }
            Send("PWR " + (powerRequest ? "1" : "0"));
        }

        public override void SetInput(DisplayDeviceInput input)
        {
            if (_availableInputs.Contains(input))
            {
                switch (input)
                {
                    case DisplayDeviceInput.HDMI1:
                        _requestedInputValue = 3;
                        break;
                    case DisplayDeviceInput.HDMI2:
                        _requestedInputValue = 4;
                        break;
                    case DisplayDeviceInput.DVI:
                        _requestedInputValue = 5;
                        break;
                    case DisplayDeviceInput.VGA:
                        _requestedInputValue = 1;
                        break;
                    case DisplayDeviceInput.SDI:
                        _requestedInputValue = 7;
                        break;
                    case DisplayDeviceInput.HDBaseT:
                        _requestedInputValue = 8;
                        break;
                }

                if (_requestedInputValue > 0)
                {
                    Send(string.Format("SIN {0}", _requestedInputValue));
                }
            }
            else
            {
                CloudLog.Error("{0} does not have the option of input: {1}", this, input);
            }
        }

        private void SocketOnStatusChanged(TCPClientSocketBase socket, SocketStatusEventType eventType)
        {
            if(eventType == SocketStatusEventType.Connected)
            {
                _pollCount = 0;
                _pollTimer = new CTimer(PollStep, null, 1000, 1000);
                DeviceCommunicating = true;
            }
            else
            {
                _pollTimer.Stop();
                _pollTimer.Dispose();
                DeviceCommunicating = false;
            }
        }

        private void PollStep(object userSpecific)
        {
            _pollCount++;

            switch (_pollCount)
            {
                case 1:
                    Send("PWR?");
                    break;
                case 2:
                    if (!Power)
                    {
                        _pollCount = 0;
                        return;
                    }
                    Send("SIN?");
                    break;
                case 3:
                    Send("LIF+LSHS?");
                    break;
                case 4:
                    if (string.IsNullOrEmpty(_modelName))
                    {
                        Send("PIF+MDLN?");
                    }
                    break;
                case 5:
                    if (string.IsNullOrEmpty(_serialNumber) || _serialNumber == "Unknown")
                    {
                        Send("PIF+SNUM?");
                    }
                    break;
            }

            if (_pollCount >= 5)
            {
                _pollCount = 0;
            }
        }

        private DisplayDeviceInput GetInputForInputValue(string value)
        {
            switch (value)
            {
                case "HDMI":
                    return DisplayDeviceInput.HDMI1;
                case "HDBT":
                    return DisplayDeviceInput.HDBaseT;
                case "D-RGB":
                    return DisplayDeviceInput.DVI;
                case "A-RGB2":
                    return DisplayDeviceInput.VGA;
                default:
                    return DisplayDeviceInput.Unknown;
            }
        }

        private void OnReceivedData(string data)
        {
            if (_serialPort != null)
            {
                DeviceCommunicating = true;
            }

            var regex = new Regex(@"(?:\(([A-Z,a-z]{3})(?:\+([A-Z,a-z,0-9]{4}))?!([^\)]+)?\))");

            var match = regex.Match(data);

            if (match.Success)
            {
                var functionCode = match.Groups[1].Value;
                var subCode = string.Empty;
                if (match.Groups[2].Success)
                {
                    subCode = match.Groups[2].Value;
                }
                if (!match.Groups[3].Success) return;
                var reply = match.Groups[3].Value;

                switch (functionCode)
                {
                    case "PWR":
                        var powerValue = int.Parse(reply);
                        switch (powerValue)
                        {
                            case 0:
                                if (PowerStatus != DevicePowerStatus.PowerWarming)
                                    SetPowerFeedback(DevicePowerStatus.PowerOff);
                                break;
                            case 1:
                                if (PowerStatus != DevicePowerStatus.PowerCooling)
                                    SetPowerFeedback(DevicePowerStatus.PowerOn);
                                break;
                            case 10:
                                SetPowerFeedback(DevicePowerStatus.PowerCooling);
                                break;
                            case 11:
                                SetPowerFeedback(DevicePowerStatus.PowerWarming);
                                break;
                        }
                        break;
                    case "SIN":
                        var inputValue = int.Parse(reply);
                        if (_requestedInputValue > 0 && _requestedInputValue != inputValue)
                        {
                            Send("SIN " + _requestedInputValue);
                        }
                        else if (_requestedInputValue > 0)
                        {
                            _requestedInputValue = 0;
                        }
                        break;
                    case "LIF":
                        if (subCode == "LSHS")
                        {
                            DisplayUsage = ushort.Parse(reply);
                        }
                        break;
                    case "PIF":
                        switch (subCode)
                        {
                            case "MDLN":
                                if (reply.StartsWith("\"") && reply.EndsWith("\""))
                                {
                                    reply = reply.Substring(1, reply.Length - 2);
                                }
                                _modelName = reply;
                                break;
                            case "SNUM":
                                _serialNumber = reply;
                                break;
                        }
                        break;
                }
            }
        }

        public override void Initialize()
        {
            if (_socket != null)
            {
                _socket.Connect();
            }
            else
            {
                _pollCount = 0;
                _pollTimer = new CTimer(PollStep, null, 1000, 1000);
            }
        }

        #endregion
    }
}