using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using Cysharp.Threading.Tasks;
using MimirMCP.Core.Dtos;
using MimirMCP.Utils.HTTPUtils;
using Newtonsoft.Json;

namespace MimirMCP.Core.HTTP
{
    public class HTTPHandler
    {
        public string Method { get; private set; }
        public string Path { get; private set; }
        public List<string> RequiredHeaders { get; protected set; } = new List<string>();
        public List<string> RequiredQueryParams { get; protected set; } = new List<string>();
        public List<Func<HttpListenerRequest, (bool, string)>> Verifications
        {
            get;
            protected set;
        } = new List<Func<HttpListenerRequest, (bool, string)>>();

        protected HTTPHandler(string path, string method)
        {
            Path = path;
            Method = method;
        }

        public virtual (bool, string) VerifyRequest(HttpListenerRequest request)
        {
            if (request.HttpMethod != Method)
            {
                return (false, "Invalid HTTP method");
            }

            if (request.Url.AbsolutePath != Path)
            {
                return (false, "Invalid URL path");
            }

            foreach (var header in RequiredHeaders)
            {
                if (request.Headers[header] == null)
                {
                    return (false, $"Missing required header: {header}");
                }
            }

            var queryParams = System.Web.HttpUtility.ParseQueryString(request.Url.Query);
            foreach (var param in RequiredQueryParams)
            {
                if (string.IsNullOrEmpty(queryParams.Get(param)))
                {
                    return (false, $"Missing required query parameter: {param}");
                }
            }

            foreach (var verify in Verifications)
            {
                var (isValid, message) = verify(request);
                if (!isValid)
                {
                    return (false, message);
                }
            }

            return (true, "");
        }

        public virtual UniTask HandleAsync(HttpListenerContext context)
        {
            Handle(context);
            return UniTask.CompletedTask;
        }

        public virtual void Handle(HttpListenerContext context)
        {
            HTTPUtils.SafeWriteJson(
                context,
                System.Net.HttpStatusCode.NotImplemented,
                new ErrorResponse("Handler not implemented.")
            );
        }
    }

    public class HTTPHandler<TRequest> : HTTPHandler
    {
        protected HTTPHandler(string path, string method)
            : base(path, method) { }

        public override async UniTask HandleAsync(HttpListenerContext ctx)
        {
            var requestBody = new StreamReader(ctx.Request.InputStream).ReadToEnd();

            TRequest reqObj;
            try
            {
                reqObj = JsonConvert.DeserializeObject<TRequest>(requestBody);
            }
            catch
            {
                HTTPUtils.SafeWriteJson(
                    ctx,
                    HttpStatusCode.BadRequest,
                    new ErrorResponse("Invalid request body")
                );
                return;
            }

            await HandleAsync(ctx, reqObj);
        }

        public override void Handle(HttpListenerContext ctx)
        {
            var requestBody = new System.IO.StreamReader(ctx.Request.InputStream).ReadToEnd();

            TRequest reqObj;
            try
            {
                reqObj = JsonConvert.DeserializeObject<TRequest>(requestBody);
            }
            catch
            {
                HTTPUtils.SafeWriteJson(
                    ctx,
                    HttpStatusCode.BadRequest,
                    new ErrorResponse("Invalid request body")
                );
                return;
            }

            Handle(ctx, reqObj);
        }

        public virtual UniTask HandleAsync(HttpListenerContext ctx, TRequest request)
        {
            Handle(ctx, request);
            return UniTask.CompletedTask;
        }

        public virtual void Handle(HttpListenerContext ctx, TRequest request)
        {
            HTTPUtils.SafeWriteJson(
                ctx,
                HttpStatusCode.NotImplemented,
                new ErrorResponse("Handler not implemented.")
            );
        }
    }
}
