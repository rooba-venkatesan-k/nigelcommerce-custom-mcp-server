using NigelCommerceMCPServer.Domain;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NigelCommerceMCPServer.Services
{
    public class McpSession
    {
        public HttpResponse Response { get; }
        public SemaphoreSlim Lock { get; } = new(1, 1);
        public ClaimsPrincipal User { get; }
        public List<Tool>? AllowedTools { get; set; }

        public McpSession(HttpResponse response, ClaimsPrincipal user)
        {
            Response = response;
            User = user;
        }
    }

    public class McpSessionManager
    {
        JsonSerializerOptions options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly ConcurrentDictionary<string, McpSession> _sessions = new();

        public string CreateSession(HttpResponse response, ClaimsPrincipal user)
        {
            var id = Guid.NewGuid().ToString();
            _sessions[id] = new McpSession(response, user);
            return id;
        }

        public void RemoveSession(string sessionId)
        {
            if (_sessions.TryRemove(sessionId, out _))
            {
                Console.WriteLine($"[MCP SESSION MANAGER]: Session {sessionId} removed.");
            }
        }

        // Helper to get session ID from object reference (needed for cleanup)
        private string? GetSessionId(McpSession session)
        {
            return _sessions.FirstOrDefault(kv => kv.Value == session).Key;
        }

        public async Task SendResponseAsync(string sessionId, JsonRpcResponse response)
        {
            if (_sessions.TryGetValue(sessionId, out var session))
            {
                var json = JsonSerializer.Serialize(response, options);
                await WriteAsync(session, $"event: message\ndata: {json}\n\n");
                Console.WriteLine($"[MCP SESSION MANAGER]: Response sent for session {sessionId}.");
            }
            else
            {
                Console.WriteLine($"[MCP SESSION MANAGER ERROR]: Failed to send response, session {sessionId} not found.");
            }
        }

        public async Task SendEndpointEventAsync(string sessionId, string url)
        {
            if (_sessions.TryGetValue(sessionId, out var session))
            {
                await WriteAsync(session, $"event: endpoint\ndata: {url}?sessionId={sessionId}\n\n");
            }
        }

        public async Task SendCommentAsync(string sessionId, string comment)
        {
            if (_sessions.TryGetValue(sessionId, out var session))
            {
                await WriteAsync(session, $": {comment}\n\n");
            }
        }

        private async Task WriteAsync(McpSession session, string data)
        {
            await session.Lock.WaitAsync();
            try
            {
                await session.Response.WriteAsync(data);
                await session.Response.Body.FlushAsync();
            }
            catch (Exception ex) when (ex is System.IO.IOException || ex is SocketException)
            {
                // Log the error and clean up the session to prevent future writes to a closed pipe.
                Console.WriteLine($"[MCP SSE WRITE ERROR]: Connection closed by client. Exception: {ex.Message}");

                var sessionId = GetSessionId(session);
                if (sessionId != null)
                {
                    RemoveSession(sessionId);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MCP SSE WRITE ERROR]: Unexpected error during write: {ex.Message}");
            }
            finally
            {
                session.Lock.Release();
            }
        }
    }
}
