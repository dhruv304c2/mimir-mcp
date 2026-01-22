using System;

namespace InGameMCP.Core.Dtos.MCP
{
    [Serializable]
    public class MCPResultResponse : MCPBase
    {
        public object result;

        public MCPResultResponse(object id, object result = null)
        {
            this.id = id;
            this.result = result ?? new object();
        }
    }
}
