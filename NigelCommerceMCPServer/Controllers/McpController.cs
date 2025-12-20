using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using NigelCommerceMCPServer.Domain;
using NigelCommerceMCPServer.Services;
using System.Text.Json;

namespace NigelCommerceMCPServer.Controllers
{
    [ApiController]
    public class McpController : ControllerBase
    {
        private readonly McpSessionManager _sessions;
        private readonly McpToolService _tools;

        public McpController(McpSessionManager sessions, McpToolService tools)
        {
            _sessions = sessions;
            _tools = tools;
            Console.WriteLine("[MCP Service]: McpToolService initialized.");
        }

        [Authorize]
        [HttpGet("mcp")]
        public async Task Connect()
        {
            HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();

            Response.Headers.ContentType = "text/event-stream";
            Response.Headers.CacheControl = "no-cache";
            Response.Headers.Connection = "keep-alive";

            var sessionId = _sessions.CreateSession(Response, User);
            Console.WriteLine($"[MCP CONNECT]: New SSE session created. ID: {sessionId}");

            // Notify client about messages endpoint
            await _sessions.SendEndpointEventAsync(
                sessionId,
                $"{Request.Scheme}://{Request.Host}/mcp/messages");

            var lastActivity = DateTime.UtcNow;

            try
            {
                while (!HttpContext.RequestAborted.IsCancellationRequested)
                {
                    await Task.Delay(15000); // Send keepalive every 15 seconds

                    await _sessions.SendCommentAsync(sessionId, "keepalive");
                    Console.WriteLine($"[MCP CONNECT]: Sending keepalive for session {sessionId}");

                    // Remove session if inactive for 60 minutes
                    if ((DateTime.UtcNow - lastActivity).TotalMinutes > 60)
                    {
                        Console.WriteLine($"[MCP CONNECT]: Session {sessionId} timed out due to inactivity.");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MCP CONNECT]: Exception in session {sessionId} - {ex.Message}");
            }
            finally
            {
                _sessions.RemoveSession(sessionId);
                Console.WriteLine($"[MCP CONNECT]: SSE connection for session {sessionId} closing.");
            }
        }


        [Authorize]
        [HttpPost("mcp/messages")]
        public async Task<IActionResult> Handle(
            [FromQuery] string sessionId,
            [FromBody] JsonRpcRequest request)
        {
            Console.WriteLine($"[MCP HANDLE]: Received method '{request.Method}' for session {sessionId}.");

            var response = new JsonRpcResponse { Id = request.Id };

            switch (request.Method)
            {
                case "initialize":
                    response.Result = new InitializeResult
                    {
                        Capabilities = new ServerCapabilities
                        {
                            Tools = new { list = true, call = true }
                        },
                        ServerInfo = new Implementation
                        {
                            Name = "NigelCommerceMcp",
                            Version = "1.0 allowing tools to work"
                        }
                    };
                    Console.WriteLine($"[MCP HANDLE]: Sent initialize response.");
                    break;

                case "notifications/initialized":
                    Console.WriteLine($"[MCP HANDLE]: Received notifications/initialized. Returning NoContent.");
                    return NoContent(); // This is a notification, no JSON-RPC response is required over SSE

                case "notifications/cancel":
                    Console.WriteLine($"[MCP HANDLE]: Received notifications/cancel for session {sessionId}.");
                    return NoContent(); // This is a notification, no JSON-RPC response is required over SSE

                case "tools/list":
                    response.Result = new ListToolsResult
                    {
                        Tools = _tools.ListTools()
                    };
                    Console.WriteLine($"[MCP TOOLS/LIST]: Tools list sent for session {sessionId}.");
                    break;

                case "tools/call":
                    if (request.Params == null)
                    {
                        response.Error = new JsonRpcError
                        {
                            Code = -32602,
                            Message = "Missing 'params' object for tools/call"
                        };
                        Console.WriteLine($"[MCP TOOLS/CALL]: Missing params for session {sessionId}.");
                        break;
                    }

                    CallToolParams? toolCallParams;
                    try
                    {
                        var jsonString = JsonSerializer.Serialize(request.Params);

                        toolCallParams = JsonSerializer.Deserialize<CallToolParams>(jsonString);

                        if (toolCallParams == null || string.IsNullOrWhiteSpace(toolCallParams.Name))
                        {
                            throw new JsonException("Tool name is missing or parameters are malformed.");

                        }
                    }
                    catch (Exception ex)
                    {
                        response.Error = new JsonRpcError
                        {
                            Code = -32602,
                            Message = $"Invalid parameters format or missing tool name: {ex.Message}"
                        };
                        Console.WriteLine($"[MCP TOOLS/CALL]: Error parsing params for session {sessionId}: {ex.Message}");
                        break;
                    }

                    Console.WriteLine($"[MCP TOOLS/CALL]: Calling tool '{toolCallParams.Name}' for session {sessionId}.");

                    try
                    {
                        response.Result = await _tools.CallToolAsync(toolCallParams.Name, toolCallParams.Arguments);
                        Console.WriteLine($"[MCP TOOLS/CALL]: Tool '{toolCallParams.Name}' execution finished for session {sessionId}.");
                    }
                    catch (Exception ex)
                    {
                        response.Error = new JsonRpcError
                        {
                            Code = -32603, // Internal error for tool execution failure
                            Message = $"Tool execution failed: {ex.Message}"
                        };
                        Console.WriteLine($"[MCP TOOLS/CALL]: Tool execution exception for session {sessionId}: {ex.Message}");
                    }

                    break;

                default:
                    response.Error = new JsonRpcError
                    {
                        Code = -32601,
                        Message = $"Method {request.Method} not found"
                    };
                    Console.WriteLine($"[MCP HANDLE]: Unknown method '{request.Method}' for session {sessionId}.");
                    break;
            }

            await _sessions.SendResponseAsync(sessionId, response);
            Console.WriteLine($"[MCP SESSION MANAGER]: Response sent for session {sessionId}.");

            return NoContent(); // No Content to the HTTP POST endpoint to acknowledge receipt, as the actual response is sent over the SSE stream.
        }
    }
}
