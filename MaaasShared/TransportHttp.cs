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
        private Uri _uri;

        private string _sessionId;

        public TransportHttp(string host)
        {
            _uri = new Uri("http://" + host);

            _httpClient = new HttpClient();

            // Not clear if this does anything (100-continue case doesn't always repro)
            _httpClient.DefaultRequestHeaders.ExpectContinue = false;

            // !!! This never seems to work...
            // _httpClient.DefaultRequestHeaders.Connection.Add("Keep-Alive");
        }

        public TransportHttp(HttpClient client, string host)
        {
            _uri = new Uri("http://" + host);
            _httpClient = client;
        }

        public override async Task sendMessage(string sessionId, JObject requestObject, Action<JObject> responseHandler)
        {
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
                JObject responseObject = JObject.Parse(responseMessage);
                responseHandler(responseObject);
            }
            catch (Exception e)
            {
                // !!! Do something more productive than just eating this...
                //
                Util.debug("HTTP Transport exceptioon caught, details: " + e.Message);
            }
        }

        public static Transport getTransport(string host)
        {
            return new TransportHttp(host);
        }
    }
}
