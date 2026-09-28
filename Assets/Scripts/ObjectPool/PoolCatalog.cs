using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pooling
{
    [CreateAssetMenu(fileName = "PoolCatalog", menuName = "Services/Pool Catalog")]
    public sealed class PoolCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            [SerializeField] private string name;
            [SerializeField] private Component prefab;
            [SerializeField, Min(0)] private int prewarmCount;

            public string Name => name;
            public Component Prefab => ResolvePoolable(prefab);
            public int PrewarmCount => prewarmCount;

            public static Component ResolvePoolable(Component selected)
            {
                if (selected == null || selected is IPoolable)
                {
                    return selected;
                }

                var components = selected.GetComponents<Component>();
                Component result = null;

                for (var index = 0; index < components.Length; index++)
                {
                    if (components[index] is not IPoolable) continue;
                    if (result != null) return null;
                    result = components[index];
                }

                return result;
            }
        }

        [SerializeField] private List<Entry> entries = new();

        public IReadOnlyList<Entry> Entries => entries;
    }
}
