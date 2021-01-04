 
namespace UX.Lib2.Devices.Cisco.Conference
{
    public class ActiveSpeaker : CodecApiElement
    {
        #region Fields
        
        [CodecApiNameAttribute("CallId")]
#pragma warning disable 649 // assigned using reflection
        private int _callId;
#pragma warning restore 649

        #endregion

        #region Constructors

        internal ActiveSpeaker(CodecApiElement parent, string propertyName)
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

        public int CallId
        {
            get { return _callId; }
        }

        #endregion

        #region Methods
        #endregion
    }
}