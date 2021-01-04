using Crestron.SimplSharp.Net.Http;

namespace UX.Lib2.Devices.Exterity
{
    internal class ServerRequest : HttpClientRequest
    {
        #region Fields

        private readonly HTTPClientResponseCallback _callback;

        #endregion

        #region Constructors

        /// <summary>
        /// The default Constructor.
        /// </summary>
        internal ServerRequest(string host, string uri, HTTPClientResponseCallback callback)
        {
            Url = new UrlParser(string.Format("http://{0}{1}", host, uri.StartsWith("/") ? uri : "/" + uri));
            _callback = callback;
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events
        #endregion

        #region Delegates
        #endregion

        #region Properties

        public HTTPClientResponseCallback Callback
        {
            get { return _callback; }
        }

        #endregion

        #region Methods
        #endregion
    }
}