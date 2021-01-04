using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharp.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UX.Lib2.Cloud.Logger;

namespace UX.Lib2.Devices.Exterity2
{
    public class ReceiverCollection : IEnumerable<Receiver>
    {

        #region Fields

        private readonly AvediaServer _server;
        private readonly List<Receiver> _receivers = new List<Receiver>();

        #endregion

        #region Constructors

        internal ReceiverCollection(AvediaServer server)
        {
            _server = server;
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events

        public event ReceiverDiscoveredEventHandler ReceiverDiscovered;

        #endregion

        #region Delegates
        #endregion

        public Receiver this[string id]
        {
            get { return _receivers.FirstOrDefault(r => r.Id == id); }
        }

        #region Properties
        #endregion

        #region Methods

        public void Discover()
        {
#if DEBUG
            Debug.WriteInfo("Discovering IPTV Receivers....");
#endif
            _server.QueueRequest(new ServerRequest(_server.HostNameOrIpAddress, "/api/control/devices?views=control,artiosign", Callback));
        }

        private void Callback(HttpClientResponse response, HTTP_CALLBACK_ERROR error)
        {
            try
            {
                if (error != HTTP_CALLBACK_ERROR.COMPLETED)
                {
                    CloudLog.Warn("Cannot communicate with AvediaServer to discover receivers");
                    return;
                }

                if (response.Code != 200)
                {
                    CloudLog.Error("{0} HttpResponse = {1}", GetType().Name, response.Code);
                    return;
                }

                var data = JArray.Parse(response.ContentString);
#if true
                Debug.WriteInfo(Debug.AnsiPurple + data.ToString(Formatting.Indented) + Debug.AnsiReset);
#endif
                foreach (
                    var device in
                        data.Where(
                            d => d["type"].Value<string>() == "Media Player" || d["type"].Value<string>() == "Receiver")
                    )
                {
                    if (this.Any(d => d.Id == device["id"].Value<string>()))
                    {
                        this[device["id"].Value<string>()].UpdateInfo(device);
                    }
                    else
                    {
                        Receiver receiver = null;
                        try
                        {
                            receiver = new Receiver(_server, device);
                        }
                        catch (Exception e)
                        {
                            CloudLog.Exception(e, "Error loading receiver object from data");
                        }
                        if (receiver == null) continue;
                        _receivers.Add(receiver);
                        OnReceiverDiscovered(receiver, receiver.Id, receiver.Name);
                    }
                }
            }
            catch (Exception e)
            {
                CloudLog.Exception(e);
            }
        }

        protected virtual void OnReceiverDiscovered(Receiver device, string id, string name)
        {
            var handler = ReceiverDiscovered;
            if (handler == null) return;
            try
            {
                handler(device, id, name);
            }
            catch (Exception e)
            {
                CloudLog.Exception(e);
            }
        }

        public IEnumerator<Receiver> GetEnumerator()
        {
            return _receivers.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }

    public delegate void ReceiverDiscoveredEventHandler(Receiver receiver, string id, string name);
}