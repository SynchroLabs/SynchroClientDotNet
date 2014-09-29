using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaaasCore
{
    public abstract class Transport
    {
        public static string SessionIdHeader = "synchro-api-session-id";

        public abstract Task sendMessage(string sessionId, JObject requestObject, Action<JObject> responseHandler);

        public async Task<JObject> getAppDefinition()
        {
            JObject appDefinition = null;

            JObject requestObject = new JObject(
                new JProperty("Mode", "AppDefinition"),
                new JProperty("TransactionId", 0)
            );
            await this.sendMessage(null, requestObject, (JObject responseAsJSON) =>
            {
                appDefinition = responseAsJSON;
            });

            return appDefinition;
        }
    }
}
