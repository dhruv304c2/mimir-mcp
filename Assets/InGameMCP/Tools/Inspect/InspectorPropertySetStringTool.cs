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
        toolName: "inspector_property_set_string",
        description: "Sets a string serialized field on a component."
    )]
    public class InspectorPropertySetStringTool : MCPToolBase
    {
        [MCPToolParam("path", "Hierarchy path to the GameObject.", MCPToolParam.ParamType.String, true)]
        public string Path;

        [MCPToolParam("component_type", "Full or short name of the component.", MCPToolParam.ParamType.String, true)]
        public string ComponentTypeName;

        [MCPToolParam("property_name", "Serialized field name to update.", MCPToolParam.ParamType.String, true)]
        public string PropertyName;

        [MCPToolParam("property_value", "New string value.", MCPToolParam.ParamType.String, true)]
        public string PropertyValue;

        protected override UniTask<ContentBase[]> ExecuteTool(
            object id,
            HttpListenerContext ctx,
            IReadOnlyDictionary<string, object> rawParameters
        )
        {
            var component = InspectorPropertyInspectTool.ResolveComponent(Path, ComponentTypeName);
            var field = InspectorPropertySetHelpers.ResolveField(component, PropertyName);
            var effectiveType = InspectorPropertySetHelpers.GetEffectiveType(field);
            if (effectiveType != typeof(string))
            {
                throw new MCPToolExecutionException(
                    -32602,
                    $"Field '{PropertyName}' is not a string."
                );
            }

            field.SetValue(component, PropertyValue);

            return UniTask.FromResult(
                new ContentBase[]
                {
                    new ContentText($"Updated {PropertyName} on '{Path}' to '{PropertyValue}'.")
                }
            );
        }
    }
}
