 
using System.Text.RegularExpressions;

namespace UX.Lib2.Devices.Cisco.Phonebook
{
    public class PhonebookContactMethod
    {
        #region Fields
        
        private readonly PhonebookContact _contact;

        #endregion

        #region Constructors

        /// <summary>
        /// The default Constructor.
        /// </summary>
        internal PhonebookContactMethod(PhonebookContact contact)
        {
            _contact = contact;
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events
        #endregion

        #region Delegates
        #endregion

        #region Properties

        public int Id { get; internal set; }
        public string Number { get; internal set; }

        public PhonebookContact Contact
        {
            get { return _contact; }
        }

        public bool IsWebex
        {
            get { return Regex.IsMatch(Number, @"^[a-f,0-9]+\-[a-f,0-9]+\-[a-f,0-9]+\-[a-f,0-9]+\-[a-f,0-9]+$"); }
        }

        #endregion

        #region Methods

        public void Dial(DialResult callback)
        {
            Contact.Codec.Calls.DialNumber(Number, callback);
        }

        #endregion
    }
}