using System.Collections.Generic;

namespace InGameMCP.Core.Dtos.MCP
{
    public class MCPRequest : MCPBase
    {
        public string method;
        public Dictionary<string, object> @params;
    } 
}
