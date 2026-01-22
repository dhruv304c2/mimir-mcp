using System;
using System.Collections.Generic;

namespace InGameMCP.Core.Dtos.MCP
{
    [Serializable]
    public class MCPToolUsage
    {
        public string name;
        public string description;
        public InputSchema input_schema;
    }

    [Serializable]
    public class InputSchema : Property
    {
        public Dictionary<string, Property> properties;
        public List<string> required;
    }

    [Serializable]
    public class Property
    {
        public string type = "object";
        public string description;
    }
}