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

        private ConcurrentDictionary<string, ConcurrentDictionary<int, PresentationSession>> _sessionsPerUserId;
        private ConcurrentDictionary<string, PresentationSession> _sessionPerToken;
        private ConcurrentDictionary<string, int> _presentationForReservedToken;
        private ConcurrentDictionary<int, string> _sessionTokenForPresentationId;
        private ConcurrentDictionary<string, string> _userIdForToken;

        public GlobalSessionManager()
        {
            _userIdForToken = new ConcurrentDictionary<string, string>();
            _sessionPerToken = new ConcurrentDictionary<string, PresentationSession>();
            _presentationForReservedToken = new ConcurrentDictionary<string, int>();
            _sessionsPerUserId = new ConcurrentDictionary<string, ConcurrentDictionary<int, PresentationSession>>();
            _sessionTokenForPresentationId = new ConcurrentDictionary<int, string>();

        }

        public async void HandleWebSocketV(WebSocket webSocket)
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

                var sessionResult = GetOrCreateSessionForToken(roomToken);
                if (sessionResult.ErrorMessageIfAny != null)
                {
                    Console.WriteLine(sessionResult.ErrorMessageIfAny);
                    return;
                }

                if (side == ViewSide)
                {
                    sessionResult.Value.ReplaceOrSetViewSocket(webSocket);
                    KeepPresentationSocketAlive(webSocket);
                } else
                {
                    sessionResult.Value.ReplaceOrSetControlSocket(webSocket);
                    sessionResult.Value.BeginRunLoopIfNecessary();
                }
            }
        }

        public List<int> ActivePresentationIdsForUser(string userId)
        {
            if (_sessionsPerUserId.ContainsKey(userId))
            {
                
                return _sessionsPerUserId[userId].Values.Select(ps => ps.PresentationId).ToList();
            }
            return new List<int>();
        }

        public string ReserveNewSessionTokenFor(string userId, Presentation p)
        {
            var pId = p.Id;

            var token = p.UploadedDate.Ticks + "" + p.Id;
            _presentationForReservedToken[token] = p.Id;
            _userIdForToken[token] = userId;
            _sessionTokenForPresentationId[p.Id] = token;

            if (!_sessionsPerUserId.ContainsKey(userId))
            {
                _sessionsPerUserId[userId] = new ConcurrentDictionary<int, PresentationSession>();
            }

            Timer timer = null;
            timer = new Timer((obj) =>
            {
                timer.Dispose();
                if (!_sessionPerToken.ContainsKey(token))
                {
                    var outString = "";
                    var outInt = 0;
                    _presentationForReservedToken.TryRemove(token, out outInt);
                    _userIdForToken.TryRemove(token, out outString);
                    _sessionTokenForPresentationId.TryRemove(pId, out outString);
                }

            }, null, 5000, System.Threading.Timeout.Infinite);

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

            Action<PresentationSession> cleanup = (session) => 
            {
                var presentationId = session.PresentationId;
                var cleanToken = _sessionTokenForPresentationId[presentationId];
                var outString = "";

                _sessionTokenForPresentationId.TryRemove(presentationId, out outString);
                _presentationForReservedToken.TryRemove(cleanToken, out presentationId);

                var userIdToClean = _userIdForToken[cleanToken];
                _userIdForToken.TryRemove(token, out outString);

                PresentationSession outS = null;
                _sessionPerToken.TryRemove(cleanToken, out outS);

                _sessionsPerUserId[userIdToClean].TryRemove(presentationId, out outS);
                if (_sessionsPerUserId[userIdToClean].Count == 0)
                {
                    ConcurrentDictionary<int, PresentationSession> outPS = null;
                    _sessionsPerUserId.TryRemove(userIdToClean, out outPS);

                }
                Console.WriteLine("\nDid clean up session for presentation " + presentationId);

            };

            var presId = _presentationForReservedToken[token];
            var newSession = new PresentationSession(presId, token, cleanup);
            var userId = _userIdForToken[token];
            _sessionsPerUserId[userId].TryAdd(presId, newSession);


            result.Value = newSession;
            _sessionPerToken[token] = newSession;

            return result;
        }

        public OperationResult<string> ForceStopSessionForPresentation(int presentationId)
        {
            var opResult = new OperationResult<string>();

            if (!_sessionTokenForPresentationId.ContainsKey(presentationId))
            {
                opResult.ErrorMessageIfAny = "No session reserved for that presentation";
                return opResult;
            }

            var token = _sessionTokenForPresentationId[presentationId];
            if (!_sessionPerToken.ContainsKey(token))
            {
                opResult.ErrorMessageIfAny = "No session has been created for that presentation";
                return opResult;
            }

            _sessionPerToken[token].ForceStopAndCleanup();

            return opResult;
        }

        public OperationResult<string> GetTokenForPresentationId(int presentationId)
        {
            var opResult = new OperationResult<string>();
            if (!_sessionTokenForPresentationId.ContainsKey(presentationId))
            {
                opResult.ErrorMessageIfAny = "No session reserved for that presentation";
                return opResult;
            }
            opResult.Value = _sessionTokenForPresentationId[presentationId];
            return opResult;
        }

        public async Task<OperationStatus> SendControlMessage(string userId, string sessionToken, string message)
        {
            var opResult = new OperationStatus();
            if (!_userIdForToken.ContainsKey(sessionToken) || _userIdForToken[sessionToken] != userId)
            {
                opResult.ErrorMessageIfAny = "The userId specified does not have a session with that token";
                return opResult;
            }

            if (!_sessionPerToken.ContainsKey(sessionToken))
            {
                opResult.ErrorMessageIfAny = "No session has been started with that token";
                return opResult;
            }

            await _sessionPerToken[sessionToken].SendControlMessageToView(message);

            return opResult;
        }

        public void KeepPresentationSocketAlive(WebSocket webSocket)
        {
            try
            {
                Console.Write(Thread.CurrentThread);
                var token = CancellationToken.None;
                var buffer = new ArraySegment<Byte>(new Byte[4096]);
                var received = webSocket.ReceiveAsync(buffer, token).Result;
            } catch(Exception e)
            {

            }
        }
    }
}
