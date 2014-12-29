using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SynchroCore
{
    public class TransportHttp : Transport
    {
        static Logger logger = Logger.GetLogger("TransportHttp");

        protected Uri _uri;
        private HttpClient _httpClient;

        private string _sessionId;

        public TransportHttp(Uri uri, HttpClient client = null) : base()
        {
            _uri = uri;
            if (client != null)
            {
                _httpClient = client;
            }
            else
            {
                _httpClient = new HttpClient();
            }

            // Not supported on WinPhone (persistent connection is automatic in HTTP 1.1, so not clear if
            // this was even doing anything).  More info: http://en.wikipedia.org/wiki/HTTP_persistent_connection
            //
            // _httpClient.DefaultRequestHeaders.Connection.Add("Keep-Alive");

            // Not clear if this does anything (100-continue case doesn't always repro)
            _httpClient.DefaultRequestHeaders.ExpectContinue = false;
        }

        static public Uri UriFromHostString(string host, string protocol = "http")
        {
            return  new Uri(protocol + "://" + host);
        }

        public override async Task sendMessage(string sessionId, JObject requestObject, ResponseHandler responseHandler, RequestFailureHandler requestFailureHandler)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            if (responseHandler == null)
            {
                responseHandler = _responseHandler;
            }
            if (requestFailureHandler == null)
            {
                requestFailureHandler = _requestFailureHandler;
            }

            if (sessionId != null)
            {
                if ((_sessionId != null) && (_sessionId != sessionId))
                {
                    // There was a previous _sessionId, and the new one is different...
                    _sessionId = null;
                    _httpClient.DefaultRequestHeaders.Remove(Transport.SessionIdHeader);
                }

                if (_sessionId == null)
                {
                    // Set the session key and add the default header (for this and future requests)
                    _sessionId = sessionId;
                    _httpClient.DefaultRequestHeaders.Add(Transport.SessionIdHeader, _sessionId);
                }
            }

            try
            {
                StringContent jsonContent = new StringContent(requestObject.ToJson(), System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(_uri, jsonContent);
                response.EnsureSuccessStatusCode();

                var responseMessage = await response.Content.ReadAsStringAsync();

                watch.Stop();
                logger.Debug("TIMER: Elapsed time for request was: {0} ms", watch.ElapsedMilliseconds);

                JObject responseObject = (JObject)JToken.Parse(responseMessage);
                responseHandler(responseObject);
            }
            catch (Exception e)
            {
                logger.Error("HTTP Transport exceptioon caught, details: {0}", e.Message);
                requestFailureHandler(requestObject, e);
            }
        }
    }
}
