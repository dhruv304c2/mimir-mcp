using System;
using System.Collections.Generic;

namespace MimirMCP.Core.Database
{
    /// <summary>
    /// Non-generic interface for MCP databases to enable registry storage by Type.
    /// </summary>
    public interface IMCPDatabase
    {
        /// <summary>
        /// Gets the type of items stored in this database.
        /// </summary>
        Type ItemType { get; }

        /// <summary>
        /// Clears all items from the database.
        /// </summary>
        void Clear();

        /// <summary>
        /// Gets the count of items in the database.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets an item by its ID as an object.
        /// </summary>
        /// <param name="objectId">The unique identifier of the item.</param>
        /// <returns>The item as an object, or null if not found.</returns>
        object GetItemByIdObject(string objectId);

        /// <summary>
        /// Gets all items in the database as objects.
        /// </summary>
        /// <returns>Read-only list of all items as objects.</returns>
        IReadOnlyList<object> AllItemsObject();
    }
}