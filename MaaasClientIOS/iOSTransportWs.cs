using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MaaasShared;
using Newtonsoft.Json.Linq;
using MaaasCore;

namespace MaaasClientIOS
{
    public class iOSTransportWs : TransportWebSocket4Net
    {
        protected UIViewController _controller;

        public iOSTransportWs(UIViewController controller, string host) : base(host)
        {
            _controller = controller;
        }

        public virtual void postResponseToUI(ResponseHandler responseHandler, JObject responseObject)
        {
            _controller.InvokeOnMainThread(() =>
            {
                base.postResponseToUI(responseHandler, responseObject);
            });
        }

        public virtual void postFailureToUI(RequestFailureHandler failureHandler, JObject request, Exception ex)
        {
            _controller.InvokeOnMainThread(() =>
            {
                base.postFailureToUI(failureHandler, request, ex);
            });
        }
    }
}