using System.Collections.Generic;
using System.Net;
using Cysharp.Threading.Tasks;
using MimirMCP.Core.Dtos.MCP;
using MimirMCP.Core.MCP.MCPTool;
using MimirMCP.Core.MCP.MCPTool.Attributes;
using UnityEngine;

namespace MimirMCP.Tools.Inspect
{
    [MCPTool(
        toolName: "inspector_property_set_boolean",
        description: "Sets a boolean serialized field on a component (inspect first to confirm property names)."
    )]
    public class InspectorPropertySetBooleanTool : MCPToolBase
    {
        [MCPToolParam("path", "Hierarchy path to the GameObject.", MCPToolParam.ParamType.String, true)]
        public string Path;

        [MCPToolParam("component_type", "Full or short name of the component.", MCPToolParam.ParamType.String, true)]
        public string ComponentTypeName;

        [MCPToolParam("property_name", "Serialized field name to update.", MCPToolParam.ParamType.String, true)]
        public string PropertyName;

        [MCPToolParam("property_value", "Boolean value.", MCPToolParam.ParamType.Boolean, true)]
        public bool PropertyValue;

        protected override UniTask<ContentBase[]> ExecuteTool(
            object id,
            HttpListenerContext ctx,
            IReadOnlyDictionary<string, object> rawParameters
        )
        {
            var component = InspectorPropertyInspectTool.ResolveComponent(Path, ComponentTypeName);
            var field = InspectorPropertySetHelpers.ResolveField(component, PropertyName);
            var effectiveType = InspectorPropertySetHelpers.GetEffectiveType(field);
            if (effectiveType != typeof(bool))
            {
                throw new MCPToolExecutionException(
                    -32602,
                    $"Field '{PropertyName}' is not a boolean."
                );
            }

            field.SetValue(component, PropertyValue);

            return UniTask.FromResult(
                new ContentBase[]
                {
                    new ContentText($"Updated {PropertyName} on '{Path}' to {PropertyValue}.")
                }
            );
        }
    }
}
