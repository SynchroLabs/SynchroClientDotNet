using MaaasCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MaaasShared
{
    class TransportHttp : Transport
    {
        private HttpClient _httpClient;

        private string _sessionId;

        public TransportHttp(string host, HttpClient client = null) : base(host)
        {
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
                StringContent jsonContent = new StringContent(requestObject.ToString(), System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(_uri, jsonContent);
                response.EnsureSuccessStatusCode();

                var responseMessage = await response.Content.ReadAsStringAsync();

                watch.Stop();
                Util.debug("TIMER: Elapsed time for request was: " + watch.ElapsedMilliseconds + " ms");

                JObject responseObject = JObject.Parse(responseMessage);
                responseHandler(responseObject);
            }
            catch (Exception e)
            {
                Util.debug("HTTP Transport exceptioon caught, details: " + e.Message);
                requestFailureHandler(requestObject, e);
            }
        }
    }
}
