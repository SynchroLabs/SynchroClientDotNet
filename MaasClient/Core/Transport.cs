using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaasClient.Core
{
    interface Transport
    {
        Task sendMessage(JObject requestObject, Action<JObject> responseHandler);
    }
}
