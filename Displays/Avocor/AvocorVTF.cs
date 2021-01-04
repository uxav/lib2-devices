using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Crestron.SimplSharp;
using UX.Lib2.DeviceSupport;
using UX.Lib2.Sockets;

namespace UX.Lib2.Devices.Displays.Avocor
{
    public class AvocorVTF : DisplayDeviceBase
    {
        #region Fields

        private readonly AvocorSocket _socket;
        private bool _firstConnect;
        private byte _requestedInput = 0xff;
        private readonly ReadOnlyCollection<DisplayDeviceInput> _availableInputs;
        private DisplayDeviceInput _currentInput;
        private CTimer _pollTimer;
        private int _pollCount;
        private CTimer _powerBusyTimer;

        #endregion

        #region Constructors

        public AvocorVTF(string name, string address)
            : base(name)
        {
            _socket = new AvocorSocket(address);
            _socket.StatusChanged += SocketOnStatusChanged;
            _socket.ReceivedData += SocketOnReceivedData;
            _availableInputs = new ReadOnlyCollection<DisplayDeviceInput>(
                new List<DisplayDeviceInput>
                {
                    DisplayDeviceInput.HDMI1,
                    DisplayDeviceInput.HDMI2,
                    DisplayDeviceInput.HDMI3,
                    DisplayDeviceInput.HDMI4,
                    DisplayDeviceInput.DisplayPort,
                    DisplayDeviceInput.BuiltIn,
                    DisplayDeviceInput.VGA
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
            get { return "Avocor"; }
        }

        public override string ModelName
        {
            get { return "VTF"; }
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
            if (!_firstConnect)
            {
                PowerStatus = newPowerState;
                _firstConnect = true;
                Power = Power;
                return;
            }
            
            if (_powerBusyTimer != null && !_powerBusyTimer.Disposed)
            {
                _powerBusyTimer.Stop();
                _powerBusyTimer.Dispose();
            }

            if(PowerStatus == DevicePowerStatus.PowerOff && newPowerState == DevicePowerStatus.PowerOn)
            {
                PowerStatus = DevicePowerStatus.PowerWarming;
                _powerBusyTimer = new CTimer(specific => SetPowerFeedback(DevicePowerStatus.PowerOn), 5000);
                return;
            }
            
            if (PowerStatus == DevicePowerStatus.PowerOn && newPowerState == DevicePowerStatus.PowerOff)
            {
                PowerStatus = DevicePowerStatus.PowerCooling;
                _powerBusyTimer = new CTimer(specific => SetPowerFeedback(DevicePowerStatus.PowerOff), 8000);
                return;
            }

            PowerStatus = newPowerState;

            if (PowerStatus == DevicePowerStatus.PowerOff || PowerStatus == DevicePowerStatus.PowerOn
                && Power != RequestedPower)
            {
                ActionPowerRequest(RequestedPower);
            }
        }

        protected override void ActionPowerRequest(bool powerRequest)
        {
            _socket.Send(1, MessageType.Write,
                powerRequest ? new byte[] {0x50, 0x4f, 0x57, 0x01} : new byte[] {0x50, 0x4f, 0x57, 0x00});
        }

        public override void SetInput(DisplayDeviceInput input)
        {
            _requestedInput = InputCommandForInput(input);
            if (_requestedInput == 0xff) return;
            _socket.Send(1, MessageType.Write, new byte[] {0x4d, 0x49, 0x4e, _requestedInput});
        }

        private byte InputCommandForInput(DisplayDeviceInput input)
        {
            switch (input)
            {
                case DisplayDeviceInput.HDMI1:
                    return 0x09;
                case DisplayDeviceInput.HDMI2:
                    return 0x0a;
                case DisplayDeviceInput.HDMI3:
                    return 0x0b;
                case DisplayDeviceInput.HDMI4:
                    return 0x0c;
                case DisplayDeviceInput.DisplayPort:
                    return 0x0d;
                case DisplayDeviceInput.BuiltIn:
                    return 0x0e;
                case DisplayDeviceInput.VGA:
                    return 0x00;
                default:
                    return 0xff;
            }
        }

        private void SocketOnStatusChanged(TCPClientSocketBase socket, SocketStatusEventType eventType)
        {
            if (eventType == SocketStatusEventType.Connected)
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


                    if (_powerBusyTimer == null || _powerBusyTimer.Disposed)
                        _socket.Send(1, MessageType.Read, new byte[] {0x50, 0x4f, 0x57});
                    break;
                case 2:
                    if (Power) // Poll Input
                        _socket.Send(1, MessageType.Read, new byte[] { 0x4d, 0x49, 0x4e });
                    break;
                case 3:
                    if (Power) // Poll Volume
                        _socket.Send(1, MessageType.Read, new byte[] {0x56, 0x4f, 0x4c});
                    break;
            }

            if (_pollCount >= 3)
            {
                _pollCount = 0;
            }
        }

        private void SocketOnReceivedData(byte[] bytes)
        {
#if DEBUG
            CrestronConsole.Print("{0} received: ", GetType());
            Tools.PrintBytes(bytes, 0, bytes.Length, true);
#endif

            if (bytes[3] == 0x50 && bytes[4] == 0x4f && bytes[5] == 0x57)
            {
#if DEBUG
                CrestronConsole.PrintLine("Power = {0}", Convert.ToBoolean(bytes[6]));
#endif
                SetPowerFeedback(Convert.ToBoolean(bytes[6]) ? DevicePowerStatus.PowerOn : DevicePowerStatus.PowerOff);
            }

            else if (bytes[3] == 0x4d && bytes[4] == 0x49 && bytes[5] == 0x4e)
            {
#if DEBUG
                CrestronConsole.PrintLine("Actual input = {0}, requested input = {1}", bytes[6], _requestedInput);
#endif

                switch (bytes[6])
                {
                    case 0x09:
                        _currentInput = DisplayDeviceInput.HDMI1;
                        break;
                    case 0x0a:
                        _currentInput = DisplayDeviceInput.HDMI2;
                        break;
                    case 0x0b:
                        _currentInput = DisplayDeviceInput.HDMI3;
                        break;
                    case 0x0c:
                        _currentInput = DisplayDeviceInput.HDMI4;
                        break;
                    case 0x0d:
                        _currentInput = DisplayDeviceInput.DisplayPort;
                        break;
                    case 0x0e:
                        _currentInput = DisplayDeviceInput.BuiltIn;
                        break;
                    case 0x00:
                        _currentInput = DisplayDeviceInput.VGA;
                        break;
                    default:
                        _currentInput = DisplayDeviceInput.Unknown;
                        break;
                }
 
                if (_requestedInput != 0xff && _requestedInput != bytes[6])
                    _socket.Send(1, MessageType.Write, new byte[] { 0x4d, 0x49, 0x4e, _requestedInput });
                else if (_requestedInput != 0xff && _requestedInput == bytes[6])
                    _requestedInput = 0xff;
            }

            else if (bytes[3] == 0x56 && bytes[4] == 0x4f && bytes[5] == 0x4c)
            {
                var v = Convert.ToUInt32(bytes[6]);
                //if (_volume.Equals(v)) return;
                //_volume = v;
                //OnVolumeChanged(this, new VolumeChangeEventArgs(VolumeLevelChangeEventType.LevelChanged));
            }
        }

        public override void Initialize()
        {
            _socket.Connect();
        }

        #endregion
    }
}