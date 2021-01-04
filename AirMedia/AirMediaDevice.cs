 
using System;
using System.Linq;
using Crestron.SimplSharp.Reflection;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.DM.AirMedia;
using Crestron.SimplSharpPro.UC;
using UX.Lib2.Cloud.Logger;
using UX.Lib2.DeviceSupport;

namespace UX.Lib2.Devices.AirMedia
{
    public class AirMediaDevice : ISourceDevice, IFusionAsset
    {
        private string _deviceIpAddress;
        private readonly GenericDevice _device;
        private bool _resetConnectionsOnStop = true;

        public AirMediaDevice(uint ipId, CrestronControlSystem controlSystem, string deviceType, string description)
        {
            try
            {
                var type = typeof (Am101);
                if (!string.IsNullOrEmpty(deviceType))
                {
                    try
                    {
                        var assembly = Assembly.Load(typeof (Am200).AssemblyName());
                        type = assembly.GetType(deviceType).GetCType();
                    }
                    catch
                    {
                        var assembly = Assembly.Load(typeof (Am101).AssemblyName());
                        type = assembly.GetType(deviceType).GetCType();
                    }
                }

                var ctor = type.GetConstructor(new CType[] {typeof (uint), typeof (CrestronControlSystem)});
                _device = (GenericDevice) ctor.Invoke(new object[] {ipId, controlSystem});
                _device.Description = description;

                _device.IpInformationChange += AmOnIpInformationChange;
                _device.OnlineStatusChange += AmOnOnlineStatusChange;
                if(_device is AmX00) {
                    ((AmX00) _device).AirMedia.AirMediaChange += AirMediaOnAirMediaChange;
                }
                var regResult = _device.Register();
                if (regResult != eDeviceRegistrationUnRegistrationResponse.Success)
                {
                    CloudLog.Error("Error registering {0} with ID 0x{1}, {2}", _device.GetType().Name, _device.ID.ToString("X2"),
                        regResult);
                }
            }
            catch (Exception e)
            {
                CloudLog.Exception(e, "Error loading Air Media Device");
            }
        }

        public string Name
        {
            get { return _device.Name; }
        }

        public string ManufacturerName
        {
            get { return "Crestron"; }
        }

        public string ModelName
        {
            get { return _device.GetType().Name; }
        }

        public string DiagnosticsName
        {
            get
            {
                if (string.IsNullOrEmpty(_device.Description))
                {
                    return _device.ToString();
                }
                return _device + " \"" + _device.Description + "\"";
            }
        }

        public bool DeviceCommunicating
        {
            get { return _device.IsOnline; }
        }

        public string DeviceAddressString
        {
            get { return _deviceIpAddress; }
        }

        public string SerialNumber
        {
            get { return "Not Avaialable"; }
        }

        public string VersionInfo
        {
            get { return "Not Available"; }
        }

        public bool ResetConnectionsOnStop
        {
            get { return _resetConnectionsOnStop; }
            set { _resetConnectionsOnStop = value; }
        }

        public CrestronCollection<ComPort> ComPorts
        {
            get
            {
                var dev = _device as IComPorts;
                if (dev != null)
                {
                    return dev.ComPorts;
                }
                return null;
            }
        }

        public GenericDevice AirMedia
        {
            get { return _device; }
        }

        public event DeviceCommunicatingChangeHandler DeviceCommunicatingChange;

        public void UpdateOnSourceRequest()
        {
            
        }

        public void StartPlaying()
        {
            
        }

        public void StopPlaying()
        {
            if(!_resetConnectionsOnStop) return;
            var am100 = _device as Am100;
            if (am100 != null)
            {
                am100.ResetConnections();
                return;
            }
            var amx00 = _device as AmX00;
            if (amx00 != null)
            {
                amx00.AirMedia.Control.ResetConnections();
            }
        }

        public void SelectInput(AmX00DisplayControl.eAirMediaX00VideoSource input)
        {
            var device = _device as AmX00;
            if (device == null)
            {
                throw new NotSupportedException(string.Format("AirMedia device is {0}", _device.Name));
            }
            device.DisplayControl.VideoOut = input;
        }

        public AmX00 Device
        {
            get { return _device as AmX00; }
        }

        public void Initialize()
        {
            
        }

        private void AmOnOnlineStatusChange(GenericBase currentDevice, OnlineOfflineEventArgs args)
        {
            if (!args.DeviceOnLine)
            {
                CloudLog.Warn("{0} is offline!", currentDevice.ToString());
            }

            if (args.DeviceOnLine && currentDevice is AmX00)
            {
                try
                {
                    ((AmX00) currentDevice).DisplayControl.DisableAutomaticRouting();
                }
                catch (Exception e)
                {
                    CloudLog.Exception(e);
                }
            }

            try
            {
                if (DeviceCommunicatingChange != null)
                {
                    DeviceCommunicatingChange(this, args.DeviceOnLine);
                }
            }
            catch (Exception e)
            {
                CloudLog.Exception(e);
            }
        }

        public void SetBackgroundUrl(string url)
        {
            
        }

        public string ConnectionName
        {
            get
            {
                var device = _device as AmX00;
                if (device != null)
                {
                    return device.AirMedia.ConnectionAddressFeedback.StringValue;
                }
                return ((Am100) _device).HostnameFeedback.StringValue;
            }
        }

        public string Code
        {
            get
            {
                var device = _device as AmX00;
                if (device != null)
                {
                    return device.AirMedia.LoginCodeFeedback.UShortValue.ToString("D4");
                }
                return ((Am100)_device).AccessCodeFeedback.UShortValue.ToString("D4");
            }
        }

        private void AmOnIpInformationChange(GenericBase currentDevice, ConnectedIpEventArgs args)
        {
            if (!args.Connected) return;
            CloudLog.Notice("{0} has connected on IP {1}", currentDevice, args.DeviceIpAddress);
            if (currentDevice.ConnectedIpList.Count == 1)
            {
                _deviceIpAddress = currentDevice.ConnectedIpList.First().DeviceIpAddress;
            }
        }

        private void AirMediaOnAirMediaChange(object sender, GenericEventArgs args)
        {
            switch (args.EventId)
            {
                case Crestron.DeviceSupport.Support.AirMediaInputSlot.AirMediaImageErrorEventId:
                    var error = ((AmX00) _device).AirMedia.DisplayControl.ImageErrorFeedback.UShortValue;
                    if (error == 0)
                    {
                        CloudLog.Notice("AirMedia {0} background set to \"{1}\"", _device.ID.ToString("X2"),
                            ((AmX00) _device).AirMedia.DisplayControl.ImageURLFeedback.StringValue);
                    }
                    else
                    {
                        CloudLog.Error("AirMedia {0} could not set background to \"{1}\", Error {2}",
                            _device.ID.ToString("X2"),
                            ((AmX00) _device).AirMedia.DisplayControl.ImageURL.StringValue, error);
                    }
                    break;
            }
        }

        public FusionAssetType AssetType
        {
            get
            {
                return FusionAssetType.AirMedia;
            }
        }
    }
}