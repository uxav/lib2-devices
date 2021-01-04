 
using System;
using System.Text;
using Crestron.SimplSharp.Net.Http;
using Crestron.SimplSharp.Net.Https;
using RequestType = Crestron.SimplSharp.Net.Https.RequestType;

namespace UX.Lib2.Devices.Cisco
{
    internal class CodecRequest
    {
        #region Fields

        private static int _requestIdCount;
        private readonly int _requestId = _requestIdCount++;

        #endregion

        #region Constructors

        /// <summary>
        /// The default Constructor.
        /// </summary>
        public CodecRequest(string address, string path, string username, string password)
        {
            var url = string.Format("https://{0}{1}", address, path.StartsWith("/") ? path : "/" + path);
            Request = new HttpsClientRequest {Url = new UrlParser(url), Encoding = Encoding.UTF8};

            var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + password));
            Request.Header.AddHeader(new HttpsHeader("Authorization", "Basic " + auth));
        }

        /// <summary>
        /// The default Constructor.
        /// </summary>
        public CodecRequest(string address, string path, string sessionId)
        {
            var url = string.Format("https://{0}{1}", address, path.StartsWith("/") ? path : "/" + path);
            Request = new HttpsClientRequest {Url = new UrlParser(url), Encoding = Encoding.UTF8};

            var cookieHeader = new HttpsHeader("Cookie", string.Format("SecureSessionId={0}", sessionId));
            Request.Header.AddHeader(cookieHeader);
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events
        #endregion

        #region Delegates
        #endregion

        #region Properties

        public int Id
        {
            get { return _requestId; }
        }

        #endregion

        #region Methods

        public HttpsClientRequest Request { get; private set; }

        public RequestType RequestType
        {
            get { return Request.RequestType; }
            set { Request.RequestType = value; }
        }

        public string ContentString
        {
            get { return Request.ContentString; }
            set { Request.ContentString = value; }
        }

        #endregion
    }
}