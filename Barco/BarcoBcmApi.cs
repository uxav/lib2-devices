using System;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SSMono.IO;
using SSMono.Net;

namespace UX.Lib2.Devices.Barco
{
    public static class BarcoBcmApi
    {
        private static JToken Get(string host, string uri)
        {
            var request = WebRequest.CreateHttp("http://" + host + (uri.StartsWith("/") ? "" : "") + uri);
            request.Timeout = 3000;

            request.Method = "GET";
            Debug.WriteInfo(request.Method, request.RequestUri.ToString());
            var response = (HttpWebResponse)request.GetResponse();
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Debug.WriteSuccess("Response", response.StatusCode.ToString());
                var reply = new StreamReader(response.GetResponseStream()).ReadToEnd();
                var json = JToken.Parse(reply);
                Debug.WriteInfo(Debug.AnsiGreen + json.ToString(Formatting.Indented) + Debug.AnsiReset);
                return json;
            }

            Debug.WriteWarn("Response", response.StatusCode.ToString());
            throw new WebException("Status code returned " + response.StatusCode);
        }

        private static JToken Post(string host, string action, JArray @params)
        {
            var request = WebRequest.CreateHttp("http://" + host + "/dramp/2/wall/actions");
            request.Timeout = 3000;
            request.Method = "POST";
            var json = JToken.FromObject(new
            {
                @action = new
                {
                    @name = action
                },
                @params
            });
            var data = Encoding.UTF8.GetBytes(json.ToString());
            Debug.WriteInfo(request.Method, request.RequestUri.ToString());
            Debug.WriteInfo(Debug.AnsiPurple + json + Debug.AnsiReset);
            request.Accept = "*/*";
            request.ContentType = "application/json";
            request.ContentLength = data.Length;
            request.ServicePoint.Expect100Continue = false;
            Debug.WriteNormal("POST data length", data.Length.ToString());
            Debug.WriteNormal("Creating request stream");
            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }
            for (var h = 0; h < request.Headers.Count; h++)
            {
                Debug.WriteInfo(request.Headers.GetKey(h), request.Headers.Get(h));
            }
            var response = (HttpWebResponse)request.GetResponse();
            if (response.StatusCode == HttpStatusCode.Accepted)
            {
                Debug.WriteSuccess("Response", response.StatusCode.ToString());
                var reply = new StreamReader(response.GetResponseStream()).ReadToEnd();
                json = JToken.Parse(reply);
                Debug.WriteInfo(Debug.AnsiGreen + json.ToString(Formatting.Indented) + Debug.AnsiReset);
                return json;
            }
            
            Debug.WriteWarn("Response", response.StatusCode.ToString());
            throw new WebException("Status code returned " + response.StatusCode);
        }

        public static OperationStateValue GetOperationState(string host)
        {
            var response = Get(host, "/dramp/2/wall/data/device/operationState");
            var param = response["params"].First;
            if (param["state"].Value<string>() == "STATE_VALID")
            {
                return
                    (OperationStateValue) Enum.Parse(typeof (OperationStateValue), param["value"].Value<string>(), true);
            }

            return OperationStateValue.Unknown;
        }

        public static void SetOperationState(string host, OperationStateValue state)
        {
            if (state == OperationStateValue.Unknown)
            {
                throw new ArgumentException("State cannot be set to " + state);
            }

            Post(host, "updateOperationState", JArray.FromObject(new[]
            {
                new
                {
                    @name = "pOperationState",
                    @value = state.ToString().ToUpper()
                }
            }));
        }

        public enum OperationStateValue
        {
            Unknown,
            Idle,
            On,
        }
    }
}