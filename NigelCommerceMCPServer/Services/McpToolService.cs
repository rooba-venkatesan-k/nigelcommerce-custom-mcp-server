using NigelCommerceMCPServer.Domain;
using NigelCommerceMCPServer.Tools;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace NigelCommerceMCPServer.Services
{
    public class McpToolService
    {
        private readonly List<Tool> _tools;
        private readonly HttpClient _client;
        private readonly IHttpContextAccessor _http;

        public McpToolService(HttpClient client, IConfiguration config, IHttpContextAccessor http)
        {
            _client = client;
            _http = http;

            _client.BaseAddress = new Uri(config["NigelCommerceBaseUrl"]!);

            _tools = new();
            _tools.AddRange(ProductTools.GetTools());
            _tools.AddRange(CategoryTools.GetTools());
            _tools.AddRange(UserTools.GetTools());
            _tools.AddRange(AdminTools.GetTools());

            Console.WriteLine("[MCP Service]: McpToolService initialized with " + _tools.Count + " tools.");
        }

        public List<Tool> ListTools()
        {
            var userRoles = _http.HttpContext!.User
                .FindAll(ClaimTypes.Role)
                .Select(r => r.Value)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var tools = _tools
                .Where(t => t.AllowedRoles.Length == 0 ||
                            t.AllowedRoles.Any(r => userRoles.Contains(r)))
                .ToList();

            Console.WriteLine($"[MCP TOOLS/LIST]: Returning {tools.Count} tools for current user.");
            return tools;
        }

        public async Task<CallToolResult> CallToolAsync(string name, Dictionary<string, object>? args, string? explicitToken = null)
        {
            args ??= new();

            var tool = _tools.FirstOrDefault(t => t.Name == name);
            if (tool == null)
            {
                Console.WriteLine($"[MCP TOOLS/CALL]: Tool '{name}' not found.");
                throw new Exception($"Tool {name} not found");
            }

            // Retrieve token
            string? token = explicitToken;
            if (string.IsNullOrWhiteSpace(token))
                token = _http.HttpContext?.Request.Headers.Authorization.FirstOrDefault()?.Replace("Bearer ", "");

            Console.WriteLine($"[MCP TOOLS/CALL]: Executing tool '{name}' with args: {JsonSerializer.Serialize(args)}");

            HttpResponseMessage response;
            string resultText = "";

            try
            {
                // Helper to create and send an authenticated request
                async Task<HttpResponseMessage> SendRequestAsync(HttpMethod method, string uri, HttpContent? content = null)
                {
                    var requestMessage = new HttpRequestMessage(method, uri)
                    {
                        Content = content
                    };
                    // FIX: Set Authorization header on the specific request message, not the shared HttpClient default headers
                    if (!string.IsNullOrEmpty(token))
                    {
                        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    }
                    return await _client.SendAsync(requestMessage);
                }

                switch (name)
                {
                    // --- Product Tools ---
                    case "list_products":
                        response = await SendRequestAsync(HttpMethod.Get, "/api/Product/GetAllProducts");
                        resultText = await response.Content.ReadAsStringAsync();
                        break;

                    case "get_product":
                        if (!args.TryGetValue("productId", out var pid))
                            return ErrorResult("Missing productId");
                        response = await SendRequestAsync(HttpMethod.Get, $"/api/Product/GetProductById?productId={pid}");
                        resultText = await response.Content.ReadAsStringAsync();
                        break;

                    case "add_product":
                        var query = string.Join("&", args.Select(kv => $"{kv.Key}={kv.Value}"));
                        // For a POST with params in the URI, content is null
                        response = await SendRequestAsync(HttpMethod.Post, $"/api/Product/AddProductUsingParams?{query}", null);
                        resultText = await response.Content.ReadAsStringAsync();
                        break;

                    case "delete_product":
                        if (!args.TryGetValue("productId", out var dpId))
                            return ErrorResult("Missing productId");
                        response = await SendRequestAsync(HttpMethod.Delete, $"/api/Product/DeleteProduct?productId={dpId}");
                        resultText = response.IsSuccessStatusCode ? "Product deleted" : "Failed to delete";
                        break;

                    // --- Category Tools ---
                    case "list_categories":
                        response = await SendRequestAsync(HttpMethod.Get, "/api/Category/GetCategories");
                        resultText = await response.Content.ReadAsStringAsync();
                        break;

                    case "get_category":
                        if (!args.TryGetValue("categoryId", out var cid))
                            return ErrorResult("Missing categoryId");
                        response = await SendRequestAsync(HttpMethod.Get, $"/api/Category/GetCategoryById?categoryId={cid}");
                        resultText = await response.Content.ReadAsStringAsync();
                        break;

                    case "add_category":
                        var catJson = JsonSerializer.Serialize(args);
                        var catContent = new StringContent(catJson, Encoding.UTF8, "application/json");
                        response = await SendRequestAsync(HttpMethod.Post, "/api/Category/AddCategoryUsingModels", catContent);
                        resultText = await response.Content.ReadAsStringAsync();
                        break;

                    // --- User Tools ---
                    case "register_user":
                        var userJson = JsonSerializer.Serialize(args);
                        var userContent = new StringContent(userJson, Encoding.UTF8, "application/json");
                        response = await SendRequestAsync(HttpMethod.Post, "/api/User/NewUserRegistry", userContent);
                        resultText = await response.Content.ReadAsStringAsync();
                        break;

                    case "list_users":
                        response = await SendRequestAsync(HttpMethod.Get, "/api/User/DisplayAllUsers");
                        resultText = await response.Content.ReadAsStringAsync();
                        break;

                    // --- Admin Tools ---
                    case "update_user_role":
                        var roleJson = JsonSerializer.Serialize(args);
                        var roleContent = new StringContent(roleJson, Encoding.UTF8, "application/json");
                        response = await SendRequestAsync(HttpMethod.Put, "/api/Admin/UpdateRoleForUsers", roleContent);
                        resultText = await response.Content.ReadAsStringAsync();
                        break;

                    default:
                        Console.WriteLine($"[MCP TOOLS/CALL]: Tool '{name}' not implemented.");
                        // Throwing an exception here will lead to a JSON-RPC error response in the controller
                        throw new Exception($"Tool {name} not implemented");
                }

                if (!response.IsSuccessStatusCode)
                {
                    // Treat non-success HTTP status codes as an execution error
                    string errorDetail = $"API call failed with status code {response.StatusCode}. Response: {resultText}";
                    return ErrorResult(errorDetail);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MCP TOOLS/CALL]: Exception executing tool '{name}': {ex.Message}");
                return ErrorResult($"Execution failed: {ex.Message}");
            }

            Console.WriteLine($"[MCP TOOLS/CALL]: Tool '{name}' finished successfully.");

            return new CallToolResult
            {
                Content = new List<Content>
            {
                new Content
                {
                    Type = "text",
                    Text = resultText // The raw JSON string from the API is placed here
                }
            }
            };
        }

        private CallToolResult ErrorResult(string message)
        {
            Console.WriteLine($"[MCP TOOLS/CALL]: ErrorResult: {message}");

            var errorObject = new
            {
                error = "ToolExecutionFailure",
                message = message
            };

            return new CallToolResult
            {
                Content = new List<Content>
            {
                new Content
                {
                    Type = "text",
                    Text = JsonSerializer.Serialize(errorObject)
                }
            }
            };
        }
    }
}
