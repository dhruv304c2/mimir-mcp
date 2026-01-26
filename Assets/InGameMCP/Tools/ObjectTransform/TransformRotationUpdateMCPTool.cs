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

        protected override UniTask<ContentBase[]> ExecuteTool(
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
            if (RotationTransitionTime.HasValue && RotationTransitionTime.Value > 0f)
            {
                TransformTweenRunner.GetOrCreate(transform).StartRotationTween(
                    targetRotation,
                    RotationTransitionTime.Value
                );
            }
            else
            {
                transform.eulerAngles = targetRotation;
            }

            return UniTask.FromResult<ContentBase[]>(
                new ContentBase[] { new ContentText($"Updated '{Path}' rotation.") }
            );
        }
    }
}
