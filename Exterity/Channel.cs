using System;
using Crestron.SimplSharp;
using Newtonsoft.Json.Linq;

namespace UX.Lib2.Devices.Exterity
{
    public class Channel
    {
        private readonly AvediaServer _server;
        private readonly string _icon;

        #region Fields
        #endregion

        #region Constructors

        /// <summary>
        /// The default Constructor.
        /// </summary>
        internal Channel(AvediaServer server, JToken data)
        {
            _server = server;
            Id = data["id"].Value<string>();
            _icon = data["icon"].Value<string>();
            Name = data["name"].Value<string>();
            Uri = data["uri"].Value<string>();
            Number = uint.Parse(data["number"].Value<string>());
#if DEBUG
            CrestronConsole.PrintLine("IPTV Channel Discovered: {0} - {1}, {2}", Number, Name, Uri);
#endif
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

        public string IconThumbUrl
        {
            get
            {
                return String.IsNullOrEmpty(_icon)
                    ? string.Empty
                    : string.Format("http://{0}/portal/images/{1}.thumb.png", _server.HostNameOrIpAddress, _icon);
            }
        }

        public string IconUrl
        {
            get
            {
                return String.IsNullOrEmpty(_icon)
                  ? string.Empty
                  : string.Format("http://{0}/portal/images/{1}.png", _server.HostNameOrIpAddress, _icon);
            }
        }

        public string Name { get; internal set; }
        public string Uri { get; internal set; }
        public uint Number { get; internal set; }

        #endregion

        #region Methods

        #endregion
    }
}