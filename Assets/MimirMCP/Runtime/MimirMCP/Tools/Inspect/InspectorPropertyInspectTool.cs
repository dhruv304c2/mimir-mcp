using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using Cysharp.Threading.Tasks;
using MimirMCP.Core.Dtos.MCP;
using MimirMCP.Core.MCP.MCPTool;
using MimirMCP.Core.MCP.MCPTool.Attributes;
using MimirMCP.Tools.ObjectTransform;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MimirMCP.Tools.Inspect
{
    [MCPTool(
        toolName: "inspector_property_inspect",
        description: "Lists numeric/string/boolean serialized properties for a component."
    )]
    public class InspectorPropertyInspectTool : MCPToolBase
    {
        [MCPToolParam(
            "path",
            "Hierarchy path to the GameObject.",
            MCPToolParam.ParamType.String,
            true
        )]
        public string Path;

        [MCPToolParam(
            "component_type",
            "Full or short name of the component/MonoBehaviour to inspect.",
            MCPToolParam.ParamType.String,
            true
        )]
        public string ComponentTypeName;

        protected override UniTask<ContentBase[]> ExecuteTool(
            object id,
            HttpListenerContext ctx,
            IReadOnlyDictionary<string, object> rawParameters
        )
        {
            var component = ResolveComponent();
            var props = CollectSerializableProperties(component);

            var response = JsonUtility.ToJson(
                new SerializablePropertyPayload
                {
                    path = Path,
                    component = component.GetType().FullName,
                    properties = props.ToArray(),
                },
                true
            );

            return UniTask.FromResult(new ContentBase[] { new ContentText(response) });
        }

        Component ResolveComponent()
        {
            return ResolveComponent(Path, ComponentTypeName);
        }

        internal static Component ResolveComponent(string path, string componentTypeName)
        {
            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(componentTypeName))
            {
                throw new MCPToolExecutionException(
                    -32602,
                    "path and component_type are required."
                );
            }

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                throw new MCPToolExecutionException(-32001, "Active scene is invalid.");
            }

            if (
                !TransformChangeMCPTool.TryFindTransform(
                    scene.GetRootGameObjects(),
                    path,
                    out var transform
                )
            )
            {
                throw new MCPToolExecutionException(-32602, $"Transform '{path}' was not found.");
            }

            var type = ResolveType(componentTypeName);
            if (type == null)
            {
                throw new MCPToolExecutionException(
                    -32602,
                    $"Component type '{componentTypeName}' not found."
                );
            }

            var component = transform.GetComponent(type);
            if (component == null)
            {
                throw new MCPToolExecutionException(
                    -32602,
                    $"Component '{type.FullName}' not found on '{path}'."
                );
            }

            return component;
        }

        static Type ResolveType(string typeName)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var asm in assemblies)
            {
                var type =
                    asm.GetType(typeName, false)
                    ?? asm.GetTypes()
                        .FirstOrDefault(t => t.Name.Equals(typeName, StringComparison.Ordinal));
                if (type != null)
                {
                    return type;
                }
            }
            return null;
        }

        internal static bool IsSupportedType(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            if (type == typeof(string) || type == typeof(bool))
            {
                return true;
            }

            if (type == typeof(Color) || type == typeof(Vector2) || type == typeof(Vector3))
            {
                return true;
            }

            return type.IsPrimitive && type != typeof(IntPtr) && type != typeof(UIntPtr);
        }

        internal static bool IsSerializableField(FieldInfo field)
        {
            var hasSerializeField = field.GetCustomAttribute<SerializeField>() != null;
            if (!field.IsPublic && !hasSerializeField)
            {
                return false;
            }

            return IsSupportedType(field.FieldType);
        }

        static List<SerializableProperty> CollectSerializableProperties(Component component)
        {
            var type = component.GetType();
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var fields = type.GetFields(flags);
            var result = new List<SerializableProperty>();

            foreach (var field in fields)
            {
                if (!IsSerializableField(field))
                {
                    continue;
                }

                var value = field.GetValue(component);
                result.Add(
                    new SerializableProperty
                    {
                        name = field.Name,
                        type = (
                            Nullable.GetUnderlyingType(field.FieldType) ?? field.FieldType
                        ).Name,
                        value = value?.ToString() ?? "<null>",
                    }
                );
            }

            return result;
        }

        [Serializable]
        class SerializablePropertyPayload
        {
            public string path;
            public string component;
            public SerializableProperty[] properties;
        }

        [Serializable]
        class SerializableProperty
        {
            public string name;
            public string type;
            public string value;
        }
    }
}
