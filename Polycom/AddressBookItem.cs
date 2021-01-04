namespace UX.Lib2.Devices.Polycom
{
    public class AddressBookItem
    {
        public AddressBookItem(PolycomGroupSeriesCodec codec)
        {
            Codec = codec;
        }

        PolycomGroupSeriesCodec Codec { get; set; }

        public string Name { get; set; }
        public string Number { get; set; }
        public string Speed { get; set; }
        public string Extension { get; set; }
        public AddressBookItemType ItemType { get; set; }

        public void Dial()
        {
            Codec.Send(string.Format("dial addressbook \"{0}\"", Name));
        }
    }

    public enum AddressBookItemType
    {
        SIP,
        H323
    }
}