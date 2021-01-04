using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Crestron.SimplSharp;
using UX.Lib2.DeviceSupport;
using UX.Lib2.Models;

namespace UX.Lib2.Devices.Displays.Smart
{
    public class SmartBoard : DisplayDeviceBase, IAudioLevelDevice
    {
        private readonly SmartBoardComPortHandler _comHandler;
        private bool _initialized;
        private CTimer _pollTimer;
        private string _modelName = "Unknown";
        private string _serialNumber = "Unknown";
        private string _version = "";
        private int _pollCount;
        private ESmartPowerState _pState;
        private string _requestedInput = string.Empty;
        private string _actualInput;
        private bool _powerActioned;
        private readonly SmartBoardVolume _volumeLevel;
        private int _inputCheckCount;

        public SmartBoard(string name, IComPortDevice comPort) 
            : base(name)
        {
            _comHandler = new SmartBoardComPortHandler(comPort);
            _comHandler.ReceivedString += ComHandlerOnReceivedString;
            _volumeLevel = new SmartBoardVolume(this);
            AudioLevels = new AudioLevelCollection()
            {
                _volumeLevel
            };
        }

        public override string ManufacturerName
        {
            get { return "Smart"; }
        }

        public override string ModelName
        {
            get { return _modelName; }
        }

        public override string DeviceAddressString
        {
            get { return _comHandler.ToString(); }
        }

        public override string SerialNumber
        {
            get { return _serialNumber; }
        }

        public override string VersionInfo
        {
            get { return _version; }
        }

        public override DisplayDeviceInput CurrentInput
        {
            get
            {
                switch (_actualInput)
                {
                    case "dp1":
                        return DisplayDeviceInput.DisplayPort;
                    case "vga1":
                        return DisplayDeviceInput.VGA;
                    case "ops1":
                        return DisplayDeviceInput.BuiltIn;
                    case "ops1cc":
                        return DisplayDeviceInput.BuiltIn2;
                    default:
                        if (_actualInput.Contains("hdmi"))
                        {
                            return (DisplayDeviceInput) Enum.Parse(typeof (DisplayDeviceInput), _actualInput, true);
                        }
                        break;
                }
                return DisplayDeviceInput.Unknown;
            }
        }

        public override IEnumerable<DisplayDeviceInput> AvailableInputs
        {
            get { throw new System.NotImplementedException(); }
        }

        public override bool SupportsDisplayUsage
        {
            get { return false; }
        }

        public enum ESmartPowerState
        {
            None,
            Powersave,
            Standby,
            Ready,
            On,
            UpdateOn,
            UpdateReady
        }

        protected override void SetPowerFeedback(DevicePowerStatus newPowerState)
        {
            PowerStatus = newPowerState;

            if (RequestedPower != Power && _powerActioned)
            {
                ActionPowerRequest(RequestedPower);
            }
            else if (_powerActioned && RequestedPower == Power)
            {
                _powerActioned = true;
            }
        }

        protected override void ActionPowerRequest(bool powerRequest)
        {
            Send(string.Format("set powerstate={0}", powerRequest ? "on" : "ready"));
            _powerActioned = true;
        }

        public override void SetInput(DisplayDeviceInput input)
        {
            if (!Power)
            {
                Power = true;
            }

            switch (input)
            {
                case DisplayDeviceInput.HDMI1:
                case DisplayDeviceInput.HDMI2:
                case DisplayDeviceInput.HDMI3:
                    SendInputCommand(input.ToString().ToLower());
                    break;
                case DisplayDeviceInput.DVI:
                    SendInputCommand("dp1");
                    break;
                case DisplayDeviceInput.VGA:
                    SendInputCommand("vga1");
                    break;
                case DisplayDeviceInput.BuiltIn:
                    SendInputCommand("ops1");
                    break;
                case DisplayDeviceInput.BuiltIn2:
                    SendInputCommand("ops1cc");
                    break;
                default:
                    throw new ArgumentOutOfRangeException("input", "Not a supported value");
            }
        }

        private void SendInputCommand(string inputValue)
        {
            _requestedInput = inputValue;

            if(string.IsNullOrEmpty(inputValue)) return;

            Send(string.Format("set input={0}", _requestedInput));
        }

        private void ComHandlerOnReceivedString(string receivedString)
        {
            DeviceCommunicating = true;
#if false
            Debug.WriteSuccess("Smart", receivedString);
#endif
            var match = Regex.Match(receivedString, @"(\w+)=([\w\.]+)");

            if (!match.Success) return;
#if DEBUG
            if (match.Groups[1].Success && match.Groups[2].Success)
            {
                Debug.WriteSuccess("Smart", "{0} = {1}", match.Groups[1].Value, match.Groups[2].Value);
            }
#endif
            switch (match.Groups[1].Value)
            {
                case "powerstate":
                    _pState = (ESmartPowerState) Enum.Parse(typeof (ESmartPowerState), match.Groups[2].Value, true);
                    if (_pState < ESmartPowerState.On)
                    {
                        SetPowerFeedback(DevicePowerStatus.PowerOff);
                    }
                    else if (_pState == ESmartPowerState.On)
                    {
                        SetPowerFeedback(DevicePowerStatus.PowerOn);
                    }
                    break;
                case "input":
                    _actualInput = match.Groups[2].Value;
                    if (_requestedInput.Length > 0 && _actualInput.Length > 0)
                    {
                        if (_actualInput == _requestedInput && _inputCheckCount < 3)
                        {
                            _inputCheckCount ++;
                        }
                        else if (_actualInput == _requestedInput)
                        {
                            _requestedInput = string.Empty;
                        }
                        else if (_actualInput != _requestedInput)
                        {
                            _inputCheckCount = 0;
                        }
                    }
                    if (string.IsNullOrEmpty(_requestedInput)) return;
                    SendInputCommand(_requestedInput);
                    break;
                case "volume":
                    _volumeLevel.OnLevelChange(ushort.Parse(match.Groups[2].Value));
                    break;
                case "mute":
                    _volumeLevel.OnMuteChange(match.Groups[2].Value == "on");
                    break;
                case "fwversion":
                    _version = match.Groups[2].Value;
                    break;
                case "serialnum":
                    _serialNumber = match.Groups[2].Value;
                    break;
                case "partnum":
                    _modelName = match.Groups[2].Value;
                    break;
            }
        }

        public void Send(string stringToSend)
        {
#if DEBUG
            Debug.WriteInfo("Smart", stringToSend);
#endif
            _comHandler.Send(stringToSend + "\x0d");
        }

        public override void Initialize()
        {
            if (_initialized) return;

            _initialized = true;

            _pollCount = 0;
            _pollTimer = new CTimer(specific =>
            {
                _pollCount ++;

                if (_pollCount > 10 && (_pState != ESmartPowerState.Ready && _pState != ESmartPowerState.On))
                {
                    _pollCount = 0;
                    return;
                }

                switch (_pollCount)
                {
                    case 10:
                        Send("get powerstate");
                        break;
                    case 11:
                        Send("get fwversion");
                        break;
                    case 12:
                        Send("get serialnum");
                        break;
                    case 13:
                        Send("get partnum");
                        break;
                    case 14:
                        Send("get input");
                        break;
                    case 15:
                        Send("get volume");
                        break;
                    case 16:
                        Send("get mute");
                        break;
                    default:
                        if (_pollCount > 100)
                        {
                            _pollCount = 0;
                        }
                        break;
                }
            }, null, 5000, 200);

            CrestronEnvironment.ProgramStatusEventHandler += type =>
            {
                if (type != eProgramStatusEventType.Stopping) return;
                _pollTimer.Stop();
                _pollTimer.Dispose();
            };
        }

        public AudioLevelCollection AudioLevels { get; private set; }
    }
}
