using System;
using System.Collections.Generic;

namespace UX.Lib2.Devices.ZeeVee
{
    public abstract class HdmiInputOutputBase
    {

        #region Fields
        
        private string _hdcp;
        private string _hdcpVersion;

        #endregion

        #region Constructors

        /// <summary>
        /// The default Constructor.
        /// </summary>
        protected HdmiInputOutputBase()
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

        public bool Connected { get; internal set; }

        public string Hdcp
        {
            get
            {
                if (String.IsNullOrEmpty(_hdcp))
                    _hdcp = "unknown";
                return _hdcp;
            }
            internal set { _hdcp = value; }
        }

        public string HdcpVersion
        {
            get
            {
                if (String.IsNullOrEmpty(_hdcpVersion))
                    _hdcp = "unknown";
                return _hdcpVersion;
            }
            internal set { _hdcpVersion = value; }
        }

        public bool Hdmi2Point0 { get; internal set; }

        public VideoResolution Format { get; private set; }

        #endregion

        #region Methods

        internal virtual void UpdateFromProperties(Dictionary<string, string> properties)
        {
            Connected = (properties.ContainsKey("cableConnected") && properties["cableConnected"] == "connected");
            Hdmi2Point0 = (properties.ContainsKey("hdmi-2.0") && properties["hdmi-2.0"] == "yes");
            if (properties.ContainsKey("hdcp"))
                Hdcp = properties["hdcp"];
            if (properties.ContainsKey("hdcp-version"))
                HdcpVersion = properties["hdcp-version"];

            if (properties.ContainsKey("horizontalSize") && properties.ContainsKey("verticalSize") &&
                properties.ContainsKey("fps") && properties.ContainsKey("interlaced"))
            {
                Format = new VideoResolution(int.Parse(properties["horizontalSize"]),
                    int.Parse(properties["verticalSize"]), float.Parse(properties["fps"]),
                    properties["interlaced"] == "yes");
            }
        }  

        #endregion
    }
}