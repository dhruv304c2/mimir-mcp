using System;
using System.Collections.Generic;
using System.Net;
using System.Web;
using Cysharp.Threading.Tasks;
using InGameMCP.Core;
using InGameMCP.Core.Dtos.MCP;
using InGameMCP.Core.HTTP;
using InGameMCP.Core.MCP.MCPTool;
using InGameMCP.Utils.HTTPUtils;
using Newtonsoft.Json.Linq;

namespace InGameMCP.Core.MCP
{
    public class MCPHandler : HTTPHandler<MCPRequest>
    {
        readonly List<MCPToolBase> _registeredTools = new List<MCPToolBase>();
        readonly string _serverName;
        readonly string _serverVersion;
        readonly ILogger _logger;

        public MCPHandler(
            string serverName = "Mimir-MCP",
            string serverVersion = "0.1.0",
            ILogger logger = null
        )
            : base("/mcp", "POST")
        {
            _serverName = serverName;
            _serverVersion = serverVersion;
            _logger = logger;
            RequiredHeaders = new List<string>() { "Content-Type" };
            Verifications.Add(
                (req) =>
                {
                    var contentType = req.Headers["Content-Type"];
                    if (string.IsNullOrWhiteSpace(contentType))
                    {
                        return (false, "Content-Type must be application/json");
                    }

                    if (!contentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase))
                    {
                        return (false, "Content-Type must be application/json");
                    }

                    return (true, "");
                }
            );
        }

        public void RegisterTool(MCPToolBase tool)
        {
            if (_registeredTools.Contains(tool))
            {
                return;
            }
            _registeredTools.Add(tool);
        }

        public bool TryGetToolByName(string toolName, out MCPToolBase tool)
        {
            foreach (var registeredTool in _registeredTools)
            {
                if (registeredTool.ToolName == toolName)
                {
                    tool = registeredTool;
                    return true;
                }
            }
            tool = null;
            return false;
        }

        public override async UniTask HandleAsync(HttpListenerContext ctx, MCPRequest request)
        {
            if (request == null)
            {
                WriteMcpError(
                    ctx,
                    null,
                    -32700,
                    "Invalid request body",
                    HttpStatusCode.BadRequest
                );
                return;
            }

            var mcpRequest = request;

            _logger?.LogInfo(
                $"MCP request: method={mcpRequest.method ?? "<null>"}, id={mcpRequest.id ?? "<notification>"}"
            );

            var isNotification = mcpRequest.id == null;

            switch (mcpRequest.method)
            {
                case "initialize":
                    var initResponse = new MCPInitializeResponse(
                        mcpRequest.id,
                        new MCPInitializeResult
                        {
                            protocolVersion = ResolveProtocolVersion(mcpRequest.@params),
                            serverInfo = new MCPServerInfo
                            {
                                name = _serverName,
                                version = _serverVersion,
                            },
                            capabilities = new MCPServerCapabilities
                            {
                                tools = new MCPToolCapabilities
                                {
                                    listChanged = true,
                                },
                            },
                        }
                    );
                    HTTPUtils.SafeWriteJson(ctx, HttpStatusCode.OK, initResponse);
                    break;
                case "ping":
                    var pingResponse = new MCPResultResponse(mcpRequest.id);
                    HTTPUtils.SafeWriteJson(ctx, HttpStatusCode.OK, pingResponse);
                    break;
                case "tools/list":
                    var toolUsages = new List<MCPToolUsage>();
                    foreach (var regTool in _registeredTools)
                    {
                        toolUsages.Add(regTool.GetToolUsage());
                    }
                    var toolResponse = new MCPToolResponse
                    {
                        id = mcpRequest.id,
                        result = new MCPToolListResult
                        {
                            tools = toolUsages,
                        }
                    };
                    HTTPUtils.SafeWriteJson(ctx, HttpStatusCode.OK, toolResponse);
                    break;
                case "tools/call":
                    if (!TryResolveToolName(mcpRequest.@params, out var toolName, out var toolNameError))
                    {
                        WriteMcpError(ctx, mcpRequest.id, -32602, toolNameError);
                        return;
                    }

                    if (TryGetToolByName(toolName, out var tool))
                    {
                        Dictionary<string, object> parameters = new Dictionary<string, object>();

                        if (mcpRequest.@params != null)
                        {
                            object argumentsObj = null;

                            // Standard MCP schema nests arguments under "arguments"
                            if (!mcpRequest.@params.TryGetValue("arguments", out argumentsObj))
                            {
                                mcpRequest.@params.TryGetValue("input", out argumentsObj);
                            }

                            if (argumentsObj is JObject jObject)
                            {
                                parameters = jObject.ToObject<Dictionary<string, object>>();
                            }
                            else if (argumentsObj is Dictionary<string, object> dict)
                            {
                                parameters = dict;
                            }
                        }

                        _logger?.LogInfo(
                            $"Executing MCP tool '{toolName}' with id={mcpRequest.id ?? "<notification>"}."
                        );

                        await tool.HandleToolCall(mcpRequest.id, ctx, parameters);
                    }
                    else
                    {
                        WriteMcpError(ctx, mcpRequest.id, -32601, "Tool not found");
                    }
                    break;
                case "notifications/initialized":
                    ctx.Response.StatusCode = (int)HttpStatusCode.NoContent;
                    ctx.Response.Close();
                    break;
                default:
                    if (isNotification)
                    {
                        ctx.Response.StatusCode = (int)HttpStatusCode.NoContent;
                        ctx.Response.Close();
                    }
                    else
                    {
                        WriteMcpError(
                            ctx,
                            mcpRequest.id,
                            -32601,
                            $"Unknown MCP method: {mcpRequest.method ?? "<null>"}"
                        );
                    }
                    break;
            }
        }

        static void WriteMcpError(
            HttpListenerContext ctx,
            object id,
            int code,
            string message,
            HttpStatusCode statusCode = HttpStatusCode.BadRequest
        )
        {
            var error = new MCPErrorResponse(id, new MCPError(code, message));
            HTTPUtils.SafeWriteJson(ctx, statusCode, error);
        }

        static string ResolveProtocolVersion(Dictionary<string, object> @params)
        {
            if (
                @params != null
                && @params.TryGetValue("protocolVersion", out var versionObj)
                && versionObj is string version
                && !string.IsNullOrWhiteSpace(version)
            )
            {
                return version;
            }

            return "2025-06-18";
        }

        static bool TryResolveToolName(
            Dictionary<string, object> parameters,
            out string toolName,
            out string errorMessage
        )
        {
            toolName = null;
            errorMessage = null;

            if (parameters == null)
            {
                errorMessage = "tool_name parameter is required and must be a string";
                return false;
            }

            object toolNameObj = null;

            if (
                !parameters.TryGetValue("tool_name", out toolNameObj)
                && !parameters.TryGetValue("name", out toolNameObj)
            )
            {
                errorMessage = "tool_name parameter is required and must be a string";
                return false;
            }

            if (toolNameObj is string toolNameString && !string.IsNullOrWhiteSpace(toolNameString))
            {
                toolName = toolNameString;
                return true;
            }

            errorMessage = "tool_name parameter is required and must be a string";
            return false;
        }
    }
}
