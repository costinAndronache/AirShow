using Newtonsoft.Json;
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
    public class PresentationSession
    {
        private int _presentationId;
        private string _sessionToken;

        private static int MaximumIdleTimeInSeconds = 1800;

        private object _viewLock = new object();
        private object _controlLock = new object();
        private object _keepRunningLock = new object();
        private object _dateTimeLock = new object();

        private WebSocket _viewSocket;
        private WebSocket _controlSocket;
        private bool _inRunLoop;
        private DateTime _lastActivityTimestamp;

        private Action _cleanupCallback;

        private DateTime LastActivityTimestamp
        {
            get
            {
                lock (_dateTimeLock)
                {
                    return _lastActivityTimestamp;
                }
            }
            set
            {
                lock (_dateTimeLock)
                {
                    _lastActivityTimestamp = value;
                }
            }
        }

        private bool InRunLoop
        {
            get
            {
                lock (_keepRunningLock)
                {
                    return _inRunLoop;
                }
            }

            set
            {
                lock (_keepRunningLock)
                {
                    _inRunLoop = value;
                }
            }
        }

        private WebSocket ViewSocket
        {
            get
            {
                lock (_viewLock)
                {
                    return _viewSocket;
                }
            }
            set
            {
                lock (_viewLock)
                {
                    _viewSocket = value;
                }
            }
        }

        private WebSocket ControlSocket
        {
            get
            {
                lock (_controlLock)
                {
                    return _controlSocket;
                }
            }
            set
            {
                lock (_controlLock)
                {
                    _controlSocket = value;
                }
            }
        }

        private static object dismissMessage = new { kActionTypeCodeKey = 9};

        public int PresentationId
        {
            get
            {
                return _presentationId;
            }
        }




        public PresentationSession(int presentationId, string sessionToken, Action cleanupCallback)
        {
            _cleanupCallback = cleanupCallback;
            _sessionToken = sessionToken;
            _presentationId = presentationId;
            _lastActivityTimestamp = DateTime.Now;
            
        }

        public void ReplaceOrSetViewSocket(WebSocket viewSocket)
        {
            var socket = this.ViewSocket;
            if (socket != null && socket.State == WebSocketState.Open)
            {
                SendObjectToSocket(dismissMessage, socket);
                //socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
            }

            this.ViewSocket = viewSocket;
        }

        public void ReplaceOrSetControlSocket(WebSocket controlSocket)
        {
            var socket = this.ControlSocket;
            this.ControlSocket = controlSocket;

            if (socket != null && socket.State == WebSocketState.Open)
            {
                SendObjectToSocket(dismissMessage, socket);
                //socket.CloseAsync(WebSocketCloseStatus.Empty, null, CancellationToken.None);
            }
        }

        public async void SendControlMessageToView(string controlMessage)
        {
            var socket = this.ViewSocket;
            var bytes = GetBytes(controlMessage);
            var ar = new ArraySegment<Byte>(bytes);
            if (socket.State == WebSocketState.Open)
            {
                await socket.SendAsync(ar, WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        public static void SendObjectToSocket(object obj, WebSocket socket)
        {
            var str = JsonConvert.SerializeObject(obj);
            var bytes = GetBytes(str);

            var ar = new ArraySegment<Byte>(bytes);
            var cancellationToken = CancellationToken.None;


            if (socket.State == WebSocketState.Open)
            {
                socket.SendAsync(ar, WebSocketMessageType.Text, true, cancellationToken);
            }
        }

        static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        public void BeginRunLoopIfNecessary()
        {
            //if (!this.InRunLoop)
            {
                this.BeginSocketsLoop();
            }
        }

        private async void BeginSocketsLoop()
        {
            this.InRunLoop = true;

            while((DateTime.Now - this.LastActivityTimestamp).TotalSeconds <= MaximumIdleTimeInSeconds)
            {
                var viewSocket = this.ViewSocket;
                var controlSocket = this.ControlSocket;

                if (controlSocket != null && viewSocket != null && 
                    viewSocket.State == WebSocketState.Open  && controlSocket.State == WebSocketState.Open )
                {
                    var token = CancellationToken.None;
                    var buffer = new ArraySegment<Byte>(new Byte[4096]);

                    using (var ms = new MemoryStream())
                    {
                        WebSocketReceiveResult result;
                        do
                        {
                            result = controlSocket.ReceiveAsync(buffer, CancellationToken.None).Result;
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
                                await viewSocket.SendAsync(buffer, type, true, token);
                                this.LastActivityTimestamp = DateTime.Now;
                            }
                        }
                    }
                }

                if (controlSocket.State == WebSocketState.CloseSent ||
                    controlSocket.State == WebSocketState.CloseReceived || 
                    controlSocket.State == WebSocketState.Closed)
                {
                    break;
                }

                if (viewSocket.State == WebSocketState.CloseSent ||
                    viewSocket.State == WebSocketState.CloseReceived ||
                    viewSocket.State == WebSocketState.Closed)
                {
                    break;
                }


            }

            this.InRunLoop = false;
            if ((DateTime.Now - this.LastActivityTimestamp).TotalSeconds > MaximumIdleTimeInSeconds)
            {
                this.Cleanup();
            }
        }


        private void Cleanup()
        {

        }
    }
}
