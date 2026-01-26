using InGameMCP.Core.MCP;
using InGameMCP.Tools;
using InGameMCP.Tools.Inspect;
using InGameMCP.Tools.Materials;
using InGameMCP.Tools.ObjectTransform;
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
        _mcpHandler.RegisterTool(new TransformInspectMCPTool());
        _mcpHandler.RegisterTool(new TransformPositionUpdateMCPTool());
        _mcpHandler.RegisterTool(new TransformRotationUpdateMCPTool());
        _mcpHandler.RegisterTool(new TransformScaleUpdateMCPTool());
        _mcpHandler.RegisterTool(new MeshMaterialInspectTool());
        _mcpHandler.RegisterTool(new InspectorComponentListTool());
        _mcpHandler.RegisterTool(new InspectorPropertyInspectTool());
        _mcpHandler.RegisterTool(new InspectorPropertySetStringTool());
        _mcpHandler.RegisterTool(new InspectorPropertySetBooleanTool());
        _mcpHandler.RegisterTool(new InspectorPropertySetFloatTool());
        _mcpHandler.RegisterTool(new InspectorPropertySetVector2Tool());
        _mcpHandler.RegisterTool(new InspectorPropertySetVector3Tool());
        _mcpHandler.RegisterTool(new InspectorPropertySetColorTool());
        _mcpHandler.RegisterTool(new MeshMaterialSetColorTool());
        _mcpHandler.RegisterTool(new MeshMaterialSetFloatTool());
        _mcpHandler.RegisterTool(new MeshMaterialSetBooleanTool());
        _mcpHost.UseMCPHandler(_mcpHandler);

        _mcpHost.StartHTTPServer();
        Debug.Log($"TestLogMCPHost listening at localhost:{port}");
    }

    void OnDestroy()
    {
        _mcpHost?.StopHTTPServer();
    }
}
