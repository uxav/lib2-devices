using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UX.Lib2.Cloud.Logger;

namespace UX.Lib2.Devices.ZeeVee
{
    public class ZeeVeeDeviceCollection : IEnumerable<ZeeVeeDeviceBase>
    {
        #region Fields

        private readonly Dictionary<string, ZeeVeeDeviceBase> _devices = new Dictionary<string, ZeeVeeDeviceBase>(); 

        #endregion

        private readonly ZeeVeeServer _server;

        #region Constructors

        /// <summary>
        /// The default Constructor.
        /// </summary>
        internal ZeeVeeDeviceCollection(ZeeVeeServer server)
        {
            _server = server;
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events

        public event NewDeviceDiscoveredEventHandler NewDeviceDiscovered;

        #endregion

        #region Delegates
        #endregion

        public ZeeVeeDeviceBase this[string macAddress]
        {
            get { return _devices[macAddress]; }
            internal set { _devices[macAddress] = value; }
        }

        #region Properties
        #endregion

        #region Methods

        internal void CreateOrUpdate(string macAddress, DeviceType type, string statusString)
        {
            ZeeVeeDeviceBase device = null;
            if (_devices.ContainsKey(macAddress))
            {
                _devices[macAddress].UpdateFromStatusString(statusString);
            }
            else
            {
                switch (type)
                {
                    case DeviceType.Encoder:
                        device = new ZeeVeeEncoder(_server, macAddress, type);
                        break;
                    case DeviceType.Decoder:
                        device = new ZeeVeeDecoder(_server, macAddress, type);
                        break;
                }

                if (device == null) return;
                device.UpdateFromStatusString(statusString);

                CloudLog.Debug("Discovered {0}, MAC: {1}, Name: {2}, State: {3}",
                    device.GetType().Name, device.MacAddress, device.Name, device.State, device.UpTime);
                
                OnNewDeviceDiscovered(device, type);
            }
        }

        protected virtual void OnNewDeviceDiscovered(ZeeVeeDeviceBase device, DeviceType devicetype)
        {
            var handler = NewDeviceDiscovered;
            if (handler != null) handler(device, devicetype);
        }

        public ZeeVeeDeviceBase GetByName(string name)
        {
            var device = _devices.Values.FirstOrDefault(d => d.Name == name);

            // If no device by name found try and see if it's a mac address and return that
            if (device == null && _devices.ContainsKey(name))
                return _devices[name];

            return device;
        }

        public IEnumerable<ZeeVeeEncoder> Encoders
        {
            get { return _devices.Values.Where(d => d.Type == DeviceType.Encoder).Cast<ZeeVeeEncoder>(); }
        }

        public IEnumerable<ZeeVeeDecoder> Decoders
        {
            get { return _devices.Values.Where(d => d.Type == DeviceType.Decoder).Cast<ZeeVeeDecoder>(); }
        }

        public IEnumerator<ZeeVeeDeviceBase> GetEnumerator()
        {
            return _devices.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }

    public delegate void NewDeviceDiscoveredEventHandler(ZeeVeeDeviceBase device, DeviceType deviceType);
}