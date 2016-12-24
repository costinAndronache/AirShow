using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters.Json;
using Newtonsoft.Json;
using System.Net;

namespace AirShow.WebSockets
{

    public class ActivationMessage
    {
        public string UserId { get; set; }
        public string PresentationName { get; set; }
        public string AsDictionaryKey
        {
            get
            {
                return this.UserId + this.PresentationName;
            }
        }
    }

    public class GlobalWebSocketServer
    {
        private WebSocket _debugSocket;
        private ConcurrentBag<WebSocket> _webSockets;
        private ConcurrentDictionary<string, ConcurrentDictionary<string,System.Net.WebSockets.WebSocket>> _firstPhaseSockets;
        private ConcurrentBag<LocalWebSocketServer> _localServers;

        public GlobalWebSocketServer()
        {
            _firstPhaseSockets = new ConcurrentDictionary<string, ConcurrentDictionary<string, WebSocket>>();
            _localServers = new ConcurrentBag<LocalWebSocketServer>();
            _webSockets = new ConcurrentBag<WebSocket>();
        }

        public async void HandleIncomingWebSocket(HttpContext http)
        {
            var webSocket = await http.WebSockets.AcceptWebSocketAsync();
            HandleWebSocket(webSocket);
        }

        public async Task<List<string>> ActivePresentationsFor(string userId)
        {
            if (_firstPhaseSockets.ContainsKey(userId))
            {
                var activeSockets = _firstPhaseSockets[userId];
                return activeSockets.Keys.ToList();
            }

            return new List<string>();
        }

        public void HandleWebSocket(WebSocket webSocket)
        {
            var token = CancellationToken.None;
                var buffer = new ArraySegment<Byte>(new Byte[4096]);
                var received = webSocket.ReceiveAsync(buffer, token).Result;
                if (received.MessageType == WebSocketMessageType.Text)
                {
                    var request = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
                    var am = JsonConvert.DeserializeObject<ActivationMessage>(WebUtility.HtmlDecode(request));

                    if (_firstPhaseSockets.ContainsKey(am.UserId))
                    {

                        var socketsDict = _firstPhaseSockets[am.UserId];
                        if (socketsDict.ContainsKey(am.PresentationName))
                        {
                            var presentationSocket = socketsDict[am.PresentationName];
                            WebSocket blah;
                            socketsDict.TryRemove(am.PresentationName, out blah);

                            LocalWebSocketServer lwss = new LocalWebSocketServer(presentationSocket, webSocket);
                            _localServers.Add(lwss);
                            lwss.Run();
                        }
                        else
                        {
                            socketsDict.GetOrAdd(am.PresentationName, webSocket);
                            KeepPresentationSocketAlive(webSocket);
                        }
                    }
                    else
                    {
                        var socketsDict = new ConcurrentDictionary<string, WebSocket>();
                        socketsDict.GetOrAdd(am.PresentationName, webSocket);
                        _firstPhaseSockets.GetOrAdd(am.UserId, socketsDict);
                    KeepPresentationSocketAlive(webSocket);
                    }
                }
            
        }

        public void KeepPresentationSocketAlive(WebSocket webSocket)
        {
            Console.Write(Thread.CurrentThread);
            var token = CancellationToken.None;
            var buffer = new ArraySegment<Byte>(new Byte[4096]);
            var received = webSocket.ReceiveAsync(buffer, token).Result;
            return;

            /*
            while (webSocket.State == WebSocketState.Open)
            {
                var token = CancellationToken.None;
                var buffer = new ArraySegment<Byte>(new Byte[4096]);
                var received =  webSocket.ReceiveAsync(buffer, token).Result;

                switch (received.MessageType)
                {
                    case WebSocketMessageType.Text:
                        var request = Encoding.UTF8.GetString(buffer.Array,
                                                buffer.Offset,
                                                buffer.Count);
                        var type = WebSocketMessageType.Text;
                        var data = Encoding.UTF8.GetBytes("Echo from server :" + request);
                        buffer = new ArraySegment<Byte>(data);
                        webSocket.SendAsync(buffer, type, true, token).RunSynchronously();
                        break;
                }
            }*/

            Console.Write("" + webSocket.State);
        }
    }
}
