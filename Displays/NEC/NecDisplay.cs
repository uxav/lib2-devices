using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using Crestron.SimplSharp;
using UX.Lib2.DeviceSupport;
using UX.Lib2.Models;
using UX.Lib2.Sockets;

namespace UX.Lib2.Devices.Displays.NEC
{
    public class NecDisplay : DisplayDeviceBase, IAudioLevelDevice, IAudioLevelControl
    {
        #region Fields

        private readonly NecDisplaySocket _socket;
        private readonly int _displayId;
        private int _pollCount;
        private CTimer _pollTimer;
        private readonly IEnumerable<DisplayDeviceInput> _availableInputs;
        private byte _requestedInput;
        private byte _currentInput;
        private ushort _volumeLevel;
        private bool _mute;

        #endregion

        #region Constructors

        public NecDisplay(string name, string address, int displayId)
            : base(name)
        {
            var inputs = new List<DisplayDeviceInput>
            {
                DisplayDeviceInput.VGA,
                DisplayDeviceInput.DVI,
                DisplayDeviceInput.DVI2,
                DisplayDeviceInput.DisplayPort,
                DisplayDeviceInput.DisplayPort2,
                DisplayDeviceInput.HDMI1,
                DisplayDeviceInput.HDMI2,
                DisplayDeviceInput.HDMI3,
                DisplayDeviceInput.HDMI4
            };
            _availableInputs = new ReadOnlyCollection<DisplayDeviceInput>(inputs);
            _displayId = displayId;
            _socket = new NecDisplaySocket(address);
            _socket.StatusChanged += SocketOnStatusChanged;
            _socket.ReceivedData += SocketOnReceivedData;
            AudioLevels = new AudioLevelCollection
            {
                this
            };
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
            get { return "NEC"; }
        }

        public override string ModelName
        {
            get { return "Unknown"; }
        }

        public override DisplayDeviceInput CurrentInput
        {
            get { return _currentInput == 0 ? DisplayDeviceInput.Unknown : GetInputForCommandValue(_currentInput); }
        }

        public override IEnumerable<DisplayDeviceInput> AvailableInputs
        {
            get { return _availableInputs; }
        }

        public override bool SupportsDisplayUsage
        {
            get { throw new NotImplementedException(); }
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

        private void SocketOnStatusChanged(TCPClientSocketBase socket, SocketStatusEventType eventType)
        {

            if (eventType == SocketStatusEventType.Connected)
            {
#if DEBUG
                CrestronConsole.PrintLine("NEC Display Connected");
#endif
                _pollTimer = new CTimer(OnPollEvent, null, 1000, 1000);
            }
            else if (eventType == SocketStatusEventType.Disconnected && _pollTimer != null)
            {
#if DEBUG
                CrestronConsole.PrintLine("NEC Display Disconnected");
#endif
                _pollTimer.Stop();
                _pollTimer.Dispose();
                DeviceCommunicating = false;
            }
        }

        private void OnPollEvent(object callBackObject)
        {
            _pollCount++;

            switch (_pollCount)
            {
                case 1:
                    SendCommand(_displayId, @"01D6");
                    break;
                case 2:
                    if (PowerStatus == DevicePowerStatus.PowerCooling ||
                        PowerStatus == DevicePowerStatus.PowerWarming)
                    {
                        SendCommand(_displayId, @"01D6");
                        _pollCount = 0;
                    }
                    break;
                case 3:
                    if (PowerStatus == DevicePowerStatus.PowerOn)
                        GetParameter(_displayId, @"0060");
                    break;
                case 4:
                    if (PowerStatus == DevicePowerStatus.PowerOn)
                        GetParameter(_displayId, @"0062");
                    _pollCount = 0;
                    break;
            }
        }

        public void SendCommand(int address, string message)
        {
            var str = "\x02" + message + "\x03";
            _socket.Send(address, MessageType.Command, str);
        }

        public void SetParameter(int address, string message)
        {
            var str = "\x02" + message + "\x03";
            _socket.Send(address, MessageType.SetParameter, str);
        }

        public void GetParameter(int address, string message)
        {
            var str = "\x02" + message + "\x03";
            _socket.Send(address, MessageType.GetParameter, str);
        }

        void SendInputCommand(byte command)
        {
            _requestedInput = command;
            var value = "00" + command.ToString("X2");
#if DEBUG
            //CrestronConsole.PrintLine("Send display input command {0}", value);
#endif
            SetParameter(_displayId, "0060" + value);
        }

        void SendVolumeCommand(ushort volume)
        {
            var level = (ushort)Tools.ScaleRange(volume, ushort.MinValue, ushort.MaxValue, 0, 100);

            var bytes = BitConverter.GetBytes(level);

            var message = string.Format("006200{0}{1}", bytes[0].ToString("X2"), bytes[1].ToString("X2"));

            SetParameter(_displayId, message);
        }

        void SendMuteCommand(bool mute)
        {
            SetParameter(_displayId, string.Format("008D000{0}", Convert.ToInt16(mute)));
        }

        protected override void SetPowerFeedback(DevicePowerStatus newPowerState)
        {
            PowerStatus = newPowerState;

            //if (!_firstConnect)
            //{
            //    _firstConnect = true;
            //    Power = Power;
            //    return;
            //}

            //if (PowerStatus == DevicePowerStatus.PowerOff || PowerStatus == DevicePowerStatus.PowerOn
            //    && Power != RequestedPower)
            //    ActionPowerRequest(RequestedPower);
        }

        protected override void ActionPowerRequest(bool powerRequest)
        {
            SendCommand(_displayId, powerRequest ? "C203D60001" : "C203D60004");
            SendCommand(_displayId, "01D6");
        }

        public override void SetInput(DisplayDeviceInput input)
        {
            SendInputCommand(GetInputCommandForInput(input));
        }

        private void SocketOnReceivedData(byte[] bytes)
        {
            DeviceCommunicating = true;

            var address = bytes[3];
            if ((address - 64) == _displayId)
            {
                DeviceCommunicating = true;
                var messageLenString = Encoding.Default.GetString(bytes, 5, 2);
                int messageLen = Int16.Parse(messageLenString, NumberStyles.HexNumber);
                var message = new byte[messageLen];
                Array.Copy(bytes, 7, message, 0, messageLen);
                var type = (MessageType)bytes[4];
                var messageStr = Encoding.Default.GetString(message, 1, message.Length - 2);
#if DEBUG
                //CrestronConsole.Print("Message Type = MessageType.{0}  ", type.ToString());
                //Tools.PrintBytes(message, message.Length);
                //CrestronConsole.PrintLine("Message = {0}, Length = {1}", messageStr, messageStr.Length);
#endif
                try
                {
                    switch (type)
                    {
                        case MessageType.CommandReply:

                            switch (messageStr)
                            {
                                case @"0200D60000040001":
                                    if (PowerStatus != DevicePowerStatus.PowerCooling)
                                    {
                                        SetPowerFeedback(DevicePowerStatus.PowerOn);
                                    }
                                    break;
                                case @"0200D60000040004":
                                    if (PowerStatus != DevicePowerStatus.PowerWarming)
                                    {
                                        SetPowerFeedback(DevicePowerStatus.PowerOff);
                                    }
                                    break;
                                case @"00C203D60001":
                                    SetPowerFeedback(DevicePowerStatus.PowerWarming);
                                    break;
                                case @"00C203D60004":
                                    SetPowerFeedback(DevicePowerStatus.PowerCooling);
                                    break;
                            }
                            break;
                        case MessageType.SetParameterReply:
                            if (messageStr.StartsWith(@"00006200006400"))
                            {
                                _volumeLevel = (ushort)Tools.ScaleRange(
                                    ushort.Parse(messageStr.Substring(14, 2), NumberStyles.HexNumber)
                                    , 0, 100, ushort.MinValue, ushort.MaxValue);
                                if (LevelChange != null)
                                {
                                    LevelChange(this, _volumeLevel);
                                }
                            }
                            break;
                        case MessageType.GetParameterReply:
                            if (messageStr.StartsWith(@"00006200006400"))
                            {
                                _volumeLevel = (ushort)Tools.ScaleRange(
                                    ushort.Parse(messageStr.Substring(14, 2), NumberStyles.HexNumber)
                                    , 0, 100, ushort.MinValue, ushort.MaxValue);
                                if (LevelChange != null)
                                {
                                    LevelChange(this, _volumeLevel);
                                }
                            }
                            else if (messageStr.StartsWith(@"000060"))
                            {
                                _currentInput = byte.Parse(messageStr.Substring(14, 2), NumberStyles.HexNumber);
                                if (_currentInput != _requestedInput && _requestedInput > 0)
                                {
                                    SendInputCommand(_requestedInput);
                                }
                                else if (_currentInput == _requestedInput && _requestedInput > 0)
                                {
                                    _requestedInput = 0x00;
                                }
                            }
                            break;
                    }
                }
                catch (Exception e)
                {
                    ErrorLog.Exception(string.Format("Error in NecLCDMonitor OnReceive(), type = {0}, messageStr = {1}", type.ToString(), messageStr), e);
                }
            }
        }

        DisplayDeviceInput GetInputForCommandValue(byte value)
        {
            switch (value)
            {
                case 0x01: return DisplayDeviceInput.VGA;
                case 0x03: return DisplayDeviceInput.DVI;
                case 0x04: return DisplayDeviceInput.DVI2;
                case 0x0f: return DisplayDeviceInput.DisplayPort;
                case 0x10: return DisplayDeviceInput.DisplayPort2;
                case 0x11: return DisplayDeviceInput.HDMI1;
                case 0x12: return DisplayDeviceInput.HDMI2;
                case 0x82: return DisplayDeviceInput.HDMI3;
                case 0x83: return DisplayDeviceInput.HDMI4;
            }
            throw new IndexOutOfRangeException("Input value out of range");
        }

        byte GetInputCommandForInput(DisplayDeviceInput input)
        {
            switch (input)
            {
                case DisplayDeviceInput.DisplayPort: return 0x0f;
                case DisplayDeviceInput.DisplayPort2: return 0x10;
                case DisplayDeviceInput.HDMI1: return 0x11;
                case DisplayDeviceInput.HDMI2: return 0x12;
                case DisplayDeviceInput.HDMI3: return 0x82;
                case DisplayDeviceInput.HDMI4: return 0x83;
                case DisplayDeviceInput.DVI: return 0x03;
                case DisplayDeviceInput.DVI2: return 0x04;
                case DisplayDeviceInput.VGA: return 0x01;
            }
            throw new IndexOutOfRangeException("Input not supported on this device");
        }

        public override void Initialize()
        {
            _socket.Connect();
        }

        #endregion

        public AudioLevelCollection AudioLevels { get; private set; }

        public AudioLevelType ControlType
        {
            get { return AudioLevelType.Source; }
        }

        public bool SupportsLevel
        {
            get { return true; }
        }

        public ushort Level
        {
            get { return _volumeLevel; }
            set { SendVolumeCommand(value); }
        }

        public string LevelString { get; private set; }

        public bool SupportsMute
        {
            get { return true; }
        }

        public bool Muted
        {
            get { return _mute; }
            set
            {
                _mute = value;
                SendMuteCommand(_mute);
                if (MuteChange != null)
                {
                    MuteChange(_mute);
                }
            }
        }

        public void Mute()
        {
            Muted = true;
        }

        public void Unmute()
        {
            Muted = false;
        }

        public void SetDefaultLevel()
        {
            Level = ushort.MaxValue/2;
        }

        public event AudioMuteChangeEventHandler MuteChange;
        public event AudioLevelChangeEventHandler LevelChange;
    }
}