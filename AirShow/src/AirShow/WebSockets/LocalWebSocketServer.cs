using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AirShow.WebSockets
{
    public class LocalWebSocketServer
    {
        private WebSocket _presentationSocket, _controllingSocket;

        public LocalWebSocketServer(WebSocket presentationSocket, WebSocket controllingSocket)
        {
            _presentationSocket = presentationSocket;
            _controllingSocket = controllingSocket;
        }


        private void RunInNewThread()
        {
            while (_presentationSocket.State == WebSocketState.Open &&
                  _controllingSocket.State == WebSocketState.Open)
            {

                var token = CancellationToken.None;
                var buffer = new ArraySegment<Byte>(new Byte[4096]);

                using (var ms = new MemoryStream())
                {
                    WebSocketReceiveResult result;
                    do
                    {
                        result = _controllingSocket.ReceiveAsync(buffer, CancellationToken.None).Result;
                        ms.Write(buffer.Array, buffer.Offset, result.Count);
                    }
                    while (!result.EndOfMessage);

                    ms.Seek(0, SeekOrigin.Begin);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        using (var reader = new StreamReader(ms, Encoding.UTF8))
                        {
                            var request = reader.ReadToEnd();
                            var type = WebSocketMessageType.Text;
                            var data = Encoding.UTF8.GetBytes(request);
                            buffer = new ArraySegment<Byte>(data);
                            _presentationSocket.SendAsync(buffer, type, true, token);
                        }
                    }
                }
            }
        }

        public void Run()
        {
            RunInNewThread();
        }
    }
}
