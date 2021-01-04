using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharp;
using Crestron.SimplSharp.Net.Http;
using Newtonsoft.Json.Linq;
using UX.Lib2.Cloud.Logger;

namespace UX.Lib2.Devices.Exterity2
{
    public class ChannelCollection : IEnumerable<Channel>
    {
        #region Fields
        private readonly AvediaServer _server;
        private readonly List<Channel> _channels = new List<Channel>(); 
        #endregion

        #region Constructors

        /// <summary>
        /// The default Constructor.
        /// </summary>
        internal ChannelCollection(AvediaServer server)
        {
            _server = server;
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events

        public event ChannelCollectionUpdatedEventHandler ChannelsUpdated;

        #endregion

        #region Delegates
        #endregion

        public Channel this[string id]
        {
            get { return _channels.First(c => c.Id == id); }
        }

        #region Properties
        #endregion

        #region Methods

        public void UpdateChannels()
        {
#if DEBUG
            CrestronConsole.PrintLine("Getting IPTV Channel list ....");
#endif
            _server.QueueRequest(new ServerRequest(_server.HostNameOrIpAddress, "/api/channels/portal", Callback));
        }

        private void Callback(HttpClientResponse response, HTTP_CALLBACK_ERROR error)
        {
            try
            {
                if (error != HTTP_CALLBACK_ERROR.COMPLETED)
                {
                    CloudLog.Warn("Cannot communicate with AvediaServer to discover channels");
                    return;
                }

                if (response.Code != 200)
                {
                    CloudLog.Error("{0} HttpResponse = {1}", GetType().Name, response.Code);
                    return;
                }

                var channels =
                    JToken.Parse(response.ContentString)["channel"].Select(channel => new Channel(_server, channel))
                        .ToList();

                if (channels.Count > 0)
                {
                    _channels.Clear();

                    foreach (var channel in channels)
                    {
                        _channels.Add(channel);
                    }

                    OnChannelsUpdated(this);
                }
                else
                {
                    CloudLog.Warn(
                        "AvediaServer returned no channels in API. Existing collection will remain to prevent loss of channel list.");
                }
            }
            catch (Exception e)
            {
                CloudLog.Exception(e);
            }
        }

        protected virtual void OnChannelsUpdated(ChannelCollection collection)
        {
            var handler = ChannelsUpdated;
            if (handler != null) handler(collection);
        }

        public IEnumerator<Channel> GetEnumerator()
        {
            return _channels.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }

    public delegate void ChannelCollectionUpdatedEventHandler(ChannelCollection collection);
}