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
        toolName: "transform_position_update",
        description:
            "Moves a transform in world space. Provide any subset of position_x/position_y/position_z and optionally position_transition_time for smoothing."
    )]
    public class TransformPositionUpdateMCPTool : MCPToolBase
    {
        [MCPToolParam(
            "path",
            "Hierarchy path to the transform (e.g., Root/Child/Cube).",
            MCPToolParam.ParamType.String,
            true
        )]
        public string Path;

        [MCPToolParam("position_x", "Optional world X position.", MCPToolParam.ParamType.Number)]
        public float? PositionX;

        [MCPToolParam("position_y", "Optional world Y position.", MCPToolParam.ParamType.Number)]
        public float? PositionY;

        [MCPToolParam("position_z", "Optional world Z position.", MCPToolParam.ParamType.Number)]
        public float? PositionZ;

        [MCPToolParam(
            "position_transition_time",
            "Seconds to ease toward the new position (0 for immediate).",
            MCPToolParam.ParamType.Number
        )]
        public float? PositionTransitionTime;

        protected override UniTask<ContentBase[]> ExecuteTool(
            object id,
            HttpListenerContext ctx,
            IReadOnlyDictionary<string, object> rawParameters
        )
        {
            if (!PositionX.HasValue && !PositionY.HasValue && !PositionZ.HasValue)
            {
                throw new MCPToolExecutionException(
                    -32602,
                    "Specify at least one of position_x, position_y, or position_z."
                );
            }

            var transform = TransformChangeMCPTool.ResolveTransformOrThrow(Path);
            var position = transform.position;
            var targetPosition = new Vector3(
                PositionX.HasValue ? PositionX.Value : position.x,
                PositionY.HasValue ? PositionY.Value : position.y,
                PositionZ.HasValue ? PositionZ.Value : position.z
            );
            if (PositionTransitionTime.HasValue && PositionTransitionTime.Value > 0f)
            {
                TransformTweenRunner.GetOrCreate(transform).StartPositionTween(
                    targetPosition,
                    PositionTransitionTime.Value
                );
            }
            else
            {
                transform.position = targetPosition;
            }

            return UniTask.FromResult<ContentBase[]>(
                new ContentBase[] { new ContentText($"Updated '{Path}' position.") }
            );
        }
    }
}
