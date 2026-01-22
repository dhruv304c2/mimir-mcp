using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using InGameMCP.Core.Dtos;
using InGameMCP.Core.HTTP;
using InGameMCP.Core.HTTP.Handlers;
using InGameMCP.Utils.HTTPUtils;

namespace InGameMCP.Core.MCP
{
    public class MCPHost
    {
        public string HostAddress { get; set; }
        public int Port { get; set; }
        public List<HTTPHandler> Handlers { get; private set; } = new List<HTTPHandler>();

        HttpListener _httpListener;
        Thread _listenerThread;
        ILogger _logger;

        public MCPHost(int port, string hostAddress = "localhost")
        {
            HostAddress = hostAddress;
            Port = port;
        }

        public void SetLogger(ILogger logger)
        {
            _logger = logger;
        }

        void RegisterHandler(HTTPHandler handler)
        {
            if (Handlers.Contains(handler))
            {
                return;
            }

            Handlers.Add(handler);
            _logger?.LogInfo($"Registered handler for {handler.Method} {handler.Path}");
        }

        public void UseMCPHandler(MCPHandler handler)
        {
            RegisterHandler(new MCPDiscoveryHandler());
            RegisterHandler(handler);
        }

        public void RegisterDefaultHandlers()
        {
            RegisterHandler(new HealthCheckHandler());
        }

        public void StartHTTPServer()
        {
            _logger?.LogInfo($"Hosting server at {HostAddress}:{Port}");
            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add($"http://{HostAddress}:{Port}/");
            _httpListener.Start();

            _listenerThread = new Thread(ListenLoop)
            {
                IsBackground = true,
                Name = "MCPHost HTTP Listener",
            };

            _listenerThread.Start();
        }

        public void StopHTTPServer()
        {
            if (_httpListener != null && _httpListener.IsListening)
            {
                _httpListener.Stop();
                _httpListener.Close();
            }
            _logger?.LogInfo("HTTP Server stopped.");
        }

        void ListenLoop()
        {
            while (_httpListener.IsListening)
            {
                var ctx = _httpListener.GetContext();
                try
                {
                    ThreadPool.QueueUserWorkItem(_ => SafeHandleContext(ctx));
                }
                catch (HttpListenerException)
                {
                    break;
                }
                catch (Exception e)
                {
                    HTTPUtils.SafeWriteJson(
                        ctx,
                        HttpStatusCode.InternalServerError,
                        new ErrorResponse(e.Message)
                    );
                }
            }
        }

        void SafeHandleContext(HttpListenerContext ctx)
        {
            try
            {
                HandleContext(ctx);
            }
            catch (Exception e)
            {
                var error = new ErrorResponse(e.Message);
                _logger?.LogError(e.Message);
                HTTPUtils.SafeWriteJson(ctx, HttpStatusCode.InternalServerError, error);
            }
        }

        void HandleContext(HttpListenerContext ctx)
        {
            var path = ctx.Request.Url.AbsolutePath;
            var method = ctx.Request.HttpMethod;

            foreach (var handler in Handlers)
            {
                if (method == handler.Method && path == handler.Path)
                {
                    var (isValid, errorMessage) = handler.VerifyRequest(ctx.Request);
                    if (isValid)
                    {
                        handler.Handle(ctx);
                        return;
                    }
                    else
                    {
                        HTTPUtils.SafeWriteJson(
                            ctx,
                            HttpStatusCode.BadRequest,
                            new ErrorResponse($"Bad Request: {errorMessage}")
                        );
                        return;
                    }
                }
            }

            HTTPUtils.SafeWriteJson(
                ctx,
                HttpStatusCode.NotFound,
                new ErrorResponse("No handler found for the requested path and method.")
            );
        }
    }
}
