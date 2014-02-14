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
        public static string SessionIdHeader = "Maaas-Session-Id";

        public abstract Task sendMessage(string sessionId, JObject requestObject, Action<JObject> responseHandler);
    }
}
