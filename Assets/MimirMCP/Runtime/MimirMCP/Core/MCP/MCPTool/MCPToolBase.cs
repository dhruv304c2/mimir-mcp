using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using MimirMCP.Core.Dtos.MCP;
using MimirMCP.Utils.HTTPUtils;

namespace MimirMCP.Core.MCP.MCPTool
{
    public class MCPToolParam
    {
        public enum ParamType
        {
            String,
            Number,
            Boolean,
        }

        public string ParamName { get; private set; }
        public string ParamDescription { get; private set; }
        public bool IsRequired { get; private set; } = false;
        public ParamType Type { get; private set; } = ParamType.String;

        public string GetTypeAsString()
        {
            switch (Type)
            {
                case ParamType.String:
                    return "string";
                case ParamType.Number:
                    return "number";
                case ParamType.Boolean:
                    return "boolean";
                default:
                    return "object";
            }
        }

        public MCPToolParam(
            string name,
            string description,
            ParamType type = ParamType.String,
            bool isRequired = false
        )
        {
            ParamName = name;
            ParamDescription = description;
            IsRequired = isRequired;
            Type = type;
        }
    }

    public abstract class MCPToolBase
    {
        public string ToolName { get; private set; }
        public string ToolDescription { get; private set; }
        public ILogger Logger { get; protected set; }
        public List<MCPToolParam> ToolParams { get; protected set; } = new List<MCPToolParam>();
        readonly Dictionary<string, ParameterBinding> _parameterBindings = new(
            StringComparer.OrdinalIgnoreCase
        );

        protected MCPToolBase()
        {
            var toolAttr = GetType().GetCustomAttribute<Attributes.MCPToolAttribute>();
            if (toolAttr != null)
            {
                ToolName = toolAttr.ToolName;
                ToolDescription = toolAttr.Description;
            }
            else
            {
                ToolName = "unnamed_tool";
                ToolDescription = "No description provided.";
            }
            BuildParameterMetadata();
        }

        void BuildParameterMetadata()
        {
            var members = GetType()
                .GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var memberParamsAdded = false;

            foreach (var member in members)
            {
                var paramAttr = member.GetCustomAttribute<Attributes.MCPToolParamAttribute>();
                if (paramAttr == null)
                {
                    continue;
                }

                if (!TryCreateBinding(member, paramAttr, out var binding))
                {
                    continue;
                }

                ToolParams.Add(
                    new MCPToolParam(
                        paramAttr.ParamName,
                        paramAttr.Description,
                        paramAttr.ParamType,
                        paramAttr.IsRequired
                    )
                );

                _parameterBindings[paramAttr.ParamName] = binding;
                memberParamsAdded = true;
            }

            if (!memberParamsAdded)
            {
                var paramAttrs = GetType().GetCustomAttributes<Attributes.MCPToolParamAttribute>();
                foreach (var paramAttr in paramAttrs)
                {
                    ToolParams.Add(
                        new MCPToolParam(
                            paramAttr.ParamName,
                            paramAttr.Description,
                            paramAttr.ParamType,
                            paramAttr.IsRequired
                        )
                    );
                }
            }
        }

        bool TryCreateBinding(
            MemberInfo member,
            Attributes.MCPToolParamAttribute paramAttr,
            out ParameterBinding binding
        )
        {
            switch (member)
            {
                case FieldInfo field when !field.IsInitOnly:
                    binding = new ParameterBinding(field, paramAttr, field.FieldType);
                    return true;
                case PropertyInfo property when property.SetMethod != null:
                    binding = new ParameterBinding(property, paramAttr, property.PropertyType);
                    return true;
            }

            binding = default;
            return false;
        }

        protected bool TryBindParameters(
            Dictionary<string, object> parameters,
            out string errorMessage
        )
        {
            parameters ??= new Dictionary<string, object>();

            foreach (var kvp in _parameterBindings)
            {
                var binding = kvp.Value;
                var attr = binding.Attribute;

                if (!parameters.TryGetValue(attr.ParamName, out var rawValue))
                {
                    if (attr.IsRequired)
                    {
                        errorMessage = $"Parameter '{attr.ParamName}' is required.";
                        return false;
                    }
                    else
                    {
                        SetMemberValue(binding.Member, GetDefaultValue(binding.MemberType));
                    }
                    continue;
                }

                if (
                    !TryConvertValue(
                        rawValue,
                        attr.ParamType,
                        binding.MemberType,
                        out var convertedValue
                    )
                )
                {
                    errorMessage =
                        $"Parameter '{attr.ParamName}' could not be converted to {attr.ParamType.ToString().ToLowerInvariant()}.";
                    return false;
                }

                SetMemberValue(binding.Member, convertedValue);
            }

            errorMessage = null;
            return true;
        }

        static bool TryConvertValue(
            object rawValue,
            MCPToolParam.ParamType paramType,
            Type memberType,
            out object convertedValue
        )
        {
            var targetType = Nullable.GetUnderlyingType(memberType) ?? memberType;

            if (rawValue == null)
            {
                convertedValue = null;
                return !targetType.IsValueType;
            }

            try
            {
                switch (paramType)
                {
                    case MCPToolParam.ParamType.String:
                        convertedValue = rawValue.ToString();
                        return true;
                    case MCPToolParam.ParamType.Number:
                        if (rawValue is IConvertible)
                        {
                            convertedValue = Convert.ChangeType(rawValue, targetType);
                            return true;
                        }
                        if (double.TryParse(rawValue.ToString(), out var doubleResult))
                        {
                            convertedValue = Convert.ChangeType(doubleResult, targetType);
                            return true;
                        }
                        break;
                    case MCPToolParam.ParamType.Boolean:
                        if (rawValue is bool boolValue)
                        {
                            convertedValue = boolValue;
                            return true;
                        }
                        if (bool.TryParse(rawValue.ToString(), out var parsedBool))
                        {
                            convertedValue = parsedBool;
                            return true;
                        }
                        break;
                }
            }
            catch
            {
                // Swallow and fall back to failure below.
            }

            convertedValue = null;
            return false;
        }

        void SetMemberValue(MemberInfo member, object value)
        {
            switch (member)
            {
                case FieldInfo field:
                    field.SetValue(this, value);
                    break;
                case PropertyInfo property:
                    property.SetValue(this, value);
                    break;
            }
        }

        object GetDefaultValue(Type type)
        {
            var targetType = Nullable.GetUnderlyingType(type) ?? type;
            return type.IsValueType && Nullable.GetUnderlyingType(type) == null
                ? Activator.CreateInstance(type)
                : null;
        }

        public async UniTask HandleToolCall(
            object id,
            HttpListenerContext ctx,
            Dictionary<string, object> parameters
        )
        {
            if (!TryBindParameters(parameters, out var bindError))
            {
                HTTPUtils.SafeWriteJson(
                    ctx,
                    HttpStatusCode.OK,
                    new MCPErrorResponse(
                        id,
                        new MCPError(-32602, bindError ?? "Invalid parameters provided.")
                    )
                );
                return;
            }

            await SwitchToUnityThreadAsync();

            try
            {
                var content = await ExecuteTool(
                    id,
                    ctx,
                    parameters ?? new Dictionary<string, object>()
                );

                if (content == null)
                {
                    throw new MCPToolExecutionException(-32603, "Tool returned no content.");
                }

                HTTPUtils.SafeWriteJson(
                    ctx,
                    HttpStatusCode.OK,
                    new MCPContentResponse
                    {
                        id = id,
                        result = new MCPContentResult { content = content },
                    }
                );
            }
            catch (MCPToolExecutionException toolEx)
            {
                HTTPUtils.SafeWriteJson(
                    ctx,
                    HttpStatusCode.OK,
                    new MCPErrorResponse(id, toolEx.Error)
                );
            }
            catch (Exception ex)
            {
                HTTPUtils.SafeWriteJson(
                    ctx,
                    HttpStatusCode.InternalServerError,
                    new MCPErrorResponse(
                        id,
                        new MCPError(-32603, ex.Message ?? "Tool execution failed.")
                    )
                );
            }
        }

        async UniTask SwitchToUnityThreadAsync()
        {
            var unityContext = MCPHost.UnityContext;
            if (unityContext == null)
            {
                return;
            }

            if (SynchronizationContext.Current == unityContext)
            {
                return;
            }

            var tcs = new UniTaskCompletionSource<bool>();
            unityContext.Post(_ => tcs.TrySetResult(true), null);
            await tcs.Task;
        }

        protected abstract UniTask<ContentBase[]> ExecuteTool(
            object id,
            HttpListenerContext ctx,
            IReadOnlyDictionary<string, object> rawParameters
        );

        public MCPToolUsage GetToolUsage()
        {
            var inputSchema = new InputSchema
            {
                properties = new Dictionary<string, Property>(),
                required = new List<string>(),
            };

            foreach (var param in ToolParams)
            {
                inputSchema.properties[param.ParamName] = new Property
                {
                    type = param.GetTypeAsString(),
                    description = param.ParamDescription,
                };

                if (param.IsRequired)
                {
                    inputSchema.required.Add(param.ParamName);
                }
            }

            return new MCPToolUsage
            {
                name = ToolName,
                description = ToolDescription,
                inputSchema = inputSchema,
            };
        }
    }

    readonly struct ParameterBinding
    {
        public ParameterBinding(
            MemberInfo member,
            Attributes.MCPToolParamAttribute attribute,
            Type memberType
        )
        {
            Member = member;
            Attribute = attribute;
            MemberType = memberType;
        }

        public MemberInfo Member { get; }
        public Attributes.MCPToolParamAttribute Attribute { get; }
        public Type MemberType { get; }
    }

    public class MCPToolExecutionException : Exception
    {
        public MCPToolExecutionException(
            int errorCode,
            string message,
            Exception innerException = null
        )
            : base(message, innerException)
        {
            Error = new MCPError(errorCode, message);
        }

        public MCPError Error { get; }
    }
}
