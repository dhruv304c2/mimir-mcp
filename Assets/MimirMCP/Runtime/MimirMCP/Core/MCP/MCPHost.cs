using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Cysharp.Threading.Tasks;
using MimirMCP.Core.Dtos;
using MimirMCP.Core.HTTP;
using MimirMCP.Core.HTTP.Handlers;
using MimirMCP.Utils.HTTPUtils;
using UnityEngine.LowLevel;
using UnityEngine;

namespace MimirMCP.Core.MCP
{
    public class MCPHost
    {
        static bool _playerLoopInitialized;
        internal static SynchronizationContext UnityContext { get; private set; }

        public string HostAddress { get; set; }
        public int Port { get; set; }
        public List<HTTPHandler> Handlers { get; private set; } = new List<HTTPHandler>();

        HttpListener _httpListener;
        Thread _listenerThread;
        ILogger _logger;

        public MCPHost(int port, string hostAddress = "localhost")
        {
            EnsurePlayerLoopInitialized();
            HostAddress = hostAddress;
            Port = port;
        }

        static void EnsurePlayerLoopInitialized()
        {
            if (_playerLoopInitialized)
            {
                return;
            }

            UnityContext = SynchronizationContext.Current ?? new SynchronizationContext();
            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
            PlayerLoopHelper.Initialize(ref playerLoop);
            PlayerLoop.SetPlayerLoop(playerLoop);
            _playerLoopInitialized = true;
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
                    ThreadPool.QueueUserWorkItem(_ => SafeHandleContextAsync(ctx).Forget());
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

        async UniTask SafeHandleContextAsync(HttpListenerContext ctx)
        {
            try
            {
                await HandleContextAsync(ctx);
            }
            catch (Exception e)
            {
                var error = new ErrorResponse(e.Message);
                _logger?.LogError(e.Message);
                HTTPUtils.SafeWriteJson(ctx, HttpStatusCode.InternalServerError, error);
            }
        }

        async UniTask HandleContextAsync(HttpListenerContext ctx)
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
                        await handler.HandleAsync(ctx);
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
