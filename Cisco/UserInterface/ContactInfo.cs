 
using System.Collections.Generic;
using Crestron.SimplSharp;

namespace UX.Lib2.Devices.Cisco.UserInterface
{
    public class ContactInfo : CodecApiElement
    {
        #region Fields

        [CodecApiNameAttribute("Name")]
#pragma warning disable 649 // assigned using reflection
        private string _name;
#pragma warning restore 649

        [CodecApiNameAttribute("ContactMethod")]
        private readonly Dictionary<int, ContactMethod> _contactMethod = new Dictionary<int, ContactMethod>();

        #endregion

        #region Constructors

        internal ContactInfo(CodecApiElement parent, string propertyName)
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

        public string Name
        {
            get { return _name; }
        }

        public ReadOnlyDictionary<int, ContactMethod> ContactMethod
        {
            get { return new ReadOnlyDictionary<int, ContactMethod>(_contactMethod); }
        }

        #endregion

        #region Methods

        #endregion
    }
}