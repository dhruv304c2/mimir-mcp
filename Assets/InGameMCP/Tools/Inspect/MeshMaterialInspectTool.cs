using System.Collections.Generic;
using System.Net;
using Cysharp.Threading.Tasks;
using InGameMCP.Core.Dtos.MCP;
using InGameMCP.Core.MCP.MCPTool;
using InGameMCP.Core.MCP.MCPTool.Attributes;
using InGameMCP.Tools.ObjectTransform;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace InGameMCP.Tools.Inspect
{
    [MCPTool(
        toolName: "mesh_material_inspect",
        description: "Returns material properties for a GameObject with a MeshRenderer."
    )]
    public class MeshMaterialInspectTool : MCPToolBase
    {
        [MCPToolParam(
            paramName: "path",
            description: "Hierarchy path to the GameObject (e.g., Root/Child/Cube).",
            paramType: MCPToolParam.ParamType.String,
            isRequired: true
        )]
        public string Path;

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

            var renderer = ResolveRenderer(Path);
            if (renderer == null)
            {
                throw new MCPToolExecutionException(
                    -32602,
                    $"MeshRenderer not found at path '{Path}'."
                );
            }

            var properties = CollectMaterialProperties(renderer);
            return UniTask.FromResult(new ContentBase[] { new ContentText(properties) });
        }

        internal static MeshRenderer ResolveRenderer(string path)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                return null;
            }

            if (
                !TransformChangeMCPTool.TryFindTransform(
                    scene.GetRootGameObjects(),
                    path,
                    out var targetTransform
                )
            )
            {
                return null;
            }

            return targetTransform.GetComponent<MeshRenderer>();
        }

        static string CollectMaterialProperties(MeshRenderer renderer)
        {
            var material = renderer.sharedMaterial ?? renderer.material;
            if (material == null)
            {
                return "Material not found.";
            }

            var lines = new List<string>
            {
                $"Material: {material.name}",
                $"Shader: {material.shader?.name ?? "<none>"}",
            };

            var shader = material.shader;
            if (shader != null)
            {
                for (var i = 0; i < shader.GetPropertyCount(); i++)
                {
                    var propName = shader.GetPropertyName(i);
                    var propType = shader.GetPropertyType(i);
                    string valueStr = null;

                    switch (propType)
                    {
                        case UnityEngine.Rendering.ShaderPropertyType.Color:
                            valueStr = material.HasProperty(propName)
                                ? material.GetColor(propName).ToString()
                                : "<not set>";
                            break;
                        case UnityEngine.Rendering.ShaderPropertyType.Vector:
                            valueStr = material.HasProperty(propName)
                                ? material.GetVector(propName).ToString()
                                : "<not set>";
                            break;
                        case UnityEngine.Rendering.ShaderPropertyType.Float:
                        case UnityEngine.Rendering.ShaderPropertyType.Range:
                            valueStr = material.HasProperty(propName)
                                ? material.GetFloat(propName).ToString("0.###")
                                : "<not set>";
                            break;
                        case UnityEngine.Rendering.ShaderPropertyType.Texture:
                            valueStr = material.HasProperty(propName)
                                ? material.GetTexture(propName)?.name ?? "<null texture>"
                                : "<not set>";
                            break;
                        default:
                            valueStr = "<unsupported property type>";
                            break;
                    }

                    lines.Add($"{propName} ({propType}): {valueStr}");
                }
            }

            return string.Join("\n", lines);
        }
    }
}
