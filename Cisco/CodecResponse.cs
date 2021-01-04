 
using System;
using System.Threading;
using Crestron.SimplSharp.CrestronXml;
using Crestron.SimplSharp.CrestronXmlLinq;
using Crestron.SimplSharp.Net.Https;
using UX.Lib2.Cloud.Logger;

namespace UX.Lib2.Devices.Cisco
{
    internal class CodecResponse
    {

        #region Fields

        private readonly CodecRequest _request;
        private readonly HttpsClientResponse _response;
        private readonly XDocument _xml;

        #endregion

        #region Constructors

        internal CodecResponse(CodecRequest request, HttpsClientResponse response)
        {
            _request = request;
            _response = response;

            if(_response.ContentLength == 0) return;
            
            try
            {
#if DEBUG
                Debug.WriteNormal(Debug.AnsiBlue + _xml + Debug.AnsiReset);
#endif
                if (response.Code == 200)
                {
                    var reader = new XmlReader(response.ContentString);
                    _xml = XDocument.Load(reader);
                }
            }
            catch (Exception e)
            {
                CloudLog.Exception(e, "Error parsing xml document from codec");
            }
        }

        internal CodecResponse(CodecRequest request, Exception e)
        {
            _request = request;
            Exception = e;
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events
        #endregion

        #region Delegates
        #endregion

        #region Properties

        public Exception Exception { get; private set; }

        public bool WasAborted
        {
            get { return Exception is ThreadAbortException; }
        }

        public int Code
        {
            get { return _response.Code; }
        }

        public HttpsHeaders Header
        {
            get { return _response.Header; }
        }

        public CodecRequest Request
        {
            get { return _request; }
        }

        public XDocument Xml
        {
            get { return _xml; }
        }

        #endregion

        #region Methods

        #endregion
    }
}