 
using System;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharp;
using UX.Lib2.DeviceSupport;
using UX.Lib2.Sockets;

namespace UX.Lib2.Devices.Audio.Polycom
{
    public class Soundstructure : IDevice
    {
        public Soundstructure(string hostAddress)
        {
            _socket = new SoundstructureSocket(hostAddress);
            _socket.ReceivedData += SocketOnReceivedData;
            _socket.StatusChanged += SocketOnStatusChanged;
        }

        private readonly SoundstructureSocket _socket;
        private readonly List<ISoundstructureItem> _listedItems = new List<ISoundstructureItem>();
        public SoundstructureItemCollection VirtualChannels { get; protected set; }
        public SoundstructureItemCollection VirtualChannelGroups { get; protected set; }
        public SoundstructureEthernetSettings LanAdapter { get; protected set; }

        public bool Initialized { get; protected set; }

        public void Initialize()
        {
            if (!Connected)
            {
                Connect();
            }
            else
            {
                CrestronConsole.PrintLine("{0}.Initialise()", GetType().Name);
                Initialized = false;
                Send("get eth_settings 1");
                _listedItems.Clear();
                Send("vclist");
            }
        }

        public void Reboot()
        {
            _socket.Send(string.Format("set {0}\r", SoundstructureCommandType.SYS_REBOOT.ToString().ToLower()));
        }

        private void SocketOnReceivedData(string data)
        {
#if DEBUG
            CrestronConsole.PrintLine("Soundstructure Rx: {0}", data);
#endif
            if (!DeviceCommunicating)
            {
                DeviceCommunicating = true;
            }

            if (data.Contains(' '))
            {
                List<string> elements = SoundstructureSocket.ElementsFromString(data);

                switch (elements[0])
                {
                    case "error":
                        {
                            ErrorLog.Error("Soundtructure received Error: {0}", elements[1]);
                        }
                        break;
                    case "ran":
                        if (PresetRan != null)
                        {
                            PresetRan(this, elements[1]);
                        }
                        break;
                    case "vcitem":
                        // this should be a response from the vclist command which sends back all virtual channels defined
                        try
                        {
                            List<uint> values = new List<uint>();

                            for (int element = 4; element < elements.Count(); element++)
                            {
                                values.Add(Convert.ToUInt32(elements[element]));
                            }

                            SoundstructurePhysicalChannelType type = (SoundstructurePhysicalChannelType)Enum.Parse(typeof(SoundstructurePhysicalChannelType), elements[3], true);

                            if (type == SoundstructurePhysicalChannelType.VOIP_OUT)
                            {
                                _listedItems.Add(new VoipOutChannel(this, elements[1], values.ToArray()));
                            }
                            else if (type == SoundstructurePhysicalChannelType.VOIP_IN)
                            {
                                _listedItems.Add(new VoipInChannel(this, elements[1], values.ToArray()));
                            }
                            else if (type == SoundstructurePhysicalChannelType.PSTN_OUT)
                            {
                                _listedItems.Add(new AnalogPhoneOutChannel(this, elements[1], values.ToArray()));
                            }
                            else if (type == SoundstructurePhysicalChannelType.PSTN_IN)
                            {
                                _listedItems.Add(new AnalogPhoneInChannel(this, elements[1], values.ToArray()));
                            }
                            else
                            {
                                _listedItems.Add(new VirtualChannel(this, elements[1],
                                    (SoundstructureVirtualChannelType)Enum.Parse(typeof(SoundstructureVirtualChannelType), elements[2], true),
                                    type, values.ToArray()));
                            }
                        }
                        catch (Exception e)
                        {
                            ErrorLog.Error("Error parsing Soundstructure vcitem: {0}", e.Message);
                        }
                        break;
                    case "vcgitem":
                        {
                            List<ISoundstructureItem> channels = new List<ISoundstructureItem>();
                            if (elements.Count() > 2)
                            {
                                for (int e = 2; e < elements.Count(); e++)
                                {
                                    if (VirtualChannels.Contains(elements[e]))
                                    {
                                        channels.Add(VirtualChannels[elements[e]]);
                                    }
                                }
                                VirtualChannelGroup group = new VirtualChannelGroup(this, elements[1], channels);
                                _listedItems.Add(group);
                            }
                            else
                            {
                                ErrorLog.Warn("Ignoring Soundstructure group item {0} as it has no members", elements[1]);
                            }
                        }
                        break;
                    case "vcrename":
                        {
                            List<ISoundstructureItem> channels = new List<ISoundstructureItem>();
                            foreach (VirtualChannel channel in VirtualChannels)
                            {
                                if (channel.Name == elements[1])
                                {
                                    VirtualChannel newChannel = new VirtualChannel(this, elements[2],
                                        channel.VirtualChannelType, channel.PhysicalChannelType, channel.PhysicalChannelIndex.ToArray());
                                    channels.Add(newChannel);
                                }
                                else
                                {
                                    channels.Add(channel);
                                }
                            }
                            VirtualChannels = new SoundstructureItemCollection(channels);
                        }
                        break;
                    case "vcgrename":
                        {
                            List<ISoundstructureItem> groups = new List<ISoundstructureItem>();
                            foreach (VirtualChannelGroup group in VirtualChannelGroups)
                            {
                                if (group.Name == elements[1])
                                {
                                    List<ISoundstructureItem> channels = new List<ISoundstructureItem>();
                                    foreach (VirtualChannel channel in group)
                                    {
                                        channels.Add(channel);
                                    }
                                    VirtualChannelGroup newGroup = new VirtualChannelGroup(this, elements[2], channels);
                                    groups.Add(newGroup);
                                }
                                else
                                {
                                    groups.Add(group);
                                }
                            }
                            VirtualChannelGroups = new SoundstructureItemCollection(groups);
                        }
                        break;
                    case "val":
                        // this should be a value response from a set or get
                        {
                            try
                            {
                                if (elements[1] == "eth_settings" && elements[2] == "1")
                                {
                                    LanAdapter = new SoundstructureEthernetSettings(elements[3]);
                                    break;
                                }

                                bool commandOK = false;
                                SoundstructureCommandType commandType = SoundstructureCommandType.FADER;

                                try
                                {
                                    commandType = (SoundstructureCommandType)Enum.Parse(typeof(SoundstructureCommandType), elements[1], true);
                                    commandOK = true;
                                }
                                catch
                                {
                                    if (elements[1].StartsWith("voip_") && VirtualChannels.Contains(elements[2]))
                                    {
                                        VirtualChannel channel = VirtualChannels[elements[2]] as VirtualChannel;
                                        if (channel.IsVoip && VoipInfoReceived != null)
                                        {
                                            string info = data.Substring(data.IndexOf(channel.Name) + channel.Name.Length + 2,
                                                data.Length - data.IndexOf(channel.Name) - channel.Name.Length - 2);
                                            VoipInfoReceived(channel, new SoundstructureVoipInfoReceivedEventArgs(elements[1], info));
                                        }
                                    }
                                }

                                if (commandOK)
                                {
                                    switch (commandType)
                                    {
                                        case SoundstructureCommandType.MATRIX_MUTE:
#if DEBUG
                                            CrestronConsole.PrintLine("Matrix Mute Input: \x22{0}\x22 Output: \x22{1}\x22 Value: {2}", elements[2], elements[3], elements[4]);
#endif
                                            break;
                                        case SoundstructureCommandType.FADER:
                                            if (elements[2] == "min" || elements[2] == "max")
                                            {
                                                OnValueChange(elements[3], commandType, elements[2], Convert.ToDouble(elements[4]));
                                            }
                                            else
                                            {
                                                OnValueChange(elements[2], commandType, Convert.ToDouble(elements[3]));
                                            }
                                            break;
                                        case SoundstructureCommandType.PHONE_DIAL:
                                            // Cannot parse reply for string values and we don't currently need to track this.
                                            break;
                                        default:
                                            if (elements.Count > 3)
                                                OnValueChange(elements[2], commandType, Convert.ToDouble(elements[3]));
                                            break;
                                    }

                                    if (!Initialized && CheckAllItemsHaveInitialised())
                                    {
                                        Initialized = true;

                                        ErrorLog.Notice("Soundstructure Initialised");
                                        CrestronConsole.PrintLine("Soundstructure Initialised!");

                                        if (HasInitialised != null)
                                            HasInitialised(this);
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                ErrorLog.Error("Soundstructure Rx: {0}, Error: {1}", data, e.Message);
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
            else
            {
                if (data == "vclist")
                {
                    VirtualChannels = new SoundstructureItemCollection(_listedItems);
                    _listedItems.Clear();

                    _socket.Send("vcglist");
                }
                else if (data == "vcglist")
                {
                    VirtualChannelGroups = new SoundstructureItemCollection(_listedItems);
                    _listedItems.Clear();

                    foreach (ISoundstructureItem item in VirtualChannelGroups)
                        item.Init();

                    foreach (ISoundstructureItem item in VirtualChannels)
                    {
                        if (!ChannelIsGrouped(item))
                        {
                            item.Init();
                        }
                    }
                }
            }
        }


        private void SocketOnStatusChanged(TCPClientSocketBase socket, SocketStatusEventType eventType)
        {
            if (eventType == SocketStatusEventType.Connected)
            {
                CrestronConsole.PrintLine("Soundstructure device Connected on {0}", _socket.HostAddress);
                ErrorLog.Notice("Soundstructure device Connected on {0}", _socket.HostAddress);
                if (!Initialized)
                    Initialize();
            } else if (eventType == SocketStatusEventType.Disconnected)
            {
                DeviceCommunicating = false;
            }
        }

        public string HostAddress
        {
            get { return _socket.HostAddress; }
        }

        public void Connect()
        {
            _socket.Connect();
        }

        public void Disconnect()
        {
            _socket.Disconnect();
        }

        public bool Connected
        {
            get { return _socket.Connected; }
        }

        public string ModelName { get; private set; }

        public bool DeviceCommunicating 
        {
            get { return _deviceCommunicating; }
            protected set
            {
                if(_deviceCommunicating == value) return;

                _deviceCommunicating = value;

                OnDeviceCommunicatingChange(this, value);
            }
        }

        public string DeviceAddressString
        {
            get { return _socket.HostAddress; }
        }

        // TODO Get serial number
        public string SerialNumber
        {
            get { return "Not Implemented"; }
        }

        // TODO Get version info
        public string VersionInfo
        {
            get { return "Not Implemented"; }
        }

        public event DeviceCommunicatingChangeHandler DeviceCommunicatingChange;

        protected virtual void OnDeviceCommunicatingChange(IDevice device, bool communicating)
        {
            var handler = DeviceCommunicatingChange;
            if (handler != null) handler(device, communicating);
        }

        public void Send(string stringToSend)
        {
            _socket.Send(stringToSend);
        }

        public event SoundstructureValueChangeHandler ValueChange;

        public event SoundstructureVoipInfoReceivedHandler VoipInfoReceived;

        void OnValueChange(string name, SoundstructureCommandType commandType, double value)
        {
            ISoundstructureItem item = GetItemForName(name);
            if (ValueChange != null && item != null)
                ValueChange(item, new SoundstructureValueChangeEventArgs(commandType, value));
        }

        void OnValueChange(string name, SoundstructureCommandType commandType, string commandModifier, double value)
        {
            ISoundstructureItem item = GetItemForName(name);
            if (ValueChange != null && item != null)
                ValueChange(item, new SoundstructureValueChangeEventArgs(commandType, commandModifier, value));
        }

        ISoundstructureItem GetItemForName(string name)
        {
            try
            {
                if (VirtualChannelGroups.Contains(name))
                    return VirtualChannelGroups[name];
                else if (VirtualChannels.Contains(name))
                    return VirtualChannels[name];
            }
            catch (Exception e)
            {
                CrestronConsole.PrintLine("GetItemForName({0}) Error: {1}", e.Message);
            }
            return null;
        }

        public void OnReceive(string receivedString)
        {
        }

        private bool CheckAllItemsHaveInitialised()
        {
            foreach (ISoundstructureItem item in VirtualChannelGroups)
                if (!item.Initialised) return false;
            foreach (ISoundstructureItem item in VirtualChannels)
                if (!item.Initialised) return false;

            return true;
        }

        public event SoundstructureInitialisedCompleteEventHandler HasInitialised;

        bool ChannelIsGrouped(ISoundstructureItem channel)
        {
            foreach (VirtualChannelGroup group in VirtualChannelGroups)
            {
                if (group.Contains(channel))
                    return true;
            }
            return false;
        }

        public event SoundstructurePresetRanEventHandler PresetRan;

        public void RunPreset(string presetName)
        {
            _socket.Send(string.Format("run \"{0}\"", presetName));
        }

        public void SetMatrixMute(ISoundstructureItem rowChannel, ISoundstructureItem colChannel, bool muteValue)
        {
            Set(rowChannel, colChannel, SoundstructureCommandType.MATRIX_MUTE, muteValue);
        }

        public static double ScaleRange(double Value,
           double FromMinValue, double FromMaxValue,
           double ToMinValue, double ToMaxValue)
        {
            try
            {
                return (Value - FromMinValue) *
                    (ToMaxValue - ToMinValue) /
                    (FromMaxValue - FromMinValue) + ToMinValue;
            }
            catch
            {
                return double.NaN;
            }
        }

        public bool Set(ISoundstructureItem channel, SoundstructureCommandType type, double value)
        {
            var str = string.Format("set {0} \"{1}\" {2:0.00}", type.ToString().ToLower(),
                channel.Name, value);
            if (!Connected) return false;
            Send(str);
            return true;
        }

        public bool Set(ISoundstructureItem channel, SoundstructureCommandType type, bool value)
        {
            var str = string.Format("set {0} \"{1}\" {2}", type.ToString().ToLower(),
                channel.Name, value ? 1 : 0);
            if (!Connected) return false;
            Send(str);
            return true;
        }

        public bool Set(ISoundstructureItem rowChannel, ISoundstructureItem colChannel, SoundstructureCommandType type, bool value)
        {
            var str = string.Format("set {0} \"{1}\" \"{2}\" {3}", type.ToString().ToLower(),
                rowChannel.Name, colChannel.Name, value ? 1 : 0);
            if (!Connected) return false;
            Send(str);
            return true;
        }

        public bool Set(ISoundstructureItem channel, SoundstructureCommandType type, string value)
        {
            var str = string.Format("set {0} \"{1}\" \"{2}\"", type.ToString().ToLower(),
                channel.Name, value);
            if (!Connected) return false;
            Send(str);
            return true;
        }

        public bool Set(ISoundstructureItem channel, SoundstructureCommandType type)
        {
            var str = string.Format("set {0} \"{1}\"", type.ToString().ToLower(),
                channel.Name);
            if (!Connected) return false;
            Send(str);
            return true;
        }

        public void Get(ISoundstructureItem channel, SoundstructureCommandType type)
        {
            var str = string.Format("get {0} \"{1}\"", type.ToString().ToLower(), channel.Name);
            Send(str);

            if (type != SoundstructureCommandType.FADER) return;

            str = string.Format("get fader min \"{0}\"", channel.Name);
            Send(str);

            str = string.Format("get fader max \"{0}\"", channel.Name);
            Send(str);
        }

        string _name = "Polycom Soundstructure";
        private bool _deviceCommunicating;

        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }

        public string ManufacturerName
        {
            get { return "Polycom"; }
        }

        public string DeviceModel
        {
            get { return "Soundstructure"; }
        }

        public string DiagnosticsName
        {
            get { return DeviceModel + " (" + DeviceAddressString + ")"; }
        }

        public string DeviceSerialNumber
        {
            get { return string.Empty; }
        }
    }

    public enum SoundstructureVirtualChannelType
    {
        MONO,
        STEREO,
        CONTROL,
        CONTROL_ARRAY
    }

    public enum SoundstructurePhysicalChannelType
    {
        CR_MIC_IN,
        CR_LINE_OUT,
        SR_MIC_IN,
        SR_LINE_OUT,
        PSTN_IN,
        PSTN_OUT,
        VOIP_IN,
        VOIP_OUT,
        SIG_GEN,
        SUBMIX,
        CLINK_IN,
        CLINK_OUT,
        CLINK_AUX_IN,
        CLINK_AUX_OUT,
        CLINK_RAW_IN,
        DIGITAL_GPIO_IN,
        DIGITAL_GPIO_OUT,
        ANALOG_GPIO_IN,
        IR_IN
    }

    public enum SoundstructureCommandType
    {
        LINE_OUT_GAIN,
        FADER,
        MUTE,
        MATRIX_MUTE,
        MIC_IN_GAIN,
        SAFETY_MUTE,
        PHONE_CONNECT,
        PHONE_DIAL,
        PHONE_REJECT,
        PHONE_IGNORE,
        PHONE_RING,
        VOIP_HOLD,
        VOIP_RESUME,
        VOIP_SEND,
        VOIP_ANSWER,
        VOIP_LINE,
        VOIP_DND,
        VOIP_REBOOT,
        SYS_REBOOT
    }

    public delegate void SoundstructureValueChangeHandler(ISoundstructureItem item, SoundstructureValueChangeEventArgs args);

    public delegate void SoundstructureVoipInfoReceivedHandler(ISoundstructureItem item, SoundstructureVoipInfoReceivedEventArgs args);

    public class SoundstructureValueChangeEventArgs : EventArgs
    {
        public SoundstructureValueChangeEventArgs(SoundstructureCommandType commandType, double value)
        {
            CommandType = commandType;
            Value = value;
        }

        public SoundstructureValueChangeEventArgs(SoundstructureCommandType commandType, string commmandModifier, double value)
        {
            CommandType = commandType;
            CommandModifier = commmandModifier;
            Value = value;
        }

        public SoundstructureCommandType CommandType;
        public string CommandModifier;
        public double Value;
    }

    public class SoundstructureVoipInfoReceivedEventArgs : EventArgs
    {
        public SoundstructureVoipInfoReceivedEventArgs(string command, string info)
        {
            Command = command;
            Info = info;
        }

        public string Command;
        public string Info;
    }

    public delegate void SoundstructureInitialisedCompleteEventHandler(Soundstructure SoundStructureDevice);

    public delegate void SoundstructurePresetRanEventHandler(Soundstructure soundStructureDevice, string presetName);
}