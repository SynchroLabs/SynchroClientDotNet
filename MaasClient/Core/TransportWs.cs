using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.Web;

// WebSocket transport
//
namespace MaasClient.Core
{
    class TransportWs : Transport
    {
        private MessageWebSocket _ws;
        private DataWriter _messageWriter;

        // This assumes no overlapped requests (the handler is set on the request, then used on the response)
        private Action<JObject> _responseHandler;

        private Uri _uri;

        public TransportWs(string host)
        {
            _uri = new Uri("ws://" + host);
        }

        public async Task sendMessage(JObject requestObject, Action<JObject> responseHandler)
        {
            try
            {
                // Make a local copy to avoid races with Closed events.
                MessageWebSocket webSocket = _ws;

                // Have we connected yet?
                if (webSocket == null)
                {
                    webSocket = new MessageWebSocket();

                    // MessageWebSocket supports both utf8 and binary messages.
                    // When utf8 is specified as the messageType, then the developer
                    // promises to only send utf8-encoded data.
                    webSocket.Control.MessageType = SocketMessageType.Utf8;

                    // Set up callbacks
                    webSocket.MessageReceived += async (sender, args) =>
                    {
                        try
                        {
                            using (DataReader reader = args.GetDataReader())
                            {
                                reader.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
                                string responseMessage = reader.ReadString(reader.UnconsumedBufferLength);
                                Util.debug("Received message from server: " + responseMessage);
                                JObject responseObject = JObject.Parse(responseMessage);

                                // OK, this is a little creepy.  The particular response handler we pass in from
                                // StateManager needs to run on the UI thread, and it's easiest to just enforce that here. 
                                // In reality, the handler should deal with that itself, but that also means the handler
                                // (or wrapper) would need to be async.  Anyway, this is easy and works and will do for now.
                                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                                {
                                    // This will run on the UI thread
                                    _responseHandler(responseObject);
                                });
                            }
                        }
                        catch (Exception ex) // For debugging
                        {
                            WebErrorStatus status = WebSocketError.GetStatus(ex.GetBaseException().HResult);
                            // Add your specific error-handling code here.
                        }
                    };

                    webSocket.Closed += Closed;

                    Util.debug("Connecting to WebSocket server on: " + _uri);
                    await webSocket.ConnectAsync(_uri);
                    Util.debug("Connected to WebSocket server on: " + _uri);
                    _ws = webSocket; // Only store it after successfully connecting.
                    _messageWriter = new DataWriter(webSocket.OutputStream);
                }

                _responseHandler = responseHandler;

                // Buffer any data we want to send.
                _messageWriter.WriteString(requestObject.ToString());

                // Send the data as one complete message.
                await _messageWriter.StoreAsync();

            }
            catch (Exception ex) // For debugging
            {
                WebErrorStatus status = WebSocketError.GetStatus(ex.GetBaseException().HResult);
                // Add your specific error-handling code here.
            }
        }

        private void Closed(IWebSocket sender, WebSocketClosedEventArgs args)
        {
            // You can add code to log or display the code and reason
            // for the closure (stored in args.Code and args.Reason)

            // This is invoked on another thread so use Interlocked 
            // to avoid races with the Start/Close/Reset methods.
            MessageWebSocket webSocket = Interlocked.Exchange(ref _ws, null);
            if (webSocket != null)
            {
                webSocket.Dispose();
            }
        }
    }
}
