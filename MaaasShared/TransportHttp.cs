﻿using MaaasCore;
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
            if ((_sessionId == null) && (sessionId != null))
            {
                _sessionId = sessionId;
                _httpClient.DefaultRequestHeaders.Add(Transport.SessionIdHeader, _sessionId);
            }
            StringContent jsonContent = new StringContent(requestObject.ToString(), System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_uri, jsonContent);
            response.EnsureSuccessStatusCode();

            var responseMessage = await response.Content.ReadAsStringAsync();
            JObject responseObject = JObject.Parse(responseMessage);
            responseHandler(responseObject);
        }

        public static Transport getTransport(string host)
        {
            return new TransportHttp(host);
        }
    }
}
