using Crestron.SimplSharp.Net.Http;

namespace UX.Lib2.Devices.TriplePlay
{
    internal class ServerRequest : HttpClientRequest
    {
        #region Fields

        private readonly TripleCareServerResponseCallback _callback;
        private static int _idCount = 0;

        #endregion

        #region Constructors

        /// <summary>
        /// The default Constructor.
        /// </summary>
        internal ServerRequest(string host, string uri, TripleCareServerResponseCallback callback)
        {
            Id = _idCount ++;
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

        public TripleCareServerResponseCallback Callback
        {
            get { return _callback; }
        }

        public int Id { get; private set; }

        #endregion

        #region Methods
        #endregion
    }

    public delegate void TripleCareServerResponseCallback(int requestId, HttpClientResponse response);
}