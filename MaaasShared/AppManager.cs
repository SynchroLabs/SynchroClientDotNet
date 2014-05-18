using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaaasCore
{
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

        public abstract void loadState();
        public abstract void saveState();
    }

    // This class is for test purposes.  Each platform should override MaaasAppManager with platform-specific serialization.
    //
    public class StatelessAppManager : MaaasAppManager
    {
        private static string localHost = "192.168.1.168";

        public override void loadState()
        {
            // _appSeed = new MaaasApp(localHost + ":1337/api", JObject.Parse("{ name: 'maaas-samples', description: 'MAAAS API Samples'}"));

            Apps.Add(new MaaasApp("maaas.io/api", JObject.Parse("{ name: 'maaas-samples', description: 'MAAAS API Samples'}")));
            Apps.Add(new MaaasApp(localHost + ":1337/api", JObject.Parse("{ name: 'maaas-samples', description: 'MAAAS API Samples (local)'}")));
        }

        public override void saveState()
        {
        }
    }
}
