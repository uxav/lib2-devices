using System;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharp;
using Crestron.SimplSharp.Net.Http;
using Crestron.SimplSharpPro.CrestronThread;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UX.Lib2.Cloud.Logger;

namespace UX.Lib2.Devices.TriplePlay
{
    public static class TripleCare
    {
        private static readonly HttpClient HttpClient = new HttpClient
        {
            UseConnectionPooling = true,
            EnableNagle = true,
            KeepAlive = true
        };

        private static Thread _dispatchThread;
        private static readonly CrestronQueue<ServerRequest> RequestQueue = new CrestronQueue<ServerRequest>();
        private static readonly Dictionary<int, TripleCareChannelResponseCallback> ChannelResponseCallbacks =
            new Dictionary<int, TripleCareChannelResponseCallback>();
        private static readonly CCriticalSection ChannelResponseLock = new CCriticalSection();

        internal static void GetRequest(ServerRequest request)
        {
            RequestQueue.Enqueue(request);

            if (_dispatchThread == null || _dispatchThread.ThreadState != Thread.eThreadStates.ThreadRunning)
            {
                _dispatchThread = new Thread(specific =>
                {
#if true
                    CrestronConsole.PrintLine("Launching TripleCare.DispacthThread, Request Count = {0}", RequestQueue.Count);
#endif

                    while (RequestQueue.Count > 0)
                    {
                        if (!HttpClient.ProcessBusy)
                        {
#if true
                            CrestronConsole.PrintLine("TripleCare.HttpClient available, dispatching ...");
#endif
                            var r = RequestQueue.Dequeue();
#if true
                            CrestronConsole.PrintLine("{0} {1}", r.RequestType.ToString(), r.Url);
                            if (r.RequestType == RequestType.Post)
                            {
                                CrestronConsole.PrintLine(r.ContentString);
                            }
#endif
                            var response = HttpClient.Dispatch(r);
                            r.Callback(r.Id, response);
                        }

                        CrestronEnvironment.AllowOtherAppsToRun();
                    }

                    return null;
                }, null);
            }
        }

        internal static int GetRequest(TripleCareServerResponseCallback callback, string host, string method, params object[] parameters)
        {
            var callJson = JToken.FromObject(new
            {
                @jsonrpc = "2.0", method,
                @params = parameters
            });
            var request = new ServerRequest(host, "/triplecare/JsonRpcHandler.php?call=" + callJson.ToString(Formatting.None), callback);
            GetRequest(request);
            return request.Id;
        }

        public static void GetChannels(string host, int clientId, TripleCareChannelResponseCallback callback)
        {
            var id = GetRequest(GetChannelsCallback, host, "GetAllServices", clientId);
            ChannelResponseLock.Enter();
            ChannelResponseCallbacks[id] = callback;
            ChannelResponseLock.Leave();
        }

        private static void GetChannelsCallback(int requestId, HttpClientResponse response)
        {
            if (ChannelResponseCallbacks.ContainsKey(requestId))
            {
                var callback = ChannelResponseCallbacks[requestId];
                ChannelResponseLock.Enter();
                ChannelResponseCallbacks.Remove(requestId);
                ChannelResponseLock.Leave();

                if (response.Code != 200)
                {
                    callback(false, null);
                    return;
                }

                try
                {
                    var json = JToken.Parse(response.ContentString);
                    var channels = json["result"].Select(item => new Channel(item));
                    callback(true, channels);
                }
                catch (Exception e)
                {
                    CloudLog.Exception(e);
                    callback(false, null);
                }
            }
        }

        public static void SetChannel(string host, uint clientId, uint channelNumber)
        {
            GetRequest((id, response) =>
            {
                if (response.Code != 200)
                {
                    CloudLog.Error("Error trying to set channel, Response code: {0}", response.Code);
                }
            }, host, "SelectChannel", clientId, channelNumber);
        }

        public static void ChannelUp(string host, uint clientId)
        {
            CallMethodForClient(host, "ChannelUp", clientId);
        }

        public static void ChannelDown(string host, uint clientId)
        {
            CallMethodForClient(host, "ChannelDown", clientId);
        }

        private static void CallMethodForClient(string host, string method, uint clientId)
        {
            GetRequest((id, response) =>
            {
                if (response.Code != 200)
                {
                    CloudLog.Error("Error trying method \"{0}\", Response code: {1}", method, response.Code);
                }
            }, host, method, clientId);
        }
    }

    public delegate void TripleCareChannelResponseCallback(bool success, IEnumerable<Channel> channels);
}