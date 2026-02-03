using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MimirMCP.Core.Database;
using MimirMCP.Core.Dtos.MCP;
using MimirMCP.Core.MCP.MCPTool;
using MimirMCP.Core.MCP.MCPTool.Attributes;
using UnityEngine;

namespace MimirMCP.Tools.Database
{
    /// <summary>
    /// Generic MCP tool for reading from MCP databases.
    /// </summary>
    /// <typeparam name="T">The type of items stored in the database.</typeparam>
    public class ReadDatabaseMCPTool<T> : MCPToolBase
        where T : IMCPDatabaseItem
    {
        private readonly IMCPDatabase<T> _database;
        private readonly string _databaseName;

        [MCPToolParam(
            paramName: "object_id",
            description: "Optional ID of a specific item to retrieve. If not provided, returns all items.",
            paramType: MCPToolParam.ParamType.String,
            isRequired: false
        )]
        public string ObjectId;

        /// <summary>
        /// Creates a new ReadDatabaseMCPTool instance.
        /// </summary>
        /// <param name="database">The database instance to read from.</param>
        /// <param name="databaseName">The name of the database (used in tool naming and messages).</param>
        public ReadDatabaseMCPTool(IMCPDatabase<T> database, string databaseName)
            : base()
        {
            _database = database;
            _databaseName = databaseName;

            // Set tool name and description dynamically
            ToolName = $"read_{_databaseName}_database";
            ToolDescription = $"Reads items from the {_databaseName} database. Can retrieve all items or a specific item by ID.";
        }

        protected override UniTask<ContentBase[]> ExecuteTool(
            IReadOnlyDictionary<string, object> rawParameters
        )
        {
            if (!string.IsNullOrEmpty(ObjectId))
            {
                // Retrieve specific item by ID
                var item = _database.GetItemById(ObjectId);
                if (item != null)
                {
                    var json = item.ToJson();
                    return UniTask.FromResult(new ContentBase[] { new ContentText(json) });
                }
                else
                {
                    return UniTask.FromResult(new ContentBase[] {
                        new ContentText($"Item with ID '{ObjectId}' not found in {_databaseName} database.")
                    });
                }
            }
            else
            {
                // Retrieve all items
                var items = _database.All();
                if (items.Count == 0)
                {
                    return UniTask.FromResult(new ContentBase[] {
                        new ContentText($"The {_databaseName} database is empty.")
                    });
                }

                // For MonoBehaviour databases, use their DatabaseTextRead method if available
                if (_database is IMCPScannableDatabase<T> scannableDatabase)
                {
                    var responseText = scannableDatabase.DatabaseTextRead();
                    return UniTask.FromResult(new ContentBase[] { new ContentText(responseText) });
                }
                else
                {
                    // For regular databases, serialize the list of items
                    var jsonList = new List<string>();
                    foreach (var item in items)
                    {
                        jsonList.Add(item.ToJson());
                    }
                    var responseText = $"[{string.Join(",", jsonList)}]";
                    return UniTask.FromResult(new ContentBase[] { new ContentText(responseText) });
                }
            }
        }
    }
}