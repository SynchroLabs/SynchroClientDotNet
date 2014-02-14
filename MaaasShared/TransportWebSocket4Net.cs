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
        protected WebSocket _ws;
        protected string _uri;

        // This assumes no overlapped requests (the handler is set on the request, then used on the response)
        protected Action<JObject> _responseHandler;

        protected TaskCompletionSource<bool> _connecting = new TaskCompletionSource<bool>();

        public TransportWebSocket4Net(string host)
        {
            _uri = "ws://" + host;
        }

        public virtual void postResponseToUI(JObject responseObject)
        {
            // Override this per platform as required to ensure that the response handler is able to update the UX...
            //
            _responseHandler(responseObject);
        }

        public override async Task sendMessage(string sessionId, JObject requestObject, Action<JObject> responseHandler)
        {
            try
            {
                // Make a local copy to avoid races with Closed events.
                WebSocket webSocket = _ws;

                // Have we connected yet?
                if (webSocket == null)
                {
                    if (sessionId != null)
                    {
                        var customHeaderItems = new List<KeyValuePair<string, string>> { { Transport.SessionIdHeader, sessionId } };
                        webSocket = new WebSocket(_uri, "", null, customHeaderItems, "", "", WebSocketVersion.Rfc6455);
                    }
                    else
                    {
                        webSocket = new WebSocket(_uri);
                    }

                    webSocket.NoDelay = true;

                    webSocket.Opened += new EventHandler(websocket_Opened);
                    webSocket.Error += new EventHandler<SuperSocket.ClientEngine.ErrorEventArgs>(websocket_Error);
                    webSocket.Closed += new EventHandler(websocket_Closed);
                    webSocket.MessageReceived += new EventHandler<MessageReceivedEventArgs>(websocket_MessageReceived);

                    _connecting = new TaskCompletionSource<bool>();
                    Util.debug("Connecting to WebSocket server on: " + _uri);
                    webSocket.Open();
                    bool connected = await _connecting.Task;
                    _connecting = null;
                    if (connected)
                    {
                        Util.debug("Connected to WebSocket server on: " + _uri);
                        _ws = webSocket; // Only store it after successfully connecting.
                    }
                }

                if (_ws != null)
                {
                    _responseHandler = responseHandler;
                    _ws.Send(requestObject.ToString());
                }
            }
            catch (Exception ex) // For debugging
            {
                // Add your specific error-handling code here.
                Console.WriteLine(ex.GetType() + ":" + ex.Message + System.Environment.NewLine + ex.StackTrace);
            }
        }

        protected void websocket_Opened(object sender, EventArgs e)
        {
            Util.debug("WebSocket opened");
            _connecting.SetResult(true);
        }

        protected void websocket_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            Util.debug("Received message from server: " + e.Message);
            JObject responseObject = JObject.Parse(e.Message);

            this.postResponseToUI(responseObject);
        }

        protected void websocket_Error(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            if (_connecting != null)
            {
                _connecting.SetResult(false);
            }
            Console.WriteLine(e.Exception.GetType() + ":" + e.Exception.Message + System.Environment.NewLine + e.Exception.StackTrace);

            if (e.Exception.InnerException != null)
            {
                Console.WriteLine(e.Exception.InnerException.GetType());
            }
        }

        protected void websocket_Closed(object sender, EventArgs e)
        {
            Util.debug("WebSocket closed");
        }
    }
}
#endif


