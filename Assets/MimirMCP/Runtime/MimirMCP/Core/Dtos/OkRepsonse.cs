using System;

namespace MimirMCP.Core.Dtos
{
    [Serializable]
    public class OkResponse
    {
        public string message;

        public OkResponse(string msg)
        {
            message = msg;
        }
    }
}
