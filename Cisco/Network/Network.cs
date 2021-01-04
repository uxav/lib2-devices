 
namespace UX.Lib2.Devices.Cisco.Network
{
    public class Network : CodecApiElement
    {
        #region Fields

        [CodecApiNameAttribute("VLAN")]
        private Vlan _vlan;

        [CodecApiNameAttribute("IPV6")]
        private Ipv6 _ipv6;

        [CodecApiNameAttribute("IPV4")]
        private Ipv4 _ipv4;

        [CodecApiNameAttribute("Ethernet")]
        private Ethernet _ethernet;

        [CodecApiNameAttribute("DNS")]
        private Dns _dns;

        [CodecApiNameAttribute("CDP")]
        private Cdp _cdp;

        #endregion

        #region Constructors

        internal Network(CiscoTelePresenceCodec codec, int indexer)
            : base(codec, indexer)
        {
            _cdp = new Cdp(this, "CDP");
            _dns = new Dns(this, "DNS");
            _ethernet = new Ethernet(this, "Ethernet");
            _ipv4 = new Ipv4(this, "IPV4");
            _ipv6 = new Ipv6(this, "IPV6");
            _vlan = new Vlan(this, "VLAN");
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events
        #endregion

        #region Delegates
        #endregion

        #region Properties

        public Cdp Cdp
        {
            get { return _cdp; }
        }

        public Dns Dns
        {
            get { return _dns; }
        }

        public Ethernet Ethernet
        {
            get { return _ethernet; }
        }

        public Ipv4 Ipv4
        {
            get { return _ipv4; }
        }

        public Ipv6 Ipv6
        {
            get { return _ipv6; }
        }

        public Vlan Vlan
        {
            get { return _vlan; }
        }

        #endregion

        #region Methods
        #endregion
    }
}