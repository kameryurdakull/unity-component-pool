# Unity Component Pool

A Unity component pool configured with a `PoolCatalog` asset. Add a prefab once, generate a strongly typed ID, and resolve one `IPoolService` through VContainer. Instances are injected when created and prewarmed at scope initialization.

## Requirements

- Unity 6 (6000.0 or newer).
- [VContainer 1.19.0](https://github.com/hadashiA/VContainer), installed in the **project** before this package. Unity does not allow a Git dependency inside another Git package's `package.json`, so Unity Component Pool cannot install VContainer automatically.

UniTask and DOTween are not required by this package.

If a project already contains an older copy under `Assets/Scripts/ObjectPool`, remove that copy before installing the Git package to avoid duplicate assemblies. Keep your `PoolCatalog` assets and regenerate `PoolId` after installation.

## Install with Package Manager

In **Window > Package Management > Package Manager**, choose **Install package from Git URL** and paste:

```text
https://github.com/kameryurdakull/unity-component-pool.git?path=/Packages/com.kameryurdakull.pool-service
```

Add VContainer first with its Git URL:

```text
https://github.com/hadashiA/VContainer.git?path=/VContainer/Assets/VContainer#1.19.0
```

You can also put both entries in the consuming project's `Packages/manifest.json`:

```json
{
  "dependencies": {
    "jp.hadashikick.vcontainer": "https://github.com/hadashiA/VContainer.git?path=/VContainer/Assets/VContainer#1.19.0",
    "com.kameryurdakull.pool-service": "https://github.com/kameryurdakull/unity-component-pool.git?path=/Packages/com.kameryurdakull.pool-service"
  }
}
```

Add these entries to an existing manifest; keep its other dependencies. Pin the Unity Component Pool URL to a release tag or commit by appending `#tag-or-commit` when you need reproducible builds.

## Create a catalog

1. Create an asset with **Assets > Create > Services > Pool Catalog**.
2. Add an entry with a unique C# identifier such as `EnemyBullet`, a prefab containing an `IPoolable` component, and a nonnegative prewarm count.
3. Click **Generate PoolId enum** in the catalog Inspector.

Dragging a prefab into the Component field selects its sole `IPoolable` component automatically. If the prefab has more than one, assign the intended script component directly. Poolable components should be on the prefab root. Names across different catalogs share one generated enum; a name can be reused across catalogs, but must be unique within each catalog. The generator rejects hash collisions and invalid entries.

Generated source and its assembly definition are written to `Assets/PoolService/Generated`. Commit those files and their `.meta` files in the consuming project. Regenerate after adding, removing, or renaming entries. The generated files live outside the Git package so Package Manager updates do not overwrite them.

## Register the service

Add one catalog reference to the game's VContainer scope:

```csharp
using Pooling;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public sealed class GameLifetimeScope : LifetimeScope
{
    [SerializeField] private PoolCatalog poolCatalog;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterPoolService(poolCatalog);
    }
}
```

By default, the pool root is parented under the registering `LifetimeScope`. Pass an optional `Transform` as the second argument to `RegisterPoolService` to choose another parent.

## Spawn and return

Implement the two callbacks on the prefab component:

```csharp
using Pooling;
using UnityEngine;

public sealed class EnemyBullet : MonoBehaviour, IPoolable
{
    public void OnSpawn() { }
    public void OnDespawn() { }
}
```

Inject `IPoolService` into a consumer, then use the generated enum:

```csharp
var bullet = poolService.Spawn<EnemyBullet>(PoolId.EnemyBullet, position, rotation);
poolService.Despawn(bullet);
```

If the consumer has its own `.asmdef`, reference both `PoolService` and `PoolService.Generated`. Scripts in the default `Assembly-CSharp` assembly see both automatically.

The service prewarms during scope initialization. New instances receive VContainer injection, including components on children. It rejects an unknown key, a mismatched spawn type, a foreign component, or a double return. The scope owns its instances and destroys them when disposed.

## Repository layout

- `Packages/com.kameryurdakull.pool-service` is the distributable package.
- `Assets` and `ProjectSettings` are the development project and examples; Package Manager ignores them when installed through the `?path=` URL.
