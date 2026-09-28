using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Pooling
{
    public static class PoolRegistrationExtensions
    {
        public static void RegisterPoolService(this IContainerBuilder builder, PoolCatalog catalog,
            Transform root = null)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));

            var parent = root != null ? root : (builder.ApplicationOrigin as LifetimeScope)?.transform;
            builder.RegisterEntryPoint<PoolService>(
                resolver => new PoolService(catalog, resolver, parent), Lifetime.Singleton);
        }
    }
}
