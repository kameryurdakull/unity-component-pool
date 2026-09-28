using Pooling;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Core
{
    public class GameLifetimeScope : LifetimeScope
    {
        [SerializeField] private PoolCatalog poolCatalog;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterPoolService(poolCatalog);
        }
    }
}