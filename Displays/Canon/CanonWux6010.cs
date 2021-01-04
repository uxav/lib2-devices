using System.Collections.Generic;
using System.Collections.ObjectModel;
using Crestron.SimplSharp;
using UX.Lib2.Cloud.Logger;
using UX.Lib2.DeviceSupport;
using UX.Lib2.Sockets;

namespace UX.Lib2.Devices.Displays.Canon
{
    public class CanonWux6010 : DisplayDeviceBase
    {
        #region Fields

        private readonly CanonTcpSocket _socket;
        private CTimer _pollTimer;
        private int _pollCount;
        private bool _firstConnect;
        private readonly ReadOnlyCollection<DisplayDeviceInput> _availableInputs;
        private string _requestedInputValue = string.Empty;
        private DisplayDeviceInput _currentInput;

        #endregion

        #region Constructors

        public CanonWux6010(string name, string address)
            : base(name)
        {
            _socket = new CanonTcpSocket(address);
            _socket.StatusChanged += SocketOnStatusChanged;
            _socket.ReceivedData += SocketOnReceivedData;
            _availableInputs = new ReadOnlyCollection<DisplayDeviceInput>(
                new List<DisplayDeviceInput>
                {
                    DisplayDeviceInput.HDMI1,
                    DisplayDeviceInput.DVI,
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
            get { return @"Canon"; }
        }

        public override string ModelName
        {
            get { return @"WUX6010"; }
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
            get { return "Unknown"; }
        }

        public override string DeviceAddressString
        {
            get { return _socket.HostAddress; }
        }

        public override string VersionInfo
        {
            get { return "Unknown"; }
        }

        #endregion

        #region Methods

        protected override void SetPowerFeedback(DevicePowerStatus newPowerState)
        {
            PowerStatus = newPowerState;

            if (!_firstConnect)
            {
                _firstConnect = true;
                Power = Power;
                return;
            }

            if (PowerStatus == DevicePowerStatus.PowerOff || PowerStatus == DevicePowerStatus.PowerOn
                && Power != RequestedPower)
                ActionPowerRequest(RequestedPower);
        }

        protected override void ActionPowerRequest(bool powerRequest)
        {
            if(powerRequest && PowerStatus == DevicePowerStatus.PowerOff)
                PowerStatus = DevicePowerStatus.PowerWarming;
            _socket.Send(powerRequest ? "POWER=ON" : "POWER=OFF");
            _socket.Send("GET=POWER");
        }

        public override void SetInput(DisplayDeviceInput input)
        {
            if (_availableInputs.Contains(input))
            {
                switch (input)
                {
                    case DisplayDeviceInput.HDMI1:
                        _requestedInputValue = "HDMI";
                        break;
                    case DisplayDeviceInput.DVI:
                        _requestedInputValue = "D-RGB";
                        break;
                    case DisplayDeviceInput.VGA:
                        _requestedInputValue = "A-RGB2";
                        break;
                    case DisplayDeviceInput.HDBaseT:
                        _requestedInputValue = "HDBT";
                        break;
                }

                _socket.Send(string.Format("INPUT={0}", _requestedInputValue));
                _socket.Send("GET=INPUT");
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
                    _socket.Send("GET=POWER");
                    break;
                case 2:
                    _socket.Send("GET=LAMPCOUNTER");
                    break;
                case 3:
                    if (Power)
                        _socket.Send("GET=INPUT");
                    break;
                case 4:
                    _socket.Send(string.Empty);
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

        private void SocketOnReceivedData(string data)
        {
            if (data.StartsWith("g:"))
            {
                var response = data.Substring(2, data.Length - 2);
#if DEBUG
                //CrestronConsole.PrintLine("{0} received: {1}", this, response);
#endif
                if (!response.Contains("=")) return;
                
                var command = response.Split('=')[0];
                var value = response.Split('=')[1];

                switch (command)
                {
                    case "POWER":
                    {
                        switch (value)
                        {
                            case "OFF":
                                SetPowerFeedback(DevicePowerStatus.PowerOff);
                                break;
                            case "ON":
                                SetPowerFeedback(DevicePowerStatus.PowerOn);
                                break;
                            case "ON2OFF":
                                SetPowerFeedback(DevicePowerStatus.PowerCooling);
                                break;
                            case "OFF2ON":
                                SetPowerFeedback(DevicePowerStatus.PowerWarming);
                                break;
                        }
                    }
                        break;
                    case "INPUT":
                    {
                        _currentInput = GetInputForInputValue(value);
                        if (_requestedInputValue.Length > 0 && _requestedInputValue != value)
                            _socket.Send(string.Format("INPUT={0}", _requestedInputValue));
                        else if (_requestedInputValue.Length > 0)
                            _requestedInputValue = string.Empty;
                    }
                        break;
                    case "LAMPCOUNTER":
                    {
                        if (value.StartsWith("\"[") && value.EndsWith("]\""))
                        {
                            var bar = value.Substring(2, value.Length - 4);

                            var count = 0;
                            foreach (var c in bar)
                            {
                                if (c != '_')
                                    count++;
                                else
                                    break;
                            }

                            DisplayUsage =
                                (ushort) Tools.ScaleRange(count, 0, bar.Length, ushort.MinValue, ushort.MaxValue);
                        }
                    }
                        break;
                }
            }
            else if (data.StartsWith("e:"))
            {
                CloudLog.Error("{0} recieved Error: {1}", this, data.Substring(2, data.Length - 2));
            }
        }

        /// <summary>
        /// Load a lens position by value. 1-3
        /// </summary>
        /// <param name="position">Position value from 1 to 3</param>
        public void LensPositionLoad(int position)
        {
            _socket.Send(string.Format("LPOSLD={0}", position));
        }

        public override void Initialize()
        {
            _socket.Connect();
        }

        #endregion
    }
}