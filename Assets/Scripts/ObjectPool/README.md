# Pool Service

Create a **Services > Pool Catalog** asset. Add a component prefab that implements `IPoolable`, a unique C# identifier such as `EnemyBullet`, and its prewarm count. In the catalog inspector, click **Generate PoolId enum** after changing names. The button includes every PoolCatalog asset in the project.

Dragging a prefab into the Component field automatically selects its sole `IPoolable` component. If a prefab has several `IPoolable` components, drag the intended script component from the prefab Inspector instead.

Assign the catalog in your game `LifetimeScope` and register the service once:

```csharp
using Pooling;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public sealed class GameLifetimeScope : LifetimeScope
{
    [SerializeField] private PoolCatalog _poolCatalog;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterPoolService(_poolCatalog);
    }
}
```

Inject `IPoolService` into consumers:

```csharp
var bullet = _poolService.Spawn<Bullet>(PoolId.EnemyBullet, position, rotation);
_poolService.Despawn(bullet);
```

The service validates the catalog and prewarms during VContainer initialization. Prefabs and their children receive VContainer injection when first instantiated. Returning a foreign or already returned component throws. The pool owns all instances it creates and destroys them when its scope is disposed.

By default, the pool root is parented under the registering `LifetimeScope`. Pass a `Transform` as the optional second argument to `RegisterPoolService` to place it elsewhere.
