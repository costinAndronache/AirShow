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
using AirShow.Models.Common;
using AirShow.Models.EF;

namespace AirShow.WebSockets
{
    public class GlobalSessionManager
    {
        private const string RoomTokenKey = "kRoomTokenKey";
        private const string SideKey = "kSideKey";
        private const string ViewSide = "view";
        private const string ControlSide = "control";

        private ConcurrentDictionary<string, ConcurrentBag<PresentationSession>> _sessionsPerUserId;
        private ConcurrentDictionary<string, PresentationSession> _sessionPerToken;
        private ConcurrentDictionary<string, int> _presentationForReservedToken;
        private ConcurrentDictionary<int, string> _sessionTokenForPresentationId;
        private ConcurrentDictionary<string, string> _userIdForToken;

        public GlobalSessionManager()
        {
            _userIdForToken = new ConcurrentDictionary<string, string>();
            _sessionPerToken = new ConcurrentDictionary<string, PresentationSession>();
            _presentationForReservedToken = new ConcurrentDictionary<string, int>();
            _sessionsPerUserId = new ConcurrentDictionary<string, ConcurrentBag<PresentationSession>>();
            _sessionTokenForPresentationId = new ConcurrentDictionary<int, string>();

        }

        public async void HandleWebSocketV2(WebSocket webSocket)
        {
            var token = CancellationToken.None;
            var buffer = new ArraySegment<Byte>(new Byte[4096]);
            var received = webSocket.ReceiveAsync(buffer, token).Result;
            if (received.MessageType == WebSocketMessageType.Text)
            {
                var request = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
                var am = JsonConvert.DeserializeObject<Dictionary<string, string>>(WebUtility.HtmlDecode(request));

                if (!(am.ContainsKey(RoomTokenKey) && am.ContainsKey(SideKey)))
                {
                    return;
                }

                var roomToken = am[RoomTokenKey];
                var side = am[SideKey];

                var session = GetOrCreateSessionForToken(roomToken);
                if (session.ErrorMessageIfAny != null)
                {
                    Console.WriteLine(session.ErrorMessageIfAny);
                    return;
                }

                if (side == ViewSide)
                {
                    session.Value.ReplaceOrSetViewSocket(webSocket);
                    KeepPresentationSocketAlive(webSocket);
                } else
                {
                    session.Value.ReplaceOrSetControlSocket(webSocket);
                    session.Value.BeginRunLoopIfNecessary();
                }
            }
        }

        public List<int> ActivePresentationIdsForUser(string userId)
        {
            if (_sessionsPerUserId.ContainsKey(userId))
            {
                return _sessionsPerUserId[userId].Select(ps => ps.PresentationId).ToList();
            }
            return new List<int>();
        }

        public string ReserveNewSessionTokenFor(string userId, Presentation p)
        {
            var token = p.UploadedDate.Ticks + "" + p.Id;
            _presentationForReservedToken[token] = p.Id;
            _userIdForToken[token] = userId;
            _sessionTokenForPresentationId[p.Id] = token;

            if (!_sessionsPerUserId.ContainsKey(userId))
            {
                _sessionsPerUserId[userId] = new ConcurrentBag<PresentationSession>();
            }

            return token;
        }

        private OperationResult<PresentationSession> GetOrCreateSessionForToken(string token)
        {
            var result = new OperationResult<PresentationSession>();

            if (!_presentationForReservedToken.ContainsKey(token))
            {
                result.ErrorMessageIfAny = "No presentation reserved for that session token";
                return result;
            }

            if (_sessionPerToken.ContainsKey(token))
            {
                result.Value = _sessionPerToken[token];
                return result;
            }

            Action cleanup = () => { };

            var newSession = new PresentationSession(_presentationForReservedToken[token], token, cleanup);
            var userId = _userIdForToken[token];
            _sessionsPerUserId[userId].Add(newSession);


            result.Value = newSession;
            _sessionPerToken[token] = newSession;

            return result;
        }

        public string GetTokenForPresentationId(int presentationId)
        {
            return _sessionTokenForPresentationId[presentationId];
        }

        public void KeepPresentationSocketAlive(WebSocket webSocket)
        {
            Console.Write(Thread.CurrentThread);
            var token = CancellationToken.None;
            var buffer = new ArraySegment<Byte>(new Byte[4096]);
            var received = webSocket.ReceiveAsync(buffer, token).Result;
        }
    }
}
