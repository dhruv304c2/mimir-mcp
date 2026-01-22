using System.Net;
using InGameMCP.Core.Dtos;
using InGameMCP.Utils.HTTPUtils;

namespace InGameMCP.Core.HTTP.Handlers
{
    public class HealthCheckHandler : HTTPHandler
    {
        public HealthCheckHandler()
            : base("/health", "GET") { }

        public override void Handle(HttpListenerContext ctx)
        {
            var response = new OkResponse("Service is healthy");
            HTTPUtils.SafeWriteJson(ctx, HttpStatusCode.OK, response);
        }
    }
}
