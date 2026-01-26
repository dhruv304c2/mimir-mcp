using System;
using System.Collections.Generic;
using System.Net;
using Cysharp.Threading.Tasks;
using InGameMCP.Core.Dtos.MCP;
using InGameMCP.Core.MCP.MCPTool;
using InGameMCP.Core.MCP.MCPTool.Attributes;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace InGameMCP.Tools
{
    [MCPTool(
        toolName: "transform_change",
        description: "Adjusts a transform's position, rotation, or scale using explicit parameters."
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

        [MCPToolParam("scale_x", "Optional local scale X value.", MCPToolParam.ParamType.Number)]
        public float? ScaleX;

        [MCPToolParam("scale_y", "Optional local scale Y value.", MCPToolParam.ParamType.Number)]
        public float? ScaleY;

        [MCPToolParam("scale_z", "Optional local scale Z value.", MCPToolParam.ParamType.Number)]
        public float? ScaleZ;

        protected override UniTask<ContentBase[]> ExecuteTool(
            object id,
            HttpListenerContext ctx,
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

            var roots = scene.GetRootGameObjects();
            if (!TryFindTransform(roots, Path, out var transform))
            {
                throw new MCPToolExecutionException(-32602, $"Transform '{Path}' was not found.");
            }

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
            if (PositionX.HasValue)
            {
                position.x = PositionX.Value;
            }
            if (PositionY.HasValue)
            {
                position.y = PositionY.Value;
            }
            if (PositionZ.HasValue)
            {
                position.z = PositionZ.Value;
            }
            target.position = position;
            return true;
        }

        bool ApplyRotation(Transform target)
        {
            if (!RotationX.HasValue && !RotationY.HasValue && !RotationZ.HasValue)
            {
                return false;
            }

            var rotation = target.eulerAngles;
            if (RotationX.HasValue)
            {
                rotation.x = RotationX.Value;
            }
            if (RotationY.HasValue)
            {
                rotation.y = RotationY.Value;
            }
            if (RotationZ.HasValue)
            {
                rotation.z = RotationZ.Value;
            }
            target.eulerAngles = rotation;
            return true;
        }

        bool ApplyScale(Transform target)
        {
            if (!ScaleX.HasValue && !ScaleY.HasValue && !ScaleZ.HasValue)
            {
                return false;
            }

            var scale = target.localScale;
            if (ScaleX.HasValue)
            {
                scale.x = ScaleX.Value;
            }
            if (ScaleY.HasValue)
            {
                scale.y = ScaleY.Value;
            }
            if (ScaleZ.HasValue)
            {
                scale.z = ScaleZ.Value;
            }
            target.localScale = scale;
            return true;
        }

        static bool TryFindTransform(GameObject[] roots, string path, out Transform transform)
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
