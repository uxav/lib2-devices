 
using System;
using System.Linq;
using UX.Lib2.Cloud.Logger;
using UX.Lib2.DeviceSupport;

namespace UX.Lib2.Devices.Cisco.Cameras
{
    public class Camera : CodecApiElement, IFusionAsset
    {
        #region Fields

        private readonly int _id;

        [CodecApiName("Connected")]
#pragma warning disable 649 // assigned using reflection
        private bool _connected;
#pragma warning restore 649

        [CodecApiName("Manufacturer")]
#pragma warning disable 649 // assigned using reflection
        private string _manufacturer;
#pragma warning restore 649

        [CodecApiName("DetectedConnector")]
#pragma warning disable 649 // assigned using reflection
        private int _detectedConnector;
#pragma warning restore 649

        [CodecApiName("HardwareID")]
#pragma warning disable 649 // assigned using reflection
        private string _hardwareId;
#pragma warning restore 649

        [CodecApiName("MacAddress")]
#pragma warning disable 649 // assigned using reflection
        private string _macAddress;
#pragma warning restore 649

        [CodecApiName("SerialNumber")]
#pragma warning disable 649 // assigned using reflection
        private string _serialNumber;
#pragma warning restore 649

        [CodecApiName("SoftwareID")]
#pragma warning disable 649 // assigned using reflection
        private string _softwareId;
#pragma warning restore 649

        [CodecApiName("LightingConditions")]
#pragma warning disable 649 // assigned using reflection
        private CameraLightingConditions _lightingConditions;
#pragma warning restore 649

        [CodecApiName("Model")]
#pragma warning disable 649 // assigned using reflection
        private string _model;
#pragma warning restore 649

        [CodecApiName("Flip")]
#pragma warning disable 649 // assigned using reflection
        private string _flip;
#pragma warning restore 649

        [CodecApiName("Framerate")]
#pragma warning disable 649 // assigned using reflection
        private int _framerate;
#pragma warning restore 649

        [CodecApiName("Position")]
        private CameraPosition _position;

        #endregion

        #region Constructors

        internal Camera(CodecApiElement parent, string propertyName, int indexer)
            : base(parent, propertyName, indexer)
        {
            _id = indexer;
            _position = new CameraPosition(this, "Position");
            CustomName = string.Empty;
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events

        public event DeviceCommunicatingChangeHandler DeviceCommunicatingChange;

        #endregion

        #region Delegates
        #endregion

        #region Properties

        public int Id
        {
            get { return _id; }
        }

        public string DeviceAddressString
        {
            get { return "Codec Camera " + _id; }
        }

        public bool Connected
        {
            get { return _connected; }
        }

        public string Manufacturer
        {
            get { return _manufacturer; }
        }

        public string Model
        {
            get { return _model; }
        }

        public CameraLightingConditions LightingConditions
        {
            get { return _lightingConditions; }
        }

        public string Flip
        {
            get { return _flip; }
        }

        public string HardwareId
        {
            get { return _hardwareId; }
        }

        public string SerialNumber
        {
            get { return _serialNumber; }
        }

        public string VersionInfo
        {
            get { return _softwareId; }
        }

        public int DetectedConnector
        {
            get
            {
                if (Codec.CameraConnectorIdsAreBroken)
                {
                    return _id;
                }
                return _detectedConnector;
            }
        }

        public string MacAddress
        {
            get { return _macAddress; }
        }

        public string SoftwareId
        {
            get { return _softwareId; }
        }

        public CameraPosition Position
        {
            get { return _position; }
        }

        public int Framerate
        {
            get { return _framerate; }
        }

        public string Name
        {
            get
            {
                if (!string.IsNullOrEmpty(CustomName))
                {
                    return CustomName;
                }
                return Cameras.ConnectedCameras.Count() > 1 ? string.Format("Camera {0}", Id) : "Main Camera";
            }
        }

        public string DiagnosticsName
        {
            get { return Name; }
        }

        public string CustomName { get; set; }

        public string ManufacturerName
        {
            get { return Manufacturer; }
        }

        public string ModelName
        {
            get { return Model; }
        }

        public bool DeviceCommunicating
        {
            get { return Connected; }
        }

        public Cameras Cameras
        {
            get { return ParentElement as Cameras; }
        }

        public bool IsMainVideoSource
        {
            get
            {
                if (DetectedConnector == 0) return false;

                var source = Codec.Video.Input.MainVideoSource;

                return source != null && source.ConnectorId == DetectedConnector;
            }
        }

        public FusionAssetType AssetType
        {
            get { return FusionAssetType.Camera; }
        }

        #endregion

        #region Methods

        public void Ramp(CameraPanCommand command)
        {
            Codec.Send("xCommand Camera Ramp CameraId: {0} Pan: {1}", _id, command);
        }

        public void Ramp(CameraPanCommand command, int speed)
        {
            Codec.Send("xCommand Camera Ramp CameraId: {0} Pan: {1}, PanSpeed: {2}", _id, command, speed);
        }

        public void Ramp(CameraTiltCommand command)
        {
            Codec.Send("xCommand Camera Ramp CameraId: {0} Tilt: {1}", _id, command);
        }

        public void Ramp(CameraTiltCommand command, int speed)
        {
            Codec.Send("xCommand Camera Ramp CameraId: {0} Tilt: {1}, TiltSpeed: {2}", _id, command, speed);
        }

        public void Ramp(CameraZoomCommand command)
        {
            Codec.Send("xCommand Camera Ramp CameraId: {0} Zoom: {1}", _id, command);
        }

        public void Ramp(CameraZoomCommand command, int speed)
        {
            Codec.Send("xCommand Camera Ramp CameraId: {0} Zoom: {1}, ZoomSpeed: {2}", _id, command, speed);
        }

        public void Ramp(CameraFocusCommand command)
        {
            Codec.Send("xCommand Camera Ramp CameraId: {0} Focus: {1}", _id, command);
        }

        public void AutoFocus()
        {
            Codec.Send("xCommand Camera TriggerAutofocus CameraId: {0}", _id);
        }

        public void UseAsMainVideoSource()
        {
            CloudLog.Debug("Camera {0} selected as main camera video source with connector ID {1}", Id,
                DetectedConnector);
            Codec.Send("xCommand Video Input SetMainVideoSource ConnectorId: {0}", DetectedConnector);
        }

        protected override void OnStatusChanged(CodecApiElement element, string[] propertyNamesWhichUpdated)
        {
            base.OnStatusChanged(element, propertyNamesWhichUpdated);

            if (propertyNamesWhichUpdated.Any(name => name == "Connected"))
            {
                try
                {
                    if (DeviceCommunicatingChange != null)
                    {
                        DeviceCommunicatingChange(this, Connected);
                    }
                }
                catch (Exception e)
                {
                    CloudLog.Exception(e);
                }
            }
        }

        #endregion
    }

    public enum CameraPanCommand
    {
        Left,
        Right,
        Stop
    }

    public enum CameraTiltCommand
    {
        Down,
        Up,
        Stop
    }

    public enum CameraZoomCommand
    {
        In,
        Out,
        Stop
    }

    public enum CameraFocusCommand
    {
        Far,
        Near,
        Stop
    }

    public enum CameraLightingConditions
    {
        Unknown,
        Good,
        Dark,
        Backlight
    }
}