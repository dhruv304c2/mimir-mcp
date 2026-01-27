using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MimirMCP.Core.Dtos.MCP;
using MimirMCP.Core.MCP.MCPTool;
using MimirMCP.Core.MCP.MCPTool.Attributes;
using UnityEngine;
using UnityEngine.Rendering;
using MimirMCP.Tools.Inspect;

namespace MimirMCP.Tools.Materials
{
    [MCPTool(
        toolName: "mesh_material_set_boolean",
        description: "Sets a boolean shader property (treated as float 0/1) on a MeshRenderer material."
    )]
    public class MeshMaterialSetBooleanTool : MCPToolBase
    {
        [MCPToolParam(
            "path",
            "Hierarchy path to the GameObject.",
            MCPToolParam.ParamType.String,
            true
        )]
        public string Path;

        [MCPToolParam(
            "property_name",
            "Shader property to update.",
            MCPToolParam.ParamType.String,
            true
        )]
        public string PropertyName;

        [MCPToolParam(
            "property_value",
            "Boolean value (true/false).",
            MCPToolParam.ParamType.String,
            true
        )]
        public string PropertyValue;

        protected override UniTask<ContentBase[]> ExecuteTool(
            IReadOnlyDictionary<string, object> rawParameters
        )
        {
            if (!bool.TryParse(PropertyValue?.Trim(), out var boolValue))
            {
                throw new MCPToolExecutionException(
                    -32602,
                    $"Failed to parse boolean from '{PropertyValue}'."
                );
            }

            var renderer = MeshMaterialInspectTool.ResolveRenderer(Path);
            if (renderer == null)
            {
                throw new MCPToolExecutionException(
                    -32602,
                    $"MeshRenderer not found for path '{Path}'."
                );
            }

            var material = renderer.material;
            if (material == null)
            {
                throw new MCPToolExecutionException(-32002, "Renderer has no material.");
            }

            if (!material.HasProperty(PropertyName))
            {
                throw new MCPToolExecutionException(
                    -32602,
                    $"Material is missing property '{PropertyName}'."
                );
            }

            var shader = material.shader;
            if (shader == null)
            {
                throw new MCPToolExecutionException(-32002, "Material shader unavailable.");
            }

            var propIndex = shader.FindPropertyIndex(PropertyName);
            if (propIndex < 0)
            {
                throw new MCPToolExecutionException(
                    -32602,
                    $"Property '{PropertyName}' not found on shader."
                );
            }

            var propType = shader.GetPropertyType(propIndex);
            if (propType != ShaderPropertyType.Float && propType != ShaderPropertyType.Range)
            {
                throw new MCPToolExecutionException(
                    -32602,
                    $"Property '{PropertyName}' must be a float/range property to be treated as boolean."
                );
            }

            material.SetFloat(PropertyName, boolValue ? 1f : 0f);
            renderer.material = material;

            return UniTask.FromResult(
                new ContentBase[]
                {
                    new ContentText($"Set {PropertyName} on '{Path}' to {boolValue}."),
                }
            );
        }
    }
}
