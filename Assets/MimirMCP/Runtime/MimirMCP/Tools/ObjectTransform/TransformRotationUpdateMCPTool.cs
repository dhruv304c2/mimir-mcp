using System.Collections.Generic;
using System.Net;
using Cysharp.Threading.Tasks;
using MimirMCP.Core.Dtos.MCP;
using MimirMCP.Core.MCP.MCPTool;
using MimirMCP.Core.MCP.MCPTool.Attributes;
using MimirMCP.Utils;
using UnityEngine;

namespace MimirMCP.Tools.ObjectTransform
{
    [MCPTool(
        toolName: "transform_rotation_update",
        description:
            "Adjusts a transform's Euler rotation in degrees. Provide any subset of rotation_x/rotation_y/rotation_z with an optional rotation_transition_time for smoothing."
    )]
    public class TransformRotationUpdateMCPTool : MCPToolBase
    {
        [MCPToolParam(
            "path",
            "Hierarchy path to the transform (e.g., Root/Child/Cube).",
            MCPToolParam.ParamType.String,
            true
        )]
        public string Path;

        [MCPToolParam("rotation_x", "Optional X rotation in degrees.", MCPToolParam.ParamType.Number)]
        public float? RotationX;

        [MCPToolParam("rotation_y", "Optional Y rotation in degrees.", MCPToolParam.ParamType.Number)]
        public float? RotationY;

        [MCPToolParam("rotation_z", "Optional Z rotation in degrees.", MCPToolParam.ParamType.Number)]
        public float? RotationZ;

        [MCPToolParam(
            "rotation_transition_time",
            "Seconds to ease toward the new rotation (0 for immediate).",
            MCPToolParam.ParamType.Number
        )]
        public float? RotationTransitionTime;

        protected override async UniTask<ContentBase[]> ExecuteTool(
            object id,
            HttpListenerContext ctx,
            IReadOnlyDictionary<string, object> rawParameters
        )
        {
            if (!RotationX.HasValue && !RotationY.HasValue && !RotationZ.HasValue)
            {
                throw new MCPToolExecutionException(
                    -32602,
                    "Specify at least one of rotation_x, rotation_y, or rotation_z."
                );
            }

            var transform = TransformChangeMCPTool.ResolveTransformOrThrow(Path);
            var rotation = transform.eulerAngles;
            var targetRotation = new Vector3(
                RotationX.HasValue ? RotationX.Value : rotation.x,
                RotationY.HasValue ? RotationY.Value : rotation.y,
                RotationZ.HasValue ? RotationZ.Value : rotation.z
            );
            var duration = Mathf.Max(RotationTransitionTime ?? 0f, 0f);
            await TransformTweenRunner.RunRotationTween(transform, targetRotation, duration);

            return new ContentBase[] { new ContentText($"Updated '{Path}' rotation.") };
        }
    }
}
