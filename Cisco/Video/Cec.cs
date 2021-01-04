 
using System;

namespace UX.Lib2.Devices.Cisco.Video
{
    public class Cec : CodecApiElement
    {
        #region Fields

        [CodecApiNameAttribute("VendorId")]
#pragma warning disable 649 // assigned using reflection
        private string _vendorId;
#pragma warning restore 649

        [CodecApiNameAttribute("DeviceType")]
#pragma warning disable 649 // assigned using reflection
        private ConnectedDeviceCECDeviceType _deviceType;
#pragma warning restore 649

        [CodecApiNameAttribute("Name")]
#pragma warning disable 649 // assigned using reflection
        private string _name;
#pragma warning restore 649

        [CodecApiNameAttribute("PowerControl")]
#pragma warning disable 649 // assigned using reflection
        private string _powerControl;
#pragma warning restore 649

        [CodecApiNameAttribute("PowerStatus")]
#pragma warning disable 649 // assigned using reflection
        private string _powerStatus;
#pragma warning restore 649

        #endregion

        #region Constructors

        public Cec(CodecApiElement parent, string propertyName)
            : base(parent, propertyName)
        {
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events
        #endregion

        #region Delegates
        #endregion

        #region Properties

        public string VendorId
        {
            get { return _vendorId; }
        }

        public ConnectedDeviceCECDeviceType DeviceType
        {
            get { return _deviceType; }
        }

        public string Name
        {
            get { return _name; }
        }

        public ConnectedDeviceCecPower PowerControl
        {
            get
            {
                try
                {
                    return (ConnectedDeviceCecPower) Enum.Parse(typeof (ConnectedDeviceCecPower), _powerControl.Replace(" ", ""), true);
                }
                catch
                {
                    return ConnectedDeviceCecPower.Unknown;
                }
            }
        }

        public ConnectedDeviceCecPower PowerStatus
        {
            get
            {
                try
                {
                    return (ConnectedDeviceCecPower)Enum.Parse(typeof(ConnectedDeviceCecPower), _powerStatus.Replace(" ", ""), true);
                }
                catch
                {
                    return ConnectedDeviceCecPower.Unknown;
                }
            }
        }

        #endregion

        #region Methods
        #endregion
    }

    public enum ConnectedDeviceCECDeviceType
    {
        Unknown,
        TV,
        Reserved,
        Recorder,
        Tuner,
        Playback,
        Audio
    }

    public enum ConnectedDeviceCecPower
    {
        Unknown,
        Ok,
        InProgress,
        FailedToPowerOn,
        FailedToStandby
    }
}