using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MimirMCP.Core.Dtos.MCP;
using MimirMCP.Core.MCP.MCPTool;
using MimirMCP.Core.MCP.MCPTool.Attributes;
using UnityEngine;

namespace MimirMCP.Tools.Inspect
{
    [MCPTool(
        toolName: "inspector_property_set_color",
        description: "Sets a Color serialized field on a component."
    )]
    public class InspectorPropertySetColorTool : MCPToolBase
    {
        [MCPToolParam("path", "Hierarchy path to the GameObject.", MCPToolParam.ParamType.String, true)]
        public string Path;

        [MCPToolParam("component_type", "Full or short name of the component.", MCPToolParam.ParamType.String, true)]
        public string ComponentTypeName;

        [MCPToolParam("property_name", "Serialized field name to update.", MCPToolParam.ParamType.String, true)]
        public string PropertyName;

        [MCPToolParam("color_r", "Red component (0-1).", MCPToolParam.ParamType.Number, true)]
        public float ColorR;

        [MCPToolParam("color_g", "Green component (0-1).", MCPToolParam.ParamType.Number, true)]
        public float ColorG;

        [MCPToolParam("color_b", "Blue component (0-1).", MCPToolParam.ParamType.Number, true)]
        public float ColorB;

        [MCPToolParam("color_a", "Alpha component (0-1). Optional, defaults to 1.", MCPToolParam.ParamType.Number)]
        public float? ColorA;

        protected override UniTask<ContentBase[]> ExecuteTool(
            IReadOnlyDictionary<string, object> rawParameters
        )
        {
            var component = InspectorPropertyInspectTool.ResolveComponent(Path, ComponentTypeName);
            var field = InspectorPropertySetHelpers.ResolveField(component, PropertyName);
            var effectiveType = InspectorPropertySetHelpers.GetEffectiveType(field);
            if (effectiveType != typeof(Color))
            {
                throw new MCPToolExecutionException(
                    -32602,
                    $"Field '{PropertyName}' is not a Color."
                );
            }

            var color = new Color(ColorR, ColorG, ColorB, ColorA ?? 1f);
            field.SetValue(component, color);

            return UniTask.FromResult(
                new ContentBase[]
                {
                    new ContentText(
                        $"Updated {PropertyName} on '{Path}' to Color({color.r:F2}, {color.g:F2}, {color.b:F2}, {color.a:F2})."
                    )
                }
            );
        }
    }
}
