using System;
using System.Collections.Generic;
using Crestron.SimplSharp;
using UX.Lib2.Cloud.Logger;

namespace UX.Lib2.Devices.ZeeVee
{
    public class ZeeVeeEncoder : ZeeVeeDeviceBase
    {
        #region Fields
        #endregion

        #region Constructors

        internal ZeeVeeEncoder(ZeeVeeServer server, string macAddress, DeviceType type)
            : base(server, macAddress, type)
        {
            HdmiInput = new HdmiInput();
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events
        #endregion

        #region Delegates
        #endregion

        #region Properties

        public HdmiInput HdmiInput { get; private set; }

        #endregion

        #region Methods

        internal override void UpdateProperties(Dictionary<string, Dictionary<string, string>> properties)
        {
            base.UpdateProperties(properties);

            try
            {
                if (properties.ContainsKey("hdmiInput"))
                {
                    HdmiInput.UpdateFromProperties(properties["hdmiInput"]);
                }
            }
            catch (Exception e)
            {
                CloudLog.Error("Error processing property values in {0}, {1}", GetType().Name, e.Message);
            }

#if DEBUG
            CrestronConsole.PrintLine("Updated {0}, \"{1}\": State: {2}", GetType().Name, Name, State);
            if (State != DeviceState.Up) return;
            CrestronConsole.PrintLine("   Uptime: {0}", UpTime.ToPrettyFormat());
            CrestronConsole.PrintLine("   HdmiInput: {0}{1}{2}",
                HdmiInput.Connected ? "Connected" : "Disconnected",
                HdmiInput.Connected ? " " + HdmiInput.Format : "",
                HdmiInput.Connected ? " HDCP: " + HdmiInput.Hdcp : "",
                HdmiInput.Connected ? " HDMI 2.0: " + HdmiInput.Hdmi2Point0 : "");
#endif
        }

        #endregion
    }
}