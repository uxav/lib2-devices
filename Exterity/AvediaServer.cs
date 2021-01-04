using System;
using System.Linq;
using Crestron.SimplSharp;
using Crestron.SimplSharp.Net.Http;
using Crestron.SimplSharpPro.CrestronThread;
using UX.Lib2.Cloud.Logger;

namespace UX.Lib2.Devices.Exterity
{
    public class AvediaServer
    {
        #region Fields
        private readonly string _hostNameOrIpAddress;

        private static readonly HttpClient HttpClient = new HttpClient
        {
            UseConnectionPooling = true,
            EnableNagle = true,
            KeepAlive = false
        };

        private readonly CrestronQueue<ServerRequest> _requestQueue = new CrestronQueue<ServerRequest>();

        private Thread _dispatchThread;

        private bool _initialized;
        private CTimer _timer;
        private int _refreshCount;

        #endregion

        #region Constructors

        /// <summary>
        /// The default Constructor.
        /// </summary>
        public AvediaServer(string hostNameOrIpAddress)
        {
            _hostNameOrIpAddress = hostNameOrIpAddress;
            Receivers = new ReceiverCollection(this);
            Channels = new ChannelCollection(this);
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events

        public event AvediaServerInitializedEventHandler HasInitialized;

        #endregion

        #region Delegates
        #endregion

        #region Properties

        public string HostNameOrIpAddress
        {
            get { return _hostNameOrIpAddress; }
        }

        public ReceiverCollection Receivers { get; private set; }
        public ChannelCollection Channels { get; private set; }

        #endregion

        #region Methods

        internal void GetRequest(ServerRequest request)
        {
            _requestQueue.Enqueue(request);

            if (_dispatchThread == null || _dispatchThread.ThreadState != Thread.eThreadStates.ThreadRunning)
            {
                _dispatchThread = new Thread(specific =>
                {
#if DEBUG
                    CrestronConsole.PrintLine("Launching {0}.DispacthThread, Request Count = {1}", GetType().Name,
                        _requestQueue.Count);
#endif

                    while (_requestQueue.Count > 0)
                    {
                        if (!HttpClient.ProcessBusy)
                        {
#if DEBUG
                            CrestronConsole.PrintLine("{0}.HttpClient available, dispatching ...", GetType().Name);
#endif
                            var r = _requestQueue.Dequeue();
#if DEBUG
                            CrestronConsole.PrintLine("{0} {1}", r.RequestType.ToString(), r.Url);
                            if (r.RequestType == RequestType.Post)
                            {
                                CrestronConsole.PrintLine(r.ContentString);
                            }
#endif
                            HttpClient.DispatchAsync(r, r.Callback);
                        }

                        CrestronEnvironment.AllowOtherAppsToRun();
                    }

                    return null;
                }, null);
            }
        }

        public void Initialize()
        {
            if (_timer != null) return;
            
            _refreshCount = 60;
            _timer = new CTimer(specific =>
            {
                _refreshCount = _refreshCount - 1;

                try
                {
                    if (_refreshCount == 0 || !Receivers.Any())
                    {
                        Receivers.Discover();
                    }
                }
                catch (Exception e)
                {
                    CloudLog.Error("Error getting receiver collection from AvediaServer, {0}", e.Message);
                }

                try
                {
                    if (_refreshCount == 0 || !Channels.Any())
                    {
                        Channels.UpdateChannels();
                    }
                }
                catch (Exception e)
                {
                    CloudLog.Error("Error getting channel collection from AvediaServer, {0}", e.Message);
                }

                if (!_initialized)
                {
                    _initialized = true;
                    OnHasInitialized(this);
                }

                if (_refreshCount == 0)
                {
                    _refreshCount = 60;
                }
            }, null, 1000, (long) TimeSpan.FromMinutes(1).TotalMilliseconds);
        }

        protected virtual void OnHasInitialized(AvediaServer server)
        {
            var handler = HasInitialized;
            try
            {
                if (handler != null) handler(server);
            }
            catch (Exception e)
            {
                CloudLog.Exception(e);   
            }
        }

        #endregion
    }

    public delegate void AvediaServerInitializedEventHandler(AvediaServer server);
}