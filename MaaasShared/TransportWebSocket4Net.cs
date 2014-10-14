#if !NETFX_CORE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using WebSocket4Net;

using MaaasCore;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace MaaasShared
{
    // A WebSocket transport based on WebSocket4Net - https://websocket4net.codeplex.com/
    //
    // This should work on Android/iOS/WinPhone.  Not sure it's safe to update the Win version to use
    // this, as the low level socket interfaces it requires are not provided on WinRT - and the Win
    // version using the Windows WebSocket APIs seems to be working fine.
    //
    public class TransportWebSocket4Net : Transport
    {
        static Logger logger = Logger.GetLogger("TransportWebSocket4Net");

        protected WebSocket _ws;

        protected TaskCompletionSource<bool> _connecting = new TaskCompletionSource<bool>();

        public TransportWebSocket4Net(string host) : base(host, "ws")
        {
        }

        public virtual void postResponseToUI(ResponseHandler responseHandler, JObject responseObject)
        {
            // Override this per platform as required to ensure that the response handler is able to update the UX...
            //
            responseHandler(responseObject);
        }

        public virtual void postFailureToUI(RequestFailureHandler failureHandler, JObject request, Exception ex)
        {
            // Override this per platform as required to ensure that the response handler is able to update the UX...
            //
            failureHandler(request, ex);
        }

        public override async Task sendMessage(string sessionId, JObject requestObject, ResponseHandler responseHandler, RequestFailureHandler requestFailureHandler)
        {
            if (responseHandler == null)
            {
                responseHandler = _responseHandler;
            }
            if (requestFailureHandler == null)
            {
                requestFailureHandler = _requestFailureHandler;
            }

            try
            {
                // Make a local copy to avoid races with Closed events.
                WebSocket webSocket = _ws;

                // Have we connected yet?
                if (webSocket == null)
                {
                    if (sessionId != null)
                    {
                        var customHeaderItems = new List<KeyValuePair<string, string>>();
                        customHeaderItems.Add(new KeyValuePair<string, string>(Transport.SessionIdHeader, sessionId));
                        webSocket = new WebSocket(_uri.ToString(), "", null, customHeaderItems, "", "", WebSocketVersion.Rfc6455);
                    }
                    else
                    {
                        webSocket = new WebSocket(_uri.ToString());
                    }

                    webSocket.NoDelay = true;

                    webSocket.Opened += new EventHandler((sender, e) => 
                    {
                        logger.Debug("WebSocket opened");
                        _connecting.SetResult(true);
                    });

                    webSocket.Error += new EventHandler<SuperSocket.ClientEngine.ErrorEventArgs>((sender, e) =>
                    {
                        if (_connecting != null)
                        {
                            _connecting.SetResult(false);
                        }
                        logger.Error("WebSocket error: {0}", e);

                        if (e.Exception.InnerException != null)
                        {
                            logger.Error("WebSocket - inner exception: {0}", e.Exception.InnerException);
                        }
                        postFailureToUI(requestFailureHandler, requestObject, e.Exception);
                    });

                    webSocket.Closed += new EventHandler((sender, e) =>
                    {
                        logger.Debug("WebSocket closed");
                    });

                    webSocket.MessageReceived += new EventHandler<MessageReceivedEventArgs>((sender, e) =>
                    {
                        logger.Debug("Received message from server: {0}", e.Message);
                        JObject responseObject = JObject.Parse(e.Message);

                        this.postResponseToUI(responseHandler, responseObject);
                    });

                    _connecting = new TaskCompletionSource<bool>();
                    logger.Debug("Connecting to WebSocket server on: {0}", _uri);
                    webSocket.Open();
                    bool connected = await _connecting.Task;
                    _connecting = null;
                    if (connected)
                    {
                        logger.Debug("Connected to WebSocket server on: {0}", _uri);
                        _ws = webSocket; // Only store it after successfully connecting.
                    }
                }

                if (_ws != null)
                {
                    _ws.Send(requestObject.ToString());
                }
            }
            catch (Exception ex) // For debugging
            {
                // Add your specific error-handling code here.
                logger.Error("WebSocket error: {0}", ex);
            }
        }
    }
}
#endif


