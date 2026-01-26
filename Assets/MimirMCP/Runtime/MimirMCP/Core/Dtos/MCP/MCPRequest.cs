using System.Collections.Generic;

namespace MimirMCP.Core.Dtos.MCP
{
    public class MCPRequest : MCPBase
    {
        public string method;
        public Dictionary<string, object> @params;
    } 
}
