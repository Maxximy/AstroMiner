using UnityEngine;
using UnityEngine.Pool;

namespace MonoBehaviours.Pool
{
    /// <summary>
    /// Wrapper around Unity's ObjectPool with pre-warming support.
    /// Pre-allocates GameObjects at construction time to avoid runtime Instantiate spikes.
    /// </summary>
    public class GameObjectPool
    {
        private readonly ObjectPool<GameObject> pool;

        /// <summary>
        /// Creates a new pool and pre-warms it with the specified number of objects.
        /// </summary>
        /// <param name="prefab">Prefab to instantiate</param>
        /// <param name="parent">Parent transform for pooled objects</param>
        /// <param name="preWarmCount">Number of objects to pre-create</param>
        /// <param name="maxSize">Maximum pool size</param>
        public GameObjectPool(GameObject prefab, Transform parent, int preWarmCount, int maxSize)
        {
            var prefab1 = prefab;
            var parent1 = parent;

            pool = new ObjectPool<GameObject>(
                createFunc: () => Object.Instantiate(prefab1, parent1),
                actionOnGet: obj => obj.SetActive(true),
                actionOnRelease: obj => obj.SetActive(false),
                actionOnDestroy: obj => Object.Destroy(obj),
                collectionCheck: true,
                defaultCapacity: preWarmCount,
                maxSize: maxSize
            );

            // Pre-warm: defaultCapacity does NOT pre-allocate objects.
            // We must manually Get then Release to force creation.
            var preWarmed = new GameObject[preWarmCount];
            for (var i = 0; i < preWarmCount; i++)
                preWarmed[i] = pool.Get();
            for (var i = 0; i < preWarmCount; i++)
                pool.Release(preWarmed[i]);
        }

        /// <summary>
        /// Get an active GameObject from the pool.
        /// </summary>
        public GameObject Get() => pool.Get();

        /// <summary>
        /// Return a GameObject to the pool.
        /// </summary>
        public void Release(GameObject obj) => pool.Release(obj);

        /// <summary>
        /// Number of objects currently in use.
        /// </summary>
        public int CountActive => pool.CountActive;

        /// <summary>
        /// Number of objects available in the pool.
        /// </summary>
        public int CountInactive => pool.CountInactive;
    }
}
