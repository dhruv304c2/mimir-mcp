using System.Collections.Generic;
using System.Net;
using InGameMCP.Core.Dtos.MCP;
using InGameMCP.Core.MCP.MCPTool;
using InGameMCP.Utils.HTTPUtils;
using UnityEngine;

public class TestLogMCPTool : MCPToolBase
{
    enum LogLevel
    {
        Info,
        Warning,
        Error,
    }

    readonly InGameMCP.Core.ILogger _logger;

    public TestLogMCPTool(InGameMCP.Core.ILogger logger = null)
        : base(
            "test_log",
            "Writes a test message to the Unity console to validate the MCP host pipeline."
        )
    {
        _logger = logger;

        ToolParams.Add(
            new MCPToolParam(
                "message",
                "Message text that should be echoed into the Unity console.",
                MCPToolParam.ParamType.String,
                true
            )
        );

        ToolParams.Add(
            new MCPToolParam(
                "level",
                "Optional log level (info, warning, error). Defaults to info.",
                MCPToolParam.ParamType.String
            )
        );
    }

    public override void HandleToolCall(
        string id,
        HttpListenerContext ctx,
        Dictionary<string, object> parameters
    )
    {
        if (!TryGetMessage(parameters, out var message))
        {
            var error = new MCPErrorResponse(
                id,
                new MCPError(-32602, "message parameter is required and must be a string.")
            );
            HTTPUtils.SafeWriteJson(ctx, HttpStatusCode.BadRequest, error);
            return;
        }

        var level = ResolveLevel(parameters);
        WriteToUnityLog(level, message);

        var result = new MCPContentResult
        {
            id = id,
            contents = new ContentBase[] { new ContentText($"Logged message as {level} level.") },
        };

        HTTPUtils.SafeWriteJson(ctx, HttpStatusCode.OK, result);
    }

    static bool TryGetMessage(Dictionary<string, object> parameters, out string message)
    {
        if (!parameters.TryGetValue("message", out var messageObj))
        {
            message = null;
            return false;
        }

        message = messageObj switch
        {
            string s => s,
            _ => messageObj?.ToString(),
        };

        return !string.IsNullOrWhiteSpace(message);
    }

    static LogLevel ResolveLevel(Dictionary<string, object> parameters)
    {
        if (parameters.TryGetValue("level", out var levelObj) && levelObj is string levelString)
        {
            switch (levelString.Trim().ToLowerInvariant())
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
