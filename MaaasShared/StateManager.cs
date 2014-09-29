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

        ulong _transactionNumber = 1;
        ulong getNewTransactionId() 
        { 
            return _transactionNumber++; 
        }

        string _path;
        uint   _instanceId;
        uint   _instanceVersion;

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
                new JProperty("naturalOrientation", this.DeviceMetrics.NaturalOrientation.ToString()),
                new JProperty("widthInches", this.DeviceMetrics.WidthInches),
                new JProperty("heightInches", this.DeviceMetrics.HeightInches),
                new JProperty("widthDeviceUnits", this.DeviceMetrics.WidthDeviceUnits),
                new JProperty("heightDeviceUnits", this.DeviceMetrics.HeightDeviceUnits),
                new JProperty("deviceScalingFactor", this.DeviceMetrics.DeviceScalingFactor),
                new JProperty("widthUnits", this.DeviceMetrics.WidthUnits),
                new JProperty("heightUnits", this.DeviceMetrics.HeightUnits),
                new JProperty("scalingFactor", this.DeviceMetrics.ScalingFactor)
            );
        }

        JObject PackageViewMetrics(MaaasOrientation orientation)
        {
            if (orientation == this.DeviceMetrics.NaturalOrientation)
            {
                return new JObject(
                    new JProperty("orientation", orientation.ToString()),
                    new JProperty("widthInches", this.DeviceMetrics.WidthInches),
                    new JProperty("heightInches", this.DeviceMetrics.HeightInches),
                    new JProperty("widthUnits", this.DeviceMetrics.WidthUnits),
                    new JProperty("heightUnits", this.DeviceMetrics.HeightUnits)
                );
            }
            else
            {
                return new JObject(
                    new JProperty("orientation", orientation.ToString()),
                    new JProperty("widthInches", this.DeviceMetrics.HeightInches),
                    new JProperty("heightInches", this.DeviceMetrics.WidthInches),
                    new JProperty("widthUnits", this.DeviceMetrics.HeightUnits),
                    new JProperty("heightUnits", this.DeviceMetrics.WidthUnits)
                );
            }
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

            if (responseAsJSON["Error"] != null)
            {
                Util.debug("Response contained error: " + responseAsJSON["Error"]);
                return;
            }

            if (responseAsJSON["ViewModel"] != null) // This means we have a new page/screen
            {
                this._instanceId = (uint)responseAsJSON["InstanceId"];
                this._instanceVersion = (uint)responseAsJSON["InstanceVersion"];

                JObject jsonViewModel = responseAsJSON["ViewModel"] as JObject;
                this._viewModel.InitializeViewModelData((JObject)jsonViewModel);

                if (responseAsJSON["View"] != null)
                {
                    this._path = (string)responseAsJSON["Path"];
                    JObject jsonPageView = (JObject)responseAsJSON["View"];
                    _onProcessPageView(jsonPageView);
                }

                this._viewModel.UpdateViewFromViewModel();
            }
            else // Updating existing page/screen
            {
                if (this._instanceId == (uint)responseAsJSON["InstanceId"])
                {
                    // You can get a new view on a view model update if the view is dynamic and was updated
                    // based on the previous command/update.
                    //
                    Boolean viewUpdatePresent = (responseAsJSON["View"] != null);

                    if (responseAsJSON["ViewModelDeltas"] != null)
                    {
                        if ((this._instanceVersion + 1) == (uint)responseAsJSON["InstanceVersion"])
                        {
                            this._instanceVersion++;

                            JToken jsonViewModelDeltas = (JToken)responseAsJSON["ViewModelDeltas"];

                            // If we don't have a new View, we'll update the current view as part of applying
                            // the deltas.  If we do have a new View, we'll skip that, since we have to
                            // render the new View and do a full update anyway (below).
                            //
                            this._viewModel.UpdateViewModelData(jsonViewModelDeltas, !viewUpdatePresent);
                        }
                        else
                        {
                            // !!! Instance version was not one more than current version on view model update
                        }
                    }

                    if (this._instanceVersion == (uint)responseAsJSON["InstanceVersion"])
                    {
                        if (viewUpdatePresent)
                        {
                            // Render the new page and bind/update it
                            //
                            this._path = (string)responseAsJSON["Path"];
                            JObject jsonPageView = (JObject)responseAsJSON["View"];
                            _onProcessPageView(jsonPageView);
                            this._viewModel.UpdateViewFromViewModel();
                        }
                    }
                    else
                    {
                        // !!! Instance version was not correct on view update
                    }
                }
                else
                {
                    // !!! Incorrect instance id
                }
            }

            if (responseAsJSON["NextRequest"] != null)
            {
                Util.debug("Got NextRequest, composing and sending it now...");
                JObject requestObject = (JObject)responseAsJSON["NextRequest"].DeepClone();
                await _transport.sendMessage(_app.SessionId, requestObject, this.ProcessJsonResponse);
            }

            if (responseAsJSON["MessageBox"] != null)
            {
                JObject jsonMessageBox = (JObject)responseAsJSON["MessageBox"];
                _onProcessMessageBox(jsonMessageBox);
            }
        }

        public async Task startApplication()
        {
            // Note that we already have an app definition in the MaaasApp that was passed in.  This method will get the 
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
                new JProperty("TransactionId", getNewTransactionId()),
                new JProperty("DeviceMetrics", this.PackageDeviceMetrics()), // Send over device metrics (these won't ever change, per session)
                new JProperty("ViewMetrics", this.PackageViewMetrics(_deviceMetrics.CurrentOrientation)) // Send over view metrics
            );

            Util.debug("Requesting main page with session ID: " + _app.SessionId);

            await _transport.sendMessage(_app.SessionId, requestObject, this.ProcessJsonResponse);
        }

        private bool addDeltasToRequestObject(JObject requestObject)
        {
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

                return true;
            }

            return false;
        }

        public async void processUpdate()
        {
            Util.debug("Process update for path: " + this._path);

            JObject requestObject = new JObject(
                new JProperty("Mode", "Update"),
                new JProperty("Path", this._path),
                new JProperty("TransactionId", getNewTransactionId()),
                new JProperty("InstanceId", this._instanceId),
                new JProperty("InstanceVersion", this._instanceVersion)
            );

            if (addDeltasToRequestObject(requestObject))
            {
                // Only going to send the updates if there were any changes...
                await _transport.sendMessage(_app.SessionId, requestObject, this.ProcessJsonResponse);
            }
        }

        public async void processCommand(string command, JObject parameters = null)
        {
            Util.debug("Process command: " + command + " for path: " + this._path);

            JObject requestObject = new JObject(
                new JProperty("Mode", "Command"),
                new JProperty("Path", this._path),
                new JProperty("TransactionId", getNewTransactionId()),
                new JProperty("InstanceId", this._instanceId),
                new JProperty("InstanceVersion", this._instanceVersion),
                new JProperty("Command", command)
            );

            if (parameters != null)
            {
                requestObject["Parameters"] = parameters;
            }

            addDeltasToRequestObject(requestObject);

            await _transport.sendMessage(_app.SessionId, requestObject, this.ProcessJsonResponse);
        }

        public async void processViewUpdate(MaaasOrientation orientation)
        {
            // Send the updated view metrics 
            JObject requestObject = new JObject(
                new JProperty("Mode", "ViewUpdate"),
                new JProperty("Path", this._path),
                new JProperty("TransactionId", getNewTransactionId()),
                new JProperty("InstanceId", this._instanceId),
                new JProperty("InstanceVersion", this._instanceVersion),
                new JProperty("ViewMetrics", this.PackageViewMetrics(orientation))
            );

            await _transport.sendMessage(_app.SessionId, requestObject, this.ProcessJsonResponse);
        }
    }
}
