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


        private async void RunInNewThread()
        {
            while (_presentationSocket.State == WebSocketState.Open &&
                  _controllingSocket.State == WebSocketState.Open)
            {
                Thread.Sleep(500);
                continue;

                var token = CancellationToken.None;
                var buffer = new ArraySegment<Byte>(new Byte[4096]);

                using (var ms = new MemoryStream())
                {
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await _controllingSocket.ReceiveAsync(buffer, CancellationToken.None);
                        ms.Write(buffer.Array, buffer.Offset, result.Count);
                    }
                    while (!result.EndOfMessage);

                    ms.Seek(0, SeekOrigin.Begin);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        using (var reader = new StreamReader(ms, Encoding.UTF8))
                        {
                            var request = reader.ToString();
                            var type = WebSocketMessageType.Text;
                            var data = Encoding.UTF8.GetBytes(request);
                            buffer = new ArraySegment<Byte>(data);
                            await _presentationSocket.SendAsync(buffer, type, true, token);
                        }
                    }
                }


                /*var received = await _controllingSocket.ReceiveAsync(buffer, token);

                switch (received.MessageType)
                {
                    case WebSocketMessageType.Text:
                        var request = Encoding.UTF8.GetString(buffer.Array,
                                                buffer.Offset,
                                                buffer.Count);
                        var type = WebSocketMessageType.Text;
                        var data = Encoding.UTF8.GetBytes(request);
                        buffer = new ArraySegment<Byte>(data);
                        await _presentationSocket.SendAsync(buffer, type, true, token);
                        break;
                } */
            }
        }

        public async void Run()
        {
            new Thread(delegate ()
            {
                this.RunInNewThread();
            }).Start();
        }
    }
}
