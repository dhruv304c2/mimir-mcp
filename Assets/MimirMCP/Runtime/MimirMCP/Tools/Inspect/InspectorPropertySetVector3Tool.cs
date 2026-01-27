using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MimirMCP.Core.Dtos.MCP;
using MimirMCP.Core.MCP.MCPTool;
using MimirMCP.Core.MCP.MCPTool.Attributes;
using UnityEngine;

namespace MimirMCP.Tools.Inspect
{
    [MCPTool(
        toolName: "inspector_property_set_vector3",
        description: "Sets a Vector3 serialized field on a component."
    )]
    public class InspectorPropertySetVector3Tool : MCPToolBase
    {
        [MCPToolParam("path", "Hierarchy path to the GameObject.", MCPToolParam.ParamType.String, true)]
        public string Path;

        [MCPToolParam("component_type", "Full or short name of the component.", MCPToolParam.ParamType.String, true)]
        public string ComponentTypeName;

        [MCPToolParam("property_name", "Serialized field name to update.", MCPToolParam.ParamType.String, true)]
        public string PropertyName;

        [MCPToolParam("value_x", "Vector3 X component.", MCPToolParam.ParamType.Number, true)]
        public float ValueX;

        [MCPToolParam("value_y", "Vector3 Y component.", MCPToolParam.ParamType.Number, true)]
        public float ValueY;

        [MCPToolParam("value_z", "Vector3 Z component.", MCPToolParam.ParamType.Number, true)]
        public float ValueZ;

        protected override UniTask<ContentBase[]> ExecuteTool(
            IReadOnlyDictionary<string, object> rawParameters
        )
        {
            var component = InspectorPropertyInspectTool.ResolveComponent(Path, ComponentTypeName);
            var field = InspectorPropertySetHelpers.ResolveField(component, PropertyName);
            var effectiveType = InspectorPropertySetHelpers.GetEffectiveType(field);
            if (effectiveType != typeof(Vector3))
            {
                throw new MCPToolExecutionException(
                    -32602,
                    $"Field '{PropertyName}' is not a Vector3."
                );
            }

            field.SetValue(component, new Vector3(ValueX, ValueY, ValueZ));

            return UniTask.FromResult(
                new ContentBase[]
                {
                    new ContentText($"Updated {PropertyName} on '{Path}' to Vector3({ValueX}, {ValueY}, {ValueZ}).")
                }
            );
        }
    }
}
