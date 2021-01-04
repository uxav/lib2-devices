using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UX.Lib2.Cloud.Logger;

namespace UX.Lib2.Devices.Audio.QSC
{
    public class QsysRequest
    {
        internal QsysRequest(JToken data)
        {
            try
            {
                if (data["method"] != null)
                {
                    Method = data["method"].Value<string>();

                    if (data["id"] != null)
                        Id = data["id"].Value<int>();

                    if (data["params"] != null)
                    {
                        Args = data["params"];
                    }

                    return;
                }

                CloudLog.Error("Error processing {0} from data string", GetType().Name);
            }
            catch (Exception e)
            {
                CloudLog.Exception(e);
            }
        }

        internal QsysRequest(QsysSocket socket, string method, object args)
        {
            Socket = socket;
            Id = socket.GetNextId();
            Method = method;
            if (args != null)
                Args = JToken.FromObject(args);
        }

        public QsysSocket Socket { get; private set; }
        public int Id { get; private set; }
        public string Method { get; private set; }
        public JToken Args { get; private set; }

        #region Overrides of Object

        public override string ToString()
        {
            return JsonConvert.SerializeObject(new
            {
                @jsonrpc = "2.0",
                @method = Method,
                @id = Id,
                @params = Args
            });
        }

        #endregion
    }
}