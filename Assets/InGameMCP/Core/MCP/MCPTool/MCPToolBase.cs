using System.Collections.Generic;
using System.Net;
using InGameMCP.Core.Dtos.MCP;
using InGameMCP.Utils.HTTPUtils;

namespace InGameMCP.Core.MCP.MCPTool
{
    public class MCPToolParam
    {
        public enum ParamType
        {
            String,
            Number,
            Boolean,
        }

        public string ParamName { get; private set; }
        public string ParamDescription { get; private set; }
        public bool IsRequired { get; private set; } = false;
        public ParamType Type { get; private set; } = ParamType.String;

        public string GetTypeAsString()
        {
            switch (Type)
            {
                case ParamType.String:
                    return "string";
                case ParamType.Number:
                    return "number";
                case ParamType.Boolean:
                    return "boolean";
                default:
                    return "object";
            }
        }

        public MCPToolParam(
            string name,
            string description,
            ParamType type = ParamType.String,
            bool isRequired = false
        )
        {
            ParamName = name;
            ParamDescription = description;
            IsRequired = isRequired;
            Type = type;
        }
    }

    public class MCPToolBase
    {
        public string ToolName { get; private set; }
        public string ToolDescription { get; private set; }
        public List<MCPToolParam> ToolParams { get; protected set; } = new List<MCPToolParam>();

        public MCPToolBase(string name, string description)
        {
            ToolName = name;
            ToolDescription = description;
        }

        public virtual void HandleToolCall(
            object id,
            HttpListenerContext ctx,
            Dictionary<string, object> parameters
        )
        {
            HTTPUtils.SafeWriteJson(
                ctx,
                HttpStatusCode.NotImplemented,
                new MCPErrorResponse(
                    id,
                    new MCPError(-32601, "Tool execution not implemented")
                )
            );
        }

        public MCPToolUsage GetToolUsage()
        {
            var inputSchema = new InputSchema
            {
                properties = new Dictionary<string, Property>(),
                required = new List<string>(),
            };

            foreach (var param in ToolParams)
            {
                inputSchema.properties[param.ParamName] = new Property
                {
                    type = param.GetTypeAsString(),
                    description = param.ParamDescription,
                };

                if (param.IsRequired)
                {
                    inputSchema.required.Add(param.ParamName);
                }
            }

            return new MCPToolUsage
            {
                name = ToolName,
                description = ToolDescription,
                inputSchema = inputSchema,
            };
        }
    }
}
