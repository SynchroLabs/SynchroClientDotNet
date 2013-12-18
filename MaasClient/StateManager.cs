using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Sockets;

namespace MaasClient
{
    class StateManager
    {
        static string host = "localhost:1337";
        //static string host = "maaas.azurewebsites.net";

        //TransportHttp transport = new TransportHttp(host + "/api");
        TransportWs transport = new TransportWs(host);

        PageView pageView;
        ViewModel viewModel;

        public StateManager()
        {
            viewModel = new ViewModel();
            pageView = new PageView(this, viewModel);
        }

        public PageView PageView 
        {
            get
            {
                return pageView;
            }
        }

        // This is used by the page view to resolve resource URIs
        public Uri buildUri(string path)
        {
            if (path.StartsWith("http://") || path.StartsWith("https://"))
            {
                return new Uri(path);
            }
            return new Uri("http://" + host + "/api/" + path);
        }

        void ProcessJsonResponse(JObject responseAsJSON)
        {
            Util.debug("Got response: " + responseAsJSON);
            if (responseAsJSON["ViewModel"] != null)
            {
                JObject jsonViewModel = responseAsJSON["ViewModel"] as JObject;
                this.viewModel.InitializeViewModelData((JObject)jsonViewModel);

                if (responseAsJSON["View"] != null)
                {
                    JObject jsonPageView = (JObject)responseAsJSON["View"];
                    pageView.processPageView(jsonPageView);
                }

                this.viewModel.UpdateViewFromViewModel();
            }
            else if (responseAsJSON["ViewModelDeltas"] != null)
            {
                JToken jsonViewModelDeltas = (JToken)responseAsJSON["ViewModelDeltas"];
                this.viewModel.UpdateViewModelData(jsonViewModelDeltas);
            }

            if (responseAsJSON["MessageBox"] != null)
            {
                JObject jsonMessageBox = (JObject)responseAsJSON["MessageBox"];
                pageView.processMessageBox(jsonMessageBox);
            }
        }

        public async void loadLayout()
        {
            Util.debug("Load layout for path: " + this.pageView.Path);

            JObject requestObject = new JObject(
                new JProperty("Path", this.pageView.Path)
            );

            await this.transport.sendMessage(requestObject, this.ProcessJsonResponse);
        }

        public async void processCommand(string command, JObject parameters = null)
        {
            Util.debug("Process command: " + command + " for path: " + this.pageView.Path);

            JObject requestObject = new JObject(
                new JProperty("Path", this.pageView.Path),
                new JProperty("Command", command)
            );

            if (parameters != null)
            {
                requestObject["Parameters"] = parameters;
            }

            var vmDeltas = new Dictionary<string, JToken>();
            this.viewModel.CollectChangedValues((key, value) => vmDeltas[key] = value);

            if (vmDeltas.Count > 0)
            {
                requestObject.Add("ViewModelDeltas", 
                    new JArray(
                        from delta in vmDeltas
                        select new JObject(
                            new JProperty("path", delta.Key),
                            new JProperty("value", delta.Value)
                        )
                    )
                );
            }

            await this.transport.sendMessage(requestObject, this.ProcessJsonResponse);
        }
    }
}
