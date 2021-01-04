 
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronXmlLinq;

namespace UX.Lib2.Devices.Cisco.Phonebook
{
    public class PhonebookContact : PhonebookItem, IEnumerable<PhonebookContactMethod>
    {
        #region Fields

        private readonly Dictionary<int, PhonebookContactMethod> _contactMethods =
            new Dictionary<int, PhonebookContactMethod>(); 

        #endregion

        #region Constructors

        internal PhonebookContact(CiscoTelePresenceCodec codec, XElement element, PhonebookType phonebookType)
            : base(codec,
                element.Element("ContactId").Value,
                element.Element("FolderId") != null ? element.Element("FolderId").Value : string.Empty,
                element.Element("Name").Value, phonebookType)
        {
            foreach (var newMethod in from contactMethod in element.Elements("ContactMethod")
                let id = int.Parse(contactMethod.Element("ContactMethodId").Value)
                let number = contactMethod.Element("Number").Value
                select new PhonebookContactMethod(this)
                {
                    Id = id,
                    Number = number
                })
            {
                _contactMethods.Add(newMethod.Id, newMethod);
            }
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events
        #endregion

        #region Delegates
        #endregion

        #region Properties

        public PhonebookContactMethod this[int index]
        {
            get { return _contactMethods[index]; }
        }

        public ReadOnlyDictionary<int, PhonebookContactMethod> ContactMethods
        {
            get { return new ReadOnlyDictionary<int, PhonebookContactMethod>(_contactMethods); }
        }

        public int NumberOfContactMethods
        {
            get { return _contactMethods.Count; }
        }

        public bool ContainsMultipleContactMethods
        {
            get { return NumberOfContactMethods > 1; }
        }

        #endregion

        #region Methods

        public IEnumerator<PhonebookContactMethod> GetEnumerator()
        {
            return _contactMethods.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}