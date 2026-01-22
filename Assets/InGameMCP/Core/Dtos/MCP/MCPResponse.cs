using System;
using System.Collections.Generic;
using UnityEditor;

namespace InGameMCP.Core.Dtos.MCP
{
    [Serializable]
    public class MCPContentResult : MCPBase
    {
        public ContentBase[] contents;
    }

    [Serializable]
    public class ContentBase
    {
        public string type;
    }

    public class ContentText : ContentBase
    {
        public string text;

        public ContentText(string text)
        {
            this.type = "text";
            this.text = text;
        }
    }

    public class ContentImage : ContentBase
    {
        public string url;

        public ContentImage(string url)
        {
            this.type = "image";
            this.url = url;
        }
    }

    public class Resource : ContentBase
    {
        public string resourceId;

        public Resource(string resourceId)
        {
            this.type = "resource";
            this.resourceId = resourceId;
        }
    }

    // Tool Response
    [Serializable]
    public class MCPToolResponse : MCPBase
    {
        public List<MCPToolUsage> tools;
    }
}
