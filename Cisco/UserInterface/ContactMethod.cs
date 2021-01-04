 
namespace UX.Lib2.Devices.Cisco.UserInterface
{
    public class ContactMethod : CodecApiElement
    {
        #region Fields

        [CodecApiNameAttribute("Number")]
#pragma warning disable 649 // assigned using reflection
        private string _number;
#pragma warning restore 649

        #endregion

        #region Constructors

        internal ContactMethod(CodecApiElement parent, string propertyName, int indexer)
            : base(parent, propertyName, indexer)
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

        public string Number
        {
            get { return _number; }
        }

        #endregion

        #region Methods

        #endregion
    }
}