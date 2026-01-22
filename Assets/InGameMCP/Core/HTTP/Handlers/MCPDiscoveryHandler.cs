using System.Net;
using InGameMCP.Core.Dtos;
using InGameMCP.Core.HTTP;
using InGameMCP.Utils.HTTPUtils;

namespace InGameMCP.Core.HTTP.Handlers
{
    /// <summary>
    /// Responds to GET /mcp requests with basic metadata so clients that probe the
    /// endpoint before opening an SSE stream receive a valid response.
    /// </summary>
    public class MCPDiscoveryHandler : HTTPHandler
    {
        readonly string _serverName;

        public MCPDiscoveryHandler(string serverName = "Mimir-MCP")
            : base("/mcp", "GET")
        {
            _serverName = serverName;
        }

        public override void Handle(HttpListenerContext ctx)
        {
            var response = new OkResponse(
                $"{_serverName} endpoint ready at /mcp (POST for MCP RPC)."
            );
            HTTPUtils.SafeWriteJson(ctx, HttpStatusCode.OK, response);
        }
    }
}
