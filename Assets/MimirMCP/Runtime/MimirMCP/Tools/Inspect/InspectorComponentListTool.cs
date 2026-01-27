using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using MimirMCP.Core.Dtos.MCP;
using MimirMCP.Core.MCP.MCPTool;
using MimirMCP.Core.MCP.MCPTool.Attributes;
using MimirMCP.Tools.ObjectTransform;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MimirMCP.Tools.Inspect
{
    [MCPTool(
        toolName: "inspector_component_list",
        description: "Lists all component types attached to a GameObject."
    )]
    public class InspectorComponentListTool : MCPToolBase
    {
        [MCPToolParam(
            "path",
            "Hierarchy path to the GameObject.",
            MCPToolParam.ParamType.String,
            true
        )]
        public string Path;

        protected override UniTask<ContentBase[]> ExecuteTool(
            IReadOnlyDictionary<string, object> rawParameters
        )
        {
            if (string.IsNullOrWhiteSpace(Path))
            {
                throw new MCPToolExecutionException(-32602, "path parameter is required.");
            }

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                throw new MCPToolExecutionException(-32001, "Active scene is invalid.");
            }

            if (
                !TransformChangeMCPTool.TryFindTransform(
                    scene.GetRootGameObjects(),
                    Path,
                    out var transform
                )
            )
            {
                throw new MCPToolExecutionException(-32602, $"Transform '{Path}' was not found.");
            }

            var components = transform.GetComponents<Component>();
            var sb = new StringBuilder();
            sb.AppendLine($"Components on '{Path}':");
            foreach (var component in components)
            {
                if (component == null)
                {
                    continue;
                }
                sb.AppendLine(component.GetType().FullName);
            }

            return UniTask.FromResult(new ContentBase[] { new ContentText(sb.ToString()) });
        }
    }
}
