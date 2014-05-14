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
        Transport _transport;
        string _host;

        JObject _appDefinition;

        string _path;
        ViewModel _viewModel;
        Action<JObject> _onProcessPageView;
        Action<JObject> _onProcessMessageBox;

        MaaasDeviceMetrics _deviceMetrics;

        string _sessionId;

        public StateManager(string host, Transport transport, MaaasDeviceMetrics deviceMetrics)
        {
            _viewModel = new ViewModel();
            _host = host;

            _transport = transport;

            _deviceMetrics = deviceMetrics;
        }

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

        JObject PackageDeviceMetrics()
        {
            return new JObject(
                new JProperty("os", this.DeviceMetrics.OS),
                new JProperty("osName", this.DeviceMetrics.OSName),
                new JProperty("deviceName", this.DeviceMetrics.DeviceName),
                new JProperty("deviceType", this.DeviceMetrics.DeviceType.ToString()),
                new JProperty("deviceClass", this.DeviceMetrics.DeviceClass.ToString()),
                new JProperty("scalingFactor", this.DeviceMetrics.ScalingFactor),
                new JProperty("widthInches", this.DeviceMetrics.WidthInches),
                new JProperty("heightInches", this.DeviceMetrics.HeightInches),
                new JProperty("widthUnits", this.DeviceMetrics.WidthDeviceUnits),
                new JProperty("heightUnits", this.DeviceMetrics.HeightDeviceUnits)
            );
        }

        void ProcessJsonResponse(JObject responseAsJSON)
        {
            Util.debug("Got response: " + responseAsJSON);

            if (responseAsJSON["NewSessionId"] != null)
            {
                if (_sessionId != null)
                {
                    // Existing client SessionId was replaced by server!
                }
                _sessionId = responseAsJSON["NewSessionId"].ToString();
            }

            if (responseAsJSON["ViewModel"] != null)
            {
                JObject jsonViewModel = responseAsJSON["ViewModel"] as JObject;
                this._viewModel.InitializeViewModelData((JObject)jsonViewModel);

                if (responseAsJSON["View"] != null)
                {
                    JObject jsonPageView = (JObject)responseAsJSON["View"];
                    this._path = (string)jsonPageView["path"];
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

        public async Task startApplication()
        {
            Util.debug("Load Maaas application definition");

            JObject requestObject = new JObject(
                new JProperty("Mode", "AppDefinition")
            );
            await _transport.sendMessage(_sessionId, requestObject, async (JObject responseAsJSON) =>
            {
                this._appDefinition = responseAsJSON;
                Util.debug("Got app definition for: " + this._appDefinition["name"] + " - " + this._appDefinition["description"]);

                // Set the path to the main page, then load that page (we'll send over device metrics, since this is the first "Page" transaction)
                //
                this._path = (string)responseAsJSON["mainPage"];
                JObject requestObject2 = new JObject(
                    new JProperty("Mode", "Page"),
                    new JProperty("Path", this._path),
                    new JProperty("DeviceMetrics", this.PackageDeviceMetrics()) // Send over device metrics
                );
                await _transport.sendMessage(_sessionId, requestObject2, this.ProcessJsonResponse);
            });
        }

        public async void processCommand(string command, JObject parameters = null)
        {
            Util.debug("Process command: " + command + " for path: " + this._path);

            JObject requestObject = new JObject(
                new JProperty("Mode", "Page"),
                new JProperty("Path", this._path),
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

            await _transport.sendMessage(_sessionId, requestObject, this.ProcessJsonResponse);
        }
    }
}
