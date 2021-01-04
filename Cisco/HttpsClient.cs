 
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using Crestron.SimplSharp;
using Crestron.SimplSharp.Net.Https;
using UX.Lib2.Cloud.Logger;
using Thread = Crestron.SimplSharpPro.CrestronThread.Thread;

namespace UX.Lib2.Devices.Cisco
{
    internal class HttpsClient
    {
        #region Fields

        private readonly Crestron.SimplSharp.Net.Https.HttpsClient _client = new Crestron.SimplSharp.Net.Https.HttpsClient
        {
            IncludeHeaders = true,
            HostVerification = false,
            PeerVerification = false,
            KeepAlive = false,
            Verbose = false,
            Timeout = 5
        };

        private readonly string _address;
        private readonly string _username;
        private readonly string _password;
        private string _sessionId;
        private readonly static CrestronQueue<CodecRequest> RequestQueue = new CrestronQueue<CodecRequest>(20);
        private readonly static CCriticalSection Lock = new CCriticalSection();

        private readonly static Dictionary<CodecRequest, CodecHttpClientRequestCallback> Callbacks =
            new Dictionary<CodecRequest, CodecHttpClientRequestCallback>(); 

        private static Thread _dispatchThread;

        #endregion

        #region Constructors

        /// <summary>
        /// The default Constructor.
        /// </summary>
        internal HttpsClient(string address, string username, string password)
        {
            _address = address;
            _username = username;
            _password = password;
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events
        #endregion

        #region Delegates
        #endregion

        #region Properties

        public bool Busy
        {
            get { return _client.ProcessBusy; }
        }

        public bool InSession
        {
            get { return !string.IsNullOrEmpty(_sessionId); }
        }

        #endregion

        #region Methods

        internal bool StartSession()
        {
            CodecRequest request;
            CodecResponse response;

            CloudLog.Info("Cisco Codec StartSession() called");

            if (!string.IsNullOrEmpty(_sessionId))
            {
                try
                {
                    CloudLog.Debug("Codec has session ID already... attempting to close it");

                    request = new CodecRequest(_address, "/xmlapi/session/end", _sessionId)
                    {
                        RequestType = RequestType.Post
                    };
                    response = Dispatch(request);
#if DEBUG
                    Debug.WriteInfo("Received headers for session end:\r\n" + response.Header);
#endif
                    CloudLog.Debug("Session close request result: {0}", response.Code);
                }
                catch (Exception e)
                {
                    Debug.WriteError("Error trying to close session on codec",
                        "Session ID to close: {0}, Error: {1}\r\nStackTrace: {2}", _sessionId, e.Message, e.StackTrace);
                    CloudLog.Error("Error trying to close the codec session, {0}", e.Message);
                }
            }
            try
            {
                request = new CodecRequest(_address, "/xmlapi/session/begin", _username, _password)
                {
                    RequestType = RequestType.Post
                };

                response = Dispatch(request);
#if DEBUG
                Debug.WriteInfo("Received headers for session begin:\r\n" + response.Header);
#endif
                if (response.Code == 401)
                {
                    CloudLog.Error("Error logging into Cisco Codec at \"{0}\" 401 - Unauthorized", _address);
                    return false;
                }

                if (response.Code == 204)
                {
                    if (!response.Header.ContainsHeaderValue("Set-Cookie"))
                    {
                        CloudLog.Error("Received 204 for Session ID but response headers contained no Cookie");
                        return false;
                    }
                    Debug.WriteSuccess("Codec Set-Cookie received", response.Header["Set-Cookie"]);
                    CloudLog.Notice("Codec Set-Cookie received\r\n{0}", response.Header["Set-Cookie"]);
                    var match = Regex.Match(response.Header["Set-Cookie"].Value, @"(.*?)=(.*?);");
                    if (match.Groups[1].Value != "SecureSessionId")
                    {
                        CloudLog.Error("Received 204 for Session ID but response headers contained no SessionId");
                        return false;
                    }
                    _sessionId = match.Groups[2].Value;

                    CloudLog.Info("Codec received new Session Id OK");
                    return true;
                }
            }
            catch (Exception e)
            {
                CloudLog.Error("Error trying to get a session ID from Cisco Codec, {0}", e.Message);
                return false;
            }
            return false;
        }

        public int GetXmlAsync(string path, CodecHttpClientRequestCallback callback)
        {
            var r = new CodecRequest(_address,
                string.Format("/getxml?location={0}", path.StartsWith("/") ? path : "/" + path), _sessionId);
            DispatchAsync(r, callback);
            return r.Id;
        }

        public CodecResponse GetXml(string path)
        {
            var r = new CodecRequest(_address,
                string.Format("/getxml?location={0}", path.StartsWith("/") ? path : "/" + path), _sessionId);

            var response = Dispatch(r);

            if (response.Code == 401)
            {
                StartSession();
            }

            return response;
        }

        public int PutXmlAsync(string xml, CodecHttpClientRequestCallback callback)
        {
            var r = new CodecRequest(_address, "/putxml", _sessionId)
            {
                RequestType = RequestType.Post,
                ContentString = xml
            };
            DispatchAsync(r, callback);
            return r.Id;
        }

        public CodecResponse PutXml(string xml)
        {
            var r = new CodecRequest(_address, "/putxml", _sessionId)
            {
                RequestType = RequestType.Post,
                ContentString = xml
            };

#if DEBUG
            Debug.WriteInfo("Dispatching request", "https://{0}/putxml", _address);
            Debug.WriteNormal(xml);
#endif

            var response = Dispatch(r);

            if (response.Code == 401)
            {
                StartSession();
            }

            return response;
        }

        private CodecResponse Dispatch(CodecRequest request)
        {
            try
            {
                var r = _client.Dispatch(request.Request);
#if DEBUG
                Debug.WriteSuccess("Received HTTPS Response", "{0}, {1} bytes", r.Code, r.ContentLength);
#endif

                return new CodecResponse(request, r);
            }
            catch (Exception e)
            {
                if (e is ThreadAbortException) return new CodecResponse(request, e);
                //CloudLog.Exception(e, "Error dispatching request ID {0} to codec", request.Id);
                return null;
            }
        }

        private void DispatchAsync(CodecRequest request, CodecHttpClientRequestCallback callback)
        {
            Lock.Enter();
            Callbacks[request] = callback;
            Lock.Leave();

            //CrestronConsole.PrintLine("Queuing request id:{0}", request.Id);
            RequestQueue.Enqueue(request);

            if (_dispatchThread == null || _dispatchThread.ThreadState == Thread.eThreadStates.ThreadFinished)
            {

                _dispatchThread = new Thread(DispatchThreadProcess, null)
                {
                    Name = "CodecHttpClient Process Thread",
                    Priority = Thread.eThreadPriority.HighPriority
                };
            }
        }

        private object DispatchThreadProcess(object userSpecific)
        {
            while (!RequestQueue.IsEmpty)
            {
                var request = RequestQueue.Dequeue();
                var callback = Callbacks[request];

                Lock.Enter();
                Callbacks.Remove(request);
                Lock.Leave();
#if DEBUG
                var ready = true;
#endif
                while (_client.ProcessBusy)
                {
#if DEBUG
                    if (ready)
                    {
                        //CrestronConsole.PrintLine("Waiting for Client...");
                        ready = false;
                    }
#endif
                    CrestronEnvironment.AllowOtherAppsToRun();
                }
#if DEBUG
                if (!ready)
                {
                    ready = true;
                    //CrestronConsole.PrintLine("Client Ready...");
                }
                CrestronConsole.PrintLine("Dispatching request...");
#endif
                try
                {
                    var r = _client.Dispatch(request.Request);
#if DEBUG
                    CrestronConsole.PrintLine("Received response, {0}, {1} bytes", r.Code, r.ContentLength);
#endif
                    var response = new CodecResponse(request, r);

                    if (response.Code == 401)
                    {
                        StartSession();
                    }

                    try
                    {
                        callback(response);
                    }
                    catch (Exception e)
                    {
                        CloudLog.Exception(e);
                    }
                }
                catch (Exception e)
                {
                    CloudLog.Exception(e);
                }
            }

            return null;
        }

        public void Abort()
        {
            _client.Abort();
        }

        #endregion
    }

    internal delegate void CodecHttpClientRequestCallback(CodecResponse response);
}