 
namespace UX.Lib2.Devices.Cisco.Capabilities
{
    public class Conference : CodecApiElement
    {
        #region Fields

        [CodecApiNameAttribute("MaxActiveCalls")]
#pragma warning disable 649 // assigned using reflection
        private int _maxActiveCalls;
#pragma warning restore 649

        [CodecApiNameAttribute("MaxAudioCalls")]
#pragma warning disable 649 // assigned using reflection
        private int _maxAudioCalls;
#pragma warning restore 649

        [CodecApiNameAttribute("MaxCalls")]
#pragma warning disable 649 // assigned using reflection
        private int _maxCalls;
#pragma warning restore 649

        [CodecApiNameAttribute("MaxVideoCalls")]
#pragma warning disable 649 // assigned using reflection
        private int _maxVideoCalls;
#pragma warning restore 649

        #endregion

        #region Constructors

        internal Conference(CodecApiElement parent, string propertyName)
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

        public int MaxActiveCalls
        {
            get { return _maxActiveCalls; }
        }

        public int MaxAudioCalls
        {
            get { return _maxAudioCalls; }
        }

        public int MaxCalls
        {
            get { return _maxCalls; }
        }

        public int MaxVideoCalls
        {
            get { return _maxVideoCalls; }
        }

        #endregion

        #region Methods
        #endregion
    }
}