 
using System;
using UX.Lib2.Cloud.Logger;

namespace UX.Lib2.Devices.Cisco
{
    public class Standby : CodecApiElement
    {
        #region Fields

        [CodecApiNameAttribute("State")]
#pragma warning disable 649 // assigned using reflection
        private StandbyState _state;
#pragma warning restore 649

        #endregion

        #region Constructors

        internal Standby(CiscoTelePresenceCodec codec)
            : base(codec)
        {
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events

        public event StandbyStateChangedEventHandler StateChange;

        #endregion

        #region Delegates
        #endregion

        #region Properties

        public StandbyState State
        {
            get { return _state; }
        }

        #endregion

        #region Methods

        public void Wake()
        {
            Codec.Send("xCommand Standby Deactivate");
        }

        public void Sleep()
        {
            Codec.Send("xCommand Standby Activate");            
        }

        public void ResetTimer(TimeSpan time)
        {
            Codec.Send("xCommand Standby ResetTimer Delay: {0}", time.TotalMinutes);
        }

        protected override void OnStatusChanged(CodecApiElement element, string[] propertyNamesWhichUpdated)
        {
            base.OnStatusChanged(element, propertyNamesWhichUpdated);
            
            try
            {
                if (StateChange != null)
                    StateChange(Codec, State);
            }
            catch (Exception e)
            {
                CloudLog.Exception(e);
            }
        }

        #endregion
    }

    public delegate void StandbyStateChangedEventHandler(CiscoTelePresenceCodec codec, StandbyState state);

    public enum StandbyState
    {
        Off,
        EnteringStandby,
        Halfwake,
        Standby
    }
}