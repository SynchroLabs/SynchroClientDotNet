using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.UI.Xaml.Controls;

namespace MaasClient
{
    class StateManager
    {
        //string urlBase = "http://MACBOOKPRO-111C:3000";
        //string urlBase = "http://localhost:1337/api";
        string urlBase = "http://maaas.azurewebsites.net/api";

        HttpClient httpClient;
        CookieContainer cookieContainer;

        PageView pageView;
        ViewModel viewModel;

        public StateManager()
        {
            viewModel = new ViewModel();
            pageView = new PageView(this, viewModel);

            //MessageWebSocket ws = new MessageWebSocket();

            cookieContainer = new CookieContainer();
            var handler = new HttpClientHandler() { CookieContainer = cookieContainer };
            this.httpClient = new HttpClient(handler);
            httpClient.DefaultRequestHeaders.ExpectContinue = false;       // !!! Not clear if this does anything (100-continue case doesn't always repro)
            httpClient.DefaultRequestHeaders.Connection.Add("Keep-Alive"); // !!! This definitely doesn't do anything
        }

        public PageView PageView 
        {
            get
            {
                return pageView;
            }
        }

        public Uri buildUri(string path)
        {
            if (path.StartsWith("http://") || path.StartsWith("https://"))
            {
                return new Uri(path);
            }
            return new Uri(urlBase + "/" + path);
        }

        async Task handleRequest(string path)
        {
            await handleRequest(path, null);
        }

        public async Task<HttpResponseMessage> KeepAliveRequest(HttpMethod method, string path, HttpContent content = null)
        {
            var request = new HttpRequestMessage()
            {
                RequestUri = buildUri(path),
                Method = method,
                Content = content,
            };

            if (content != null)
            {
                content.Headers.Add("Keep-Alive", "true"); // !!! This adds a header, but not Connection: Keep-Alive
            }
            else 
            {
                request.Headers.Add("Connection", new string[] { "Keep-Alive" }); // !!! Not sure this ever works
            }
            Util.debug("Added Keep-Alive");

            return await this.httpClient.SendAsync(request);
        }

        async Task handleRequest(string path, string jsonPostBody)
        {
            try
            {
                Util.debug("Loading JSON from " + urlBase + "/" + path);

                HttpResponseMessage response = null;
                if (jsonPostBody != null)
                {
                    StringContent jsonContent = new StringContent(jsonPostBody, System.Text.Encoding.UTF8, "application/json"); 
                    //response = await httpClient.PostAsync(buildUri(path), jsonContent);
                    Util.debug("Post request to: " + urlBase + "/" + path);
                    response = await KeepAliveRequest(HttpMethod.Post, path, jsonContent);
                }
                else
                {
                    //response = await httpClient.GetAsync(buildUri(path));
                    Util.debug("Get request to: " + urlBase + "/" + path);
                    response = await KeepAliveRequest(HttpMethod.Get, path);
                }
                response.EnsureSuccessStatusCode();

                foreach (KeyValuePair<string, IEnumerable<string>> header in response.Headers)
                {
                    Util.debug("Got header: " + header.Key);
                    if (header.Key == "Set-Cookie")
                    {
                        foreach (string value in header.Value)
                        {
                            Util.debug("Cookie value: " + value);
                        }
                    }
                }

                Util.debug("Number of cookies: " + cookieContainer.Count);
                CookieCollection cookies = cookieContainer.GetCookies(buildUri(""));
                foreach (Cookie cookie in cookies)
                {
                    Util.debug("Found cookie - " + cookie.Name + ": " + cookie.Value);
                }

                var statusText = response.StatusCode + " " + response.ReasonPhrase + Environment.NewLine;
                var responseBodyAsText = await response.Content.ReadAsStringAsync();
                Util.debug("Status: " + statusText);
                Util.debug("Body: " + responseBodyAsText);

                JObject responseAsJSON = JObject.Parse(responseBodyAsText);

                if (responseAsJSON["BoundItems"] != null)
                {
                    JObject jsonBoundItems = (JObject)responseAsJSON["BoundItems"];
                    this.viewModel.InitializeViewModelData(jsonBoundItems);
                }
                else if (responseAsJSON["BoundItemUpdates"] != null)
                {
                    JToken jsonBoundItems = (JToken)responseAsJSON["BoundItemUpdates"];
                    this.viewModel.UpdateViewModelData(jsonBoundItems);
                }

                if (responseAsJSON["View"] != null)
                {
                    JObject jsonPageView = (JObject)responseAsJSON["View"];
                    pageView.processPageView(jsonPageView);
                }

                this.viewModel.UpdateView();

                if (responseAsJSON["MessageBox"] != null)
                {
                    JObject jsonMessageBox = (JObject)responseAsJSON["MessageBox"];
                    pageView.processMessageBox(jsonMessageBox);
                }
            }
            catch (HttpRequestException hre)
            {
                Util.debug("Request exception - " + hre.ToString());
            }
        }

        public async void loadLayout()
        {
            await handleRequest(this.pageView.Path);
        }

        public async void processCommand(string command)
        {
            Util.debug("Process command: " + command);
            var boundValues = new Dictionary<string, JToken>();
            this.viewModel.CollectChangedValues((key, value) => boundValues[key] =  value);

            if (boundValues.Count > 0)
            {
                var response = new Dictionary<string, object>();
                response.Add("BoundItems", boundValues);
                String json = JsonConvert.SerializeObject(response, Formatting.Indented);
                Util.debug("JSON: " + json);

                await handleRequest(this.pageView.Path + "?command=" + command, json);
            }
            else
            {
                await handleRequest(this.pageView.Path + "?command=" + command);
            }
        }
    }
}
