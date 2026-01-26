using System.Collections.Generic;
using System.Net;
using Cysharp.Threading.Tasks;
using InGameMCP.Core.Dtos.MCP;
using InGameMCP.Core.MCP.MCPTool;
using InGameMCP.Core.MCP.MCPTool.Attributes;
using UnityEngine;

namespace InGameMCP.Tools.ObjectTransform
{
    [MCPTool(
        toolName: "transform_scale_update",
        description:
            "Adjusts a transform's local scale. Provide any subset of scale_x/scale_y/scale_z with an optional scale_transition_time for smoothing."
    )]
    public class TransformScaleUpdateMCPTool : MCPToolBase
    {
        [MCPToolParam(
            "path",
            "Hierarchy path to the transform (e.g., Root/Child/Cube).",
            MCPToolParam.ParamType.String,
            true
        )]
        public string Path;

        [MCPToolParam("scale_x", "Optional local scale X value.", MCPToolParam.ParamType.Number)]
        public float? ScaleX;

        [MCPToolParam("scale_y", "Optional local scale Y value.", MCPToolParam.ParamType.Number)]
        public float? ScaleY;

        [MCPToolParam("scale_z", "Optional local scale Z value.", MCPToolParam.ParamType.Number)]
        public float? ScaleZ;

        [MCPToolParam(
            "scale_transition_time",
            "Seconds to ease toward the new scale (0 for immediate).",
            MCPToolParam.ParamType.Number
        )]
        public float? ScaleTransitionTime;

        protected override UniTask<ContentBase[]> ExecuteTool(
            object id,
            HttpListenerContext ctx,
            IReadOnlyDictionary<string, object> rawParameters
        )
        {
            if (!ScaleX.HasValue && !ScaleY.HasValue && !ScaleZ.HasValue)
            {
                throw new MCPToolExecutionException(
                    -32602,
                    "Specify at least one of scale_x, scale_y, or scale_z."
                );
            }

            var transform = TransformChangeMCPTool.ResolveTransformOrThrow(Path);
            var scale = transform.localScale;
            var targetScale = new Vector3(
                ScaleX.HasValue ? ScaleX.Value : scale.x,
                ScaleY.HasValue ? ScaleY.Value : scale.y,
                ScaleZ.HasValue ? ScaleZ.Value : scale.z
            );
            if (ScaleTransitionTime.HasValue && ScaleTransitionTime.Value > 0f)
            {
                TransformTweenRunner.GetOrCreate(transform).StartScaleTween(
                    targetScale,
                    ScaleTransitionTime.Value
                );
            }
            else
            {
                transform.localScale = targetScale;
            }

            return UniTask.FromResult<ContentBase[]>(
                new ContentBase[] { new ContentText($"Updated '{Path}' scale.") }
            );
        }
    }
}
