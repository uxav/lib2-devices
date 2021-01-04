 
namespace UX.Lib2.Devices.Cisco.Video
{
    public class Selfview : CodecApiElement
    {
        #region Fields

        [CodecApiNameAttribute("Mode")]
#pragma warning disable 649 // assigned using reflection
        private string _enabled;
#pragma warning restore 649

        [CodecApiNameAttribute("FullscreenMode")]
#pragma warning disable 649 // assigned using reflection
        private string _fullscreen;
#pragma warning restore 649

        [CodecApiNameAttribute("PIPPosition")]
#pragma warning disable 649 // assigned using reflection
        private SelfviewPipPosition _pipPosition;
#pragma warning restore 649

        [CodecApiNameAttribute("OnMonitorRole")]
#pragma warning disable 649 // assigned using reflection
        private SelfviewMonitorRole _onMonitorRole;
#pragma warning restore 649

        #endregion

        #region Constructors

        internal Selfview(CodecApiElement parent, string propertyName)
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

        public bool Enabled
        {
            get { return _enabled == "On"; }
            set
            {
                if(Enabled == value) return;
                Codec.Send("xCommand Video Selfview Set Mode: {0}", value ? "On" : "Off");
            }
        }

        public bool Fullscreen
        {
            get { return _fullscreen == "On"; }
            set
            {
                if (Fullscreen == value) return;
                Codec.Send("xCommand Video Selfview Set FullscreenMode: {0}", value ? "On" : "Off");
            }
        }

        public SelfviewPipPosition PipPosition
        {
            get { return _pipPosition; }
            set
            {
                if(_pipPosition == value) return;
                Codec.Send("xCommand Video Selfview Set PIPPosition: {0}", value);
            }
        }

        public SelfviewMonitorRole OnMonitorRole
        {
            get { return _onMonitorRole; }
            set
            {
                if (_onMonitorRole == value) return;
                Codec.Send("xCommand Video Selfview Set OnMonitorRole: {0}", value);
            }
        }

        #endregion

        #region Methods
        #endregion
    }

    public enum SelfviewPipPosition
    {
        CenterLeft,
        CenterRight,
        LowerLeft,
        LowerRight,
        UpperCenter,
        UpperLeft
    }

    public enum SelfviewMonitorRole
    {
        First,
        Second,
        Third,
        Fourth
    }
}