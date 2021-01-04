 
using System;
using System.Collections.Generic;
using Crestron.SimplSharp;

namespace UX.Lib2.Devices.Cisco.Conference
{
    public class Conference : CodecApiElement
    {
        #region Fields

        [CodecApiNameAttribute("Call")]
        private Dictionary<int, Call> _call = new Dictionary<int, Call>();

        [CodecApiNameAttribute("Line")]
        private Dictionary<int, ConferenceLine> _lines = new Dictionary<int, ConferenceLine>();

        [CodecApiNameAttribute("DoNotDisturb")]
#pragma warning disable 649 // assigned using reflection
        private DoNotDisturbMode _doNotDisturb;
#pragma warning restore 649

        [CodecApiNameAttribute("ActiveSpeaker")]
        private ActiveSpeaker _activeSpeaker;

        [CodecApiNameAttribute("Presentation")]
        private Presentation _presentation;

        [CodecApiNameAttribute("MultiPoint")]
        private MultiPoint _multiPoint;

        #endregion

        #region Constructors

        internal Conference(CiscoTelePresenceCodec codec)
            : base(codec)
        {
            _activeSpeaker = new ActiveSpeaker(this, "ActiveSpeaker");
            _presentation = new Presentation(this, "Presentation");
            _multiPoint = new MultiPoint(this, "MultiPoint");
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events
        #endregion

        #region Delegates
        #endregion

        #region Properties

        public ActiveSpeaker ActiveSpeaker
        {
            get { return _activeSpeaker; }
        }

        public DoNotDisturbMode DoNotDisturb
        {
            get { return _doNotDisturb; }
        }

        public Presentation Presentation
        {
            get { return _presentation; }
        }

        public MultiPoint MultiPoint
        {
            get { return _multiPoint; }
        }

        public ReadOnlyDictionary<int, Call> Call
        {
            get { return new ReadOnlyDictionary<int, Call>(_call); }
        }

        public ReadOnlyDictionary<int, ConferenceLine> Lines
        {
            get { return new ReadOnlyDictionary<int, ConferenceLine>(_lines); }
        }

        #endregion

        #region Methods

        public void SetDoNotDisturb(DoNotDisturbMode mode)
        {
            Codec.Send("xCommand Conference DoNotDisturb {0}",
                mode == DoNotDisturbMode.Active ? "Activate" : "Deactivate");
        }

        public void SetDoNotDisturb(DoNotDisturbMode mode, TimeSpan timeOut)
        {
            if (timeOut.TotalMinutes > 1440) throw new Exception("Timeout cannot be more than 24 hours");
            Codec.Send("xCommand Conference DoNotDisturb {0}{1}",
                mode == DoNotDisturbMode.Active ? "Activate" : "Deactivate",
                mode == DoNotDisturbMode.Active && timeOut.TotalMinutes > 0
                    ? string.Format("Timeout: {0}", timeOut.TotalMinutes)
                    : string.Empty);
        }

        #endregion
    }

    public enum DoNotDisturbMode
    {
        Active,
        Inactive
    }
}