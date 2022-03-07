using System;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.CrestronThread;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SSMono.Threading.Tasks;
using UX.Lib2.Cloud.Logger;
using UX.Lib2.DeviceSupport;
using UX.Lib2.Models;

namespace UX.Lib2.Devices.Displays.Sony
{
    public class SonyBravia : DisplayDeviceBase, IAudioLevelDevice
    {
        private readonly string _deviceAddress;
        private readonly string _psk;
        private Thread _checkStatusThread;
        private bool _programStopping;
        private bool _connectionOk;
        private string _serialNumber;
        private string _versionInfo;
        private string _macAddress;
        private string _model;
        private bool _firstFeedback = true;
        private DisplayDeviceInput _currentInput;
        private DisplayDeviceInput _requestedInput;
        private List<SonyContentSource> _availableSources; 
        private readonly AudioLevelCollection _volumeControls = new AudioLevelCollection();
        private CEvent _checkWait;
        private Thread _powerSetThread;
        private Thread _inputSetThread;
        private CTimer _checkTimer;
        private bool _threadRunning;
        private bool _inputStatusNotChecked;

        public SonyBravia(string name, string deviceAddress, string psk) : base(name)
        {
            _deviceAddress = deviceAddress;
            _psk = psk;
            CrestronEnvironment.ProgramStatusEventHandler += type =>
            {
                _programStopping = type == eProgramStatusEventType.Stopping;
                if (_programStopping && _checkWait != null)
                {
                    _checkWait.Set();
                }
            };
            _volumeControls.Add(new SonyBraviaVolumeControl(this, TargetVolumeDeviceType.Speaker));
            _volumeControls.Add(new SonyBraviaVolumeControl(this, TargetVolumeDeviceType.Headphone));
        }

        public override string ManufacturerName
        {
            get { return "Sony"; }
        }

        public override string ModelName
        {
            get { return _model; }
        }

        public override string DeviceAddressString
        {
            get { return _deviceAddress; }
        }

        public override string SerialNumber
        {
            get { return _serialNumber; }
        }

        public override string VersionInfo
        {
            get { return _versionInfo; }
        }

        public string MacAddress
        {
            get { return _macAddress; }
        }

        public override DisplayDeviceInput CurrentInput
        {
            get { return _currentInput; }
        }

        public override IEnumerable<DisplayDeviceInput> AvailableInputs
        {
            get
            {
                return _availableSources.Where(s => s.InputType != DisplayDeviceInput.Unknown).Select(s => s.InputType);
            }
        }

        public override bool SupportsDisplayUsage
        {
            get { return false; }
        }

        public AudioLevelCollection AudioLevels
        {
            get { return _volumeControls; }
        }

        public string Psk
        {
            get { return _psk; }
        }

        protected override void SetPowerFeedback(DevicePowerStatus newPowerState)
        {
            PowerStatus = newPowerState;

            if (_firstFeedback)
            {
                _firstFeedback = false;
                Power = Power;
            }
            else if(RequestedPower != Power)
            {
                ActionPowerRequest(RequestedPower);
            }
        }

        protected override void ActionPowerRequest(bool powerRequest)
        {
            if (_powerSetThread != null && _powerSetThread.ThreadState == Thread.eThreadStates.ThreadRunning)
            {
                if (powerRequest == RequestedPower)
                {
                    return;
                }
            }

            _powerSetThread = new Thread(specific =>
            {
                try
                {
                    var response =
                        SonyBraviaHttpClient.Request(_deviceAddress, _psk, "/sony/system", "setPowerStatus", "1.0",
                            new JObject
                            {
                                {"status", powerRequest}
                            }).Await();

                    if (response.Type == SonyBraviaResponseType.Success)
                    {
                        if (powerRequest && PowerStatus == DevicePowerStatus.PowerOff)
                        {
                            PowerStatus = DevicePowerStatus.PowerWarming;
                        }
                        else if (!powerRequest && PowerStatus == DevicePowerStatus.PowerOn)
                        {
                            PowerStatus = DevicePowerStatus.PowerCooling;
                        }
                    }
                    else if (response.Type == SonyBraviaResponseType.Failed)
                    {
                        CloudLog.Error("Could not set display power, Response = {0}, {1}", response.Type,
                            response.Exception.Message);
                    }
                    else
                    {
                        CloudLog.Error("Could not set display power, Response = {0}", response.Type);
                    }
                }
                catch (Exception e)
                {
                    CloudLog.Exception(e, "Error trying to set display power");
                    _connectionOk = false;
                }

                _checkWait.Set();

                return null;
            }, null);
        }

        public override void SetInput(DisplayDeviceInput input)
        {
            SetInput(input, TimeSpan.Zero);
        }

        public void SetInput(DisplayDeviceInput input, TimeSpan delay)
        {
            _requestedInput = input;

            CloudLog.Debug("{0}.SetInput({1})", GetType().Name, _requestedInput);

            if (RequestedPower != Power)
            {
                CloudLog.Debug("{0}.SetInput({1}) ... Waiting for power", GetType().Name, _requestedInput);
                return;
            }

            if (_inputSetThread != null && _inputSetThread.ThreadState == Thread.eThreadStates.ThreadRunning) return;

            _inputSetThread = new Thread(specific =>
            {
                try
                {
                    CloudLog.Debug("{0}._inputSetThread Started", GetType().Name);

                    var delayWait = (int)((TimeSpan)specific).TotalMilliseconds;

                    CloudLog.Debug("{0}._inputSetThread waiting for {1}ms", GetType().Name, delayWait);

                    Thread.Sleep(delayWait);

                    if (_availableSources == null || _availableSources.Count == 0)
                    {
                        CloudLog.Warn("{0}.SetInput - _availableSources is null or empty", GetType().Name);
                        _connectionOk = false;
                    }

                    if (!_connectionOk)
                    {
                        CloudLog.Warn(
                            "{0} not initialized properly at time of trying to set input. Alerting poll thread and waiting",
                            GetType().Name);
                        _checkWait.Set();
                        Thread.Sleep(2000);
                    }

                    if (PowerStatus == DevicePowerStatus.PowerWarming)
                    {
                        CloudLog.Debug("{0}.SetInput - device is warming up, waiting...", GetType().Name);
                        Thread.Sleep(2000);
                    }

                    CloudLog.Debug("{0}._inputSetThread finding the Uri for \"{1}\"", GetType().Name, _requestedInput);

                    var availableSource = _availableSources.FirstOrDefault(s => s.InputType == _requestedInput);
                    if (availableSource == null)
                    {
                        CloudLog.Error(
                            "Cannot set display to {0} as it's not in the list of available sources. Init error?", _requestedInput);
                        _connectionOk = false;
                        return null;
                    }

                    var uri = availableSource.Uri;

                    var response =
                        SonyBraviaHttpClient.Request(_deviceAddress, _psk, "/sony/avContent", "setPlayContent", "1.0",
                            new JObject
                            {
                                {"uri", uri}
                            }).Await();

                    if (response.Type == SonyBraviaResponseType.Success)
                    {
                        _currentInput = _requestedInput;
                    }
                    else if (response.Type == SonyBraviaResponseType.Failed)
                    {
                        CloudLog.Error("Could not set display input, Response = {0}, {1}", response.Type,
                            response.Exception.Message);
                    }
                    else
                    {
                        CloudLog.Error("Could not set display input, Response = {0}", response.Type);
                    }
                }
                catch (Exception e)
                {
                    CloudLog.Exception(e, "Error setting input for display, {0}", e.Message);
                }

                _checkWait.Set();

                return null;
            }, delay);
        }

        private object CheckStatusThread(object userSpecific)
        {
            CloudLog.Info("{0} Started status checking thread", GetType().Name);

            while (!_programStopping)
            {
                try
                {
                    if (_inputStatusNotChecked)
                    {
                        _checkWait.Wait(5000);
                    }
                    else
                    {
                        _checkWait.Wait(60000);
                    }

                    _threadRunning = true;

                    CrestronEnvironment.AllowOtherAppsToRun();
                    Thread.Sleep(0);

                    if (_programStopping) return null;

                    #region Get System Info

                    var response =
                        SonyBraviaHttpClient.Request(_deviceAddress, _psk, "/sony/system", "getSystemInformation", "1.0")
                            .Await();
                    if (response.Type != SonyBraviaResponseType.Success)
                    {
                        if (response.ConnectionFailed)
                        {
                            DeviceCommunicating = false;
                            _connectionOk = false;
                        }
                        continue;
                    }

                    try
                    {
                        var info = response.Data.First() as JObject;
                        _serialNumber = info["serial"].Value<string>();
                        _model = info["model"].Value<string>();
                        _macAddress = info["macAddr"].Value<string>();
                        _versionInfo = info["generation"].Value<string>();
                    }
                    catch (Exception e)
                    {
                        CloudLog.Exception(e, "Error in request: \"{0}\", {1}", response.RequestUri,
                            response.RequestMethod);
                    }

                    #endregion

                    #region Get Power Status

                    var previousPowerWasNotOn = PowerStatus != DevicePowerStatus.PowerOn;

                    response =
                        SonyBraviaHttpClient.Request(_deviceAddress, _psk, "/sony/system", "getPowerStatus", "1.0")
                            .Await();
                    if (response.Type != SonyBraviaResponseType.Success)
                    {
                        if (response.ConnectionFailed)
                        {
                            DeviceCommunicating = false;
                            _connectionOk = false;
                        }
                        continue;
                    }

                    try
                    {
                        var info = response.Data.First() as JObject;

                        switch (info["status"].Value<string>())
                        {
                            case "standby":
                                SetPowerFeedback(DevicePowerStatus.PowerOff);
                                break;
                            case "active":
                                SetPowerFeedback(DevicePowerStatus.PowerOn);
                                break;
                        }
                    }
                    catch (Exception e)
                    {
                        CloudLog.Exception(e, "Error in request: \"{0}\", {1}", response.RequestUri,
                            response.RequestMethod);
                        continue;
                    }

                    #endregion

                    DeviceCommunicating = true;

                    if (PowerStatus == DevicePowerStatus.PowerOn)
                    {
                        #region Get Input Status

                        response =
                            SonyBraviaHttpClient.Request(_deviceAddress, _psk, "/sony/avContent",
                                "getPlayingContentInfo", "1.0").Await();
                        if (response.Type != SonyBraviaResponseType.Success)
                        {
                            CloudLog.Warn("Error trying to get input status from Sony Display, will try again in 5 seconds");
                            _inputStatusNotChecked = true;
                            continue;
                        }

                        try
                        {
                            var info = response.Data.First() as JObject;

                            var uri = info["uri"].Value<string>();

                            if (_availableSources != null && _availableSources.Any(s => s != null && s.Uri == uri))
                            {
                                _currentInput = _availableSources.First(s => s.Uri == uri).InputType;

                                if (_requestedInput != DisplayDeviceInput.Unknown && _currentInput != _requestedInput)
                                {
                                    SetInput(_requestedInput,
                                        previousPowerWasNotOn ? TimeSpan.FromSeconds(2) : TimeSpan.Zero);
                                }
                                else if (_requestedInput == _currentInput)
                                {
                                    _requestedInput = DisplayDeviceInput.Unknown;
                                }
                            }
                            else
                            {
                                _currentInput = DisplayDeviceInput.Unknown;
                            }

                            _inputStatusNotChecked = false;
                        }
                        catch (Exception e)
                        {
                            CloudLog.Exception(e, "Error in request: \"{0}\", {1}", response.RequestUri,
                                response.RequestMethod);
                        }

                        #endregion

                        #region Get Volume Level

                        response =
                            SonyBraviaHttpClient.Request(_deviceAddress, _psk, "/sony/audio", "getVolumeInformation",
                                "1.0").Await();
                        if (response.Type != SonyBraviaResponseType.Success)
                        {
                            continue;
                        }

                        try
                        {
                            foreach (var token in response.Data.First())
                            {
                                var info = token as JObject;

                                var type =
                                    (TargetVolumeDeviceType)
                                        Enum.Parse(typeof (TargetVolumeDeviceType), info["target"].Value<string>(), true);

                                var currentLevel = info["volume"].Value<int>();
                                var minLevel = info["minVolume"].Value<int>();
                                var maxLevel = info["maxVolume"].Value<int>();
                                var mute = info["mute"].Value<bool>();

                                var control =
                                    _volumeControls.Cast<SonyBraviaVolumeControl>()
                                        .FirstOrDefault(c => c.TargetType == type);

                                if (control == null) continue;

                                control.VolumeMin = minLevel;
                                control.VolumeMax = maxLevel;
                                control.InternalLevel = currentLevel;
                                control.InternalMute = mute;
                            }
                        }
                        catch (Exception e)
                        {
                            CloudLog.Exception(e, "Error in request: \"{0}\", {1}", response.RequestUri,
                                response.RequestMethod);
                        }

                        #endregion
                    }

                    if (_connectionOk) continue;

                    CloudLog.Info("{0} at {1} will now get status. Either failed previously or first connect",
                        GetType().Name, DeviceAddressString);

                    #region Get Input and Source Schemes

                    var schemes = new List<string>();

                    response =
                        SonyBraviaHttpClient.Request(_deviceAddress, _psk, "/sony/avContent", "getSchemeList", "1.0")
                            .Await();
                    if (response.Type != SonyBraviaResponseType.Success)
                    {
                        CloudLog.Error("{0} could not get SchemeList, {1}", GetType().Name, response.Type);
                        continue;
                    }

                    try
                    {
                        var info = response.Data.First() as JArray;

                        schemes.AddRange(info.Select(token => token as JObject).Select(o => o["scheme"].Value<string>()));

                        if (schemes.Count == 0)
                        {
                            CloudLog.Warn("Received empty array for {0} getSchemeList, moving to generate defaults",
                                GetType().Name);
                            goto ErrorGettingInputs;
                        }
                    }
                    catch (Exception e)
                    {
                        CloudLog.Exception(e, "Error in request: \"{0}\", {1}", response.RequestUri,
                            response.RequestMethod);
                        continue;
                    }

                    var sources = new Dictionary<string, List<string>>();

                    foreach (var scheme in schemes)
                    {
                        response =
                            SonyBraviaHttpClient.Request(_deviceAddress, _psk, "/sony/avContent", "getSourceList", "1.0",
                                new JObject
                                {
                                    {"scheme", scheme}
                                }).Await();

                        if (response.Type != SonyBraviaResponseType.Success)
                        {
                            CloudLog.Warn("{0} could not get SourceList for scheme \"{1}\", {2}",
                                GetType().Name, scheme, response.Type);
                            continue;
                        }

                        try
                        {
                            var info = response.Data.First() as JArray;

                            var list = new List<string>();
                            sources[scheme] = list;
                            list.AddRange(info.Select(token => token as JObject)
                                .Select(o => o["source"].Value<string>()));
                        }
                        catch (Exception e)
                        {
                            CloudLog.Exception(e, "Error in request: \"{0}\", {1}", response.RequestUri,
                                response.RequestMethod);
                        }
                    }

                    var newContentList = new List<SonyContentSource>();

                    foreach (
                        var source in
                            sources.Where(k => k.Key != "fav").Select(k => k.Value).SelectMany(scheme => scheme))
                    {
                        response =
                            SonyBraviaHttpClient.Request(_deviceAddress, _psk, "/sony/avContent", "getContentList",
                                "1.0", new JObject
                                {
                                    {"source", source}
                                }).Await();

                        if (response.Type != SonyBraviaResponseType.Success)
                        {
                            CloudLog.Warn("{0} could not get ConntentList for source \"{1}\", {2}",
                                GetType().Name, source, response.Type);
                            continue;
                        }

                        try
                        {
                            var info = response.Data.First() as JArray;

                            foreach (var token in info)
                            {
                                try
                                {
                                    var input = new SonyContentSource(token as JObject);
                                    newContentList.Add(input);
                                    Debug.WriteInfo("Sony content source", input.ToString());
                                }
                                catch (Exception e)
                                {
                                    CloudLog.Exception(e, "Error in request: \"{0}\", {1}", response.RequestUri,
                                        response.RequestMethod);
                                }
                            }

                            goto FoundInputs;
                        }
                        catch (Exception e)
                        {
                            CloudLog.Exception(e, "Error in request: \"{0}\", {1}", response.RequestUri,
                                response.RequestMethod);
                        }
                    }

                    ErrorGettingInputs:
                    CloudLog.Warn("Error getting input schemes for {0}, Generating default HDMI inputs", GetType().Name);
                    _availableSources = new List<SonyContentSource>()
                    {
                        new SonyContentSource("extInput:hdmi?port=1", "HDMI 1", 0),
                        new SonyContentSource("extInput:hdmi?port=2", "HDMI 2", 1),
                        new SonyContentSource("extInput:hdmi?port=3", "HDMI 3", 2),
                        new SonyContentSource("extInput:hdmi?port=4", "HDMI 4", 3)
                    };
                    goto InputsDone;

                    FoundInputs:
                    _availableSources = newContentList;

                    InputsDone:
                    Debug.WriteSuccess("Available inputs");
                    foreach (var source in _availableSources)
                    {
                        Debug.WriteNormal(source.ToString());
                    }

                    #endregion

                    _connectionOk = true;
                }
                catch (Exception e)
                {
                    CloudLog.Error("Error in {0}, message = {1}", Thread.CurrentThread.Name, e.Message);
                }
            }

            return null;
        }

        public override void Initialize()
        {
            if(_checkTimer != null) return;

            _checkTimer = new CTimer(CheckStatusTimer, null, 1000, (long) TimeSpan.FromMinutes(5).TotalMilliseconds);
        }

        private void StartCheckingThread()
        {
            if (_checkStatusThread != null && _checkStatusThread.ThreadState == Thread.eThreadStates.ThreadRunning)
                return;
            _checkWait = new CEvent();
            CloudLog.Info("{0} Starting status checking thread", GetType().Name);
            _checkStatusThread = new Thread(CheckStatusThread, null)
            {
                Name = "Sony Bravia Polling"
            };
        }

        private void CheckStatusTimer(object userSpecific)
        {
            if (!_threadRunning)
            {
                if (_checkStatusThread != null)
                {
                    CloudLog.Warn("{0} Status Checking Thread is not responding. Aborting and Restarting!", GetType().Name);
                    _checkStatusThread.Abort();
                    _checkStatusThread = null;
                }

                StartCheckingThread();
                return;
            }

            _threadRunning = false;
        }
    }

    public class SonyContentSource
    {
        private readonly string _uri;
        private readonly string _title;
        private readonly int _index;

        internal SonyContentSource(JObject inputData)
        {
            _uri = inputData["uri"].ToObject<string>();
            _title = inputData["title"].ToObject<string>();
            _index = inputData["index"].ToObject<int>();
        }

        internal SonyContentSource(string uri, string title, int index)
        {
            _uri = uri;
            _title = title;
            _index = index;
        }

        public string Uri
        {
            get { return _uri; }
        }

        public string Title
        {
            get { return _title; }
        }

        public int Index
        {
            get { return _index; }
        }

        public DisplayDeviceInput InputType
        {
            get
            {
                try
                {
                    if (Uri.StartsWith("extInput:hdmi"))
                    {
                        switch (Index)
                        {
                            case 0:
                                return DisplayDeviceInput.HDMI1;
                            case 1:
                                return DisplayDeviceInput.HDMI2;
                            case 2:
                                return DisplayDeviceInput.HDMI3;
                            case 3:
                                return DisplayDeviceInput.HDMI4;
                        }
                    }

                    if (Uri.StartsWith("extInput:widi"))
                    {
                        return DisplayDeviceInput.Wireless;
                    }

                    if (Uri.StartsWith("extInput:airPlay"))
                    {
                        return DisplayDeviceInput.AirPlay;
                    }

                    if (Uri.StartsWith("extInput:component"))
                    {
                        return DisplayDeviceInput.YUV;
                    }

                    if (Uri.StartsWith("extInput:composite"))
                    {
                        return DisplayDeviceInput.Composite;
                    }
                }
                catch (Exception e)
                {
                    CloudLog.Exception(e);
                }

                return DisplayDeviceInput.Unknown;
            }
        }

        public override string ToString()
        {
            return string.Format("Input, Uri:\"{0}\" {1} \"{2}\"", Uri, InputType, Title);
        }
    }
}