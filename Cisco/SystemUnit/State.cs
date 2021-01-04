 
namespace UX.Lib2.Devices.Cisco.SystemUnit
{
    public class State : CodecApiElement
    {
        #region Fields

        [CodecApiNameAttribute("NumberOfActiveCalls")]
#pragma warning disable 649 // assigned using reflection
        private int _numberOfActiveCalls;
#pragma warning restore 649

        [CodecApiNameAttribute("NumberOfInProgressCalls")]
#pragma warning disable 649 // assigned using reflection
        private int _numberOfInProgressCalls;
#pragma warning restore 649

        [CodecApiNameAttribute("NumberOfSuspendedCalls")]
#pragma warning disable 649 // assigned using reflection
        private int _numberOfSuspendedCalls;
#pragma warning restore 649

        #endregion

        #region Constructors

        /// <summary>
        /// The default Constructor.
        /// </summary>
        internal State(CodecApiElement parent, string propertyName)
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

        public int NumberOfActiveCalls
        {
            get { return _numberOfActiveCalls; }
        }

        public int NumberOfInProgressCalls
        {
            get { return _numberOfInProgressCalls; }
        }

        public int NumberOfSuspendedCalls
        {
            get { return _numberOfSuspendedCalls; }
        }

        public int TotalNumberOfCalls
        {
            get { return NumberOfActiveCalls + NumberOfInProgressCalls + NumberOfSuspendedCalls; }
        }

        #endregion

        #region Methods
        #endregion
    }
}