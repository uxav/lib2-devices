using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UX.Lib2.Cloud.Logger;

namespace UX.Lib2.Devices.ZeeVee
{
    public abstract class ZeeVeeDeviceBase
    {
        #region Fields

        private readonly ZeeVeeServer _server;
        private DateTime _onlineTime;

        #endregion

        #region Constructors

        /// <summary>
        /// The default Constructor.
        /// </summary>
        protected ZeeVeeDeviceBase(ZeeVeeServer server, string macAddress, DeviceType type)
        {
            _server = server;
            Type = type;
            MacAddress = macAddress;
            _server.Devices[macAddress] = this;
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events
        #endregion

        #region Delegates
        #endregion

        #region Properties

        public ZeeVeeServer Server
        {
            get { return _server; }
        }

        public string MacAddress { get; private set; }
        public string Name { get; internal set; }
        public string Model { get; internal set; }
        public DeviceType Type { get; private set; }
        public DeviceState State { get; internal set; }

        public TimeSpan UpTime
        {
            get { return DateTime.Now - _onlineTime; }
            internal set { _onlineTime = DateTime.Now - value; }
        }

        #endregion

        #region Methods

        internal void UpdateFromStatusString(string statusString)
        {
            var lines = Regex.Matches(statusString, @"device\.([\w]+);[\s]*([^\r\n]+)");

            var values = new Dictionary<string, Dictionary<string, string>>();

            foreach (Match line in lines)
            {
                var functionName = line.Groups[1].Value;
                if (!values.ContainsKey(functionName))
                {
                    values[functionName] = new Dictionary<string, string>();
                }

                foreach (Match property in Regex.Matches(line.Groups[2].Value, @"([^\s]+)=([^\s,]+)"))
                {
                    values[functionName][property.Groups[1].Value] = property.Groups[2].Value;
                }
            }

            UpdateProperties(values);
        }

        internal virtual void UpdateProperties(Dictionary<string, Dictionary<string, string>> properties)
        {
            try
            {
                foreach (var property in properties["gen"])
                {
                    switch (property.Key)
                    {
                        case "model":
                            Model = property.Value;
                            break;
                        case "state":
                            State = (DeviceState) Enum.Parse(typeof (DeviceState), property.Value, true);
                            break;
                        case "name":
                            Name = property.Value;
                            break;
                        case "uptime":
                            var timeMatch = Regex.Match(property.Value, @"(\d+)d:(\d+)h:(\d+)m:(\d+)s");
                            UpTime = TimeSpan.FromDays(int.Parse(timeMatch.Groups[1].Value))
                                     + TimeSpan.FromHours(int.Parse(timeMatch.Groups[2].Value))
                                     + TimeSpan.FromMinutes(int.Parse(timeMatch.Groups[3].Value))
                                     + TimeSpan.FromSeconds(int.Parse(timeMatch.Groups[4].Value));
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                CloudLog.Error("Error processing property values in {0}, {1}", GetType().Name, e.Message);
            }
        }

        #endregion
    }

    public enum DeviceType
    {
        Encoder,
        Decoder
    }

    public enum DeviceState
    {
        Down,
        Up
    }
}