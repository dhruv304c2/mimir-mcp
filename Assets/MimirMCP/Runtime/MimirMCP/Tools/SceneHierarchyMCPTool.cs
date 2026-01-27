using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MimirMCP.Core.Dtos.MCP;
using MimirMCP.Core.MCP.MCPTool;
using MimirMCP.Core.MCP.MCPTool.Attributes;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MimirMCP.Tools
{
    [MCPTool(
        toolName: "scene_hierarchy",
        description: "Returns the active scene hierarchy as JSON grouped by transform name."
    )]
    public class SceneHierarchyMCPTool : MCPToolBase
    {
        protected override UniTask<ContentBase[]> ExecuteTool(
            IReadOnlyDictionary<string, object> rawParameters
        )
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                throw new MCPToolExecutionException(-32001, "Active scene is invalid.");
            }

            var roots = scene.GetRootGameObjects();
            var payload = new SceneHierarchyPayload
            {
                scene = scene.name,
                rootCount = roots.Length,
                hierarchy = new List<HierarchyNode>(),
            };

            foreach (var root in roots)
            {
                payload.hierarchy.Add(BuildNodeRecursive(root.transform, root.transform.name));
            }

            var json = JsonConvert.SerializeObject(payload, Formatting.Indented);
            return UniTask.FromResult(new ContentBase[] { new ContentText(json) });
        }

        static HierarchyNode BuildNodeRecursive(Transform transform, string path)
        {
            var node = new HierarchyNode
            {
                id = transform.name,
                path = path,
                children = new List<HierarchyNode>(),
            };

            for (var i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                var childPath = string.Concat(path, "/", child.name);
                node.children.Add(BuildNodeRecursive(child, childPath));
            }

            return node;
        }

        class SceneHierarchyPayload
        {
            public string scene { get; set; }
            public int rootCount { get; set; }
            public List<HierarchyNode> hierarchy { get; set; }
        }

        class HierarchyNode
        {
            public string id { get; set; }
            public string path { get; set; }
            public List<HierarchyNode> children { get; set; }
        }
    }
}
