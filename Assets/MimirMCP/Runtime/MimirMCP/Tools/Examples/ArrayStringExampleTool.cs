using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cysharp.Threading.Tasks;
using MimirMCP.Core.Dtos.MCP;
using MimirMCP.Core.MCP.MCPTool;
using MimirMCP.Core.MCP.MCPTool.Attributes;
using UnityEngine;

namespace MimirMCP.Tools.Examples
{
    [MCPTool(
        "array_string_example",
        "Example tool that demonstrates ArrayString parameter type usage"
    )]
    [MCPToolParam(
        "tags",
        "List of tags to process",
        MCPToolParam.ParamType.ArrayString,
        isRequired: true
    )]
    [MCPToolParam(
        "prefix",
        "Prefix to add to each tag",
        MCPToolParam.ParamType.String,
        isRequired: false
    )]
    public class ArrayStringExampleTool : MCPToolBase
    {
        // This field will be automatically populated with the array of strings
        public List<string> tags;

        // Optional prefix parameter
        public string prefix = "";

        protected override async UniTask<ContentBase[]> ExecuteTool(
            IReadOnlyDictionary<string, object> rawParameters
        )
        {
            var result = new StringBuilder();
            result.AppendLine($"Received {tags.Count} tags:");

            foreach (var tag in tags)
            {
                var processedTag = string.IsNullOrEmpty(prefix) ? tag : $"{prefix}{tag}";
                result.AppendLine($"- {processedTag}");
            }

            // Example of using the tags in Unity
            Debug.Log($"Processing tags: {string.Join(", ", tags)}");

            return new ContentBase[] { new ContentText(result.ToString()) };
        }
    }
}
