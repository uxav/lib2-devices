using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SSMono.IO;
using SSMono.Net;

namespace UX.Lib2.Devices.Barco
{
    public static class BarcoE2Api
    {
        private static int _idCount = 0;

        private static JToken Post(string host, string method, JToken @params)
        {
            var id = _idCount ++;
            var request = WebRequest.CreateHttp("http://" + host + ":9999");
            request.Timeout = 3000;
            request.Method = "POST";
            var json = JToken.FromObject(new
            {
                @jsonrpc = "2.0", method, id, @params
            });
            var data = Encoding.UTF8.GetBytes(json.ToString());
            Debug.WriteInfo(request.Method, request.RequestUri.ToString());
            Debug.WriteInfo(Debug.AnsiPurple + json + Debug.AnsiReset);
            request.ContentType = "application/json";
            request.ContentLength = data.Length;
            Debug.WriteNormal("POST data length", data.Length.ToString());
            Debug.WriteNormal("Creating request stream");
            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }
            var response = (HttpWebResponse)request.GetResponse();
            if (response.StatusCode == HttpStatusCode.OK)
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

        public static IEnumerable<LayoutPreset> ListPresets(string host, int screenDest)
        {
            return
                Post(host, "listPresets", JToken.FromObject(new {@ScreenDest = screenDest}))["result"]["response"]
                    .OrderBy(item => item["presetSno"].Value<float>())
                    .Select(item => new LayoutPreset(item["id"].Value<int>(), item["Name"].Value<string>()));
        }

        public static void ActivatePreset(string host, int presetId, PresetRecallType type)
        {
            Post(host, "activatePreset", JToken.FromObject(new
            {
                @id = presetId,
                @type = (int) type
            }));
        }

        public enum PresetRecallType
        {
            Preview,
            Program
        }
    }

    public class LayoutPreset
    {
        private readonly int _id;
        private readonly string _name;

        public LayoutPreset(int id, string name)
        {
            _id = id;
            _name = name;
        }

        public int Id
        {
            get { return _id; }
        }

        public string Name
        {
            get { return _name; }
        }
    }
}