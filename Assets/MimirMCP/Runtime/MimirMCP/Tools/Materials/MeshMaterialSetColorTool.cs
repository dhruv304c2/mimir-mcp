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
        toolName: "mesh_material_set_color",
        description: "Sets a color shader property on a MeshRenderer material."
    )]
    public class MeshMaterialSetColorTool : MCPToolBase
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

        [MCPToolParam("color_r", "Red component (0-1).", MCPToolParam.ParamType.Number, true)]
        public float ColorR;

        [MCPToolParam("color_g", "Green component (0-1).", MCPToolParam.ParamType.Number, true)]
        public float ColorG;

        [MCPToolParam("color_b", "Blue component (0-1).", MCPToolParam.ParamType.Number, true)]
        public float ColorB;

        [MCPToolParam(
            "color_a",
            "Alpha component (0-1). Optional, defaults to 1.",
            MCPToolParam.ParamType.Number
        )]
        public float? ColorA;

        private Core.ILogger _logger;

        public MeshMaterialSetColorTool(Core.ILogger logger = null)
        {
            _logger = logger;
        }

        protected override UniTask<ContentBase[]> ExecuteTool(
            IReadOnlyDictionary<string, object> rawParameters
        )
        {
            var renderer = ResolveRenderer();
            var material = renderer.material;
            ValidateColorProperty(material);

            var color = new Color(ColorR, ColorG, ColorB, ColorA ?? 1f);
            material.SetColor(PropertyName, color);
            renderer.material = material;

            _logger?.LogInfo(
                $"Set color {PropertyName} on '{Path}' to ({color.r:F2}, {color.g:F2}, {color.b:F2}, {color.a:F2})."
            );

            return UniTask.FromResult(
                new ContentBase[]
                {
                    new ContentText(
                        $"Set color {PropertyName} on '{Path}' to ({color.r:F2}, {color.g:F2}, {color.b:F2}, {color.a:F2})."
                    ),
                }
            );
        }

        MeshRenderer ResolveRenderer()
        {
            if (string.IsNullOrWhiteSpace(Path) || string.IsNullOrWhiteSpace(PropertyName))
            {
                throw new MCPToolExecutionException(-32602, "path and property_name are required.");
            }

            var renderer = MeshMaterialInspectTool.ResolveRenderer(Path);
            if (renderer == null)
            {
                throw new MCPToolExecutionException(
                    -32602,
                    $"MeshRenderer not found for path '{Path}'."
                );
            }
            return renderer;
        }

        void ValidateColorProperty(Material material)
        {
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
            if (propIndex < 0 || shader.GetPropertyType(propIndex) != ShaderPropertyType.Color)
            {
                throw new MCPToolExecutionException(
                    -32602,
                    $"Property '{PropertyName}' is not a color property."
                );
            }
        }
    }
}
