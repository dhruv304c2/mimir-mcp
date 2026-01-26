using System;

namespace MimirMCP.Core.Dtos.MCP
{
    [Serializable]
    public class MCPInitializeResponse : MCPBase
    {
        public MCPInitializeResult result;

        public MCPInitializeResponse(object id, MCPInitializeResult result)
        {
            this.id = id;
            this.result = result;
        }
    }

    [Serializable]
    public class MCPInitializeResult
    {
        public string protocolVersion;
        public MCPServerInfo serverInfo;
        public MCPServerCapabilities capabilities;
    }

    [Serializable]
    public class MCPServerInfo
    {
        public string name;
        public string version;
    }

    [Serializable]
    public class MCPServerCapabilities
    {
        public MCPToolCapabilities tools;
    }

    [Serializable]
    public class MCPToolCapabilities
    {
        public bool listChanged;
    }
}
