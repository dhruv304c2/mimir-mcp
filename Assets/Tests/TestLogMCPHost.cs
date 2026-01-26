using InGameMCP.Core.MCP;
using InGameMCP.Tools;
using UnityEditor.SearchService;
using UnityEngine;

public class TestLogMCPHost : MonoBehaviour
{
    [SerializeField]
    int port = 3000;

    MCPHost _mcpHost;
    MCPHandler _mcpHandler;

    void Start()
    {
        var logger = new UnityLogger();

        _mcpHost = new MCPHost(port);
        _mcpHost.SetLogger(logger);
        _mcpHost.RegisterDefaultHandlers();

        _mcpHandler = new MCPHandler(logger: logger);
        _mcpHandler.RegisterTool(new LogMCPTool(logger));
        _mcpHandler.RegisterTool(new SceneHierarchyMCPTool());
        _mcpHandler.RegisterTool(new TransformChangeMCPTool());
        _mcpHost.UseMCPHandler(_mcpHandler);

        _mcpHost.StartHTTPServer();
        Debug.Log($"TestLogMCPHost listening at localhost:{port}");
    }

    void OnDestroy()
    {
        _mcpHost?.StopHTTPServer();
    }
}
