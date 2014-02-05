using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MaaasShared;
using Newtonsoft.Json.Linq;

namespace MaaasClientIOS
{
    public class iOSTransportWs : TransportWebSocket4Net
    {
        protected UIViewController _controller;

        public iOSTransportWs(UIViewController controller, string host) : base(host)
        {
            _controller = controller;
        }

        public override void postResponseToUI(JObject responseObject)
        {
            _controller.InvokeOnMainThread(() =>
            {
                base.postResponseToUI(responseObject);
            });
        }
    }
}