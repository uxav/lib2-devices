using System;
using System.Collections.Generic;
using Crestron.SimplSharp;

namespace UX.Lib2.Devices.ZeeVee
{
    public class HdmiOutput : HdmiInputOutputBase
    {
        #region Fields
        #endregion

        #region Constructors

        /// <summary>
        /// The default Constructor.
        /// </summary>
        internal HdmiOutput()
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

        public VideoResolution PreferredFormat { get; private set; }

        public string EdidStatus { get; private set; }

        #endregion

        #region Methods

        internal override void UpdateFromProperties(Dictionary<string, string> properties)
        {
            base.UpdateFromProperties(properties);

            if (properties.ContainsKey("preferred-horizontalSize") && properties.ContainsKey("preferred-verticalSize") &&
                properties.ContainsKey("preferred-fps"))
            {
                try
                {
                    PreferredFormat = new VideoResolution(int.Parse(properties["preferred-horizontalSize"]),
                        int.Parse(properties["preferred-verticalSize"]), float.Parse(properties["preferred-fps"]),
                        false);
                }
                catch (Exception e)
                {
                    if (e is FormatException)
                    {

                    }
                    else
                    {
                        ErrorLog.Exception("Error parsing video resolution", e);
                    }
                }
            }
        }

        #endregion
    }
}