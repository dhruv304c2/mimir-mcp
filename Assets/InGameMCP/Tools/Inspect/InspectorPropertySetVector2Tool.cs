using System.Collections.Generic;
using System.Net;
using Cysharp.Threading.Tasks;
using InGameMCP.Core.Dtos.MCP;
using InGameMCP.Core.MCP.MCPTool;
using InGameMCP.Core.MCP.MCPTool.Attributes;
using UnityEngine;

namespace InGameMCP.Tools.Inspect
{
    [MCPTool(
        toolName: "inspector_property_set_vector2",
        description: "Sets a Vector2 serialized field on a component."
    )]
    public class InspectorPropertySetVector2Tool : MCPToolBase
    {
        [MCPToolParam("path", "Hierarchy path to the GameObject.", MCPToolParam.ParamType.String, true)]
        public string Path;

        [MCPToolParam("component_type", "Full or short name of the component.", MCPToolParam.ParamType.String, true)]
        public string ComponentTypeName;

        [MCPToolParam("property_name", "Serialized field name to update.", MCPToolParam.ParamType.String, true)]
        public string PropertyName;

        [MCPToolParam("value_x", "Vector2 X component.", MCPToolParam.ParamType.Number, true)]
        public float ValueX;

        [MCPToolParam("value_y", "Vector2 Y component.", MCPToolParam.ParamType.Number, true)]
        public float ValueY;

        protected override UniTask<ContentBase[]> ExecuteTool(
            object id,
            HttpListenerContext ctx,
            IReadOnlyDictionary<string, object> rawParameters
        )
        {
            var component = InspectorPropertyInspectTool.ResolveComponent(Path, ComponentTypeName);
            var field = InspectorPropertySetHelpers.ResolveField(component, PropertyName);
            var effectiveType = InspectorPropertySetHelpers.GetEffectiveType(field);
            if (effectiveType != typeof(Vector2))
            {
                throw new MCPToolExecutionException(
                    -32602,
                    $"Field '{PropertyName}' is not a Vector2."
                );
            }

            field.SetValue(component, new Vector2(ValueX, ValueY));

            return UniTask.FromResult(
                new ContentBase[]
                {
                    new ContentText($"Updated {PropertyName} on '{Path}' to Vector2({ValueX}, {ValueY}).")
                }
            );
        }
    }
}
