 
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Crestron.SimplSharp.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UX.Lib2.Cloud.Logger;
using UX.Lib2.DeviceSupport;

namespace UX.Lib2.Devices.Audio.Biamp
{
    public class Tesira : IFusionAsset, IEnumerable<TesiraBlockBase>
    {
        private readonly TtpSshClient _client;
        private string _name = "Biamp Tesira";
        private string _modelName = "Tesira";
        private string _serialNumber = "Unknown";
        private string _versionInfo = "Unknown";
        private bool _deviceCommunicating;
        private readonly Dictionary<string, TesiraBlockBase> _controls = new Dictionary<string, TesiraBlockBase>(); 

        public Tesira(string address, string username, string password)
        {
            _client = new TtpSshClient(address, username, password);
            _client.ReceivedData += ClientOnReceivedData;
            _client.ConnectionStatusChange += ClientOnConnectionStatusChange;
        }

        public Tesira(string address)
            : this(address, "default", "default")
        {

        }

        public TesiraBlockBase this[string instanceId]
        {
            get { return _controls[instanceId]; }
        }

        public string Name
        {
            get { return _name; }
        }

        public string ManufacturerName
        {
            get { return "Biamp"; }
        }

        public string ModelName
        {
            get { return _modelName; }
        }

        public bool DeviceCommunicating
        {
            get { return _deviceCommunicating; }
            private set
            {
                if(_deviceCommunicating == value) return;
                
                _deviceCommunicating = value;

                if (DeviceCommunicatingChange == null) return;
                try
                {
                    DeviceCommunicatingChange(this, _deviceCommunicating);
                }
                catch (Exception e)
                {
                    CloudLog.Exception(e, "Error calling event handler");
                }
            }
        }

        public string DeviceAddressString
        {
            get { return _client.DeviceAddress; }
        }

        public string DiagnosticsName
        {
            get { return Name + " (" + DeviceAddressString + ")"; }
        }

        public string SerialNumber
        {
            get { return _serialNumber; }
        }

        public string VersionInfo
        {
            get { return _versionInfo; }
        }

        internal Dictionary<string, TesiraBlockBase> Controls
        {
            get { return _controls; }
        }

        public event DeviceCommunicatingChangeHandler DeviceCommunicatingChange;
        public event TtpSshClientReceivedDataEventHandler ReceivedData;

        internal static string FormatBaseMessage(string instanceTag, TesiraCommand command, TesiraAttributeCode attributeCode)
        {
            return string.Format("{0} {1} {2}", instanceTag, command.ToString().ToLower(), attributeCode.ToCommandString());
        }

        internal static string FormatBaseMessage(string instanceTag, TesiraCommand command)
        {
            return string.Format("{0} {1}", instanceTag, command.ToCommandString());
        }

        public void Send(string data)
        {
            _client.Send(data);
        }

        public void Send(string instanceTag, TesiraCommand command, TesiraAttributeCode attributeCode)
        {
            _client.Send(FormatBaseMessage(instanceTag, command, attributeCode));
        }

        public void Send(string instanceTag, TesiraCommand command, TesiraAttributeCode attributeCode, uint[] indexes)
        {
            var message = FormatBaseMessage(instanceTag, command, attributeCode);
            message = indexes.Aggregate(message, (current, index) => current + " " + index);
            _client.Send(message);
        }

        public void Send(string instanceTag, TesiraCommand command, TesiraAttributeCode attributeCode, uint[] indexes, string value)
        {
            var message = FormatBaseMessage(instanceTag, command, attributeCode);
            message = indexes.Aggregate(message, (current, index) => current + " " + index);
            message = message + " \"" + value + "\"";
            _client.Send(message);
        }

        public void Send(string instanceTag, TesiraCommand command, TesiraAttributeCode attributeCode, uint[] indexes, int value)
        {
            var message = FormatBaseMessage(instanceTag, command, attributeCode);
            message = indexes.Aggregate(message, (current, index) => current + " " + index);
            message = message + " " + value;
            _client.Send(message);
        }

        public void Send(string instanceTag, TesiraCommand command, TesiraAttributeCode attributeCode, uint[] indexes, double value)
        {
            var message = FormatBaseMessage(instanceTag, command, attributeCode);
            message = indexes.Aggregate(message, (current, index) => current + " " + index);
            message = message + " " + value.ToString("F1");
            _client.Send(message);
        }

        public void Send(string instanceTag, TesiraCommand command, TesiraAttributeCode attributeCode, uint[] indexes, bool value)
        {
            var message = FormatBaseMessage(instanceTag, command, attributeCode);
            message = indexes.Aggregate(message, (current, index) => current + " " + index);
            message = message + " " + value.ToString().ToLower();
            _client.Send(message);
        }

        public void Send(string instanceTag, TesiraCommand command, uint[] indexes)
        {
            var message = FormatBaseMessage(instanceTag, command);
            message = indexes.Aggregate(message, (current, index) => current + " " + index);
            _client.Send(message);
        }

        public void Send(string instanceTag, TesiraCommand command, uint[] indexes, string value)
        {
            var message = FormatBaseMessage(instanceTag, command);
            message = indexes.Aggregate(message, (current, index) => current + " " + index);
            message = message + " \"" + value + "\"";
            _client.Send(message);
        }

        public void Send(string instanceTag, TesiraCommand command, uint[] indexes, int value)
        {
            var message = FormatBaseMessage(instanceTag, command);
            message = indexes.Aggregate(message, (current, index) => current + " " + index);
            message = message + " " + value;
            _client.Send(message);
        }

        public void Send(string instanceTag, TesiraCommand command, uint[] indexes, double value)
        {
            var message = FormatBaseMessage(instanceTag, command);
            message = indexes.Aggregate(message, (current, index) => current + " " + index);
            message = message + " " + value.ToString("F1");
            _client.Send(message);
        }

        public void Send(string instanceTag, TesiraCommand command, uint[] indexes, bool value)
        {
            var message = FormatBaseMessage(instanceTag, command);
            message = indexes.Aggregate(message, (current, index) => current + " " + index);
            message = message + " " + value.ToString().ToLower();
            _client.Send(message);
        }

        private void ClientOnConnectionStatusChange(TtpSshClient client, TtpSshClient.ClientStatus status)
        {
            switch (status)
            {
                case TtpSshClient.ClientStatus.Connected:
                    Send("DEVICE", TesiraCommand.Get, TesiraAttributeCode.NetworkStatus);
                    Send("SESSION", TesiraCommand.Get, TesiraAttributeCode.Aliases);
                    DeviceCommunicating = true;
                    break;
                case TtpSshClient.ClientStatus.Disconnected:
                    DeviceCommunicating = false;
                    break;
            }
        }

        private void ClientOnReceivedData(TtpSshClient client, TesiraMessage message)
        {
            try
            {
                JToken json;
                switch (message.Type)
                {
                    case TesiraMessageType.OkWithResponse:
                        var response = message as TesiraResponse;
                        if (response == null) return;
                        if (response.Command == "DEVICE get networkStatus")
                        {
                            json = response.TryParseResponse();
                            if (json != null)
                            {
                                Debug.WriteSuccess("Network Config", "\r\n" + json.ToString(Formatting.Indented));
                            }
                        }
                        break;
                    case TesiraMessageType.Notification:
                        json = message.TryParseResponse();
                        if (json != null)
                        {
                            Debug.WriteSuccess(json.ToString(Formatting.Indented));
                        }
                        break;
                    default:
                        break;
                }
            }
            catch (Exception e)
            {
                CloudLog.Exception(e, "Error handling received data");
            }

            if (ReceivedData == null) return;
            try
            {
                ReceivedData(client, message);
            }
            catch (Exception e)
            {
                CloudLog.Exception(e, "Error calling event handler");
            }
        }

        public TesiraBlockBase RegisterControlBlock(TesiraBlockType type, string instanceId)
        {
            var blockType = typeof(TesiraBlockBase).GetCType().Assembly
                .GetType("UX.Lib2.Devices.Audio.Biamp.ControlBlocks." + type);
            var ctor = blockType.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null,
                new CType[] {typeof (Tesira), typeof (string)}, null);
            return (TesiraBlockBase) ctor.Invoke(new object[] {this, instanceId});
        }

        public bool HasControlWithInstanceId(string instanceId)
        {
            return _controls.ContainsKey(instanceId);
        }

        public void Initialize()
        {
            _client.Connect();
        }

        internal static string FixJsonData(string jsonData)
        {
#if DEBUG
            Debug.WriteInfo("Trying to fix json with Regex");
#endif
            var fix = Regex.Replace(jsonData, @""":(?![:\d])(?!true|false)([\w]+)", @""":""$1""");
            fix = Regex.Replace(fix, @"(""|: *[\d\-\.]+|: *null|}|\]|: *true|: *false) ([""{\[])(?![:,}])", @"$1,$2");
            fix = Regex.Replace(fix, @"(\w)? ([\w\-\.])", @"$1,$2");
            fix = "{" + fix + "}";
#if DEBUG
            Debug.WriteNormal(Debug.AnsiYellow + fix + Debug.AnsiReset);
#endif
            return fix;
        }

        public IEnumerator<TesiraBlockBase> GetEnumerator()
        {
            return _controls.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public FusionAssetType AssetType
        {
            get { return FusionAssetType.AudioProcessor; }
        }
    }

    public static class TesiraExtenstions
    {
        public static string ToCommandString(this TesiraAttributeCode attribute)
        {
            var str = attribute.ToString();
            return str.Substring(0, 1).ToLower() + str.Substring(1, str.Length - 1);
        }

        public static string ToCommandString(this TesiraCommand command)
        {
            var str = command.ToString();
            return str.Substring(0, 1).ToLower() + str.Substring(1, str.Length - 1);
        }
    }

    public enum TesiraCommand
    {
        Get,
        Set,
        Increment,
        Decrement,
        Toggle,
        Subscribe,
        Unsubscribe,
        Dial,
        OnHook,
        OffHook,
        End,
        Answer,
        RecallPreset
    }

    public enum TesiraAttributeCode
    {
        Unknown,
        Verbose,
        Aliases,
        Ganged,
        Label,
        Level,
        Levels,
        MaxLevel,
        MinLevel,
        Mute,
        Mutes,
        NumChannels,
        RampInterval,
        RampStep,
        UseRamping,
        NetworkStatus,
        CallState,
        DisplayNameLabel,
        LineLabel,
        State,
        NumInputs,
        NumOutputs,
        NumSources,
        SourceSelection,
        StereoEnable,
        Gain
    }
}