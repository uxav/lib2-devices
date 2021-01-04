using System;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UX.Lib2.Cloud.Logger;
using UX.Lib2.DeviceSupport;

namespace UX.Lib2.Devices.Exterity
{
    public class Receiver : ISourceDevice, IFusionAsset
    {
        #region Fields

        private readonly AvediaServer _server;
        private bool _deviceCommunicating;
        private ReceiverMode _mode;
        private string _username = "admin";
        private string _password = "labrador";
        private int _fails = 0;
        private CTimer _timer;
        private int _pollCount;
        private bool _initialized;

        #endregion

        #region Constructors

        /// <summary>
        /// The default Constructor.
        /// </summary>
        internal Receiver(AvediaServer server, JToken data)
        {
#if DEBUG
            CrestronConsole.PrintLine("{0}.ctor()...", GetType());
#endif
            _server = server;
            Id = data["id"].Value<string>();
            UpdateInfo(data);
            //GetCurrentChannel();
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events

        public event CurrentChannelUriChangeHandler CurrentChannelUriChanged;
        public event CurrentModeChangeHandler CurrentModeChanged;

        public event DeviceCommunicatingChangeHandler DeviceCommunicatingChange;

        #endregion

        #region Delegates
        #endregion

        #region Properties

        public string Id { get; internal set; }
        public string Name { get; internal set; }

        public string ManufacturerName
        {
            get { return "Exterity"; }
        }

        public string ModelName
        {
            get { return "IPTV Receiver"; }
        }

        public string DiagnosticsName
        {
            get { return "Exterity Receiver (" + DeviceAddressString + ")"; }
        }

        public bool DeviceCommunicating
        {
            get { return _deviceCommunicating; }
            private set
            {
                if(_deviceCommunicating == value) return;

                _deviceCommunicating = value;

                try
                {
                    if (DeviceCommunicatingChange != null)
                    {
                        DeviceCommunicatingChange(this, _deviceCommunicating);
                    }
                }
                catch (Exception e)
                {
                    CloudLog.Exception(e);
                }
            }
        }

        public string DeviceAddressString
        {
            get { return IpAddress; }
        }

        public string SerialNumber
        {
            get { return MacAddress; }
        }

        // Todo get version info for receiver
        public string VersionInfo
        {
            get { return "Unknown"; }
        }

        public string Location { get; internal set; }
        public string MacAddress { get; internal set; }
        public string IpAddress { get; internal set; }
        public string CurrentChannelUri { get; internal set; }

        public string Username
        {
            get { return _username; }
            set { _username = value; }
        }

        public string Password
        {
            get { return _password; }
            set { _password = value; }
        }

        public ReceiverMode Mode
        {
            get { return _mode; }
            internal set
            {
                if(_mode == value) return;
#if DEBUG
                CrestronConsole.PrintLine("{0}.Mode set to {1}", GetType().Name, value.ToString());
#endif
                _mode = value;
                OnCurrentModeChanged(this, _mode);
            }
        }

        public AvediaServer Server
        {
            get { return _server; }
        }

        #endregion

        #region Methods

        private void GetRequest(string uri, HTTPClientResponseCallback callback)
        {
            var request = new ServerRequest(IpAddress, uri, callback);
            var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes(_username + ":" + _password));
            request.Header.AddHeader(new HttpHeader("Authorization", "Basic " + auth));
            _server.GetRequest(request);
        }

        private void PostRequest(string uri, string data, HTTPClientResponseCallback callback)
        {
            var request = new ServerRequest(IpAddress, uri, callback) { RequestType = RequestType.Post };
            var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes(_username + ":" + _password));
            request.Header.AddHeader(new HttpHeader("Authorization", "Basic " + auth));
            request.Header.ContentType = "application/json";
            request.ContentString = data;
            _server.GetRequest(request);
        }

        private void PollTimerProcess(object userSpecific)
        {
            _pollCount++;
            if (_pollCount != 60) return;

            _pollCount = 0;
            GetCurrentMode();
            GetCurrentChannel();
        }

        public void GetCurrentMode()
        {
            GetRequest("/cgi-bin/json_xfer?currentMode=true", (userobj, error) =>
            {
                try
                {
                    if (error != HTTP_CALLBACK_ERROR.COMPLETED || userobj == null)
                    {
                        CheckComms(false);
                        return;
                    }

                    try
                    {
                        var data = JToken.Parse(userobj.ContentString);
#if DEBUG
                        CrestronConsole.PrintLine("{0} received response:", GetType().Name);
                        CrestronConsole.PrintLine(data.ToString(Formatting.Indented));
#endif
                        Mode = (ReceiverMode)
                            Enum.Parse(typeof (ReceiverMode), data["currentMode"]["value"].Value<string>(), true);

                        CheckComms(true);
                    }
                    catch
                    {
                        CloudLog.Warn("Exterity GetCurrentMode() failed to parse content, Response Code = {0}",
                            userobj.Code);
                        CheckComms(false);
                    }
                }
                catch (Exception e)
                {
                    CloudLog.Exception(e);
                }
            });
        }

        public void SetCurrentMode(ReceiverMode mode)
        {
            var json = string.Format("{{ \"params\": {{ \"currentMode\": \"{0}\" }} }}", mode.ToString().ToLower());

            Mode = mode;

            PostRequest("/cgi-bin/json_xfer", json, (userobj, error) =>
            {
                if (error != HTTP_CALLBACK_ERROR.COMPLETED || userobj == null)
                {
                    CheckComms(false);
                    return;
                }

                try
                {
                    var data = JToken.Parse(userobj.ContentString);
#if DEBUG
                    CrestronConsole.PrintLine("{0} received response:", GetType().Name);
                    CrestronConsole.PrintLine(data.ToString(Formatting.Indented));
#endif
                    CheckComms(true);
                }
                catch (Exception e)
                {
#if DEBUG
                    CrestronConsole.PrintLine(userobj.ContentString);
#endif
                    CloudLog.Error("Error in {0}.SetCurrentMode(ReceiverMode mode), {1}", GetType().Name, e.Message);

                    CheckComms(false);
                }
            });
        }

        public void GetCurrentChannel()
        {
            GetRequest("/cgi-bin/json_xfer?currentChannel=true", (userobj, error) =>
            {
                if (error != HTTP_CALLBACK_ERROR.COMPLETED || userobj == null)
                {
                    CheckComms(false);
                    return;
                }

                try
                {
                    try
                    {
                        var data = JToken.Parse(userobj.ContentString);
#if DEBUG
                        CrestronConsole.PrintLine("{0} received response:", GetType().Name);
                        CrestronConsole.PrintLine(data.ToString(Formatting.Indented));
#endif
                        CurrentChannelUri = data["currentChannel"].Value<string>();

                        CheckComms(true);
                    }
                    catch
                    {
                        CloudLog.Warn("Exterity GetCurrentChannel() failed to parse content, Response Code = {0}", userobj.Code);
                        CheckComms(false);
                        return;
                    }

                    OnCurrentChannelUriChanged(this, CurrentChannelUri);
                }
                catch (Exception e)
                {
                    CloudLog.Exception(e);
                }
            });
        }

        public void SetCurrentChannel(Channel channel)
        {
            CurrentChannelUri = channel.Uri;
            var json = string.Format("{{ \"params\": {{ \"currentChannel\": \"{0}\" }} }}", channel.Uri);

            PostRequest("/cgi-bin/json_xfer", json, (userobj, error) =>
            {
                if (error != HTTP_CALLBACK_ERROR.COMPLETED || userobj == null)
                {
                    CheckComms(false);
                    return;
                }

                try
                {
                    var data = JToken.Parse(userobj.ContentString);
#if DEBUG
                    CrestronConsole.PrintLine("{0} received response:", GetType().Name);
                    CrestronConsole.PrintLine(data.ToString(Formatting.Indented));
#endif
                    CheckComms(true);
                }
                catch (Exception e)
                {
#if DEBUG
                    CrestronConsole.PrintLine(userobj.ContentString);
#endif
                    CloudLog.Error("Error in {0}.SetCurrentChannel(Channel channel), {1}", GetType().Name, e.Message);

                    CheckComms(false);
                }
            });
        }

        public void SetVolume(int volumeLevel)
        {
            var json = string.Format("{{ \"params\": {{ \"receivervolume\": {0} }} }}", volumeLevel);

            PostRequest("/cgi-bin/json_xfer", json, (userobj, error) =>
            {
                if (error != HTTP_CALLBACK_ERROR.COMPLETED || userobj == null)
                {
                    CheckComms(false);
                    return;
                }

                try
                {
                    var data = JToken.Parse(userobj.ContentString);
                    //#if DEBUG
                    CrestronConsole.PrintLine("{0} received response:", GetType().Name);
                    CrestronConsole.PrintLine(data.ToString(Formatting.Indented));
                    //#endif
                    CheckComms(true);
                }
                catch (Exception e)
                {
                    //#if DEBUG
                    CrestronConsole.PrintLine(userobj.ContentString);
                    //#endif
                    CloudLog.Error("Error in {0}.SetVolume(int volumeLevel), {1}", GetType().Name, e.Message);

                    CheckComms(false);
                }
            });
        }

        private void CheckComms(bool ok)
        {
            if (!ok)
            {
                _fails++;

                if (_fails > 1)
                {
                    DeviceCommunicating = false;
                }
            }
            else
            {
                _fails = 0;
                DeviceCommunicating = true;
            }
        }

        internal void UpdateInfo(JToken data)
        {
#if DEBUG
            CrestronConsole.PrintLine("Updated data for IPTV Receiver...");
            CrestronConsole.PrintLine(data.ToString(Formatting.Indented));
#endif
            Name = data["name"].Value<string>();
            Location = data["location"].Value<string>();
            MacAddress = data["mac"].Value<string>();
            try
            {
                IpAddress = data["ip"].Value<string>();
            }
            catch (Exception e)
            {
                IpAddress = data["address"].Value<string>();
            }
            finally
            {
                if (string.IsNullOrEmpty(IpAddress))
                {
                    CloudLog.Error(
                        "Could not parse Device hostname / address from data in {0}.UpdateInfo(JToken data)",
                        GetType().Name);
                }
            }
        }

        protected virtual void OnCurrentChannelUriChanged(Receiver receiver, string uri)
        {
            var handler = CurrentChannelUriChanged;
            if (handler != null) handler(receiver, uri);
        }

        protected virtual void OnCurrentModeChanged(Receiver receiver, ReceiverMode mode)
        {
            var handler = CurrentModeChanged;
            if (handler != null) handler(receiver, mode);
        }

        #endregion

        public void UpdateOnSourceRequest()
        {
            _pollCount = 59;
        }

        public void StartPlaying()
        {
            
        }

        public void StopPlaying()
        {
            
        }

        public void Initialize()
        {
            if (!_initialized)
            {
                _initialized = true;
                _timer = new CTimer(PollTimerProcess, null, 1000, 1000);
            }            
        }

        public FusionAssetType AssetType
        {
            get
            {
                return FusionAssetType.IpTvReceiver;
            }
        }
    }

    public delegate void CurrentChannelUriChangeHandler(Receiver receiver, string uri);

    public delegate void CurrentModeChangeHandler(Receiver receiver, ReceiverMode mode);

    public enum ReceiverMode
    {
        Av,
        Signage,
        Splash,
        Off,
        Browser
    }
}