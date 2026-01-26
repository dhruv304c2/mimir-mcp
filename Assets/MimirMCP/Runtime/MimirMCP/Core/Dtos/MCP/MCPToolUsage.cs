using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MimirMCP.Core.Dtos.MCP
{
    [Serializable]
    public class MCPToolUsage
    {
        public string name;
        public string description;
        public InputSchema inputSchema;
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

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string description;
    }
}
