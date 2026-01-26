using System.Net;
using MimirMCP.Core.Dtos;
using MimirMCP.Utils.HTTPUtils;

namespace MimirMCP.Core.HTTP.Handlers
{
    public class HealthCheckHandler : HTTPHandler
    {
        public HealthCheckHandler()
            : base("/", "GET") { }

        public override void Handle(HttpListenerContext ctx)
        {
            var response = new OkResponse("Service is healthy");
            HTTPUtils.SafeWriteJson(ctx, HttpStatusCode.OK, response);
        }
    }
}
