 
using System;
using Newtonsoft.Json.Linq;
using UX.Lib2.Cloud.Logger;

namespace UX.Lib2.Devices.Audio.QSC.Components
{
    public class IoStatus : ComponentBase
    {
        #region Fields
        #endregion

        #region Constructors

        internal IoStatus(QsysCore core, JToken data)
            : base(core, data)
        {
            RegisterControl("status");
            RegisterControl("system.info");

            foreach (var control in this)
            {
                control.ValueChange += ControlOnValueChange;
            }
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events

        public event IoStatusChangedEventHandler StatusChanged;

        #endregion

        #region Delegates
        #endregion

        #region Properties

        public string Status
        {
            get
            {
                if (HasControl("status"))
                {
                    return this["status"].String;
                }

                CloudLog.Error("Error getting Status property, control missing in component \"{0}\", returned empty string", Name);
                return string.Empty;
            }
        }

        public int StatusCode
        {
            get
            {
                return (int) this["status"].Value;
            }
        }

        public string NetworkId
        {
            get
            {
                if (!Properties.ContainsKey("network_id"))
                {
                    return "IO Frame (Unknown Network ID)";
                }
                return Properties["network_id"];
            }
        }

        #endregion

        #region Methods

        private void ControlOnValueChange(QsysControl control, QsysControlValueChangeEventArgs args)
        {
            Debug.WriteInfo("IOFrame control change: " + control);
            switch (control.Name)
            {
                case "status":
                    OnStatusChanged(this);
                    break;
            }
        }

        protected virtual void OnStatusChanged(IoStatus statuscomponent)
        {
            var handler = StatusChanged;
            if (handler == null) return;
            try
            {
                handler(statuscomponent);
            }
            catch (Exception e)
            {
                CloudLog.Error("Error raising event {0}.OnStatusChanged, {1}", GetType().Name, e.Message);
            }
        }

        #endregion
    }

    public delegate void IoStatusChangedEventHandler(IoStatus statusComponent);
}