 
namespace UX.Lib2.Devices.Cisco.Phonebook
{
    public abstract class PhonebookItem
    {
        #region Fields
        
        private readonly CiscoTelePresenceCodec _codec;

        #endregion

        #region Constructors

        protected PhonebookItem(CiscoTelePresenceCodec codec, string id, string parentId, string name, PhonebookType phonebookType)
        {
            _codec = codec;
            Id = id;
            ParentId = parentId;
            Name = name;
            if(this is PhonebookContact)
                Type = PhonebookItemType.Contact;
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events
        #endregion

        #region Delegates
        #endregion

        #region Properties

        public string Id { get; internal set; }
        public string ParentId { get; internal set; }
        public string Name { get; internal set; }
        public PhonebookItemType Type { get; private set; }
        public PhonebookType PhonebookType { get; private set; }

        public CiscoTelePresenceCodec Codec
        {
            get { return _codec; }
        }

        #endregion

        #region Methods
        #endregion
    }

    public enum PhonebookItemType
    {
        Folder,
        Contact
    }
}