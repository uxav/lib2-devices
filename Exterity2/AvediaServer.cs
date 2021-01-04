using System;
using System.Linq;
using Crestron.SimplSharp;
using Crestron.SimplSharp.Net.Http;
using Crestron.SimplSharpPro.CrestronThread;
using UX.Lib2.Cloud.Logger;

namespace UX.Lib2.Devices.Exterity2
{
    public class AvediaServer
    {
        #region Fields
        private readonly string _hostNameOrIpAddress;

        private static readonly HttpClient HttpClient = new HttpClient
        {
            EnableNagle = true,
            Timeout = 5
        };

        private static readonly CrestronQueue<ServerRequest> RequestQueue = new CrestronQueue<ServerRequest>(20);

        private static Thread _dispatchThread;

        private bool _initialized;
        private CTimer _timer;
        private int _refreshCount;
        private bool _programStopping;

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
            Recording = new Recordings(this);
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
        public Recordings Recording { get; private set; }

        #endregion

        #region Methods

        internal void QueueRequest(ServerRequest request)
        {
            RequestQueue.Enqueue(request);

            if (_dispatchThread != null && _dispatchThread.ThreadState == Thread.eThreadStates.ThreadRunning) return;

            _dispatchThread = new Thread(specific =>
            {
#if true
                Debug.WriteSuccess("AvediaServer", "Launching {0}.DispacthThread, Request Count = {1}", GetType().Name,
                    RequestQueue.Count);
                Debug.WriteInfo("AvediaServer", "HttpClient Timeout = {0}, TimeoutEnabled = {1}", HttpClient.Timeout,
                    HttpClient.TimeoutEnabled);
#endif

                while (true)
                {
                    var r = RequestQueue.Dequeue();
                    if (request == null)
                    {
                        CloudLog.Info("Exiting {0}", Thread.CurrentThread.Name);
                        return null;
                    }
#if true
                    CrestronConsole.PrintLine("{0} {1}", r.RequestType.ToString(), r.Url);
                    if (r.RequestType == RequestType.Post)
                    {
                        CrestronConsole.PrintLine(r.ContentString);
                    }
#endif
                    try
                    {
                        var response = HttpClient.Dispatch(r);

                        try
                        {
                            r.Callback(response, HTTP_CALLBACK_ERROR.COMPLETED);
                        }
                        catch (Exception e)
                        {
                            CloudLog.Exception(e);
                        }
                    }
                    catch
                    {
                        r.Callback(null, HTTP_CALLBACK_ERROR.UNKNOWN_ERROR);
                    }

                    CrestronEnvironment.AllowOtherAppsToRun();
                }
            }, null)
            {
                Name = "Avedia HTTP dispatch process"
            };
        }

        public void Initialize()
        {
            if (_timer != null) return;
            
            _refreshCount = 60;

            CrestronEnvironment.ProgramStatusEventHandler += type =>
            {
                _programStopping = type == eProgramStatusEventType.Stopping;
                if (_programStopping)
                {
                    RequestQueue.Enqueue(null);
                }
            };

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