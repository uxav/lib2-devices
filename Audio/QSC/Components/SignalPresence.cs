 
using System;
using Newtonsoft.Json.Linq;
using UX.Lib2.Cloud.Logger;

namespace UX.Lib2.Devices.Audio.QSC.Components
{
    public class SignalPresence : ComponentBase
    {
        #region Fields

        private readonly bool _hasMultipleChannels = true;
        private readonly int _channelCount;

        #endregion

        #region Constructors

        internal SignalPresence(QsysCore core, JToken data)
            : base(core, data)
        {
            _channelCount = int.Parse(Properties["multi_channel_count"]);

            QsysControl control;
            switch (Properties["multi_channel_type"])
            {
                case "1":
                    RegisterControl("signal.presence");
                    _hasMultipleChannels = false;
                    _channelCount = 1;
                    break;
                case "2":
                    _channelCount = 2;
                    for (var i = 1; i <= _channelCount; i++)
                    {
                        RegisterControl(string.Format("signal.presence.{0}", i));
                    }
                    break;
                case "3":
                    for (var i = 1; i <= _channelCount; i++)
                    {
                        RegisterControl(string.Format("signal.presence.{0}", i));
                    }
                    break;
            }

            RegisterControl("hold.time");
            RegisterControl("infinite.hold");
            RegisterControl("threshold");
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events

        public event SignalPresenceValueChange SignalPresenceChanged;

        #endregion

        #region Delegates
        #endregion

        #region Properties

        public bool HasMultipleChannels
        {
            get { return _hasMultipleChannels; }
        }

        public int ChannelCount
        {
            get { return _channelCount; }
        }

        #endregion

        #region Methods

        public bool GetPresence(int channel)
        {
            if (!_hasMultipleChannels) return this["signal.presence"].Value > 0;
            if (channel == 0 || channel > _channelCount)
            {
                throw new IndexOutOfRangeException(string.Format("SignalPresence component has {0} channels",
                    _channelCount));
            }
            return this[string.Format("signal.presence.{0}", channel)].Value > 0;
        }

        internal override void OnControlChange(QsysControl control, QsysControlValueChangeEventArgs args)
        {
            //Debug.WriteInfo("OnControlChange", "{0}", control.ToString());
            base.OnControlChange(control, args);
            try
            {
                if (control.Name == "signal.presence")
                {
                    SignalPresenceChanged(args.NewValue > 0);
                }
                //Debug.WriteInfo("SignalPresenceChanged");
            }
            catch (Exception e)
            {
                //Debug.WriteError("OnControlChange", e.Message);
            }
        }

        protected virtual void OnSignalPresenceChanged(bool value)
        {
            var handler = SignalPresenceChanged;
            if (handler == null) return;
            try
            {
                handler(value);
            }
            catch (Exception e)
            {
                CloudLog.Exception(e);
            }
        }

        #endregion
    }

    public delegate void SignalPresenceValueChange(bool value);
}