using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MimirMCP.Core.Dtos.MCP;
using MimirMCP.Core.MCP.MCPTool;
using MimirMCP.Core.MCP.MCPTool.Attributes;
using UnityEngine;

namespace MimirMCP.Tools
{
    [MCPTool(toolName: "log", description: "A tool to log messages to the Unity console via MCP.")]
    public class LogMCPTool : MCPToolBase
    {
        enum LogLevel
        {
            Info,
            Warning,
            Error,
        }

        [MCPToolParam(
            paramName: "message",
            description: "The message to log to the Unity console.",
            paramType: MCPToolParam.ParamType.String,
            isRequired: true
        )]
        public string Message;

        [MCPToolParam(
            paramName: "level",
            description: "Optional log level (info, warning, error). Defaults to info.",
            paramType: MCPToolParam.ParamType.String
        )]
        public string Level;

        readonly Core.ILogger _logger;

        public LogMCPTool(Core.ILogger logger = null)
            : base()
        {
            _logger = logger;
        }

        protected override UniTask<ContentBase[]> ExecuteTool(
            IReadOnlyDictionary<string, object> rawParameters
        )
        {
            if (string.IsNullOrWhiteSpace(Message))
            {
                throw new MCPToolExecutionException(
                    -32602,
                    "message parameter is required and must be a string."
                );
            }

            var level = ResolveLevel(Level);
            WriteToUnityLog(level, Message);

            return UniTask.FromResult(
                new ContentBase[] { new ContentText($"Logged message as {level} level.") }
            );
        }

        static LogLevel ResolveLevel(string levelInput)
        {
            if (!string.IsNullOrWhiteSpace(levelInput))
            {
                switch (levelInput.Trim().ToLowerInvariant())
                {
                    case "warning":
                    case "warn":
                        return LogLevel.Warning;
                    case "error":
                        return LogLevel.Error;
                }
            }

            return LogLevel.Info;
        }

        void WriteToUnityLog(LogLevel level, string message)
        {
            var formatted = $"[MCP Test Log] {message}";

            // Avoid double logging by preferring the injected logger when present
            void LogViaUnity()
            {
                switch (level)
                {
                    case LogLevel.Warning:
                        Debug.LogWarning(formatted);
                        break;
                    case LogLevel.Error:
                        Debug.LogError(formatted);
                        break;
                    default:
                        Debug.Log(formatted);
                        break;
                }
            }

            if (_logger != null)
            {
                switch (level)
                {
                    case LogLevel.Warning:
                        _logger.LogWarning(formatted);
                        break;
                    case LogLevel.Error:
                        _logger.LogError(formatted);
                        break;
                    default:
                        _logger.LogInfo(formatted);
                        break;
                }
            }
            else
            {
                LogViaUnity();
            }
        }
    }
}
