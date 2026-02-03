using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace MimirMCP.Core.Database
{
    /// <summary>
    /// Thread-safe base implementation of IMCPDatabase with ReaderWriterLockSlim for concurrent access.
    /// </summary>
    /// <typeparam name="T">The type of items stored in the database.</typeparam>
    public class MCPDatabaseBase<T> : IMCPDatabase<T>, IDisposable where T : IMCPDatabaseItem
    {
        private readonly List<T> _items = new List<T>();
        private readonly Dictionary<string, T> _itemsById = new Dictionary<string, T>();
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private bool _disposed = false;

        /// <inheritdoc/>
        public Type ItemType => typeof(T);

        /// <inheritdoc/>
        public int Count
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    return _items.Count;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        /// <inheritdoc/>
        public virtual void AddItem(T item, string existingId = null)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            _lock.EnterWriteLock();
            try
            {
                // Use existing ID if provided, otherwise generate new one
                string id = !string.IsNullOrEmpty(existingId) ? existingId : Guid.NewGuid().ToString();

                // Remove existing item with same ID if any
                if (_itemsById.ContainsKey(id))
                {
                    var existingItem = _itemsById[id];
                    _items.Remove(existingItem);
                    _itemsById.Remove(id);
                }

                item.ObjectId = id;
                _items.Add(item);
                _itemsById[id] = item;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <inheritdoc/>
        public T GetItemById(string objectId)
        {
            if (string.IsNullOrEmpty(objectId))
                return default(T);

            _lock.EnterReadLock();
            try
            {
                return _itemsById.TryGetValue(objectId, out T item) ? item : default(T);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <inheritdoc/>
        public IReadOnlyList<T> All()
        {
            _lock.EnterReadLock();
            try
            {
                // Return a copy to ensure thread safety
                return _items.ToList().AsReadOnly();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <inheritdoc/>
        public bool ContainsId(string objectId)
        {
            if (string.IsNullOrEmpty(objectId))
                return false;

            _lock.EnterReadLock();
            try
            {
                return _itemsById.ContainsKey(objectId);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <inheritdoc/>
        public void RemoveItem(string objectId)
        {
            if (string.IsNullOrEmpty(objectId))
                return;

            _lock.EnterWriteLock();
            try
            {
                if (_itemsById.TryGetValue(objectId, out T item))
                {
                    _items.Remove(item);
                    _itemsById.Remove(objectId);
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <inheritdoc/>
        public void Clear()
        {
            _lock.EnterWriteLock();
            try
            {
                _items.Clear();
                _itemsById.Clear();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <inheritdoc/>
        public object GetItemByIdObject(string objectId)
        {
            return GetItemById(objectId);
        }

        /// <inheritdoc/>
        public IReadOnlyList<object> AllItemsObject()
        {
            _lock.EnterReadLock();
            try
            {
                return _items.Cast<object>().ToList().AsReadOnly();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        /// Disposes the reader-writer lock.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected implementation of Dispose pattern.
        /// </summary>
        /// <param name="disposing">True if disposing managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _lock?.Dispose();
                }
                _disposed = true;
            }
        }
    }
}