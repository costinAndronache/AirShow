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

        private static int MaximumIdleTimeInSeconds = 60 * 15;

        private object _viewLock = new object();
        private object _controlLock = new object();
        private object _excutedCleanupLock = new object();
        private object _dateTimeLock = new object();



        private WebSocket _viewSocket;
        private WebSocket _controlSocket;
        private bool _executedCleanup;
        private DateTime _lastActivityTimestamp;

        private Action<PresentationSession> _cleanupCallback;

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

        private bool ExecutedCleanup
        {
            get
            {
                lock (_excutedCleanupLock)
                {
                    return _executedCleanup;
                }
            }

            set
            {
                lock (_excutedCleanupLock)
                {
                    _executedCleanup = value;
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
        private static object closeMessage = new { kActionTypeCodeKey = 10 };

        public int PresentationId
        {
            get
            {
                return _presentationId;
            }
        }




        public PresentationSession(int presentationId, string sessionToken, Action<PresentationSession> cleanupCallback)
        {
            _cleanupCallback = cleanupCallback;
            _sessionToken = sessionToken;
            _presentationId = presentationId;
            _lastActivityTimestamp = DateTime.Now;
            
        }

        public void ReplaceOrSetViewSocket(WebSocket viewSocket)
        {
            if (this.ExecutedCleanup)
            {
                return;
            }
            var socket = this.ViewSocket;
            if (socket != null && socket.State == WebSocketState.Open)
            {
                SendObjectToSocket(dismissMessage, socket);
                //socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
            }

            this.ViewSocket = viewSocket;
            BeginViewSocketRunloop();
        }

        public void ReplaceOrSetControlSocket(WebSocket controlSocket)
        {
            if (this.ExecutedCleanup)
            {
                return;
            }
            var socket = this.ControlSocket;
            this.ControlSocket = controlSocket;
            this.LastActivityTimestamp = DateTime.Now;

            if (socket != null && socket.State == WebSocketState.Open)
            {
                SendObjectToSocket(dismissMessage, socket);
                //socket.CloseAsync(WebSocketCloseStatus.Empty, null, CancellationToken.None);
            }
        }

        public async Task SendControlMessageToView(string controlMessage)
        {
            if (this.ExecutedCleanup)
            {
                return;
            }

            var cancellationToken = CancellationToken.None;
            var socket = this.ViewSocket;
            var bytes = Encoding.UTF8.GetBytes(controlMessage);
            var ar = new ArraySegment<Byte>(bytes);
            if (socket.State == WebSocketState.Open)
            {
                await socket.SendAsync(ar, WebSocketMessageType.Text, true, cancellationToken);
            }

            this.LastActivityTimestamp = DateTime.Now;
        }

        public static void SendObjectToSocket(object obj, WebSocket socket)
        {
            var str = JsonConvert.SerializeObject(obj);
            var bytes = Encoding.UTF8.GetBytes(str);

            var ar = new ArraySegment<Byte>(bytes);
            var cancellationToken = CancellationToken.None;


            if (socket.State == WebSocketState.Open)
            {
                socket.SendAsync(ar, WebSocketMessageType.Text, true, cancellationToken);
            }
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
            if (this.ExecutedCleanup)
            {
                return;
            }

            this.LastActivityTimestamp = DateTime.Now;

            while(true)
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
                        try
                        {
                            do
                            {
                                result = controlSocket.ReceiveAsync(buffer, CancellationToken.None).Result;
                                ms.Write(buffer.Array, buffer.Offset, result.Count);
                            }
                            while (!result.EndOfMessage);
                        }catch(Exception e)
                        {
                            break;
                        }
                        ms.Seek(0, SeekOrigin.Begin);

                        if ((DateTime.Now - this.LastActivityTimestamp).TotalSeconds > MaximumIdleTimeInSeconds)
                        {
                            break;
                        }

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

            
            if ((DateTime.Now - this.LastActivityTimestamp).TotalSeconds > MaximumIdleTimeInSeconds)
            {
                this.Cleanup();
            }
        }

        private void BeginViewSocketRunloop()
        {
            if (this.ExecutedCleanup)
            {
                return;
            }

            var self = this;

            var maxTimeOfInactivityMiliseconds = 10000;
            Timer timer = null;
            timer = new Timer((obj) =>
            {
                if (self.ViewSocket == null || self.ViewSocket.State != WebSocketState.Open)
                {
                    self.Cleanup();
                    timer.Dispose();
                }
            }, null, maxTimeOfInactivityMiliseconds, maxTimeOfInactivityMiliseconds);



            
        }

        public void ForceStopAndCleanup()
        {
            if (this.ExecutedCleanup)
            {
                return;
            }
            this.Cleanup();
            this.LastActivityTimestamp = DateTime.MinValue;
        }

        private void Cleanup()
        {
            if (this.ExecutedCleanup)
            {
                return;
            }

            this.ExecutedCleanup = true;

            Console.WriteLine("Will begin cleaning session " + this.PresentationId);
            if (this.ViewSocket != null)
            {
                SendObjectToSocket(closeMessage, this.ViewSocket);
            }

            if (this.ControlSocket != null)
            {
                SendObjectToSocket(closeMessage, this.ControlSocket);
            }


            _cleanupCallback?.Invoke(this);
        }
    }
}
