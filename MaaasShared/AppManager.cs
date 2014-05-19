using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaaasCore
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

        public string Name { get { return (string)AppDefinition["name"]; } }
        public string Description { get { return (string)AppDefinition["description"]; } }

        public MaaasApp(string endpoint, JObject appDefinition)
        {
            this.Endpoint = endpoint;
            this.AppDefinition = appDefinition;
        }
    }

    // If AppSeed is present, client apps should launch that app directly and suppress any "launcher" interface. If not present,
    // then client apps should provide launcher interface showing content of Apps (with ability to add/edit/remove and launch).
    //
    public abstract class MaaasAppManager
    {
        protected MaaasApp _appSeed = null;
        protected List<MaaasApp> _apps = new List<MaaasApp>();

        public MaaasApp AppSeed { get { return _appSeed; } }
        public List<MaaasApp> Apps { get { return _apps; } }

        private static MaaasApp appFromJson(JObject json)
        {
            String endpoint = (string)json["endpoint"];
            JObject appDefinition = (JObject)json["definition"].DeepClone();

            return new MaaasApp(endpoint, appDefinition);
        }

        private static JObject appToJson(MaaasApp app)
        {
            return new JObject(
                new JProperty("endpoint", app.Endpoint),
                new JProperty("definition", app.AppDefinition.DeepClone())
                );
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
                foreach (JToken item in apps.Children())
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
                obj.Add("apps", new JArray(
                    from app in _apps
                    select appToJson(app)
                    ));
            }

            return obj;
        }

        public abstract void loadState();
        public abstract void saveState();
    }

    // This class is for test purposes.  Each platform should override MaaasAppManager with platform-specific serialization.
    //
    public class StatelessAppManager : MaaasAppManager
    {
        private static string localHost = "192.168.1.168";

        // A real app state object will typically have either a hard-coded "seed" *or* a list of "apps" (managed
        // by the MAAAS mobile client).
        //
        private static string testAppState = String.Join(Environment.NewLine, new string [] {
            "{",
            "  \"seed\":",
            "  {",
            "    \"endpoint\": \"" + localHost + ":1337/api\",",
            "    \"definition\": { \"name\": \"maaas-samples\", \"description\": \"MAAAS API Samples\" }",
            "  },",
            "  \"apps\":",
            "  [",
            "    {",
            "      \"endpoint\": \"maaas.io/api\",",
            "      \"definition\": { \"name\": \"maaas-samples\", \"description\": \"MAAAS API Samples\" }",
            "    },",
            "    {",
            "      \"endpoint\": \"" + localHost + ":1337/api\",",
            "      \"definition\": { \"name\": \"maaas-samples\", \"description\": \"MAAAS API Samples (local)\" }",
            "    }",
            "  ]",
            "}"
        });

        public override void loadState()
        {
            JObject parsedState = JObject.Parse(testAppState);
            serializeFromJson(parsedState);

            // Serialization test (take this out and/or make into a real unit test)...
            //
            JObject generatedState = serializeToJson();
            if (JToken.DeepEquals(parsedState, generatedState))
            {
                Util.debug("AppManager serialization test passed!");
            }
            else
            {
                Util.debug("AppManager serialization test FAILED!");
            }
        }

        public override void saveState()
        {
            // NOOP
        }
    }
}
