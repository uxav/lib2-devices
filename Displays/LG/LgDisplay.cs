using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Crestron.SimplSharp;
using UX.Lib2.Cloud.Logger;
using UX.Lib2.DeviceSupport;
using UX.Lib2.Models;
using UX.Lib2.Sockets;

namespace UX.Lib2.Devices.Displays.LG
{
    public class LgDisplay : DisplayDeviceBase, IAudioLevelDevice
    {
        #region Fields

        private readonly LgComPortHandler _portHandler;
        private readonly uint _displayId;
        private bool _initialized;
        private CTimer _pollTimer;
        private bool _firstConnect;
        private readonly ReadOnlyCollection<DisplayDeviceInput> _availableInputs;
        private DisplayDeviceInput _currentInput;
        private DisplayDeviceInput _requestedInput;
        private readonly AudioLevelCollection _audioLevelCollection = new AudioLevelCollection();
        private readonly LgDisplayVolumeLevel _audioLevel;
        private int _pollCount;
        private readonly LgSocket _socket;

        #endregion

        #region Constructors

        public LgDisplay(string name, LgComPortHandler portHandler, uint displayId)
            : base(name)
        {
            _portHandler = portHandler;
            _portHandler.ReceivedData += OnReceivedData;
            _displayId = displayId;

            _audioLevel = new LgDisplayVolumeLevel(this)
            {
                ControlType = AudioLevelType.Source,
                Name = "Display Volume"
            };
            _audioLevelCollection.Add(_audioLevel);

            _availableInputs = new ReadOnlyCollection<DisplayDeviceInput>(
                new List<DisplayDeviceInput>
                {
                    DisplayDeviceInput.HDMI1,
                    DisplayDeviceInput.HDMI2,
                    DisplayDeviceInput.HDMI3,
                    DisplayDeviceInput.HDMI4
                });
        }

        public LgDisplay(string name, string ipAddress, int port, uint displayId)
            : base(name)
        {
            _socket = new LgSocket(ipAddress, port);
            _socket.ReceivedData += OnReceivedData;
            _socket.StatusChanged += SocketOnStatusChanged;
            _displayId = displayId;

            _audioLevel = new LgDisplayVolumeLevel(this)
            {
                ControlType = AudioLevelType.Source,
                Name = "Display Volume"
            };
            _audioLevelCollection.Add(_audioLevel);

            _availableInputs = new ReadOnlyCollection<DisplayDeviceInput>(
                new List<DisplayDeviceInput>
                {
                    DisplayDeviceInput.HDMI1,
                    DisplayDeviceInput.HDMI2,
                    DisplayDeviceInput.HDMI3,
                    DisplayDeviceInput.HDMI4
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
            get { return "LG"; }
        }

        public override string ModelName
        {
            get { return "Unknown"; }
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
            get { return false; }
        }

        public AudioLevelCollection AudioLevels
        {
            get { return _audioLevelCollection; }
        }

        public override string SerialNumber
        {
            get { return "Unknown"; }
        }

        public override string DeviceAddressString
        {
            get
            {
                if (_socket != null)
                {
                    return _socket.HostAddress;
                }
                return _portHandler.ComPort.ToString();
            }
        }

        public override string VersionInfo
        {
            get { return "Unknown"; }
        }

        #endregion

        #region Methods

        internal void Send(char cmd1, char cmd2, byte data)
        {
            ResetPolling();
            if (_socket != null)
            {
                _socket.Send(cmd1, cmd2, _displayId, data);
                return;
            }
            _portHandler.Send(cmd1, cmd2, _displayId, data);
        }

        private void OnReceivedData(byte[] data)
        {
            DeviceCommunicating = true;
            string rx = Encoding.ASCII.GetString(data, 0, data.Length);
            var match = Regex.Match(rx, @"(\w) (\w{2}) OK(\w{2})");
            if (!match.Success) return;
            var commandType = match.Groups[1].Value;
            var id = uint.Parse(match.Groups[2].Value);
            if (id != _displayId) return;
            var value = uint.Parse(match.Groups[3].Value, NumberStyles.HexNumber);

            //Debug.WriteSuccess("LG Response", "Command \"{0}\" = {1}", commandType, value.ToString("X2"));

            switch (commandType)
            {
                // Power Response
                case "a":
                    if (PowerStatus == DevicePowerStatus.PowerOff && value == 0x01)
                    {
                        PowerStatus = DevicePowerStatus.PowerWarming;
                    }
                    else if (PowerStatus == DevicePowerStatus.PowerWarming && value == 0x01)
                    {
                        PowerStatus = DevicePowerStatus.PowerOn;
                    }
                    else if (PowerStatus == DevicePowerStatus.PowerOn && value == 0x00)
                    {
                        PowerStatus = DevicePowerStatus.PowerCooling;
                    }
                    else if (PowerStatus == DevicePowerStatus.PowerCooling && value == 0x00)
                    {
                        PowerStatus = DevicePowerStatus.PowerOff;
                    }
                    break;
                // Input Response
                case "b":
                    switch (value)
                    {
                        case 0x90:
                            _currentInput = DisplayDeviceInput.HDMI1;
                            break;
                        case 0x91:
                            _currentInput = DisplayDeviceInput.HDMI2;
                            break;
                        case 0x92:
                            _currentInput = DisplayDeviceInput.HDMI3;
                            break;
                        case 0x93:
                            _currentInput = DisplayDeviceInput.HDMI4;
                            break;
                    }

                    if (_requestedInput != DisplayDeviceInput.Unknown && _requestedInput != _currentInput)
                        SetInput(_requestedInput);
                    else
                        _requestedInput = DisplayDeviceInput.Unknown;
                    break;
                // Mute Response
                case "e":
                    _audioLevel.UpdateFromFeedback(value == 0x00);
                    break;
                // Volume Response
                case "f":
                    var scaledLevel = Tools.ScaleRange(value, 0x00, 0x64, ushort.MinValue, ushort.MaxValue);
                    _audioLevel.UpdateFromFeedback((ushort)scaledLevel);
                    break;
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

            if (PowerStatus == DevicePowerStatus.PowerOff || PowerStatus == DevicePowerStatus.PowerOn
                && Power != RequestedPower)
                ActionPowerRequest(RequestedPower);
        }

        protected override void ActionPowerRequest(bool powerRequest)
        {
            ResetPolling();
            Send('k', 'a', (byte)(powerRequest ? 0x01 : 0x00));
        }

        public override void SetInput(DisplayDeviceInput input)
        {
            if (_availableInputs.Contains(input))
            {
                _requestedInput = input;

                switch (input)
                {
                    case DisplayDeviceInput.HDMI1:
                        Send('x', 'b', 0x90);
                        break;
                    case DisplayDeviceInput.HDMI2:
                        Send('x', 'b', 0x91);
                        break;
                    case DisplayDeviceInput.HDMI3:
                        Send('x', 'b', 0x92);
                        break;
                    case DisplayDeviceInput.HDMI4:
                        Send('x', 'b', 0x93);
                        break;
                }
            }
            else
            {
                CloudLog.Error("{0} does not have the option of input: {1}", this, input);
            }
        }

        public override void Initialize()
        {
            if (_initialized) return;

            _initialized = true;

            if (_socket != null)
            {
                _socket.Connect();
            }
            else
            {
                ResetPolling();
            }
        }

        private void ResetPolling()
        {
            if(_socket != null && !_socket.Connected) return;

            _pollCount = 0;

            if (_pollTimer == null || _pollTimer.Disposed)
            {
                Debug.WriteInfo("Starting polling", Name);
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
            _pollCount ++;

            switch (_pollCount)
            {
                case 10:
                    Send('k', 'a', 0xFF);
                    break;
                case 11:
                    Send('x', 'b', 0xFF);
                    break;
                case 12:
                    Send('k', 'e', 0xFF);
                    break;
                case 13:
                    //_portHandler.Send('k', 'f', _displayId, 0xFF);
                    break;

            }

            if ((_pollCount >= 10 && PowerStatus != DevicePowerStatus.PowerOn) || _pollCount == 12)
            {
                _pollCount = 0;
            }
        }

        private void SocketOnStatusChanged(TCPClientSocketBase socket, SocketStatusEventType eventType)
        {
            if (eventType == SocketStatusEventType.Connected)
            {
                Debug.WriteSuccess("Display connected", Name);
                ResetPolling();
            }
            else if (eventType == SocketStatusEventType.Disconnected)
            {
                Debug.WriteError("Display disconnected", Name);

                if (_pollTimer != null && !_pollTimer.Disposed)
                {
                    _pollTimer.Stop();
                    _pollTimer.Dispose();
                    _pollTimer = null;
                }
                DeviceCommunicating = false;
            }
        }

        #endregion
    }
}