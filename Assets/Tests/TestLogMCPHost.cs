using MimirMCP.Core.MCP;
using MimirMCP.Tools;
using MimirMCP.Tools.Inspect;
using MimirMCP.Tools.Materials;
using MimirMCP.Tools.ObjectTransform;
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
        _mcpHandler.UseDefaultToolSet();
        _mcpHost.UseMCPHandler(_mcpHandler);

        _mcpHost.StartHTTPServer();
        Debug.Log($"TestLogMCPHost listening at localhost:{port}");
    }

    void OnDestroy()
    {
        _mcpHost?.StopHTTPServer();
    }
}
