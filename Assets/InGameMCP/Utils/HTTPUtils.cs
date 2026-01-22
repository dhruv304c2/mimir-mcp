using System;
using System.Net;
using System.Text;
using InGameMCP.Core.Dtos;
using Newtonsoft.Json;

namespace InGameMCP.Utils.HTTPUtils
{
    public static class HTTPUtils
    {
        public static void SafeWriteJson(HttpListenerContext ctx, HttpStatusCode code, object obj)
        {
            try
            {
                WriteJson(ctx, code, obj);
            }
            catch (Exception e)
            {
                var error = new ErrorResponse(e.Message);
                WriteJson(ctx, HttpStatusCode.InternalServerError, error);
            }
        }

        static void WriteJson(HttpListenerContext ctx, HttpStatusCode code, object obj)
        {
            var json = JsonConvert.SerializeObject(obj);
            var bytes = Encoding.UTF8.GetBytes(json);
            ctx.Response.StatusCode = (int)code;
            ctx.Response.ContentType = "application/json";
            ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
            ctx.Response.Close();
        }
    }
}
