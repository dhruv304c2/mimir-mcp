using System;

namespace InGameMCP.Core.Dtos
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
