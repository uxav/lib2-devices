using System;
using Newtonsoft.Json.Linq;

namespace UX.Lib2.Devices.Exterity2
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
            Debug.WriteInfo("IPTV Channel Discovered", "{0} - {1}, {2}", Number, Name, Uri);
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
                    : string.Format("http://{0}/portal/feeds/image?imageid={1}&max=80",
                        _server.HostNameOrIpAddress, _icon);
            }
        }

        public string IconUrl
        {
            get
            {
                return String.IsNullOrEmpty(_icon)
                    ? string.Empty
                    : string.Format("http://{0}/portal/feeds/image?imageid={1}",
                        _server.HostNameOrIpAddress, _icon);
            }
        }

        public string GetIconUrl(int maxSize)
        {
            return String.IsNullOrEmpty(_icon)
                ? string.Empty
                : string.Format("http://{0}/portal/feeds/image?imageid={1}&max={2}",
                    _server.HostNameOrIpAddress, _icon, maxSize);
        }

        public string Name { get; internal set; }
        public string Uri { get; internal set; }
        public uint Number { get; internal set; }

        #endregion

        #region Methods

        #endregion
    }
}