using System;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace MimirMCP.Core.Database
{
    /// <summary>
    /// Database implementation with flexible read strategies, including Unity scene scanning.
    /// </summary>
    /// <typeparam name="T">The type of items stored in the database.</typeparam>
    public class MCPMonoBehaviourDatabase<T> : MCPDatabaseBase<T>, IMCPScannableDatabase<T>
        where T : IMCPDatabaseItem
    {
        /// <inheritdoc/>
        public Action ReadFunction { get; set; }

        /// <summary>
        /// Creates a new database instance with no default read function.
        /// </summary>
        public MCPMonoBehaviourDatabase()
        {
            // No default read function - must be set explicitly
        }

        /// <summary>
        /// Creates a database and immediately populates it from the scene hierarchy.
        /// Only works if T derives from MonoBehaviour.
        /// </summary>
        /// <param name="sortMode">The sort mode for finding objects.</param>
        /// <param name="includeInactive">Whether to include inactive GameObjects.</param>
        /// <returns>A populated database instance.</returns>
        public static MCPMonoBehaviourDatabase<T> CreateFromScene(
            FindObjectsSortMode sortMode = FindObjectsSortMode.None,
            bool includeInactive = false)
        {
            var database = new MCPMonoBehaviourDatabase<T>();
            database.SetReadFromSceneHierarchy(sortMode, includeInactive);
            database.Read();
            return database;
        }

        /// <inheritdoc/>
        public void SetReadFromSceneHierarchy(
            FindObjectsSortMode sortMode = FindObjectsSortMode.None,
            bool includeInactive = false)
        {
            ReadFunction = () =>
            {
                // Check if T is a MonoBehaviour at runtime
                if (!typeof(MonoBehaviour).IsAssignableFrom(typeof(T)))
                {
                    throw new InvalidOperationException(
                        $"Cannot use ReadFromSceneHierarchy with type {typeof(T).Name}. " +
                        $"Type must derive from MonoBehaviour to scan Unity scenes.");
                }

                // Clear existing items
                Clear();

                // Find all objects of type T in the scene
                MonoBehaviour[] itemsInScene;

                if (includeInactive)
                {
                    itemsInScene = UnityEngine.Object.FindObjectsByType(
                        typeof(T),
                        FindObjectsInactive.Include,
                        sortMode) as MonoBehaviour[];
                }
                else
                {
                    itemsInScene = UnityEngine.Object.FindObjectsByType(
                        typeof(T),
                        FindObjectsInactive.Exclude,
                        sortMode) as MonoBehaviour[];
                }

                // Add each found item to the database
                if (itemsInScene != null)
                {
                    foreach (var item in itemsInScene)
                    {
                        if (item is T typedItem)
                        {
                            // Let AddItem generate a new GUID for each item
                            AddItem(typedItem);
                        }
                    }
                }
            };
        }

        /// <inheritdoc/>
        public void Read()
        {
            if (ReadFunction == null)
            {
                throw new InvalidOperationException(
                    "No read function configured. Use SetReadFromSceneHierarchy() or set a custom ReadFunction.");
            }

            ReadFunction.Invoke();
        }

        /// <inheritdoc/>
        public string DatabaseTextRead()
        {
            var allItems = All();

            if (allItems.Count == 0)
            {
                return "[]";
            }

            // Build JSON array using each item's ToJson() method
            var jsonItems = allItems.Select(item => item.ToJson());

            // Construct properly formatted JSON array
            return "[\n" + string.Join(",\n", jsonItems) + "\n]";
        }
    }
}