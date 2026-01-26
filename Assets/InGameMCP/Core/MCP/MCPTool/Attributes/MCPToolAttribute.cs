using System;

namespace InGameMCP.Core.MCP.MCPTool.Attributes
{
    public class MCPToolAttribute : Attribute
    {
        public string ToolName { get; private set; }
        public string Description { get; private set; }

        public MCPToolAttribute(string toolName, string description)
        {
            ToolName = toolName;
            Description = description;
        }
    }
}
