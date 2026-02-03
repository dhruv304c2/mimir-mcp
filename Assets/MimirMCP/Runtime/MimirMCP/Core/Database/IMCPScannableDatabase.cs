using System;
using UnityEngine;

namespace MimirMCP.Core.Database
{
    /// <summary>
    /// Interface for MCP databases that can populate themselves from various sources.
    /// </summary>
    /// <typeparam name="T">The type of items stored in the database.</typeparam>
    public interface IMCPScannableDatabase<T> : IMCPDatabase<T>
        where T : IMCPDatabaseItem
    {
        /// <summary>
        /// Gets or sets the read function used to populate the database.
        /// </summary>
        Action ReadFunction { get; set; }

        /// <summary>
        /// Sets the read function to scan the Unity scene hierarchy for MonoBehaviour items.
        /// Only works if T derives from MonoBehaviour.
        /// </summary>
        /// <param name="sortMode">The sort mode for finding objects.</param>
        /// <param name="includeInactive">Whether to include inactive GameObjects in the search.</param>
        void SetReadFromSceneHierarchy(
            FindObjectsSortMode sortMode = FindObjectsSortMode.None,
            bool includeInactive = false);

        /// <summary>
        /// Executes the configured read function to populate the database.
        /// </summary>
        void Read();

        /// <summary>
        /// Serializes the entire database to JSON format.
        /// </summary>
        /// <returns>JSON representation of all items in the database.</returns>
        string DatabaseTextRead();
    }
}