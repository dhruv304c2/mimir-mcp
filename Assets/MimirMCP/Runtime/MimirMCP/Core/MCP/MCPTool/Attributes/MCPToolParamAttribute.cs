using System;

namespace MimirMCP.Core.MCP.MCPTool.Attributes
{
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property,
        AllowMultiple = true,
        Inherited = true
    )]
    public class MCPToolParamAttribute : Attribute
    {
        public string ParamName { get; private set; }
        public string Description { get; private set; }
        public bool IsRequired { get; private set; }
        public MCPToolParam.ParamType ParamType { get; set; }

        public MCPToolParamAttribute(
            string paramName,
            string description,
            MCPToolParam.ParamType paramType,
            bool isRequired = false
        )
        {
            ParamName = paramName;
            Description = description;
            ParamType = paramType;
            IsRequired = isRequired;
        }
    }
}
