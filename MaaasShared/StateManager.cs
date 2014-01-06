using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaaasCore
{
    public class StateManager
    {
        string _host;
        Transport _transport;
        ViewModel _viewModel;
        Action<JObject> _onProcessPageView;
        Action<JObject> _onProcessMessageBox;

        MaaasDeviceMetrics _deviceMetrics;

        public StateManager(string host, Transport transport, MaaasDeviceMetrics deviceMetrics)
        {
            _viewModel = new ViewModel();
            _host = host;

            /* !!! Refactor - Need to pass transport in from client - always use http for now...
             * 
            if (transport == "websocket")
            {
                 _transport = new TransportWs(host);
            }
            else
            {
                _transport = new TransportHttp(host + "/api");
            }
            */
            _transport = transport;

            _deviceMetrics = deviceMetrics;
        }

        public String Path { get; set; }
        public ViewModel ViewModel { get { return _viewModel; } }

        public MaaasDeviceMetrics DeviceMetrics { get { return _deviceMetrics; } }

        public void SetProcessingHandlers(Action<JObject> OnProcessPageView, Action<JObject> OnProcessMessageBox)
        {
            _onProcessPageView = OnProcessPageView;
            _onProcessMessageBox = OnProcessMessageBox;
        }

        // This is used by the page view to resolve resource URIs
        public Uri buildUri(string path)
        {
            if (path.StartsWith("http://") || path.StartsWith("https://"))
            {
                return new Uri(path);
            }
            return new Uri("http://" + _host + "/api/" + path);
        }

        void ProcessJsonResponse(JObject responseAsJSON)
        {
            Util.debug("Got response: " + responseAsJSON);
            if (responseAsJSON["ViewModel"] != null)
            {
                JObject jsonViewModel = responseAsJSON["ViewModel"] as JObject;
                this._viewModel.InitializeViewModelData((JObject)jsonViewModel);

                if (responseAsJSON["View"] != null)
                {
                    JObject jsonPageView = (JObject)responseAsJSON["View"];
                    this.Path = (string)jsonPageView["path"];
                    _onProcessPageView(jsonPageView);
                }

                this._viewModel.UpdateViewFromViewModel();
            }
            else if (responseAsJSON["ViewModelDeltas"] != null)
            {
                JToken jsonViewModelDeltas = (JToken)responseAsJSON["ViewModelDeltas"];
                this._viewModel.UpdateViewModelData(jsonViewModelDeltas);
            }

            if (responseAsJSON["MessageBox"] != null)
            {
                JObject jsonMessageBox = (JObject)responseAsJSON["MessageBox"];
                _onProcessMessageBox(jsonMessageBox);
            }
        }

        public async Task loadLayout()
        {
            Util.debug("Load layout for path: " + this.Path);

            JObject requestObject = new JObject(
                new JProperty("Path", this.Path)
            );

            await _transport.sendMessage(requestObject, this.ProcessJsonResponse);
        }

        public async void processCommand(string command, JObject parameters = null)
        {
            Util.debug("Process command: " + command + " for path: " + this.Path);

            JObject requestObject = new JObject(
                new JProperty("Path", this.Path),
                new JProperty("Command", command)
            );

            if (parameters != null)
            {
                requestObject["Parameters"] = parameters;
            }

            var vmDeltas = new Dictionary<string, JToken>();
            this._viewModel.CollectChangedValues((key, value) => vmDeltas[key] = value);

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

            await _transport.sendMessage(requestObject, this.ProcessJsonResponse);
        }
    }
}
