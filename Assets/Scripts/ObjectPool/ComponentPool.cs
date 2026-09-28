using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace Pooling
{
    internal sealed class ComponentPool : IDisposable
    {
        private readonly Component _prefab;
        private readonly Transform _root;
        private readonly IObjectResolver _resolver;
        private readonly Dictionary<Component, ComponentPool> _owners;
        private readonly Stack<Component> _available;
        private readonly List<Component> _instances;

        public Type ComponentType => _prefab.GetType();

        public ComponentPool(Component prefab, int prewarmCount, Transform root,
            IObjectResolver resolver, Dictionary<Component, ComponentPool> owners)
        {
            _prefab = prefab;
            _root = root;
            _resolver = resolver;
            _owners = owners;
            _available = new Stack<Component>(prewarmCount);
            _instances = new List<Component>(prewarmCount);

            for (var index = 0; index < prewarmCount; index++)
            {
                _available.Push(CreateInstance());
            }
        }

        public Component Spawn(Vector3 position, Quaternion rotation, Transform parent)
        {
            var member = _available.Count > 0 ? _available.Pop() : CreateInstance();
            var transform = member.transform;
            transform.SetParent(parent != null ? parent : _root, false);
            transform.SetPositionAndRotation(position, rotation);

            try
            {
                member.gameObject.SetActive(true);
                ((IPoolable)member).OnSpawn();
                return member;
            }
            catch
            {
                member.gameObject.SetActive(false);
                transform.SetParent(_root, false);
                _available.Push(member);
                throw;
            }
        }

        public void Despawn(Component member)
        {
            try
            {
                ((IPoolable)member).OnDespawn();
            }
            finally
            {
                member.gameObject.SetActive(false);
                member.transform.SetParent(_root, false);
                _available.Push(member);
            }
        }

        public void Dispose()
        {
            for (var index = 0; index < _instances.Count; index++)
            {
                if (_instances[index] != null)
                {
                    Object.Destroy(_instances[index].gameObject);
                }
            }

            _instances.Clear();
            _available.Clear();
        }

        private Component CreateInstance()
        {
            var prefabObject = _prefab.gameObject;
            var wasActive = prefabObject.activeSelf;
            Component member = null;

            using (new ObjectResolverUnityExtensions.PrefabDirtyScope(prefabObject))
            {
                try
                {
                    prefabObject.SetActive(false);
                    member = Object.Instantiate(_prefab, _root, false);
                    _resolver.InjectGameObject(member.gameObject);
                    _owners.Add(member, this);
                    _instances.Add(member);
                    return member;
                }
                catch
                {
                    if (member != null) Object.Destroy(member.gameObject);
                    throw;
                }
                finally
                {
                    prefabObject.SetActive(wasActive);
                }
            }
        }
    }
}
