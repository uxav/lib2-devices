 
using System;

namespace UX.Lib2.Devices.Cisco.Bookings
{
    public class CurrentBooking : CodecApiElement
    {
        #region Fields

        [CodecApiName("Id")]
#pragma warning disable 649
        private string _id;
#pragma warning restore 649

        #endregion

        #region Constructors

        internal CurrentBooking(CodecApiElement parent, string propertyName)
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

        public string Id
        {
            get { return _id; }
        }

        #endregion

        #region Methods
        #endregion
    }
}