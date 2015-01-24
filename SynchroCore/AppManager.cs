using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynchroCore
{
    // The AppDefinition is provided by the Maaas server (it is the contents of the maaas.json file in the Maaas app directory).
    // This is a JSON object that is modeled more or less after the NPM package structure.  For now we store it in the 
    // MaaasApp as the JSON object that it is and just provide getters for some well-known members.  Once the AppDefinition
    // gets nailed down, we might do more processing of it here (or we might not).
    //
    public class MaaasApp
    {
        public string Endpoint { get; set; }
        public JObject AppDefinition { get; set; }
        public string SessionId { get; set; }

        public string Name { get { return (string)AppDefinition["name"]; } }
        public string Description { get { return (string)AppDefinition["description"]; } }

        public MaaasApp(string endpoint, JObject appDefinition, string sessionId = null)
        {
            this.Endpoint = endpoint;
            this.AppDefinition = appDefinition; // !!! Should we use appDefinition.DeepClone();
            this.SessionId = sessionId;
        }
    }

    // If AppSeed is present, client apps should launch that app directly and suppress any "launcher" interface. If not present,
    // then client apps should provide launcher interface showing content of Apps.
    //
    // Implementation:
    //
    // On startup:
    //   Inspect bundled seed.json to see if it contains a "seed"
    //     If yes: Start Maaas app at that seed.
    //     If no: determine whether any local app manager state exists (stored locally on the device)
    //       If no: initialize local app manager state from seed.json
    //     Show launcher interface based on local app manager state
    //
    // Launcher interface shows a list of apps (from the "apps" key in the app manager state)
    //   Provides ability to launch an app
    //   Provides ability to add (find?) and remove app
    //     Add/find app:
    //       User provides endpoint
    //       We look up app definition at endpoint and display to user
    //       User confirms and we add to AppManager.Apps (using endpoint and appDefinition to create MaaasApp)
    //       We serialize AppManager via saveState()
    //
    public abstract class MaaasAppManager
    {
        protected MaaasApp _appSeed = null;
        protected ObservableCollection<MaaasApp> _apps = new ObservableCollection<MaaasApp>();

        public MaaasApp AppSeed { get { return _appSeed; } }
        public ObservableCollection<MaaasApp> Apps { get { return _apps; } }

        public MaaasApp GetApp(string endpoint)
        {
            if ((_appSeed != null) && (_appSeed.Endpoint == endpoint))
            {
                return _appSeed;
            }
            else
            {
                foreach (MaaasApp app in _apps)
                {
                    if (app.Endpoint == endpoint)
                    {
                        return app;
                    }
                }
            }
            return null;
        }

        public void UpdateApp(MaaasApp app)
        {
            if (_appSeed.Endpoint == app.Endpoint)
            {
                _appSeed = app;
            }
            else
            {
                for (int i = _apps.Count - 1; i >= 0; i--)
                {
                    if (app.Endpoint == _apps[i].Endpoint)
                    {
                        _apps.RemoveAt(i);
                    }
                }
                _apps.Add(app);
            }
        }

        private static MaaasApp appFromJson(JObject json)
        {
            String endpoint = (string)json["endpoint"];
            JObject appDefinition = (JObject)json["definition"].DeepClone();
            String sessionId = (string)json["sessionId"];

            return new MaaasApp(endpoint, appDefinition, sessionId);
        }

        private static JObject appToJson(MaaasApp app)
        {
            return new JObject()
            {
                { "endpoint", new JValue(app.Endpoint) },
                { "definition", app.AppDefinition.DeepClone() },
                { "sessionId", new JValue(app.SessionId) }
            };
        }

        public void serializeFromJson(JObject json)
        {
            JObject seed = json["seed"] as JObject;
            if (seed != null)
            {
                _appSeed = appFromJson(seed);
            }

            JArray apps = json["apps"] as JArray;
            if (apps != null)
            {
                foreach (JToken item in apps)
                {
                    JObject app = item as JObject;
                    if (app != null)
                    {
                        _apps.Add(appFromJson(app));
                    }
                }
            }
        }

        public JObject serializeToJson()
        {
            JObject obj = new JObject();

            if (_appSeed != null)
            {
                obj.Add("seed", appToJson(_appSeed));
            }

            if (_apps.Count > 0)
            {
                JArray array = new JArray();
                foreach (var app in _apps)
                {
                    array.Add(appToJson(app));
                }                
                obj.Add("apps", array);
            }

            return obj;
        }

        public async Task<bool> loadState()
        {
            string bundledState = await this.loadBundledState();
            JObject parsedBundledState = (JObject)JToken.Parse(bundledState);

            JObject seed = parsedBundledState["seed"] as JObject;
            if (seed != null)
            {
                // If the bundled state contains a "seed", then we're just going to use that as the
                // app state (we'll launch the app inidicated by the seed and suppress the launcher).
                //
                serializeFromJson(parsedBundledState);
            }
            else
            {
                // If the bundled state doesn't contain a seed, load the local state...
                //
                string localState = await this.loadLocalState();
                if (localState == null)
                {
                    // If there is no local state, initialize the local state from the bundled state.
                    //
                    localState = bundledState;
                    await this.saveLocalState(localState);
                }
                JObject parsedLocalState = (JObject)JToken.Parse(localState);
                serializeFromJson(parsedLocalState);
            }

            return true;
        }

        public async Task<bool> saveState()
        {
            JObject json = this.serializeToJson();
            return await this.saveLocalState(json.ToJson());
        }

        // Abstract serialization that may or may not be async per platform.
        //
        // The pattern used here is not totally obvious.  Each platform will implement the methods below.
        // Those method implementation may or may not be asynchronous.  To accomodate this, the calls to
        // these methods from the base class need to use await and the implementations must return a task.
        // Derived class implementations that are async will just declare as async and return the basic
        // return value (string or bool, as appropriate).  Derived class implementations that are not async
        // will simpley execute synchronously and return a completed task (wrapped around the basic return
        // value).  This is really the only workable way to deal with the issue of not knowing whether all
        // derived class implentations either will or will not be async.
        //
        protected abstract Task<string> loadBundledState();
        protected abstract Task<string> loadLocalState();
        protected abstract Task<bool> saveLocalState(string state);
    }
}