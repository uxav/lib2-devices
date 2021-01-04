using System;
using System.Collections.Generic;
using Crestron.SimplSharpPro.CrestronThread;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SSMono;
using SSMono.Net;
using SSMono.Net.Http;
using SSMono.Net.Http.Headers;
using SSMono.Threading.Tasks;
using UX.Lib2.Cloud.Logger;

namespace UX.Lib2.Devices.Displays.Sony
{
    public static class SonyBraviaHttpClient
    {
        private static readonly HttpClient Client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        private static long _id = 1;

        public static Task<SonyBraviaResponse> Request(string host, string psk, string path, string method, string version, params JObject[] args)
        {
            return Task.Run(() => RequestWork(host, psk, path, method, version, args));
        }

        private static SonyBraviaResponse RequestWork(string host, string psk, string path, string method, string version, params JObject[] args)
        {
            var id = _id ++;

            var uriBuilder = new UriBuilder {Path = path, Host = host};
            var uri = uriBuilder.Uri;

#if DEBUG
            Debug.WriteWarn("SonyHttpClient", "New \"{0}\" Request to \"{1}\"", method, uri);
#endif

            var jArray = new JArray();
            foreach (var o in args)
            {
                jArray.Add(o);
            }
            var jObject = new JObject
            {
                {"method", method},
                {"id", id},
                {"version", version},
                {"params", jArray}
            };
            var dataString = jObject.ToString(Formatting.Indented);
            var content = new StringContent(dataString);
            content.Headers.Add("X-Auth-PSK", psk);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

#if DEBUG
            Debug.WriteInfo("SonyHttpClient", "{0} {1}\r\n{2}", "POST", uri, dataString);
#endif
            var attempt = 0;
            HttpResponseMessage response = null;
            Exception error = null;
            while (attempt <= 4)
            {
                attempt ++;
                try
                {
                    var task = Client.PostAsync(uri, content);
#if DEBUG
                    Debug.WriteInfo("SonyHttp", "Task created... awaiting response");
#endif
                    response = task.Await();
                    break;
                }
                catch (Exception e)
                {
                    error = e;
                    if (e is TaskCanceledException)
                    {
                        CloudLog.Warn("SonyHttpClient Timed out trying to connect to {0}", uri.Host);
                    }
                    else
                    {
                        CloudLog.Error("SonyHttpClient Failed to connect to {0}, {2} {1}", uri.Host,
                            e.Message, e.GetType().Name);
                        if (e is WebException)
                        {
                            return new SonyBraviaResponse(uri, method, SonyBraviaResponseType.Failed, id, error);                            
                        }
                    }

                    Thread.Sleep(200);
                }
            }

            if (response == null)
            {
                return new SonyBraviaResponse(uri, method, SonyBraviaResponseType.Failed, id, error);
            }
#if DEBUG
            Debug.WriteInfo("SonyHttpClient", "Response Code: {0}", response.StatusCode);
#endif
            if (!response.IsSuccessStatusCode)
            {
                CloudLog.Warn("SonyHttpClient Received error, response code {0}", response.StatusCode);
                return new SonyBraviaResponse(uri, method, SonyBraviaResponseType.Failed, id, null);
            }

            JObject result;
            try
            {
                result = JObject.Parse(response.Content.ReadAsStringAsync().Await());
            }
            catch (Exception e)
            {
                CloudLog.Error("Could not parse data from display, {0}", e.Message);
                return new SonyBraviaResponse(uri, method, SonyBraviaResponseType.Failed, id, e);
            }
#if DEBUG
            Debug.WriteSuccess("SonyHttpClient", "Response\r\n{0}", result.ToString(Formatting.Indented));
#endif
            if (result["error"] != null)
            {
                CloudLog.Error("Error received from Sony Display for request: {0}, {1}", uri, result["error"].ToString());
            }

            return new SonyBraviaResponse(uri, method, result, id);
        }
    }

    public class SonyBraviaResponse
    {
        private readonly string _method;

        internal SonyBraviaResponse(Uri requestUri, string method, IDictionary<string, JToken> data, long id)
        {
            _method = method;
            RequestUri = requestUri;
            Id = id;
            try
            {
                if (data["result"] != null)
                {
                    Data = data["result"] as JArray;
                    Type = SonyBraviaResponseType.Success;
                }
                
                else if (data["error"] != null)
                {
                    Data = data["error"] as JArray;
                    Type = SonyBraviaResponseType.Error;
                }
            }
            catch (Exception e)
            {
                Exception = e;
                Type = SonyBraviaResponseType.Failed;
            }
        }

        internal SonyBraviaResponse(Uri requestUri, string method, SonyBraviaResponseType type, long id, Exception exception)
        {
            _method = method;
            RequestUri = requestUri;
            Type = type;
            Id = id;
            Exception = exception;
        }

        public Uri RequestUri { get; private set; }

        public string RequestMethod
        {
            get { return _method; }
        }

        public long Id { get; private set; }
        public JArray Data { get; private set; }
        public SonyBraviaResponseType Type { get; private set; }
        public Exception Exception { get; private set; }

        public bool ConnectionFailed
        {
            get
            {
                return Exception != null && Exception is WebException && Exception.Message.Contains("ConnectFailure");
            }
        }
    }

    public enum SonyBraviaResponseType
    {
        Success,
        Error,
        Failed
    }
}