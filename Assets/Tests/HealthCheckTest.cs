using MimirMCP.Core;
using MimirMCP.Core.MCP;
using UnityEngine;

public class HealthCheckTest : MonoBehaviour
{
    [SerializeField]
    int port = 3000;

    MCPHost _mcpHost;

    void Start()
    {
        Debug.Log("Starting MCP Host for HealthCheckTest...");
        _mcpHost = new MCPHost(port);
        _mcpHost.SetLogger(new UnityLogger());
        _mcpHost.RegisterDefaultHandlers();
        _mcpHost.StartHTTPServer();
    }
}
