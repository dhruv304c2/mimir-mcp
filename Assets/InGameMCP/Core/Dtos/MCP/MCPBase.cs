using System;

namespace InGameMCP.Core.Dtos.MCP
{
    [Serializable]
    public class MCPBase
    {
        public string jsonrpc = "2.0";
        public string id;
    }
}