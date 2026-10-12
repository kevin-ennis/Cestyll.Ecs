using Cestyll.Ecs.Storage;

namespace Cestyll.Ecs;

// Convenience queries: optional reads, multi-type tests, counts and entity walks.
public partial class Registry<TEntity>
    where TEntity : unmanaged, IEntity<TEntity>
{
    // A copy: safe code can't return a ref that may point at nothing.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGet<T>(TEntity e, out T value)
        where T : IComponent
    {
        if (_bornCount > 0)
        {
            return TryGetBorn(e, out value);
        }

        var pool = FindPool<T>();
        var slot = pool?.IndexOf(e) ?? -1;
        if (slot >= 0)
        {
            value = pool!.At(slot);
            return true;
        }

        value = default!;
        return false;
    }

    // Adds through Add, so OnAdd fires and a stale e throws.
    public ref T GetOrAdd<T>(TEntity e, T value)
        where T : IComponent
    {
        if (_bornCount > 0)
        {
            return ref GetOrAddBorn(e, value);
        }

        var pool = Storage<T>();
        var slot = pool.IndexOf(e);
        return ref slot >= 0 ? ref pool.At(slot) : ref Add(e, value);
    }

    public ref T GetOrAdd<T>(TEntity e) where T : IComponent => ref GetOrAdd<T>(e, default!);

    public bool HasAll<T1, T2>(TEntity e)
        where T1 : IComponent
        where T2 : IComponent =>
        Has<T1>(e) && Has<T2>(e);
    public bool HasAll<T1, T2, T3>(TEntity e)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent =>
        HasAll<T1, T2>(e) && Has<T3>(e);
    public bool HasAll<T1, T2, T3, T4>(TEntity e)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent =>
        HasAll<T1, T2, T3>(e) && Has<T4>(e);
    public bool HasAll<T1, T2, T3, T4, T5>(TEntity e)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent =>
        HasAll<T1, T2, T3, T4>(e) && Has<T5>(e);
    public bool HasAll<T1, T2, T3, T4, T5, T6>(TEntity e)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent =>
        HasAll<T1, T2, T3, T4, T5>(e) && Has<T6>(e);
    public bool HasAll<T1, T2, T3, T4, T5, T6, T7>(TEntity e)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent =>
        HasAll<T1, T2, T3, T4, T5, T6>(e) && Has<T7>(e);
    public bool HasAll<T1, T2, T3, T4, T5, T6, T7, T8>(TEntity e)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent =>
        HasAll<T1, T2, T3, T4, T5, T6, T7>(e) && Has<T8>(e);
    public bool HasAll<T1, T2, T3, T4, T5, T6, T7, T8, T9>(TEntity e)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent =>
        HasAll<T1, T2, T3, T4, T5, T6, T7, T8>(e) && Has<T9>(e);
    public bool HasAll<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(TEntity e)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent =>
        HasAll<T1, T2, T3, T4, T5, T6, T7, T8, T9>(e) && Has<T10>(e);
    public bool HasAll<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(TEntity e)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent =>
        HasAll<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(e) && Has<T11>(e);
    public bool HasAll<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(TEntity e)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent =>
        HasAll<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(e) && Has<T12>(e);
    public bool HasAll<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(TEntity e)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent
        where T13 : IComponent =>
        HasAll<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(e) && Has<T13>(e);
    public bool HasAll<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(TEntity e)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent
        where T13 : IComponent
        where T14 : IComponent =>
        HasAll<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(e) && Has<T14>(e);
    public bool HasAll<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(TEntity e)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent
        where T13 : IComponent
        where T14 : IComponent
        where T15 : IComponent =>
        HasAll<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(e) && Has<T15>(e);
    public bool HasAll<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(TEntity e)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent
        where T13 : IComponent
        where T14 : IComponent
        where T15 : IComponent
        where T16 : IComponent =>
        HasAll<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(e) && Has<T16>(e);

    public bool HasAny<T1, T2>(TEntity e)
        where T1 : IComponent
        where T2 : IComponent =>
        Has<T1>(e) || Has<T2>(e);
    public bool HasAny<T1, T2, T3>(TEntity e)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent =>
        HasAny<T1, T2>(e) || Has<T3>(e);
    public bool HasAny<T1, T2, T3, T4>(TEntity e)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent =>
        HasAny<T1, T2, T3>(e) || Has<T4>(e);
    public bool HasAny<T1, T2, T3, T4, T5>(TEntity e)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent =>
        HasAny<T1, T2, T3, T4>(e) || Has<T5>(e);
    public bool HasAny<T1, T2, T3, T4, T5, T6>(TEntity e)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent =>
        HasAny<T1, T2, T3, T4, T5>(e) || Has<T6>(e);
    public bool HasAny<T1, T2, T3, T4, T5, T6, T7>(TEntity e)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent =>
        HasAny<T1, T2, T3, T4, T5, T6>(e) || Has<T7>(e);
    public bool HasAny<T1, T2, T3, T4, T5, T6, T7, T8>(TEntity e)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent =>
        HasAny<T1, T2, T3, T4, T5, T6, T7>(e) || Has<T8>(e);
    public bool HasAny<T1, T2, T3, T4, T5, T6, T7, T8, T9>(TEntity e)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent =>
        HasAny<T1, T2, T3, T4, T5, T6, T7, T8>(e) || Has<T9>(e);
    public bool HasAny<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(TEntity e)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent =>
        HasAny<T1, T2, T3, T4, T5, T6, T7, T8, T9>(e) || Has<T10>(e);
    public bool HasAny<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(TEntity e)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent =>
        HasAny<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(e) || Has<T11>(e);
    public bool HasAny<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(TEntity e)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent =>
        HasAny<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(e) || Has<T12>(e);
    public bool HasAny<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(TEntity e)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent
        where T13 : IComponent =>
        HasAny<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(e) || Has<T13>(e);
    public bool HasAny<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(TEntity e)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent
        where T13 : IComponent
        where T14 : IComponent =>
        HasAny<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(e) || Has<T14>(e);
    public bool HasAny<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(TEntity e)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent
        where T13 : IComponent
        where T14 : IComponent
        where T15 : IComponent =>
        HasAny<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(e) || Has<T15>(e);
    public bool HasAny<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(TEntity e)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent
        where T13 : IComponent
        where T14 : IComponent
        where T15 : IComponent
        where T16 : IComponent =>
        HasAny<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(e) || Has<T16>(e);

    // Alive and in no store of any kind. Walks every store.
    public bool IsOrphan(TEntity e)
    {
        if (!IsAlive(e))
        {
            return false;
        }

        foreach (var s in CollectionsMarshal.AsSpan(_stores))
        {
            if (s.Contains(e)) // tag sets are index-only, fine once e is known alive
            {
                return false;
            }
        }
        return true;
    }

    // Run-stable hash of T's pool for determinism checks (HashCode is per-process seeded).
    // hash must be stable too: string.GetHashCode is randomized. 0 when there is no pool.
    public ulong Fingerprint<T>(Func<T, ulong> hash)
        where T : IComponent
    {
        ArgumentNullException.ThrowIfNull(hash);
        var pool = FindPool<T>();
        if (pool is null)
        {
            return 0;
        }

        var h = 0xCBF29CE484222325UL;
        var ents = pool.Entities;
        var values = pool.Components;
        for (var i = ents.Length - 1; i >= 0; i--) // view order: last slot first
        {
            h = Mix(h, ((ulong)(uint)ents[i].Index << 32) | ents[i].Version);
            h = Mix(h, hash(values[i]));
        }

        return h;
    }

    // splitmix-style finalizer round.
    private static ulong Mix(ulong h, ulong word)
    {
        var x = (h ^ word) * 0x9E3779B97F4A7C15UL;
        x ^= x >> 31;
        x *= 0xBF58476D1CE4E5B9UL;
        return x ^ (x >> 29);
    }

    // Never creates the pool.
    public int ComponentCount<T>() where T : IComponent => FindPool<T>()?.Count ?? 0;

    // Live entities, including any whose Destroy is still running.
    public int AliveCount => _created - 1 - _freeCount;

    // Ascending index; may destroy any entity. Ones created mid-walk are seen only above the cursor.
    public EntityEnumerator Entities => new(this);

    public void Each(Action<TEntity> action)
    {
        foreach (var e in Entities)
        {
            action(e);
        }
    }

    // Default pools only, in creation order; the walk may add or remove components.
    public ComponentTypeEnumerator GetComponentTypes(TEntity e) => new(this, e);

    // Default and named pools, in creation order.
    public EntityStorageEnumerator GetStorages(TEntity e) => new(this, e);

    public ref struct EntityEnumerator
    {
        private readonly Registry<TEntity> _registry;
        private int _index;

        internal EntityEnumerator(Registry<TEntity> registry) => _registry = registry;

        public TEntity Current { get; private set; }
        public readonly EntityEnumerator GetEnumerator() => this;

        public bool MoveNext()
        {
            var r = _registry;
            while (++_index < r._created)
            {
                if (!r._free.Test(_index))
                {
                    Current = TEntity.Make(_index, r._versions[_index]);
                    return true;
                }
            }
            return false;
        }
    }

    public ref struct ComponentTypeEnumerator
    {
        private readonly Registry<TEntity> _registry;
        private readonly TEntity _entity;
        private int _i;

        internal ComponentTypeEnumerator(Registry<TEntity> registry, TEntity entity)
        {
            _registry = registry;
            _entity = entity;
            _i = -1;
            Current = typeof(void);
        }

        public Type Current { get; private set; }
        public readonly ComponentTypeEnumerator GetEnumerator() => this;

        public bool MoveNext()
        {
            var stores = _registry._stores;
            while (++_i < stores.Count)
            {
                if (stores[_i] is SparseSet<TEntity> { Name: null } pool && pool.Has(_entity))
                {
                    Current = pool.ComponentType!;
                    return true;
                }
            }
            return false;
        }
    }

    public ref struct EntityStorageEnumerator
    {
        private readonly Registry<TEntity> _registry;
        private readonly TEntity _entity;
        private int _i;

        internal EntityStorageEnumerator(Registry<TEntity> registry, TEntity entity)
        {
            _registry = registry;
            _entity = entity;
            _i = -1;
            Current = null!;
        }

        public SparseSet<TEntity> Current { get; private set; }
        public readonly EntityStorageEnumerator GetEnumerator() => this;

        public bool MoveNext()
        {
            var stores = _registry._stores;
            while (++_i < stores.Count)
            {
                if (stores[_i] is SparseSet<TEntity> pool && pool.Has(_entity))
                {
                    Current = pool;
                    return true;
                }
            }

            return false;
        }
    }

    public ref struct StorageEnumerator
    {
        private readonly Registry<TEntity> _registry;
        private int _i;

        internal StorageEnumerator(Registry<TEntity> registry)
        {
            _registry = registry;
            _i = -1;
            Current = null!;
        }

        public SparseSet<TEntity> Current { get; private set; }
        public readonly StorageEnumerator GetEnumerator() => this;

        public bool MoveNext()
        {
            var stores = _registry._stores;
            while (++_i < stores.Count)
            {
                if (stores[_i] is SparseSet<TEntity> pool)
                {
                    Current = pool;
                    return true;
                }
            }

            return false;
        }
    }
}
