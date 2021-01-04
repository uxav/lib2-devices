using System;
using Newtonsoft.Json.Linq;
using UX.Lib2.Cloud.Logger;

namespace UX.Lib2.Devices.Audio.QSC
{
    public class QsysResponse
    {
        private readonly JToken _data;

        internal QsysResponse(JToken data, QsysRequest request)
        {
            try
            {
                _data = data;
                Request = request;

                Id = _data["id"].Value<int>();

                if (_data["error"] != null)
                {
                    IsError = true;
                    ErrorMessage = _data["error"]["message"].Value<string>();
                    return;
                }

                if (_data["result"] == null) return;

                if (_data["result"].Type != JTokenType.Boolean) return;
                if (!_data["result"].Value<bool>()) return;
                IsAck = true;
            }
            catch (Exception e)
            {
                IsError = true;
                ErrorMessage = "Response Parsing Error";
                CloudLog.Exception(e);
            }
        }

        public int Id { get; private set; }
        public bool IsAck { get; private set; }
        public bool IsError { get; private set; }
        public string ErrorMessage { get; private set; }
        public QsysRequest Request { get; private set; }

        public JToken Result
        {
            get { return _data["result"]; }
        }

        #region Overrides of Object

        public override string ToString()
        {
            return _data != null ? _data.ToString() : "QsysResponse Incomplete";
        }

        #endregion
    }
}