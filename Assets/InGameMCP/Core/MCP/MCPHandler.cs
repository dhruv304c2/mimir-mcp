using System;
using System.Collections.Generic;
using System.Net;
using System.Web;
using InGameMCP.Core.Dtos;
using InGameMCP.Core.Dtos.MCP;
using InGameMCP.Core.HTTP;
using InGameMCP.Core.MCP.MCPTool;
using InGameMCP.Utils.HTTPUtils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace InGameMCP.Core.MCP
{
    public class MCPHandler : HTTPHandler<MCPRequest>
    {
        List<MCPToolBase> _registeredTools = new List<MCPToolBase>();

        public MCPHandler()
            : base("/mcp", "POST")
        {
            RequiredHeaders = new List<string>() { "Content-Type" };
            Verifications.Add(
                (req) =>
                {
                    if (req.Headers["Content-Type"] != "application/json")
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

        public override void Handle(HttpListenerContext ctx, MCPRequest request)
        {
            if (request == null)
            {
                HTTPUtils.SafeWriteJson(
                    ctx,
                    HttpStatusCode.BadRequest,
                    new ErrorResponse("Invalid request body")
                );
                return;
            }

            var mcpRequest = request;

            switch (mcpRequest.method)
            {
                case "tools/list":
                    var toolUsages = new List<MCPToolUsage>();
                    foreach (var regTool in _registeredTools)
                    {
                        toolUsages.Add(regTool.GetToolUsage());
                    }
                    var toolResponse = new MCPToolResponse
                    {
                        id = mcpRequest.id,
                        tools = toolUsages
                    };
                    HTTPUtils.SafeWriteJson(ctx, HttpStatusCode.OK, toolResponse);
                    break;
                case "tools/call":
                    if (
                        mcpRequest.@params == null
                        || !mcpRequest.@params.TryGetValue("tool_name", out var toolNameObj)
                        || toolNameObj is not string toolName
                    )
                    {
                        HTTPUtils.SafeWriteJson(
                            ctx,
                            HttpStatusCode.BadRequest,
                            new ErrorResponse(
                                "tool_name parameter is required and must be a string"
                            )
                        );
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

                        tool.HandleToolCall(mcpRequest.id, ctx, parameters);
                    }
                    else
                    {
                        var mcpError = new MCPErrorResponse(
                            mcpRequest.id,
                            new MCPError(-32601, "Tool not found")
                        );

                        HTTPUtils.SafeWriteJson(ctx, HttpStatusCode.BadRequest, mcpError);
                    }
                    break;
                default:
                    var mcpErrorDefault = new MCPErrorResponse(
                        mcpRequest.id,
                        new MCPError(-32601, "Unknown MCP method")
                    );
                    HTTPUtils.SafeWriteJson(ctx, HttpStatusCode.BadRequest, mcpErrorDefault);
                    break;
            }
        }
    }
}
