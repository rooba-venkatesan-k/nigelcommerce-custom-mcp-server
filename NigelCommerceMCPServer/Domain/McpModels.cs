using System.Text.Json.Serialization;

namespace NigelCommerceMCPServer.Domain
{
    public class JsonRpcRequest
    {
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        [JsonPropertyName("method")]
        public string Method { get; set; } = default!;

        [JsonPropertyName("params")]
        public object? Params { get; set; }

        [JsonPropertyName("id")]
        public object? Id { get; set; }
    }

    public class JsonRpcResponse
    {
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        [JsonPropertyName("result")]
        public object? Result { get; set; }

        [JsonPropertyName("error")]
        public JsonRpcError? Error { get; set; }

        [JsonPropertyName("id")]
        public object? Id { get; set; }
    }

    public class JsonRpcError
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = default!;
    }

    public class InitializeResult
    {
        public string ProtocolVersion { get; set; } = "2024-11-05";
        public ServerCapabilities Capabilities { get; set; } = default!;
        public Implementation ServerInfo { get; set; } = default!;
    }

    public class ServerCapabilities
    {
        public object Tools { get; set; } = default!;
    }

    public class Implementation
    {
        public string Name { get; set; } = default!;
        public string Version { get; set; } = default!;
    }

    public class Tool
    {
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public object InputSchema { get; set; } = default!;

        [JsonIgnore]
        public string[] AllowedRoles { get; set; } = Array.Empty<string>();
    }

    public class ListToolsResult
    {
        public List<Tool> Tools { get; set; } = new();
    }

    public class CallToolParams
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = default!;

        [JsonPropertyName("arguments")]
        public Dictionary<string, object>? Arguments { get; set; }
    }

    public class CallToolResult
    {
        public List<Content> Content { get; set; } = new();
    }

    public class Content
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "text";


        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}
