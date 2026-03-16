using System;
using System.Net;
using System.Text;
using MimirMCP.Core.Dtos;
using Newtonsoft.Json;

namespace MimirMCP.Utils.HTTPUtils
{
    public static class HTTPUtils
    {
        public static void SafeWriteJson(HttpListenerContext ctx, HttpStatusCode code, object obj)
        {
            try
            {
                WriteJson(ctx, code, obj);
            }
            catch
            {
                try { ctx.Response.Close(); } catch { }
            }
        }

        public static void SafeWriteSse(HttpListenerContext ctx, object obj)
        {
            try
            {
                WriteSse(ctx, obj);
            }
            catch
            {
                try { ctx.Response.Close(); } catch { }
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

        static void WriteSse(HttpListenerContext ctx, object obj)
        {
            var json = JsonConvert.SerializeObject(obj);
            var sseMessage = $"data: {json}\n\n";
            var bytes = Encoding.UTF8.GetBytes(sseMessage);
            ctx.Response.StatusCode = (int)HttpStatusCode.OK;
            ctx.Response.ContentType = "text/event-stream";
            ctx.Response.Headers["Cache-Control"] = "no-cache";
            ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
            ctx.Response.Close();
        }
    }
}
