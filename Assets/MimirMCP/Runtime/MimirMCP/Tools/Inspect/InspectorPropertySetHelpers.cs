using System;
using System.Reflection;
using MimirMCP.Core.MCP.MCPTool;
using UnityEngine;

namespace MimirMCP.Tools.Inspect
{
    internal static class InspectorPropertySetHelpers
    {
        internal static FieldInfo ResolveField(Component component, string propertyName)
        {
            if (component == null)
            {
                throw new MCPToolExecutionException(-32602, "Component reference is null.");
            }

            var type = component.GetType();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var field = type.GetField(propertyName, flags);
            if (field == null)
            {
                throw new MCPToolExecutionException(
                    -32602,
                    $"Field '{propertyName}' not found on {type.FullName}. Run inspector_property_inspect to list available serialized fields."
                );
            }

            if (!InspectorPropertyInspectTool.IsSerializableField(field))
            {
                throw new MCPToolExecutionException(
                    -32602,
                    $"Field '{propertyName}' is not a serialized primitive/string/bool/vector/color. Use inspector_property_inspect to see supported options."
                );
            }

            return field;
        }

        internal static Type GetEffectiveType(FieldInfo field)
        {
            return Nullable.GetUnderlyingType(field.FieldType) ?? field.FieldType;
        }
    }
}
