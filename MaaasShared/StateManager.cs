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
        MaaasAppManager _appManager;
        MaaasApp _app;
        Transport _transport;

        string _path;
        ViewModel _viewModel;
        Action<JObject> _onProcessPageView;
        Action<JObject> _onProcessMessageBox;

        MaaasDeviceMetrics _deviceMetrics;

        public StateManager(MaaasAppManager appManager, MaaasApp app, Transport transport, MaaasDeviceMetrics deviceMetrics)
        {
            _viewModel = new ViewModel();

            _appManager = appManager;
            _app = app;
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

        async void ProcessJsonResponse(JObject responseAsJSON)
        {
            Util.debug("Got response: " + responseAsJSON);

            if (responseAsJSON["NewSessionId"] != null)
            {
                string newSessionId = responseAsJSON["NewSessionId"].ToString();
                if (_app.SessionId != null)
                {
                    // Existing client SessionId was replaced by server.  Do we care?  Should we do something (maybe clear any
                    // other client session state, if there was any).
                    //
                    Util.debug("Client session ID of: " + _app.SessionId + " was replaced with new session ID: " + newSessionId);
                }
                else
                {
                    Util.debug("Client was assigned initial session ID of: " + newSessionId);
                }

                // SessionId was created/updated by server.  Record it and save state.
                //
                _app.SessionId = newSessionId;
                await _appManager.saveState();
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
            // Note the we already have an app definition in the MaaasApp that was passed in.  This method will get the 
            // current app definition from the server, which may have changed.
            //
            // !!! Do we want to update our stored app defintion (in MaaasApp, via the AppManager)?  Maybe only if changed?
            //
            JObject appDefinition;

            Util.debug("Loading Maaas application definition");
            appDefinition = await _transport.getAppDefinition();
            Util.debug("Got app definition for: " + appDefinition["name"] + " - " + appDefinition["description"]);

            // Set the path to the main page, then load that page (we'll send over device metrics, since this is the first "Page" transaction)
            //
            this._path = (string)appDefinition["mainPage"];
            JObject requestObject = new JObject(
                new JProperty("Mode", "Page"),
                new JProperty("Path", this._path),
                new JProperty("DeviceMetrics", this.PackageDeviceMetrics()) // Send over device metrics
            );

            Util.debug("Requesting main page with session ID: " + _app.SessionId);

            await _transport.sendMessage(_app.SessionId, requestObject, this.ProcessJsonResponse);
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

            await _transport.sendMessage(_app.SessionId, requestObject, this.ProcessJsonResponse);
        }
    }
}
