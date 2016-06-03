using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynchroCore
{
    public delegate void CommandHandler(string command);

    public delegate void ProcessPageView(JObject pageView);
    public delegate void ProcessAppExit();
    public delegate void ProcessMessageBox(JObject messageBox, CommandHandler commandHandler);
    public delegate void ProcessLaunchUrl(String appUrl, String webUrl);

    public class StateManager
    {
        static Logger logger = Logger.GetLogger("StateManager");

        MaaasAppManager _appManager;
        MaaasApp _app;
        JObject _appDefinition;
        Transport _transport;

        ulong _transactionNumber = 1;
        ulong getNewTransactionId() 
        { 
            return _transactionNumber++; 
        }

        string _path;
        ulong  _instanceId;
        ulong  _instanceVersion;
        bool   _isBackSupported = false;
        bool   _isExited = false;

        ViewModel _viewModel;
        ProcessPageView _onProcessPageView;
        ProcessAppExit _onProcessAppExit;
        ProcessMessageBox _onProcessMessageBox;
        ProcessLaunchUrl _onProcessLaunchUrl;

        MaaasDeviceMetrics _deviceMetrics;

        public StateManager(MaaasAppManager appManager, MaaasApp app, Transport transport, MaaasDeviceMetrics deviceMetrics)
        {
            _viewModel = new ViewModel();

            _appManager = appManager;
            _app = app;
            _appDefinition = app.AppDefinition;
            _transport = transport;
            _transport.setDefaultHandlers(this.ProcessResponseAsync, this.ProcessRequestFailure);

            _deviceMetrics = deviceMetrics;
        }

        public bool IsBackSupported()
        {
            return _isBackSupported;
        }

        public bool IsOnMainPath()
        {
            return ((_path != null) && (_appDefinition != null) && _path.Equals((string)_appDefinition["main"], StringComparison.Ordinal));
        }

        public ViewModel ViewModel { get { return _viewModel; } }

        public MaaasDeviceMetrics DeviceMetrics { get { return _deviceMetrics; } }

        public void SetProcessingHandlers(
            ProcessPageView OnProcessPageView, 
            ProcessAppExit OnProcessAppExit,
            ProcessMessageBox OnProcessMessageBox, 
            ProcessLaunchUrl OnProcessLaunchUrl
            )
        {
            _onProcessPageView = OnProcessPageView;
            _onProcessAppExit = OnProcessAppExit;
            _onProcessMessageBox = OnProcessMessageBox;
            _onProcessLaunchUrl = OnProcessLaunchUrl;
        }

        JObject PackageDeviceMetrics()
        {
            return new JObject()
            {
                { "clientName", new JValue(this.DeviceMetrics.ClientName) },
                { "clientVersion", new JValue(this.DeviceMetrics.ClientVersion) },
                { "os", new JValue(this.DeviceMetrics.OS) },
                { "osName", new JValue(this.DeviceMetrics.OSName) },
                { "deviceName", new JValue(this.DeviceMetrics.DeviceName) },
                { "deviceType", new JValue(this.DeviceMetrics.DeviceType.ToString()) },
                { "deviceClass", new JValue(this.DeviceMetrics.DeviceClass.ToString()) },
                { "naturalOrientation", new JValue(this.DeviceMetrics.NaturalOrientation.ToString()) },
                { "widthInches", new JValue(this.DeviceMetrics.WidthInches) },
                { "heightInches", new JValue(this.DeviceMetrics.HeightInches) },
                { "widthDeviceUnits", new JValue(this.DeviceMetrics.WidthDeviceUnits) },
                { "heightDeviceUnits", new JValue(this.DeviceMetrics.HeightDeviceUnits) },
                { "deviceScalingFactor", new JValue(this.DeviceMetrics.DeviceScalingFactor) },
                { "widthUnits", new JValue(this.DeviceMetrics.WidthUnits) },
                { "heightUnits", new JValue(this.DeviceMetrics.HeightUnits) },
                { "scalingFactor", new JValue(this.DeviceMetrics.ScalingFactor) }
            };
        }

        JObject PackageViewMetrics(MaaasOrientation orientation)
        {
            if (orientation == this.DeviceMetrics.NaturalOrientation)
            {
                return new JObject()
                {
                    { "orientation", new JValue(orientation.ToString()) },
                    { "widthInches", new JValue(this.DeviceMetrics.WidthInches) },
                    { "heightInches", new JValue(this.DeviceMetrics.HeightInches) },
                    { "widthUnits", new JValue(this.DeviceMetrics.WidthUnits) },
                    { "heightUnits", new JValue(this.DeviceMetrics.HeightUnits) }
                };
            }
            else
            {
                return new JObject()
                {
                    { "orientation", new JValue(orientation.ToString()) },
                    { "widthInches", new JValue(this.DeviceMetrics.HeightInches) },
                    { "heightInches", new JValue(this.DeviceMetrics.WidthInches) },
                    { "widthUnits", new JValue(this.DeviceMetrics.HeightUnits) },
                    { "heightUnits", new JValue(this.DeviceMetrics.WidthUnits) }
                };
            }
        }

        void messageBox(string title, string message, string buttonLabel, string buttonCommand, CommandHandler onCommand)
        {
            var messageBox = new JObject()
            {
                { "title", new JValue(title) },
                { "message", new JValue(message) },
                { "options", new JArray()
                    {
                        new JObject()
                        {
                            { "label", new JValue(buttonLabel) },
                            { "command", new JValue(buttonCommand) }
                        }
                    }
                }
            };

            _onProcessMessageBox(messageBox, (command) =>
            {
                onCommand(command);
            });
        }

        void ProcessRequestFailure(JObject request, Exception ex)
        {            
            logger.Warn("Got request failure for request: {0}, exception: {1}", request, ex);

            messageBox("Connection Error", "Error connecting to application server", "Retry", "retry", (command) =>
            {
                logger.Debug("Retrying request after user confirmation ({0})...", command);
                _transport.sendMessage(_app.SessionId, request);
            });
        }

        async void ProcessResponseAsync(JObject responseAsJSON)
        {
            // logger.Info("Got response: {0}", (string)responseAsJSON);

            if (_isExited)
            {
                logger.Error("StateManager called after exit, ignoring");
                return;
            }

            if (responseAsJSON["NewSessionId"] != null)
            {
                string newSessionId = responseAsJSON["NewSessionId"].ToString();
                if (_app.SessionId != null)
                {
                    // Existing client SessionId was replaced by server.  Do we care?  Should we do something (maybe clear any
                    // other client session state, if there was any).
                    //
                    logger.Debug("Client session ID of: {0} was replaced with new session ID: {1}", _app.SessionId, newSessionId);
                }
                else
                {
                    logger.Debug("Client was assigned initial session ID of: {0}", newSessionId);
                }

                // SessionId was created/updated by server.  Record it and save state.
                //
                _app.SessionId = newSessionId;
                await _appManager.saveState();
            }

            if (responseAsJSON["Error"] != null)
            {
                JObject jsonError = responseAsJSON["Error"] as JObject;
                logger.Warn("Response contained error: {0}", jsonError.GetValue("message"));
                if ((string)jsonError.GetValue("name") == "SyncError")
                {
                    if (responseAsJSON["InstanceId"] == null)
                    {
                        // This is a sync error indicating that the server has no instance (do to a corrupt or
                        // re-initialized session).  All we can really do here is re-initialize the app (clear
                        // our local state and do a Page request for the app entry point).  
                        //
                        logger.Error("ERROR - corrupt server state - need app restart");
                        messageBox("Synchronization Error", "Server state was lost, restarting application", "Restart", "restart", async (command) =>
                        {
                            logger.Warn("Corrupt server state, restarting application...");
                            await this.sendAppStartPageRequestAsync();
                        });

                    }
                    else if (this._instanceId == (ulong)responseAsJSON["InstanceId"])
                    {
                        // The instance that we're on now matches the server instance, so we can safely ignore
                        // the sync error (the request that caused it was sent against a previous instance).
                    }
                    else
                    {
                        // We got a sync error, and the current instance on the server is different that our
                        // instance.  It's possible that the response with the new (correct) instance is still
                        // coming, but unlikey (it would mean it had async/wait user code after page navigation,
                        // which it should not, or that it somehow got sent out of order with respect to this
                        // error response, perhaps over a separate connection that was somehow delayed, but 
                        // will eventually complete).
                        //
                        // The best option in this situation is to request a Resync with the server...
                        //
                        logger.Warn("ERROR - client state out of sync - need resync");
                        await this.sendResyncInstanceRequestAsync();
                    }
                }
                else
                {
                    // Some other kind of error (ClientError, ClientVersionError, or UserCodeError).
                    //
                    var userMessage = (string)jsonError.GetValue("userMessage");
                    var userMessageCaption = (string)jsonError.GetValue("userMessageCaption") ?? "Synchro";

                    if (userMessage != null)
                    {
                        // The server indicated a message to be displayed to the end user, so do that...
                        //
                        messageBox(userMessageCaption, userMessage, "Close", "close", (command) =>
                        {
                        });
                    }
                    else
                    {
                        // Error with no specified user-appropriate message. 
                        //
                        // !!! Maybe we should allow the user to choose an option/button to click to get more details?
                        //
                        messageBox("Application Error", "The application experienced an error.  Please contact your administrator.", "Close", "close", (command) =>
                        {
                        });
                    }
                }

                return;
            }

            bool updateRequired = false;

            if (responseAsJSON["Exit"] != null) // Navigation back out of the app
            {
                // Return to launcher (if there is one).  Disable StateManager (so it won't keep processing responses).
                //
                logger.Info("Navigated back out of the app");
                if (this._onProcessAppExit != null)
                {
                    _isExited = true;
                    this._onProcessAppExit();
                }
                return;
            }
            else if (responseAsJSON["App"] != null) // This means we have a new app
            {
                // Note that we already have an app definition from the MaaasApp that was passed in.  The App in this
                // response was triggered by a request at app startup for the current version of the app metadata 
                // fresh from the endpoint (which may have updates relative to whatever we stored when we first found
                // the app at this endpoint and recorded its metadata).
                //
                // !!! Do we want to update our stored app defintion (in MaaasApp, via the AppManager)?  Maybe only if changed?
                //
                _appDefinition = responseAsJSON["App"] as JObject;
                logger.Info("Got app definition for: {0} - {1}", _appDefinition["name"], _appDefinition["description"]);
                await this.sendAppStartPageRequestAsync();
                return;
            }
            else if ((responseAsJSON["ViewModel"] != null) && (responseAsJSON["View"] != null)) // ViewModel and View - means we have a new page/screen
            {
                this._instanceId = (ulong)responseAsJSON["InstanceId"];
                this._instanceVersion = (ulong)responseAsJSON["InstanceVersion"];

                JObject jsonViewModel = responseAsJSON["ViewModel"] as JObject;

                this._viewModel.InitializeViewModelData((JObject)jsonViewModel);

                this._path = (string)responseAsJSON["Path"];
                logger.Info("Got ViewModel for new view - path: '{0}', instanceId: {1}, instanceVersion: {2}", this._path, this._instanceId, this._instanceVersion);

                this._isBackSupported = (bool)responseAsJSON["Back"];

                JObject jsonPageView = (JObject)responseAsJSON["View"];
                _onProcessPageView(jsonPageView);

                // If the view model is dirty after rendering the page, then the changes are going to have been
                // written by new view controls that produced initial output (such as location or sensor controls).
                // We need to signal than a viewModel "Update" is required to get these changes to the server.
                //
                updateRequired = this._viewModel.IsDirty();
            }
            else if (responseAsJSON["ViewModel"] != null) // ViewModel without View (resync)
            {
                ulong responseInstanceId = (ulong)responseAsJSON["InstanceId"];
                if (responseInstanceId == this._instanceId)
                {
                    this._instanceVersion = (ulong)responseAsJSON["InstanceVersion"];

                    JObject jsonViewModel = responseAsJSON["ViewModel"] as JObject;

                    this._viewModel.SetViewModelData((JObject)jsonViewModel);

                    logger.Info("Got ViewModel resync for existing view - path: '{0}', instanceId: {1}, instanceVersion: {2}", this._path, this._instanceId, this._instanceVersion);
                    this._viewModel.UpdateViewFromViewModel();
                }
                else if (responseInstanceId < this._instanceId)
                {
                    // Resync response was for a previous instance, so we can safely ignore it (we've moved on).
                }
                else
                {
                    // Incorrect instance id on resync - For this to happen, we'd have to get a resync for a "future" instance (meaning one for which
                    // we haven't seen the initial view/viewModel).  This should never happen, but if it does, it's not clear how to recover from it.
                    // Requesting an "instance" resync might very well result in just hitting this case again repeatedy.  The only potential way out of
                    // this (if it ever does happen) is to request the "big" resync.
                    //
                    logger.Warn("ERROR - instance id mismatch (response instance id > local instance id), updates not applied - app resync requested");
                    await this.sendResyncRequestAsync();
                    return;
                }
            }
            else // Updating existing page/screen
            {
                ulong responseInstanceId = (ulong)responseAsJSON["InstanceId"];
                if (responseInstanceId == this._instanceId)
                {
                    ulong responseInstanceVersion = (ulong)responseAsJSON["InstanceVersion"];

                    // You can get a new view on a view model update if the view is dynamic and was updated
                    // based on the previous command/update.
                    //
                    Boolean viewUpdatePresent = (responseAsJSON["View"] != null);

                    if (responseAsJSON["ViewModelDeltas"] != null)
                    {
                        logger.Info("Got ViewModelDeltas for path: '{0}' with instanceId: {1} and instanceVersion: {2}", this._path, responseInstanceId, responseInstanceVersion);

                        if ((this._instanceVersion + 1) == responseInstanceVersion)
                        {
                            this._instanceVersion++;

                            JToken jsonViewModelDeltas = (JToken)responseAsJSON["ViewModelDeltas"];
                            // logger.Debug("ViewModel deltas: {0}", jsonViewModelDeltas);

                            // If we don't have a new View, we'll update the current view as part of applying
                            // the deltas.  If we do have a new View, we'll skip that, since we have to
                            // render the new View and do a full update anyway (below).
                            //
                            this._viewModel.UpdateViewModelData(jsonViewModelDeltas, !viewUpdatePresent);
                        }
                        else
                        {
                            // Instance version was not one more than current version on view model update
                            //
                            logger.Warn("ERROR - instance version mismatch, updates not applied - need resync");
                            await this.sendResyncInstanceRequestAsync();
                            return;
                        }
                    }

                    if (viewUpdatePresent)
                    {
                        if (this._instanceVersion == responseInstanceVersion)
                        {
                            // Render the new page and bind/update it
                            //
                            this._path = (string)responseAsJSON["Path"];
                            JObject jsonPageView = (JObject)responseAsJSON["View"];
                            _onProcessPageView(jsonPageView);
                            updateRequired = this._viewModel.IsDirty();
                        }
                        else
                        {
                            // Instance version was not correct on view update
                            //
                            logger.Warn("ERROR - instance version mismatch on view update - need resync");
                            await this.sendResyncInstanceRequestAsync();
                            return;
                        }
                    }
                }
                else if (responseInstanceId < this._instanceId)
                {
                    // Response was for a previous instance, so we can safely ignore it (we've moved on).
                }
                else
                {
                    // Incorrect instance id
                    //
                    logger.Warn("ERROR - instance id mismatch (response instance id > local instance id), updates not applied - need resync");
                    await this.sendResyncInstanceRequestAsync();
                    return;
                }
            }

            // Commands
            //
            if (responseAsJSON["MessageBox"] != null)
            {
                logger.Info("Launching message box...");
                JObject jsonMessageBox = (JObject)responseAsJSON["MessageBox"];
                _onProcessMessageBox(jsonMessageBox, async (command) =>
                {
                    logger.Info("Message box completed with command: '{0}'", command);
                    await this.sendCommandRequestAsync(command);
                });
            }
            else if (responseAsJSON["LaunchUrl"] != null)
            {
                JObject jsonLaunchUrl = (JObject)responseAsJSON["LaunchUrl"];
                _onProcessLaunchUrl((string)jsonLaunchUrl["primaryUrl"], (string)jsonLaunchUrl["secondaryUrl"]);
            }

            if (responseAsJSON["NextRequest"] != null)
            {
                logger.Debug("Got NextRequest, composing and sending it now...");
                JObject requestObject = (JObject)responseAsJSON["NextRequest"].DeepClone();

                if (updateRequired)
                {
                    logger.Debug("Adding pending viewModel updates to next request (after request processing)");
                    addDeltasToRequestObject(requestObject);
                }

                await _transport.sendMessage(_app.SessionId, requestObject);
            }
            else if (updateRequired)
            {
                logger.Debug("Sending pending viewModel updates (after request processing)");
                await this.sendUpdateRequestAsync();
            }
        }

        public async Task startApplicationAsync()
        {
            logger.Info("Loading Synchro application definition for app at: {0}", _app.Endpoint);
            JObject requestObject = new JObject()
            {
                { "Mode", new JValue("AppDefinition") },
                { "TransactionId", new JValue(0) }
            };
            await _transport.sendMessage(null, requestObject);
        }

        private async Task sendAppStartPageRequestAsync()
        {
            this._path = (string)_appDefinition["main"];

            logger.Info("Request app start page at path: '{0}'", this._path);

            JObject requestObject = new JObject()
            {
                { "Mode", new JValue("Page") },
                { "Path", new JValue(this._path) },
                { "TransactionId", new JValue(getNewTransactionId()) },
                { "DeviceMetrics", this.PackageDeviceMetrics() }, // Send over device metrics (these won't ever change, per session)
                { "ViewMetrics", this.PackageViewMetrics(_deviceMetrics.CurrentOrientation) } // Send over view metrics
            };

            await _transport.sendMessage(_app.SessionId, requestObject);
        }

        private async Task sendResyncInstanceRequestAsync()
        {
            logger.Info("Sending resync for path: '{0}'", this._path);

            JObject requestObject = new JObject()
            {
                { "Mode", new JValue("Resync") },
                { "Path", new JValue(this._path) },
                { "TransactionId", new JValue(getNewTransactionId()) },
                { "InstanceId", new JValue(this._instanceId) },
                { "InstanceVersion", new JValue(this._instanceVersion) }
            };

            await _transport.sendMessage(_app.SessionId, requestObject);
        }

        private bool addDeltasToRequestObject(JObject requestObject)
        {
            var vmDeltas = this._viewModel.CollectChangedValues();
            if (vmDeltas.Count > 0)
            {
                JArray deltas = new JArray();
                foreach (var delta in vmDeltas)
                {
                    deltas.Add(new JObject()
                    {
                        { "path", new JValue(delta.Key) },
                        { "value", delta.Value.DeepClone() }
                    });
                }

                requestObject.Add("ViewModelDeltas", deltas);

                return true;
            }

            return false;
        }

        public async Task sendUpdateRequestAsync()
        {
            logger.Debug("Process update for path: '{0}'", this._path);

            // We check dirty here, even though addDeltas is a noop if there aren't any deltas, in order
            // to avoid generating a new transaction id when we're not going to do a new transaction.
            //
            if (this._viewModel.IsDirty())
            {
                JObject requestObject = new JObject()
                {
                    { "Mode", new JValue("Update") },
                    { "Path", new JValue(this._path) },
                    { "TransactionId", new JValue(getNewTransactionId()) },
                    { "InstanceId", new JValue(this._instanceId) },
                    { "InstanceVersion", new JValue(this._instanceVersion) }
                };

                if (addDeltasToRequestObject(requestObject))
                {
                    // Only going to send the updates if there were any changes...
                    await _transport.sendMessage(_app.SessionId, requestObject);
                }
            }
        }

        public async Task sendCommandRequestAsync(string command, JObject parameters = null)
        {
            logger.Info("Sending command: '{0}' for path: '{1}'", command, this._path);

            JObject requestObject = new JObject()
            {
                { "Mode", new JValue("Command") },
                { "Path", new JValue(this._path) },
                { "TransactionId", new JValue(getNewTransactionId()) },
                { "InstanceId", new JValue(this._instanceId) },
                { "InstanceVersion", new JValue(this._instanceVersion) },
                { "Command", new JValue(command) }

            };

            if (parameters != null)
            {
                requestObject["Parameters"] = parameters;
            }

            addDeltasToRequestObject(requestObject);

            await _transport.sendMessage(_app.SessionId, requestObject);
        }

        public async Task sendBackRequestAsync()
        {
            logger.Info("Sending 'back' for path: '{0}'", this._path);

            JObject requestObject = new JObject()
            {
                { "Mode", new JValue("Back") },
                { "Path", new JValue(this._path) },
                { "TransactionId", new JValue(getNewTransactionId()) },
                { "InstanceId", new JValue(this._instanceId) },
                { "InstanceVersion", new JValue(this._instanceVersion) }
            };

            await _transport.sendMessage(_app.SessionId, requestObject);
        }

        public async Task sendViewUpdateAsync(MaaasOrientation orientation)
        {
            logger.Info("Sending ViewUpdate for path: '{0}'", this._path);

            // Send the updated view metrics 
            JObject requestObject = new JObject()
            {
                { "Mode", new JValue("ViewUpdate") },
                { "Path", new JValue(this._path) },
                { "TransactionId", new JValue(getNewTransactionId()) },
                { "InstanceId", new JValue(this._instanceId) },
                { "InstanceVersion", new JValue(this._instanceVersion) },
                { "ViewMetrics", this.PackageViewMetrics(orientation) }
            };

            await _transport.sendMessage(_app.SessionId, requestObject);
        }

        // If your app has a session, but no other state, such as on recovery from tombstoning, you 
        // can call this method instead of startApplicationAsync().  The server will respond with the
        // full state required to resume your app.
        //
        // This method should only be called in a restart from tombstoning state.  For example, if a 
        // user had navigated into the app and then shut it down via the operating system, when they 
        // restart they do not expect to return to where they were (as they would with this method), 
        // they expect to return to the entry sreen of the app.
        //
        public async Task sendResyncRequestAsync()
        {
            logger.Info("Sending resync (no path/instance)");

            JObject requestObject = new JObject()
            {
                { "Mode", new JValue("Resync") },
                { "TransactionId", new JValue(getNewTransactionId()) }
            };

            await _transport.sendMessage(_app.SessionId, requestObject);
        }
    }
}
