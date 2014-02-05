using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using WebSocket4Net;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using MaaasShared;

namespace MaaasClientAndroid
{
    public class AndroidTransportWs : TransportWebSocket4Net
    {
        private Activity _activity;

        public AndroidTransportWs(Activity activity, string host)
            : base(host)
        {
            _activity = activity;
        }

        public override void postResponseToUI(JObject responseObject)
        {
            // OK, this is a little creepy.  The particular response handler we pass in from
            // StateManager needs to run on the UI thread, and it's easiest to just enforce that here. 
            // In reality, the handler should deal with that itself, but that also means the handler
            // (or wrapper) would need to be async.  Anyway, this is easy and works and will do for now.
            //
            _activity.RunOnUiThread(delegate
            {
                _responseHandler(responseObject);
            });
        }
    }
}

 
