using System;
using Crestron.SimplSharp;
using Crestron.SimplSharp.Net.Http;
using Newtonsoft.Json.Linq;
using UX.Lib2.Cloud.Logger;
using UX.Lib2.DeviceSupport;

namespace UX.Lib2.Devices.Mersive
{
    public class SolsticePod : ISourceDevice
    {
        private readonly string _address;
        private readonly string _password;
        private static HttpClient _client;
        private string _displayName;
        private string _productName;
        private string _productVarient;
        private CTimer _timer;
        private bool _firstAttempt = true;

        public SolsticePod(string address, string password)
        {
            _address = address;
            _password = password;
            if (_client == null)
            {
                _client = new HttpClient
                {
                    UseConnectionPooling = true,
                    Timeout = 10,
                    TimeoutEnabled = true
                };
            }
            Name = GetType().Name + (string.IsNullOrEmpty(address) ? string.Empty : " " + address);
        }

        public string Name { get; private set; }
        public string ManufacturerName { get; private set; }
        public string ModelName { get; private set; }

        public string DiagnosticsName
        {
            get { return Name; }
        }

        public bool DeviceCommunicating { get; private set; }

        public string DeviceAddressString
        {
            get { return _address; }
        }

        public string SerialNumber { get; private set; }
        public string VersionInfo { get; private set; }
        public event DeviceCommunicatingChangeHandler DeviceCommunicatingChange;
        public void UpdateOnSourceRequest()
        {
            _timer.Reset(0, 60000);
        }

        public void StartPlaying()
        {
            
        }

        public void StopPlaying()
        {

        }

        /// <summary>
        /// Boots all users off the solstice, clears posts and returns to splash screen
        /// </summary>
        public void Boot()
        {
            var uri = string.Format("http://{0}/api/control/boot?password={1}", _address, _password);
            _client.GetAsync(uri, (userobj, error) =>
            {
                if (error != HTTP_CALLBACK_ERROR.COMPLETED)
                {
                    CloudLog.Warn("Error trying to send end session command to Solstice Pod at \"{0}\"", _address);                    
                }
            });
        }

        public void Initialize()
        {
            if(_timer != null && !_timer.Disposed) return;

            CloudLog.Notice("{0}.Initialize() for address {1}", GetType().Name, _address);

            _timer = new CTimer(specific => GetStatus(), null, 1000, 60000);
        }

        public void GetStatus()
        {
            try
            {
                //CloudLog.Debug("{0}.GetStatus() for address {1}", GetType().Name, _address);
                if (string.IsNullOrEmpty(_address) && _firstAttempt)
                {
                    _firstAttempt = false;
                    CloudLog.Error("Error with Solstice Pod, no address set, please check config");
                    return;
                }
                var uri = string.Format("http://{0}/api/stats?password={1}", _address, _password);
#if DEBUG
                CloudLog.Debug("Gettiing .. " + uri);
#endif
                _client.GetAsync(uri, (response, error) =>
                {
                    try
                    {
                        if (error == HTTP_CALLBACK_ERROR.COMPLETED)
                        {
#if DEBUG
                            Debug.WriteNormal(Debug.AnsiPurple + response + Debug.AnsiReset);
#endif
                            var data = JToken.Parse(response);
                            var info = data["m_displayInformation"];
                            _displayName = info["m_displayName"].Value<string>();
                            _productName = info["m_productName"].Value<string>();
                            _productVarient = info["m_productVariant"].Value<string>();
                            Name = _productName + " " + _productVarient + " (" + _displayName + ")";
                            DeviceCommunicating = true;
                            return;
                        }
                    }
                    catch (Exception e)
                    {
                        CloudLog.Error("Error with response from Solstice Pod at \"{0}\", {1}", _address, e.Message);
                    }

                    if (DeviceCommunicating || _firstAttempt)
                    {
                        _firstAttempt = false;
                        CloudLog.Error("Error waiting for response from Solstice Pod at \"{0}\"", _address);
                        DeviceCommunicating = false;
                    }
                });
            }
            catch (Exception e)
            {
                if (DeviceCommunicating || _firstAttempt)
                {
                    _firstAttempt = false;
                    CloudLog.Error("Error calling request for Solstice Pod at \"{0}\", {1}", _address, e.Message);
                    DeviceCommunicating = false;
                }
            }
        }
    }
}