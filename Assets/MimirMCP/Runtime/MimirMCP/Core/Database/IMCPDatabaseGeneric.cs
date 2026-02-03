using System.Collections.Generic;

namespace MimirMCP.Core.Database
{
    /// <summary>
    /// Generic interface for type-safe MCP database operations.
    /// </summary>
    /// <typeparam name="T">The type of items stored in the database.</typeparam>
    public interface IMCPDatabase<T> : IMCPDatabase where T : IMCPDatabaseItem
    {
        /// <summary>
        /// Adds an item to the database.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <param name="existingId">Optional existing ID to use instead of generating a new one.</param>
        void AddItem(T item, string existingId = null);

        /// <summary>
        /// Gets an item by its unique identifier.
        /// </summary>
        /// <param name="objectId">The unique identifier of the item.</param>
        /// <returns>The item if found, null otherwise.</returns>
        T GetItemById(string objectId);

        /// <summary>
        /// Gets all items in the database.
        /// </summary>
        /// <returns>Read-only list of all items.</returns>
        IReadOnlyList<T> All();

        /// <summary>
        /// Checks if the database contains an item with the specified ID.
        /// </summary>
        /// <param name="objectId">The unique identifier to check.</param>
        /// <returns>True if the item exists, false otherwise.</returns>
        bool ContainsId(string objectId);

        /// <summary>
        /// Removes an item from the database.
        /// </summary>
        /// <param name="objectId">The unique identifier of the item to remove.</param>
        void RemoveItem(string objectId);
    }
}