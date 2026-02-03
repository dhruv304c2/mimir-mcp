using Newtonsoft.Json;
using UnityEngine;

namespace MimirMCP.Core.Database
{
    /// <summary>
    /// Base class for MonoBehaviours that can be stored in an MCP database.
    /// </summary>
    public abstract class MCPMonoBehaviourDatabaseItem : MonoBehaviour, IMCPDatabaseItem
    {
        private string _objectId;

        /// <inheritdoc/>
        public string ObjectId
        {
            get => _objectId;
            set => _objectId = value;
        }

        /// <inheritdoc/>
        public virtual string ToJson()
        {
            return JsonConvert.SerializeObject(
                this,
                Formatting.Indented,
                new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    // Include type information for polymorphic serialization
                    TypeNameHandling = TypeNameHandling.Auto
                }
            );
        }
    }
}