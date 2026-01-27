using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MimirMCP.Core.Dtos.MCP;
using MimirMCP.Core.MCP.MCPTool;
using MimirMCP.Core.MCP.MCPTool.Attributes;
using UnityEngine;

namespace MimirMCP.Tools.Inspect
{
    [MCPTool(
        toolName: "inspector_property_set_float",
        description: "Sets a numeric serialized field (float/int/etc.) on a component."
    )]
    public class InspectorPropertySetFloatTool : MCPToolBase
    {
        [MCPToolParam("path", "Hierarchy path to the GameObject.", MCPToolParam.ParamType.String, true)]
        public string Path;

        [MCPToolParam("component_type", "Full or short name of the component.", MCPToolParam.ParamType.String, true)]
        public string ComponentTypeName;

        [MCPToolParam("property_name", "Serialized field name to update.", MCPToolParam.ParamType.String, true)]
        public string PropertyName;

        [MCPToolParam("property_value", "Numeric value.", MCPToolParam.ParamType.Number, true)]
        public float PropertyValue;

        protected override UniTask<ContentBase[]> ExecuteTool(
            IReadOnlyDictionary<string, object> rawParameters
        )
        {
            var component = InspectorPropertyInspectTool.ResolveComponent(Path, ComponentTypeName);
            var field = InspectorPropertySetHelpers.ResolveField(component, PropertyName);
            var effectiveType = InspectorPropertySetHelpers.GetEffectiveType(field);

            if (!effectiveType.IsPrimitive || effectiveType == typeof(bool))
            {
                throw new MCPToolExecutionException(
                    -32602,
                    $"Field '{PropertyName}' is not a numeric primitive."
                );
            }

            var converted = Convert.ChangeType(PropertyValue, effectiveType);
            field.SetValue(component, converted);

            return UniTask.FromResult(
                new ContentBase[]
                {
                    new ContentText($"Updated {PropertyName} on '{Path}' to {PropertyValue}.")
                }
            );
        }
    }
}
