using System;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharp;
using UX.Lib2.Cloud.Logger;

namespace UX.Lib2.Devices.ZeeVee
{
    public class ZeeVeeDecoder : ZeeVeeDeviceBase
    {
        #region Fields

        private ZeeVeeEncoder _connectedEncoder;

        #endregion

        #region Constructors

        internal ZeeVeeDecoder(ZeeVeeServer server, string macAddress, DeviceType type)
            : base(server, macAddress, type)
        {
            HdmiOutput = new HdmiOutput();
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events
        #endregion

        #region Delegates
        #endregion

        #region Properties

        public ZeeVeeEncoder ConnectedEncoder
        {
            get { return _connectedEncoder; }
            set
            {
                Server.JoinFastSwitched(value, this);
            }
        }

        public bool ReceivingVideoFromEncoder { get; internal set; }

        public HdmiOutput HdmiOutput { get; private set; }

        #endregion

        #region Methods

        internal override void UpdateProperties(Dictionary<string, Dictionary<string, string>> properties)
        {
            base.UpdateProperties(properties);

            try
            {
                if (properties.ContainsKey("connectedEncoder"))
                {
                    var values = properties["connectedEncoder"];

                    if (Server.Devices.Any(d => d.MacAddress == values["mac"]))
                        _connectedEncoder = Server.Devices[values["mac"]] as ZeeVeeEncoder;
                    else
                        _connectedEncoder = null;
                    ReceivingVideoFromEncoder = (values["receivingVideoFromEncoder"] == "yes");
                }

                if (properties.ContainsKey("hdmiOutput"))
                {
                    HdmiOutput.UpdateFromProperties(properties["hdmiOutput"]);
                }
            }
            catch (Exception e)
            {
                CloudLog.Exception("Error processing property values", e);
            }

#if DEBUG
            CrestronConsole.PrintLine("Updated {0}, \"{1}\": State: {2}", GetType().Name, Name, State);
            if (State != DeviceState.Up) return;
            CrestronConsole.PrintLine("   Uptime: {0}", UpTime.ToPrettyFormat());
            if (ConnectedEncoder != null)
                CrestronConsole.PrintLine("   Connected Encoder: {0}, Receiving Video: {1}", ConnectedEncoder.Name,
                    ReceivingVideoFromEncoder);
            CrestronConsole.PrintLine("   HdmiOutput: {0}{1}{2}",
                HdmiOutput.Connected ? "Connected" : "Disconnected",
                HdmiOutput.Connected ? " " + HdmiOutput.Format : "",
                HdmiOutput.Connected ? " HDCP: " + HdmiOutput.Hdcp : "",
                HdmiOutput.Connected ? " HDMI 2.0: " + HdmiOutput.Hdmi2Point0 : "");
            if(HdmiOutput.Connected)
                CrestronConsole.PrintLine("   Edid Status: {0}", HdmiOutput.EdidStatus);
#endif
        }

        #endregion
    }
}