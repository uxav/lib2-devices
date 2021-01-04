 
namespace UX.Lib2.Devices.Cisco.Video
{
    public class Presentation : CodecApiElement
    {
        #region Fields

        [CodecApiNameAttribute("PIPPosition")]
#pragma warning disable 649 // assigned using reflection
        private PresentationPIPPosition _pipPosition;
#pragma warning restore 649

        #endregion

        #region Constructors

        internal Presentation(CodecApiElement parent, string propertyName)
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

        public PresentationPIPPosition PipPosition
        {
            get { return _pipPosition; }
        }

        #endregion

        #region Methods
        #endregion
    }

    public enum PresentationPIPPosition
    {
        UpperLeft,
        UpperCenter,
        UpperRight,
        CenterLeft,
        CenterRight,
        LowerLeft,
        LowerRight
    }
}