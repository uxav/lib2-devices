 
using System;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.CrestronThread;
using Newtonsoft.Json.Linq;
using SSMono;
using SSMono.Net;
using SSMono.Net.Http;
using SSMono.Threading.Tasks;
using UX.Lib2.Cloud.Logger;
using UX.Lib2.DeviceSupport;

namespace UX.Lib2.Devices.AJA
{
    public class KiProRecorder : IDevice, ITransportDevice
    {
        private readonly string _ipAddress;
        private readonly HttpClient _client;
        private int _connectionId;
        private Thread _connectionThread;
        private CEvent _connectionWait = new CEvent();
        private bool _programStopping;
        private bool _connectionActive;
        private ETransportState _transportState;
        private bool _deviceCommunicating;
        private string _timeCode = "00:00:00:00";
        private string _currentClip = string.Empty;

        public KiProRecorder(string ipAddress)
        {
            _ipAddress = ipAddress;
            _client = new HttpClient()
            {
                Timeout = TimeSpan.FromSeconds(10)
            };
            CrestronEnvironment.ProgramStatusEventHandler += type =>
            {
                _programStopping = type == eProgramStatusEventType.Stopping;
                _connectionWait.Set();
            };
        }

        public event DeviceCommunicatingChangeHandler DeviceCommunicatingChange;

        public event KiProTransportStateChangeEventHandler TransportModeChanged;

        public bool Playing
        {
            get { return _transportState == ETransportState.PlayingForward; }
        }

        public bool Stopped
        {   
            get { return _transportState == ETransportState.Idle; }
        }

        public bool Paused
        {
            get { return _transportState == ETransportState.Paused; }
        }

        public bool Recording
        {
            get { return _transportState == ETransportState.Recording; }
        }

        public string TimeCode
        {
            get { return _timeCode; }
        }

        private void Set(string paramId, object value)
        {
            var uri = new UriBuilder("http", _ipAddress, 80, string.Format("config"))
            {
                Query = string.Format("action=set&paramid={0}&value={1}", paramId, value)
            };
#if true
            CloudLog.Debug("Request to KiPro: GET {0}", uri.Uri.ToString());
#endif
            var task = _client.GetAsync(uri.Uri);
            task.ContinueWith(delegate(Task<HttpResponseMessage> task1)
            {
                var response = task1.Await();
#if true
                CloudLog.Debug("Response from KiPro: Code {0}", response.StatusCode);
#endif
                var transportState = int.Parse(Get("eParamID_TransportState"));
                OnTransportModeChanged(this);
            });
        }

        private string Get(string paramId)
        {
            var uri = new UriBuilder("http", _ipAddress, 80, string.Format("config"))
            {
                Query = string.Format("action=get&paramid={0}", paramId)
            };
#if true
            CloudLog.Debug("Request to KiPro: GET {0}", uri.Uri.ToString());
#endif
            var task = _client.GetAsync(uri.Uri);
            var response = task.Await();
#if true
            CloudLog.Debug("Response from KiPro: Code {0}", response.StatusCode);
#endif
            response.EnsureSuccessStatusCode();

            var readTask = response.Content.ReadAsStringAsync();
            var content = readTask.Await();
#if true
            CloudLog.Debug("Response content:\r\n{0}", content);
#endif
            var json = JToken.Parse(content);

            return json["value"].Value<string>();
        }

        public void ConnectionStart()
        {
            if (_connectionId > 0 || (_connectionThread != null && _connectionThread.ThreadState == Thread.eThreadStates.ThreadRunning)) return;
            _connectionActive = true;
            _connectionWait.Reset();
            CloudLog.Notice("{0} Initialize() called", GetType().Name);
            _connectionThread = new Thread(specific =>
            {
                while (_connectionActive && !_programStopping)
                {
                    var suppressErrors = false;

                    UriBuilder uri;
                    Task<HttpResponseMessage> task;
                    HttpResponseMessage response;
                    Task<string> readTask;
                    string content;
                    JToken json;

                    while (_connectionId == 0)
                    {
                        try
                        {
                            uri = new UriBuilder("http", _ipAddress, 80, string.Format("config"))
                            {
                                Query = "action=connect"
                            };

                            Debug.WriteInfo("Trying to connect to " + uri.Uri);

                            task = _client.GetAsync(uri.Uri);
                            response = task.Await();

                            response.EnsureSuccessStatusCode();

                            readTask = response.Content.ReadAsStringAsync();
                            content = readTask.Await();
#if DEBUG
                            Debug.WriteInfo("Response content:\r\n" + content);
#endif
                            json = JToken.Parse(content);

                            _connectionId = int.Parse(json["connectionid"].Value<string>());
                            CloudLog.Debug("{0} connected and received conenction id of \"{1}\"", GetType().Name,
                                _connectionId);

                            try
                            {
                                var eventInfo = json["configevents"].First;
                                _currentClip = eventInfo["eParamID_CurrentClip"].Value<string>();
                                _transportState =
                                    (ETransportState) int.Parse(eventInfo["eParamID_TransportState"].Value<string>());
                                OnTransportModeChanged(this);
                            }
                            catch (Exception e)
                            {
                                Debug.WriteWarn("Could not parse configevents on connect, {0}", e.Message);
                            }
                        }
                        catch (Exception e)
                        {
                            if (!suppressErrors)
                            {
                                suppressErrors = true;
                                CloudLog.Error("Error trying to connect to {0}, {1}", GetType().Name, e.Message);
                                CloudLog.Warn("{0} will try again in 5 seconds", GetType().Name);
                            }

                            _connectionWait.Wait(5000);

                            DeviceCommunicating = false;
                        }
                    }

                    DeviceCommunicating = true;

                    uri = new UriBuilder("http", _ipAddress, 80, string.Format("config"))
                    {
                        Query = "action=get&paramid=eParamID_TransportState"
                    };

                    Debug.WriteInfo("Trying to connect to " + uri.Uri);

                    task = _client.GetAsync(uri.Uri);
                    response = task.Await();

                    response.EnsureSuccessStatusCode();
                    readTask = response.Content.ReadAsStringAsync();
                    content = readTask.Await();
#if DEBUG
                    Debug.WriteWarn("Response content:\r\n" + content);
#endif
                    json = JToken.Parse(content);

                    TransportState = (ETransportState)int.Parse(json["value"].Value<string>());

                    while (_connectionActive)
                    {
                        var timeStamp = (Int32) (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

                        uri = new UriBuilder("http", _ipAddress, 80, string.Format("config"))
                        {
                            Query =
                                string.Format("action=wait_for_config_events&connectionid={0}&_={1}", _connectionId,
                                    timeStamp)
                        };

                        Debug.WriteInfo("Trying to connect to " + uri.Uri);

                        task = _client.GetAsync(uri.Uri);
                        response = task.Await();

                        if (response.StatusCode == HttpStatusCode.Gone)
                        {
                            _connectionId = 0;
                            break;
                        }

                        response.EnsureSuccessStatusCode();
                        readTask = response.Content.ReadAsStringAsync();
                        content = readTask.Await();
#if DEBUG
                        Debug.WriteWarn("Response content:\r\n" + content);
#endif
                        json = JToken.Parse(content);

                        foreach (var item in json)
                        {
                            var paramId = item["param_id"].Value<string>();
                            switch (paramId)
                            {
                                case "eParamID_DisplayTimecode":
                                    _timeCode = item["str_value"].Value<string>();
                                    break;
                                case "eParamID_CurrentClip":
                                    _currentClip = item["str_value"].Value<string>();
                                    break;
                                case "eParamID_TransportState":
                                    _transportState = (ETransportState) item["int_value"].Value<int>();
                                    break;
                            }
                        }

                        OnTransportModeChanged(this);

                        CrestronEnvironment.AllowOtherAppsToRun();

                        _connectionWait.Wait(100);
                    }
                }

                _connectionId = 0;

                return null;
            }, null);
        }

        public void ConnectionStop()
        {
            _connectionActive = false;
            _connectionWait.Set();
        }

        public void Play()
        {
            SendCommand(TransportDeviceCommand.Play);
        }

        public void Stop()
        {
            SendCommand(TransportDeviceCommand.Stop);
        }

        public void Pause()
        {
            SendCommand(TransportDeviceCommand.Pause);
        }

        public void Record()
        {
            SendCommand(TransportDeviceCommand.Record);
        }

        public void SkipForward()
        {
            SendCommand(TransportDeviceCommand.SkipForward);            
        }

        public void SkipBack()
        {
            SendCommand(TransportDeviceCommand.SkipBack);            
        }

        public void SendCommand(TransportDeviceCommand command)
        {
            switch (command)
            {
                case TransportDeviceCommand.Play:
                    Set("eParamID_TransportCommand", 1);
                    break;
                case TransportDeviceCommand.Stop:
                    Set("eParamID_TransportCommand", 4);
                    break;
                case TransportDeviceCommand.Record:
                    Set("eParamID_TransportCommand", 3);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        public void SendCommandPress(TransportDeviceCommand command)
        {
            throw new NotImplementedException();
        }

        public void SendCommandRelease(TransportDeviceCommand command)
        {
            throw new NotImplementedException();
        }

        public ETransportState TransportState
        {
            get { return _transportState; }
            private set
            {
                if(_transportState == value) return;

                _transportState = value;

                OnTransportModeChanged(this);
            }
        }

        public string CurrentClip
        {
            get { return _currentClip; }
        }

        public enum ETransportState
        {
            Unknown = 0,
            Idle = 1,
            Recording = 2,
            PlayingForward = 3,
            FastForward = 4,
            PlayingReverse = 9,
            Paused = 15,
        }

        public string Name
        {
            get { return "KiPro Recorder"; }
        }

        public string ManufacturerName
        {
            get { return "AJA"; }
        }

        public string ModelName
        {
            get { return Name; }
        }

        public bool DeviceCommunicating
        {
            get { return _deviceCommunicating; }
            private set
            {
                if(_deviceCommunicating == value) return;

                _deviceCommunicating = value;

                OnDeviceCommunicatingChange(this, value);
            }
        }

        protected virtual void OnDeviceCommunicatingChange(IDevice device, bool communicating)
        {
            var handler = DeviceCommunicatingChange;
            if (handler == null) return;
            try
            {
                handler(device, communicating);
            }
            catch (Exception e)
            {
                CloudLog.Exception(e);
            }
        }

        public string DeviceAddressString
        {
            get { return _ipAddress; }
        }

        public string DiagnosticsName
        {
            get { return Name + " (" + DeviceAddressString + ")"; }
        }

        public string SerialNumber { get; private set; }
        public string VersionInfo { get; private set; }

        protected virtual void OnTransportModeChanged(KiProRecorder recorder)
        {
            var handler = TransportModeChanged;
            if (handler == null) return;
            try
            {
                handler(recorder);
            }
            catch (Exception e)
            {
                CloudLog.Exception(e);
            }
        }
    }

    public delegate void KiProTransportStateChangeEventHandler(KiProRecorder recorder);
}