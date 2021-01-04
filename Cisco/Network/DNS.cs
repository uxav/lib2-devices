 
using System.Collections.Generic;
using Crestron.SimplSharp;

namespace UX.Lib2.Devices.Cisco.Network
{
    public class Dns : CodecApiElement
    {
        #region Fields

        [CodecApiNameAttribute("Server")]
        private Dictionary<int, Server> _servers = new Dictionary<int, Server>();

        [CodecApiNameAttribute("Domain")]
        private DnsDomain _domain;

        #endregion

        #region Constructors

        internal Dns(CodecApiElement parent, string propertyName)
            : base(parent, propertyName)
        {
            _domain = new DnsDomain(this, "Domain");
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events
        #endregion

        #region Delegates
        #endregion

        #region Properties

        public DnsDomain Domain
        {
            get { return _domain; }
        }

        public ReadOnlyDictionary<int, Server> Servers
        {
            get { return new ReadOnlyDictionary<int, Server>(_servers); }
        }

        #endregion

        #region Methods
        #endregion
    }
}