using System.Collections.Generic;
using Crestron.SimplSharp;

namespace UX.Lib2.Devices.Cisco.UserInterface
{
    public class UserInterface : CodecApiElement
    {
        #region Fields

        [CodecApiNameAttribute("ContactInfo")]
        private ContactInfo _contactInfo;

        private Presentation _presentation;

        [CodecApiNameAttribute("Extensions")]
        private Extensions _extensions;

        #endregion

        #region Constructors

        internal UserInterface(CiscoTelePresenceCodec codec)
            : base(codec)
        {
            _contactInfo = new ContactInfo(this, "ContactInfo");
            _presentation = new Presentation(codec);
            _extensions = new Extensions(this, "Extensions");
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events
        #endregion

        #region Delegates
        #endregion

        #region Properties

        public ContactInfo ContactInfo
        {
            get { return _contactInfo; }
        }

        public Presentation Presentation
        {
            get { return _presentation; }
        }

        public Extensions Extensions
        {
            get { return _extensions; }
        }

        #endregion

        #region Methods
        #endregion
    }
}