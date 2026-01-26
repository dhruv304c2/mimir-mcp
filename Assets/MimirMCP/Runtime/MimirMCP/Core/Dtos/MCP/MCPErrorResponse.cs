using System;

namespace MimirMCP.Core.Dtos.MCP
{
    public class MCPError
    {
        public int code;
        public string message;

        public MCPError(int code, string message)
        {
            this.code = code;
            this.message = message;
        }
    }

    public class MCPError<TData> : MCPError
    {
        public TData data;

        public MCPError(int code, string message, TData data)
            : base(code, message)
        {
            this.data = data;
        }
    }

    [Serializable]
    public class MCPErrorResponse : MCPBase
    {
        public MCPError error;

        public MCPErrorResponse(object id, MCPError error)
        {
            this.id = id;
            this.error = error;
        }
    }
}
