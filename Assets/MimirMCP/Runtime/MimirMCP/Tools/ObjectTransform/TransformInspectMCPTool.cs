using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MimirMCP.Core.Dtos.MCP;
using MimirMCP.Core.MCP.MCPTool;
using MimirMCP.Core.MCP.MCPTool.Attributes;
using UnityEngine;

namespace MimirMCP.Tools.ObjectTransform
{
    [MCPTool(
        toolName: "transform_inspect",
        description: "Reports the world position, Euler rotation, and local scale for a transform."
    )]
    public class TransformInspectMCPTool : MCPToolBase
    {
        [MCPToolParam(
            "path",
            "Hierarchy path to the transform (e.g., Root/Child/Cube).",
            MCPToolParam.ParamType.String,
            true
        )]
        public string Path;

        protected override UniTask<ContentBase[]> ExecuteTool(
            IReadOnlyDictionary<string, object> rawParameters
        )
        {
            var transform = TransformChangeMCPTool.ResolveTransformOrThrow(Path);
            var payload = new TransformSnapshot
            {
                path = Path,
                position = transform.position,
                rotation = transform.eulerAngles,
                scale = transform.localScale,
            };
            var json = JsonUtility.ToJson(payload, true);
            return UniTask.FromResult<ContentBase[]>(new ContentBase[] { new ContentText(json) });
        }

        [System.Serializable]
        struct TransformSnapshot
        {
            public string path;
            public Vector3 position;
            public Vector3 rotation;
            public Vector3 scale;
        }
    }
}
