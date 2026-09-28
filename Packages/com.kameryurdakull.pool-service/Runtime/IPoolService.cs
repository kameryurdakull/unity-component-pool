using UnityEngine;

namespace Pooling
{
    public interface IPoolService
    {
        T Spawn<T>(int id, Vector3 position, Quaternion rotation, Transform parent = null)
            where T : Component, IPoolable;

        void Despawn(Component member);
    }
}
