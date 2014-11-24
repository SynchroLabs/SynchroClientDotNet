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

        protected ResponseHandler _responseHandler;
        protected RequestFailureHandler _requestFailureHandler;

        public Transport()
        {
        }

        public void setDefaultHandlers(ResponseHandler responseHandler, RequestFailureHandler requestFailureHandler)
        {
            _responseHandler = responseHandler;
            _requestFailureHandler = requestFailureHandler;
        }

        public abstract Task sendMessage(
            string sessionId, 
            JObject requestObject, 
            ResponseHandler responseHandler = null, 
            RequestFailureHandler requestFailureHandler = null
            );

        public async Task<JObject> getAppDefinition()
        {
            JObject appDefinition = null;

            JObject requestObject = new JObject()
            {
                { "Mode", new JValue("AppDefinition") },
                { "TransactionId", new JValue(0) }
            };
            await this.sendMessage(
                null, 
                requestObject, 
                (JObject responseAsJSON) =>
                {
                    appDefinition = responseAsJSON["App"] as JObject;
                },
                (JObject request, Exception ex) =>
                {
                    // !!! Fail
                }
            );

            return appDefinition;
        }
    }
}
