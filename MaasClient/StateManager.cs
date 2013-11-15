﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace MaasClient
{
    class StateManager
    {
        string urlBase = "http://MACBOOKPRO-111C:3000";

        PageView pageView;
        HttpClient httpClient;
        CookieContainer cookieContainer;

        public StateManager()
        {
            debug("Creating state manager");
            pageView = new PageView(this);

            cookieContainer = new CookieContainer();
            var handler = new HttpClientHandler() { CookieContainer = cookieContainer };
            this.httpClient = new HttpClient(handler);
        }

        public PageView PageView 
        {
            get
            {
                return pageView;
            }
        }

        void debug(string str)
        {
            System.Diagnostics.Debug.WriteLine(str);
        }

        public Uri buildUri(string path)
        {
            return new Uri(urlBase + "/" + path);
        }

        async Task handleRequest(string path)
        {
            await handleRequest(path, null);
        }

        async Task handleRequest(string path, string jsonPostBody)
        {
            try
            {
                debug("Loading JSON from " + urlBase + "/" + path);

                HttpResponseMessage response = null;
                if (jsonPostBody != null)
                {
                    StringContent jsonContent = new StringContent(jsonPostBody, System.Text.Encoding.UTF8, "application/json"); 
                    response = await httpClient.PostAsync(buildUri(path), jsonContent);
                }
                else
                {
                    response = await httpClient.GetAsync(buildUri(path));
                }
                response.EnsureSuccessStatusCode();

                foreach (KeyValuePair<string, IEnumerable<string>> header in response.Headers)
                {
                    debug("Got header: " + header.Key);
                    if (header.Key == "Set-Cookie")
                    {
                        foreach (string value in header.Value)
                        {
                            debug("Cookie value: " + value);
                        }
                    }
                }

                debug("Number of cookies: " + cookieContainer.Count);
                CookieCollection cookies = cookieContainer.GetCookies(buildUri(""));
                foreach (Cookie cookie in cookies)
                {
                    debug("Found cookie - " + cookie.Name + ": " + cookie.Value);
                }

                var statusText = response.StatusCode + " " + response.ReasonPhrase + Environment.NewLine;
                var responseBodyAsText = await response.Content.ReadAsStringAsync();
                debug("Status: " + statusText);
                debug("Body: " + responseBodyAsText);

                JObject responseAsJSON = JObject.Parse(responseBodyAsText);

                if (responseAsJSON["BoundItems"] != null)
                {
                    JObject jsonBoundItems = (JObject)responseAsJSON["BoundItems"];
                    if (responseAsJSON["View"] != null)
                    {
                        pageView.newViewItems(jsonBoundItems);
                    }
                    else
                    {
                        pageView.updatedViewItems(jsonBoundItems);
                    }
                }

                if (responseAsJSON["View"] != null)
                {
                    JObject jsonPageView = (JObject)responseAsJSON["View"];
                    pageView.processPageView(jsonPageView);
                }

                pageView.updateView();

                if (responseAsJSON["MessageBox"] != null)
                {
                    JObject jsonMessageBox = (JObject)responseAsJSON["MessageBox"];
                    pageView.processMessageBox(jsonMessageBox);
                }
            }
            catch (HttpRequestException hre)
            {
                debug("Request exception - " + hre.ToString());
            }
        }

        public async void loadLayout()
        {
            await handleRequest(this.pageView.Path);
        }

        public async void processCommand(string command)
        {
            debug("Process command: " + command);
            var boundValues = new Dictionary<string, string>();
            pageView.collectBoundItemValues((key, value) => boundValues.Add(key, value));

            if (boundValues.Count > 0)
            {
                var response = new Dictionary<string, object>();
                response.Add("BoundItems", boundValues);
                String json = JsonConvert.SerializeObject(response, Formatting.Indented);
                debug("JSON: " + json);

                await handleRequest(this.pageView.Path + "?command=" + command, json);
            }
            else
            {
                await handleRequest(this.pageView.Path + "?command=" + command);
            }
        }
    }
}
