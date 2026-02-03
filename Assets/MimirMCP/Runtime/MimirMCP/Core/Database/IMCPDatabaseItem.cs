namespace MimirMCP.Core.Database
{
    /// <summary>
    /// Interface for items that can be stored in an MCP database.
    /// </summary>
    public interface IMCPDatabaseItem
    {
        /// <summary>
        /// Gets or sets the unique identifier for this database item.
        /// </summary>
        string ObjectId { get; set; }

        /// <summary>
        /// Serializes this item to JSON format.
        /// </summary>
        /// <returns>JSON representation of this item.</returns>
        string ToJson();
    }
}