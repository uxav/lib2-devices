using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.CrestronThread;
using UX.Lib2.Cloud.Logger;
using UX.Lib2.DeviceSupport;
using UX.Lib2.Models;
using UX.Lib2.Sockets;

namespace UX.Lib2.Devices.Displays.Samsung
{
    public class SamsungDisplay : DisplayDeviceBase, IAudioLevelDevice
    {
        private readonly SamsungDisplayComPortHandler _comPortHandler;
        private readonly SamsungDisplaySocket _socket;
        private CTimer _pollTimer;
        private int _pollCount;
        private bool _osd;
        private bool _mute;
        private ushort _volume;
        private bool _videoSync;
        private byte _requestedInput;
        private string _deviceSerialNumber = string.Empty;
        private DisplayDeviceInput _currentInput;
        private readonly AudioLevelCollection _volumeLevels;
        private bool _booted;
        private CTimer _disconnectStatusTimer;
        private CTimer _reconnectTimer;
        private bool _actualPower;

        public SamsungDisplay(string name, int displayId, SamsungDisplaySocket socket)
            : base(name)
        {
            DisplayId = displayId;
            _socket = socket;
            _socket.ReceivedData += (mdcSocket, data) => OnReceive(data);
            _socket.StatusChanged += SocketOnStatusChanged;
            _volumeLevels = new AudioLevelCollection
            {
                new SamsungVolumeLevel(this)
            };
        }

        public SamsungDisplay(string name, string socketAddress)
            : this(name, 1, new SamsungDisplaySocket(socketAddress)) { }

        public SamsungDisplay(string name, int displayId, SamsungDisplayComPortHandler comPortHandler)
            : base (name)
        {
            DisplayId = displayId;
            _comPortHandler = comPortHandler;
            comPortHandler.ReceivedPacket += (handler, data) =>
            {
                if (_disconnectStatusTimer == null || _disconnectStatusTimer.Disposed)
                {
                    _disconnectStatusTimer = new CTimer(specific =>
                    {
                        CloudLog.Error("Samsung serial rx timed out, setting as offline");
                        DeviceCommunicating = false;
                    }, 60000);
                }
                else
                {
                    _disconnectStatusTimer.Reset(60000);
                }

                OnReceive(data);
            };
            CrestronEnvironment.ProgramStatusEventHandler += type =>
            {
                if (type != eProgramStatusEventType.Stopping || _pollTimer == null) return;
                try
                {
                    _pollTimer.Dispose();
                }
                catch (Exception e)
                {
                    CloudLog.Exception(e);
                }
            };

            _volumeLevels = new AudioLevelCollection
            {
                new SamsungVolumeLevel(this)
            };
        }

        public int DisplayId { get; protected set; }

        public void OnReceive(byte[] packet)
        {
            if (packet[2] != DisplayId) return;

            if (_disconnectStatusTimer != null && !_disconnectStatusTimer.Disposed)
            {
                _disconnectStatusTimer.Stop();
            }

            DeviceCommunicating = true;

            try
            {
                if (packet[1] != 0xff) return;
#if DEBUG
                Debug.WriteInfo("Samsung Rx", Tools.GetBytesAsReadableString(packet, 0, packet.Length, false));
#endif
                switch (packet[4])
                {
                    case 65: // A
                    {
                        byte cmd = packet[5];
                        int dataLength = packet[3];
                        var values = new byte[dataLength - 2];
                        for (var b = 6; b < (dataLength + 4); b++)
                            values[b - 6] = packet[b];
                        if (Enum.IsDefined(typeof(CommandType), cmd))
                        {
                            var cmdType = (CommandType)cmd;
#if DEBUG
                            //Debug.WriteInfo("Samsung", "Command type = {0}, datalength = {1}, data = {2}",
                            //    cmdType.ToString(), dataLength,
                            //    Tools.GetBytesAsReadableString(values, 0, values.Length, false));
#endif
                            switch (cmdType)
                            {
//                                case CommandType.PanelPower:
//#if DEBUG
//                                    Debug.WriteInfo("Samsung Panel Power", !Convert.ToBoolean(values[0]));
//#endif
//                                    if (values[0] == 0)
//                                    {
//                                        SetPowerFeedback(DevicePowerStatus.PowerOn);
//                                    }
//                                    else if (values[0] > 0)
//                                    {
//                                        SetPowerFeedback(DevicePowerStatus.PowerOff);
//                                    }
//                                    break;
                                case CommandType.Power:
                                    var lastPowerStatus = _actualPower;
                                    _actualPower = Convert.ToBoolean(values[0]);
#if DEBUG
                                    Debug.WriteWarn("Samsung Power Feedback Received", _actualPower.ToString());
#endif
                                    if (_booted && !lastPowerStatus && _actualPower)
                                    {
                                        SetPowerFeedback(DevicePowerStatus.PowerWarming);

                                        if(_socket == null) return;
#if DEBUG
                                        Debug.WriteWarn("Disconnecting while display sorts it's shit out");
#endif
                                        _socket.Disconnect();

                                        var thread = new Thread(specific =>
                                        {
                                            Thread.Sleep(10000);
#if DEBUG
                                            Debug.WriteWarn("Reconnecting now!");
#endif
                                            _socket.Connect();
                                            return null;
                                        }, null);
                                    }
                                    else if (!_booted && !lastPowerStatus && _actualPower)
                                    {
                                        Debug.WriteSuccess("Samsung Power OK on boot", _actualPower.ToString());
                                    }
                                    else if (!_actualPower)
                                    {
                                        SetPowerFeedback(DevicePowerStatus.PowerOff);
#if DEBUG
                                        Debug.WriteWarn("Setting Samsung out of standby as it's off!!", _actualPower.ToString());
#endif
                                        SendCommand(CommandType.Power, new[] {Convert.ToByte(true)});
                                    }
                                    break;
                                case CommandType.PanelPower:
                                    var newPanelPowerStatus = !Convert.ToBoolean(values[0]);
#if DEBUG
                                    Debug.WriteInfo("Samsung Panel Power Feedback Received",
                                        newPanelPowerStatus.ToString());
#endif
                                    if (!_booted)
                                    {
                                        _booted = true;
                                        Power = newPanelPowerStatus;
                                    }

                                    if (RequestedPower != newPanelPowerStatus)
                                    {
#if DEBUG
                                        Debug.WriteWarn("Samsung power not as requested... actioning power request");
#endif
                                        ActionPowerRequest(RequestedPower);
                                    }
                                    else
                                    {
                                        SetPowerFeedback(newPanelPowerStatus
                                            ? DevicePowerStatus.PowerOn
                                            : DevicePowerStatus.PowerOff);
                                    }
                                    break;
                                case CommandType.Status:
                                    var standbyState = !Convert.ToBoolean(values[0]);
                                    OnVolumeChange(values[1]);
                                    OnMuteChange(Convert.ToBoolean(values[2]));
                                    _currentInput = GetInputForCommandValue(values[3]);
                                    CheckInputValue(values[3]);
#if DEBUG
                                    Debug.WriteInfo("Samsung", "Mute = {0}, Volume = {1}, Input = {2}, Aspect = {3}, Standby = {4}",
                                        VolumeMute, _volume, _currentInput.ToString(), values[4], standbyState);
#endif
                                    break;
                                case CommandType.DisplayStatus:
                                    OnVideoSyncChange(!Convert.ToBoolean(values[3]));
#if DEBUG
                                    //Debug.WriteInfo("Samsung", "Lamp: {0}, Temp: {1}, No_Sync: {2}", values[0],
                                    //    values[4], values[3]);
#endif
                                    break;
                                case CommandType.SerialNumber:
                                    if (values.Length >= 14)
                                    {
                                        _deviceSerialNumber = Encoding.ASCII.GetString(values, 0, 14);
                                        //Debug.WriteInfo("Samsung", "Serial number = {0}", _deviceSerialNumber);
                                    }
                                    break;
                                case CommandType.Volume:
                                    OnVolumeChange(values[0]);
                                    break;
                                case CommandType.Mute:
                                    OnMuteChange(Convert.ToBoolean(values[0]));
                                    break;
                                case CommandType.InputSource:
                                    _currentInput = GetInputForCommandValue(values[0]);
                                    CheckInputValue(values[0]);
                                    break;
                                case CommandType.OSD:
                                    _osd = Convert.ToBoolean(values[0]);
                                    break;
                                default:
#if DEBUG
                                    Debug.WriteInfo("Samsung", "Other Command \x22{0}\x22, Data: ", cmd.ToString("X2"),
                                        Tools.GetBytesAsReadableString(values, 0, values.Length, false));
# endif
                                    break;
                            }
                        }
                    }
                        break;
                    case 78: // N

                        break;
                }
            }
            catch (Exception e)
            {
                var packetString = packet.Aggregate(string.Empty, (current, t) => string.Format("{0}\\x{1}", current, t));
                ErrorLog.Error("Could not parse packet from {0}, Packet ({1} bytes) = \"{2}\", {3}", GetType().Name,
                    packet.Length, packetString, e.Message);
            }
        }

        private void SocketOnStatusChanged(TCPClientSocketBase socket, SocketStatusEventType eventType)
        {
            if (eventType == SocketStatusEventType.Connected)
            {
#if DEBUG
                Debug.WriteSuccess("Connected! Starting polling display");
#endif
                _pollTimer = new CTimer(OnPollEvent, null, 2000, 2000);
            }
            else if (eventType == SocketStatusEventType.Disconnected && _pollTimer != null)
            {
#if DEBUG
                Debug.WriteError("Disconnected! Stopping polling display");
#endif
                _pollTimer.Stop();
                _pollTimer.Dispose();

                if (DeviceCommunicating)
                {
                    if (_disconnectStatusTimer == null || _disconnectStatusTimer.Disposed)
                    {
                        _disconnectStatusTimer = new CTimer(specific =>
                        {
                            CloudLog.Error("Samsung reconnect timed out, setting as offline");
                            DeviceCommunicating = false;
                        }, 30000);
                    }
                    else
                    {
                        _disconnectStatusTimer.Reset(30000);
                    }
                }
            }
        }

        protected override void SetPowerFeedback(DevicePowerStatus newPowerState)
        {
            PowerStatus = newPowerState;

            if (Power == RequestedPower ||
                (newPowerState == DevicePowerStatus.PowerWarming || newPowerState == DevicePowerStatus.PowerCooling))
                return;
#if DEBUG
            Debug.WriteWarn("Samsung power not as requested... actioning power request");
#endif
            ActionPowerRequest(RequestedPower);
        }

        protected override void ActionPowerRequest(bool powerRequest)
        {
            if (!_actualPower)
            {
                CloudLog.Debug("Cannot set panel power for display, waiting for screen to come out of standby");
                return;
            }

            if (!powerRequest)
            {
                _requestedInput = 0;
            }

            var data = new byte[1];
            data[0] = Convert.ToByte(!powerRequest);
            SendCommand(CommandType.PanelPower, data);
        }

        public override void SetInput(DisplayDeviceInput input)
        {
            var thread = new Thread(specific =>
            {
                if (!Power)
                {
                    Power = true;
                    Thread.Sleep(200);
                }
                var i = (DisplayDeviceInput) specific;
#if true
                Debug.WriteInfo("Samsung Display set to", i.ToString());
#endif
                var data = new byte[1];
                _requestedInput = GetInputCommandForInput(i);
                data[0] = _requestedInput;
                SendCommand(CommandType.InputSource, data);
                return null;
            }, input);
        }

        public override void Initialize()
        {
            if (_socket != null)
            {
                _socket.Connect();
            }
            else
            {
                _comPortHandler.Initialize();
                _pollTimer = new CTimer(OnPollEvent, null, 2000, 2000);
                CrestronEnvironment.ProgramStatusEventHandler += type =>
                {
                    if (type == eProgramStatusEventType.Stopping && _pollTimer != null && !_pollTimer.Disposed)
                    {
                        _pollTimer.Dispose();
                    }
                };
            }
        }

        void OnPollEvent(object callBackObject)
        {
            _pollCount++;

            switch (_pollCount)
            {
                case 1:
                    PollCommand(CommandType.Power);
                    if (!_actualPower)
                    {
                        _pollCount = 0;
                    }
                    break;
                case 2:
                    PollCommand(CommandType.PanelPower);
                    if (PowerStatus != DevicePowerStatus.PowerOn)
                    {
                        _pollCount = 0;
                    }
                    break;
                case 3:
                    PollCommand(CommandType.Status);
                    break;
                case 4:
                    PollCommand(CommandType.DisplayStatus);
                    break;
                case 5:
                    if (string.IsNullOrEmpty(SerialNumber))
                    {
                        PollCommand(CommandType.SerialNumber);
                    }
                    else
                    {
                        _pollCount = 0;
                    }
                    break;
                default:
                    _pollCount = 0;
                    break;
            }
        }

        void SendCommand(CommandType command, byte[] data)
        {
            var packet = SamsungDisplaySocket.BuildCommand(command, DisplayId, data);
            if (_socket != null)
            {
                _socket.Send(packet);
            }
            else
            {
                _comPortHandler.Send(packet);                
            }
        }

        void PollCommand(CommandType commandType)
        {
            var packet = SamsungDisplaySocket.BuildCommand(commandType, DisplayId);
            if (_socket != null)
            {
                _socket.Send(packet);
            }
            else
            {
                _comPortHandler.Send(packet);
            }
        }

        public override string ManufacturerName
        {
            get { return "Samsung"; }
        }

        public override string ModelName
        {
            get { return "Unknown"; }
        }

        public override string DeviceAddressString
        {
            get
            {
                if (_socket != null)
                    return _socket.HostAddress;
                return _comPortHandler.Name;
            }
        }

        public override string SerialNumber
        {
            get { return _deviceSerialNumber; }
        }

        public override string VersionInfo
        {
            get { return "Unknown"; }
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
                    DisplayDeviceInput.HDMI1,
                    DisplayDeviceInput.HDMI2,
                    DisplayDeviceInput.HDMI3,
                    DisplayDeviceInput.HDMI4,
                    DisplayDeviceInput.VGA, 
                    DisplayDeviceInput.DVI, 
                    DisplayDeviceInput.Composite, 
                    DisplayDeviceInput.YUV, 
                    DisplayDeviceInput.MagicInfo, 
                    DisplayDeviceInput.TV, 
                    DisplayDeviceInput.RGBHV
                };
            }
        }

        public override bool SupportsDisplayUsage
        {
            get { return false; }
        }

        internal ushort VolumeLevel
        {
            get { return (ushort) Tools.ScaleRange(_volume, 0, 100, ushort.MinValue, ushort.MaxValue); }
            set
            {
                var scaledVal = (int) Tools.ScaleRange(value, ushort.MinValue, ushort.MaxValue, 0, 100);
                var data = new[]
                {
                    (byte)scaledVal
                };
                SendCommand(CommandType.Volume, data);
            }
        }

        internal string VolumeLevelString
        {
            get { return _volume.ToString(CultureInfo.InvariantCulture); }
        }

        private void OnVolumeChange(ushort value)
        {
            if (value != _volume)
            {
                _volume = value;
                var level = _volumeLevels.First() as SamsungVolumeLevel;
                level.OnVolumeChange();
            }
        }

        public bool VolumeMute
        {
            get
            {
                return _mute;
            }
            set
            {
                byte[] data = new byte[1];
                data[0] = Convert.ToByte(value);
                SendCommand(CommandType.Mute, data);
            }
        }

        private void OnMuteChange(bool value)
        {
            if (value != _mute)
            {
                _mute = value;
                var level = _volumeLevels.First() as SamsungVolumeLevel;
                level.OnMuteChange();
            }
        }

        public bool VideoSync
        {
            get
            {
                return _videoSync;
            }
        }

        private void OnVideoSyncChange(bool value)
        {
            if (_videoSync != value)
            {
                _videoSync = value;
                if (VideoSyncChange != null)
                    VideoSyncChange(this, _videoSync);
            }
        }

        public event SamsungMdcDisplayVideoSyncEventHandler VideoSyncChange;

        public bool Osd
        {
            get
            {
                return _osd;
            }
            set
            {
                var data = new byte[1];
                data[0] = Convert.ToByte(value);
                SendCommand(CommandType.OSD, data);
            }
        }

        private void CheckInputValue(byte value)
        {
            if (_requestedInput > 0 && value != _requestedInput)
            {
                var data = new byte[1];
                data[0] = _requestedInput;
                if (RequestedPower)
                {
                    SendCommand(CommandType.InputSource, data);
                }
            }
            else if (_requestedInput > 0)
            {
                _requestedInput = 0;
            }
        }

        private static DisplayDeviceInput GetInputForCommandValue(byte value)
        {
            switch (value)
            {
                case 0x14:
                    return DisplayDeviceInput.VGA;
                case 0x18:
                    return DisplayDeviceInput.DVI;
                case 0x1f:
                    return DisplayDeviceInput.DVI;
                case 0x0c:
                    return DisplayDeviceInput.Composite;
                case 0x04:
                    return DisplayDeviceInput.SVideo;
                case 0x08:
                    return DisplayDeviceInput.YUV;
                case 0x21:
                    return DisplayDeviceInput.HDMI1;
                case 0x22:
                    return DisplayDeviceInput.HDMI1;
                case 0x23:
                    return DisplayDeviceInput.HDMI2;
                case 0x24:
                    return DisplayDeviceInput.HDMI2;
                case 0x31:
                    return DisplayDeviceInput.HDMI3;
                case 0x32:
                    return DisplayDeviceInput.HDMI3;
                case 0x25:
                    return DisplayDeviceInput.DisplayPort;
                case 0x60:
                    return DisplayDeviceInput.MagicInfo;
                case 0x40:
                    return DisplayDeviceInput.TV;
                case 0x1e:
                    return DisplayDeviceInput.RGBHV;
            }
            throw new IndexOutOfRangeException("Input value out of range");
        }

        private static byte GetInputCommandForInput(DisplayDeviceInput input)
        {
            switch (input)
            {
                case DisplayDeviceInput.HDMI1:
                    return 0x21;
                case DisplayDeviceInput.HDMI2:
                    return 0x23;
                case DisplayDeviceInput.HDMI3:
                    return 0x31;
                case DisplayDeviceInput.VGA:
                    return 0x14;
                case DisplayDeviceInput.DVI:
                    return 0x18;
                case DisplayDeviceInput.Composite:
                    return 0x0c;
                case DisplayDeviceInput.YUV:
                    return 0x08;
                case DisplayDeviceInput.DisplayPort:
                    return 0x25;
                case DisplayDeviceInput.MagicInfo:
                    return 0x60;
                case DisplayDeviceInput.TV:
                    return 0x40;
                case DisplayDeviceInput.RGBHV:
                    return 0x1e;
            }
            throw new IndexOutOfRangeException("Input not supported on this device");
        }

        public AudioLevelCollection AudioLevels
        {
            get { return _volumeLevels; }
        }
    }

    public class SamsungVolumeLevel : IAudioLevelControl
    {
        private readonly SamsungDisplay _display;

        internal SamsungVolumeLevel(SamsungDisplay display)
        {
            _display = display;
        }

        public string Name { get { return "Display Volume"; } }

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
            get { return _display.VolumeLevel; }
            set { _display.VolumeLevel = value; }
        }

        public string LevelString
        {
            get { return _display.VolumeLevelString; }
        }

        public bool SupportsMute
        {
            get { return true; }
        }

        public bool Muted
        {
            get { return _display.VolumeMute; }
            set { _display.VolumeMute = value; }
        }

        public void Mute()
        {
            _display.VolumeMute = true;
        }

        public void Unmute()
        {
            _display.VolumeMute = false;
        }

        public virtual void SetDefaultLevel()
        {
            Level = ushort.MaxValue / 2;
        }

        internal void OnMuteChange()
        {
            if (MuteChange == null) return;
            try
            {
                MuteChange(Muted);
            }
            catch (Exception e)
            {
                CloudLog.Exception(e, "Error calling event handler");
            }
        }

        internal void OnVolumeChange()
        {
            if (LevelChange == null) return;
            try
            {
                LevelChange(this, Level);
            }
            catch (Exception e)
            {
                CloudLog.Exception(e, "Error calling event handler");
            }
        }

        public event AudioMuteChangeEventHandler MuteChange;
        public event AudioLevelChangeEventHandler LevelChange;
    }

    public delegate void SamsungMdcDisplayVideoSyncEventHandler(SamsungDisplay display, bool value);

    public enum CommandType : byte
    {
        Status = 0x00,
        Power = 0x11,
        SerialNumber = 0x0b,
        DisplayStatus = 0x0d,
        Volume = 0x12,
        Mute = 0x13,
        InputSource = 0x14,
        ModelName = 0x8a,
        EnergySaving = 0x92,
        OSD = 0x70,
        OSDType = 0xa3,
        PanelPower = 0xf9
    }
}