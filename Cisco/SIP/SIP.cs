 
using System.Collections.Generic;
using Crestron.SimplSharp;

namespace UX.Lib2.Devices.Cisco.SIP
{
    public class SIP : CodecApiElement
    {
        #region Fields

        [CodecApiNameAttribute("Proxy")]
        private Dictionary<int, Proxy> _proxy = new Dictionary<int, Proxy>();

        [CodecApiNameAttribute("Registration")]
        private Dictionary<int, Registration> _registration = new Dictionary<int, Registration>();

        [CodecApiNameAttribute("Verified")]
#pragma warning disable 649 // assigned using reflection
        private bool _verified;
#pragma warning restore 649

        [CodecApiNameAttribute("Secure")]
#pragma warning disable 649 // assigned using reflection
        private bool _secure;
#pragma warning restore 649

        [CodecApiNameAttribute("Authentication")]
#pragma warning disable 649 // assigned using reflection
        private string _authentication;
#pragma warning restore 649

        #endregion

        #region Constructors

        internal SIP(CiscoTelePresenceCodec codec)
            : base(codec)
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

        public string Authentication
        {
            get { return _authentication; }
        }

        public bool Secure
        {
            get { return _secure; }
        }

        public bool Verified
        {
            get { return _verified; }
        }

        public ReadOnlyDictionary<int, Proxy> Proxy
        {
            get { return new ReadOnlyDictionary<int, Proxy>(_proxy); }
        }

        public ReadOnlyDictionary<int, Registration> Registration
        {
            get { return new ReadOnlyDictionary<int, Registration>(_registration); }
        }

        #endregion

        #region Methods
        #endregion
    }
}