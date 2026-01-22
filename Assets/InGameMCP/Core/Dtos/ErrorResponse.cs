using System;

namespace InGameMCP.Core.Dtos
{
    [Serializable]
    class ErrorResponse
    {
        public string error;

        public ErrorResponse(string message)
        {
            error = message;
        }
    }
}
