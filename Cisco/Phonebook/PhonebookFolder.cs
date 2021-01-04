 
using Crestron.SimplSharp.CrestronXmlLinq;

namespace UX.Lib2.Devices.Cisco.Phonebook
{
    public class PhonebookFolder : PhonebookItem
    {
        #region Fields
        #endregion

        #region Constructors

        public PhonebookFolder(CiscoTelePresenceCodec codec, XElement element, PhonebookType phonebookType)
            : base(codec, element.Element("LocalId").Value, element.Element("FolderId").Value, element.Element("Name").Value, phonebookType)
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
        #endregion

        #region Methods
        #endregion
    }
}