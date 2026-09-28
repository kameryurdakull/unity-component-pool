using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace Pooling
{
    public sealed class PoolService : IPoolService, IInitializable, IDisposable
    {
        private const string RootName = "PoolService";

        private readonly PoolCatalog _catalog;
        private readonly IObjectResolver _resolver;
        private readonly Transform _parent;
        private readonly Dictionary<int, ComponentPool> _pools = new();
        private readonly Dictionary<Component, ComponentPool> _owners = new();
        private readonly HashSet<Component> _active = new();
        private Transform _root;

        public PoolService(PoolCatalog catalog, IObjectResolver resolver, Transform parent = null)
        {
            _catalog = catalog != null ? catalog : throw new ArgumentNullException(nameof(catalog));
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            _parent = parent;
        }

        public void Initialize()
        {
            if (_root != null)
            {
                return;
            }

            var entries = _catalog.Entries;
            var names = new HashSet<string>(StringComparer.Ordinal);
            var keys = new HashSet<int>();

            // Validate every entry before creating any scene objects.
            for (var index = 0; index < entries.Count; index++)
            {
                var entry = entries[index];
                if (entry == null || string.IsNullOrWhiteSpace(entry.Name) || entry.Prefab == null ||
                    entry.Prefab is not IPoolable || entry.PrewarmCount < 0)
                {
                    throw new InvalidOperationException($"Invalid pool catalog entry at index {index}.");
                }

                var key = PoolKey.FromName(entry.Name);
                if (!names.Add(entry.Name) || key == 0 || !keys.Add(key))
                {
                    throw new InvalidOperationException(
                        $"Pool '{entry.Name}' has a duplicate name or key collision.");
                }
            }

            _root = new GameObject(RootName).transform;
            if (_parent != null)
            {
                _root.SetParent(_parent, false);
            }

            try
            {
                for (var index = 0; index < entries.Count; index++)
                {
                    var entry = entries[index];
                    var key = PoolKey.FromName(entry.Name);
                    _pools.Add(key, new ComponentPool(entry.Prefab, entry.PrewarmCount,
                        _root, _resolver, _owners));
                }
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public T Spawn<T>(int id, Vector3 position, Quaternion rotation, Transform parent = null)
            where T : Component, IPoolable
        {
            if (!_pools.TryGetValue(id, out var pool))
            {
                throw new ArgumentOutOfRangeException(nameof(id), id, "Pool is not registered.");
            }

            if (!typeof(T).IsAssignableFrom(pool.ComponentType))
            {
                throw new InvalidOperationException($"Pool '{id}' contains {pool.ComponentType.Name}, not {typeof(T).Name}.");
            }

            var member = pool.Spawn(position, rotation, parent);
            _active.Add(member);
            return (T)member;
        }

        public void Despawn(Component member)
        {
            if (member == null || !_owners.TryGetValue(member, out var pool) || !_active.Remove(member))
            {
                throw new InvalidOperationException("Component is not an active member of this pool service.");
            }

            pool.Despawn(member);
        }

        public void Dispose()
        {
            foreach (var pool in _pools.Values)
            {
                pool.Dispose();
            }

            _pools.Clear();
            _owners.Clear();
            _active.Clear();

            if (_root != null)
            {
                Object.Destroy(_root.gameObject);
                _root = null;
            }
        }
    }
}
