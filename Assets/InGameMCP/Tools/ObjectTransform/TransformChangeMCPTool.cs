using System;
using System.Collections.Generic;
using System.Net;
using Cysharp.Threading.Tasks;
using InGameMCP.Core.Dtos.MCP;
using InGameMCP.Core.MCP.MCPTool;
using InGameMCP.Core.MCP.MCPTool.Attributes;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace InGameMCP.Tools.ObjectTransform
{
    [MCPTool(
        toolName: "transform_change",
        description:
            "Adjusts a transform's position, rotation, or scale using explicit parameters. Specify at least one axis per call and include an optional transition_time for smoothing."
    )]
    public class TransformChangeMCPTool : MCPToolBase
    {
        [MCPToolParam(
            paramName: "path",
            description: "Hierarchy path to the transform (e.g., Root/Child/Cube).",
            paramType: MCPToolParam.ParamType.String,
            isRequired: true
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
            "Optional seconds to ease toward the new position (0 for immediate).",
            MCPToolParam.ParamType.Number
        )]
        public float? PositionTransitionTime;

        [MCPToolParam(
            "rotation_x",
            "Optional X rotation in degrees.",
            MCPToolParam.ParamType.Number
        )]
        public float? RotationX;

        [MCPToolParam(
            "rotation_y",
            "Optional Y rotation in degrees.",
            MCPToolParam.ParamType.Number
        )]
        public float? RotationY;

        [MCPToolParam(
            "rotation_z",
            "Optional Z rotation in degrees.",
            MCPToolParam.ParamType.Number
        )]
        public float? RotationZ;

        [MCPToolParam(
            "rotation_transition_time",
            "Optional seconds to ease toward the new rotation (0 for immediate).",
            MCPToolParam.ParamType.Number
        )]
        public float? RotationTransitionTime;

        [MCPToolParam("scale_x", "Optional local scale X value.", MCPToolParam.ParamType.Number)]
        public float? ScaleX;

        [MCPToolParam("scale_y", "Optional local scale Y value.", MCPToolParam.ParamType.Number)]
        public float? ScaleY;

        [MCPToolParam("scale_z", "Optional local scale Z value.", MCPToolParam.ParamType.Number)]
        public float? ScaleZ;

        [MCPToolParam(
            "scale_transition_time",
            "Optional seconds to ease toward the new scale (0 for immediate).",
            MCPToolParam.ParamType.Number
        )]
        public float? ScaleTransitionTime;

        protected override UniTask<ContentBase[]> ExecuteTool(
            object id,
            HttpListenerContext ctx,
            IReadOnlyDictionary<string, object> rawParameters
        )
        {
            var transform = ResolveTransformOrThrow(Path);
            var appliedChanges = new List<string>();

            if (ApplyPosition(transform))
            {
                appliedChanges.Add("position");
            }

            if (ApplyRotation(transform))
            {
                appliedChanges.Add("rotation");
            }

            if (ApplyScale(transform))
            {
                appliedChanges.Add("scale");
            }

            if (appliedChanges.Count == 0)
            {
                throw new MCPToolExecutionException(
                    -32602,
                    "No transform properties were specified to update."
                );
            }

            var message = $"Updated '{Path}' ({string.Join(", ", appliedChanges)}).";
            return UniTask.FromResult(new ContentBase[] { new ContentText(message) });
        }

        bool ApplyPosition(Transform target)
        {
            if (!PositionX.HasValue && !PositionY.HasValue && !PositionZ.HasValue)
            {
                return false;
            }

            var position = target.position;
            var targetPosition = new Vector3(
                PositionX.HasValue ? PositionX.Value : position.x,
                PositionY.HasValue ? PositionY.Value : position.y,
                PositionZ.HasValue ? PositionZ.Value : position.z
            );
            if (PositionTransitionTime.HasValue && PositionTransitionTime.Value > 0f)
            {
                TransformTweenRunner.GetOrCreate(target).StartPositionTween(
                    targetPosition,
                    PositionTransitionTime.Value
                );
            }
            else
            {
                target.position = targetPosition;
            }
            return true;
        }

        bool ApplyRotation(Transform target)
        {
            if (!RotationX.HasValue && !RotationY.HasValue && !RotationZ.HasValue)
            {
                return false;
            }

            var rotation = target.eulerAngles;
            var targetRotation = new Vector3(
                RotationX.HasValue ? RotationX.Value : rotation.x,
                RotationY.HasValue ? RotationY.Value : rotation.y,
                RotationZ.HasValue ? RotationZ.Value : rotation.z
            );
            if (RotationTransitionTime.HasValue && RotationTransitionTime.Value > 0f)
            {
                TransformTweenRunner.GetOrCreate(target).StartRotationTween(
                    targetRotation,
                    RotationTransitionTime.Value
                );
            }
            else
            {
                target.eulerAngles = targetRotation;
            }
            return true;
        }

        bool ApplyScale(Transform target)
        {
            if (!ScaleX.HasValue && !ScaleY.HasValue && !ScaleZ.HasValue)
            {
                return false;
            }

            var scale = target.localScale;
            var targetScale = new Vector3(
                ScaleX.HasValue ? ScaleX.Value : scale.x,
                ScaleY.HasValue ? ScaleY.Value : scale.y,
                ScaleZ.HasValue ? ScaleZ.Value : scale.z
            );
            if (ScaleTransitionTime.HasValue && ScaleTransitionTime.Value > 0f)
            {
                TransformTweenRunner.GetOrCreate(target).StartScaleTween(
                    targetScale,
                    ScaleTransitionTime.Value
                );
            }
            else
            {
                target.localScale = targetScale;
            }
            return true;
        }

        internal static Transform ResolveTransformOrThrow(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new MCPToolExecutionException(-32602, "path parameter is required.");
            }

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                throw new MCPToolExecutionException(-32001, "Active scene is invalid.");
            }

            var roots = scene.GetRootGameObjects();
            if (!TryFindTransform(roots, path, out var transform))
            {
                throw new MCPToolExecutionException(-32602, $"Transform '{path}' was not found.");
            }

            return transform;
        }

        internal static bool TryFindTransform(
            GameObject[] roots,
            string path,
            out Transform transform
        )
        {
            var segments = path.Split('/');
            transform = null;
            if (segments.Length == 0)
            {
                return false;
            }

            var root = System.Array.Find(roots, go => go.name == segments[0]);
            if (root == null)
            {
                return false;
            }

            transform = root.transform;
            for (var i = 1; i < segments.Length && transform != null; i++)
            {
                var segment = segments[i];
                if (string.IsNullOrWhiteSpace(segment))
                {
                    continue;
                }
                transform = transform.Find(segment);
            }

            return transform != null;
        }
    }
}
