using System;

namespace MimirMCP.Core.Dtos.MCP
{
    [Serializable]
    public class MCPBase
    {
        public string jsonrpc = "2.0";
        public object id;
    }
}
