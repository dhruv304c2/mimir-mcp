using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Cysharp.Threading.Tasks;
using MimirMCP.Core.Database;
using MimirMCP.Core.Dtos;
using MimirMCP.Core.HTTP;
using MimirMCP.Core.HTTP.Handlers;
using MimirMCP.Tools.Database;
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
        CancellationTokenSource _listenerCts;
        UniTask _listenerTask;
        ILogger _logger;
        MCPHandler _mcpHandler;
        readonly ConcurrentDictionary<Type, object> _databases = new ConcurrentDictionary<Type, object>();

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

        /// <summary>
        /// Registers a database with the host and automatically creates an MCP tool for reading it.
        /// </summary>
        /// <typeparam name="T">The type of items stored in the database.</typeparam>
        /// <param name="databaseName">The name of the database (used in tool naming).</param>
        /// <param name="database">The database instance to register.</param>
        /// <exception cref="ArgumentNullException">Thrown when database or databaseName is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when a database of the same type is already registered or MCPHandler is not set.</exception>
        public void RegisterDatabase<T>(string databaseName, IMCPDatabase<T> database)
            where T : IMCPDatabaseItem
        {
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            if (string.IsNullOrEmpty(databaseName))
                throw new ArgumentNullException(nameof(databaseName));

            if (_mcpHandler == null)
                throw new InvalidOperationException("MCPHandler must be set via UseMCPHandler before registering databases.");

            var type = typeof(T);

            if (!_databases.TryAdd(type, database))
            {
                throw new InvalidOperationException($"A database for type {type.Name} is already registered.");
            }

            // Create and register the read tool
            var readTool = new ReadDatabaseMCPTool<T>(database, databaseName);
            _mcpHandler.RegisterTool(readTool);

            _logger?.LogInfo($"Registered database '{databaseName}' for type {type.Name}");
        }

        /// <summary>
        /// Gets a database by its item type.
        /// </summary>
        /// <typeparam name="T">The type of items stored in the database.</typeparam>
        /// <returns>The database instance.</returns>
        /// <exception cref="InvalidOperationException">Thrown when no database is found for the specified type.</exception>
        public IMCPDatabase<T> GetDatabase<T>() where T : IMCPDatabaseItem
        {
            if (TryGetDatabase<T>(out var database))
            {
                return database;
            }

            throw new InvalidOperationException($"No database found for type {typeof(T).Name}. Register it first using RegisterDatabase.");
        }

        /// <summary>
        /// Tries to get a database by its item type.
        /// </summary>
        /// <typeparam name="T">The type of items stored in the database.</typeparam>
        /// <param name="database">The database instance if found.</param>
        /// <returns>True if the database was found, false otherwise.</returns>
        public bool TryGetDatabase<T>(out IMCPDatabase<T> database) where T : IMCPDatabaseItem
        {
            var type = typeof(T);

            if (_databases.TryGetValue(type, out var dbObject))
            {
                database = (IMCPDatabase<T>)dbObject;
                return true;
            }

            database = null;
            return false;
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
            _mcpHandler = handler;
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
            _listenerCts = new CancellationTokenSource();
            _listenerTask = ListenLoopAsync(_listenerCts.Token);
            _listenerTask.Forget();
        }

        public void StopHTTPServer()
        {
            if (_listenerCts != null)
            {
                _listenerCts.Cancel();
                _listenerCts.Dispose();
                _listenerCts = null;
            }

            if (_httpListener != null && _httpListener.IsListening)
            {
                _httpListener.Stop();
                _httpListener.Close();
            }
            _logger?.LogInfo("HTTP Server stopped.");
        }

        async UniTask ListenLoopAsync(CancellationToken cancellationToken)
        {
            while (_httpListener != null && _httpListener.IsListening && !cancellationToken.IsCancellationRequested)
            {
                HttpListenerContext ctx = null;
                try
                {
                    ctx = await _httpListener.GetContextAsync().AsUniTask();
                    cancellationToken.ThrowIfCancellationRequested();

                    await UniTask.SwitchToMainThread();
                    await SafeHandleContextAsync(ctx);
                }
                catch (HttpListenerException)
                {
                    break;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception e)
                {
                    _logger?.LogError($"HTTP listener error: {e.Message}");
                    if (ctx != null)
                    {
                        HTTPUtils.SafeWriteJson(
                            ctx,
                            HttpStatusCode.InternalServerError,
                            new ErrorResponse(e.Message)
                        );
                    }
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
