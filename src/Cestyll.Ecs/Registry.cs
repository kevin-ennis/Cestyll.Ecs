using System.Diagnostics.CodeAnalysis;
using Cestyll.Ecs.Collections;
using Cestyll.Ecs.Commands;
using Cestyll.Ecs.Storage;
using Cestyll.Ecs.Views;

namespace Cestyll.Ecs;

// Entity lifecycle and component storage; the rest lives in the Registry.*.cs partials.
//   global using Registry = Cestyll.Ecs.Registry<Cestyll.Ecs.Entity32>;
public sealed partial class Registry<TEntity>
    where TEntity : unmanaged, IEntity<TEntity>
{
    private TypeSlots<SparseSet<TEntity>> _pools = new();

    // Every store, in IStore.DestroyOrder then creation order; Destroy and the other walks use this.
    // Replaced, not edited, when a store is added: open walks keep their span.
    private List<IStore<TEntity>> _stores = [];

    // The pools, also first in _stores; Destroy calls them directly (the interface call cost 14% there).
    private readonly List<SparseSet<TEntity>> _poolList = [];

    // Also in _stores.
    private readonly Dictionary<(int Type, string Name), SparseSet<TEntity>> _namedPools = [];

    // _created is the index high-water mark; starts at 1 since index 0 is the null entity.
    private uint[] _versions = new uint[64];
    private int _created = 1;

    // Create takes the lowest free index, keeping live indices dense after churn.
    private readonly EntityBitset _free = new();
    private int _freeCount;
    private int _freeHint; // every word of _free below this is zero

    // Mid-Destroy entities, so a re-entrant Destroy can't free one twice.
    private readonly EntityBitset _dying = new();
    private int _destroying;
    // Connected handlers Destroy can fire.
    private int _destroySignals;

    internal void DestroySignalConnected() => _destroySignals++;
    internal void DestroySignalDisconnected() => _destroySignals--;
    internal int DestroySignals => _destroySignals;

    public Registry() => CheckLayout();

    // A bad layout would corrupt storage silently; the JIT folds this away for built-ins.
    private static void CheckLayout()
    {
        var max = TEntity.MaxIndex;
        var top = TEntity.Make(max, TEntity.VersionMask);
        if (TEntity.Make(0, 0) != default || !default(TEntity).IsNull || max < 1
            || top.Index != max || top.Version != TEntity.VersionMask
            || TEntity.Make(1, TEntity.VersionMask + 1).Version != 0
            || !TEntity.SameVersion(TEntity.Make(1, 0), TEntity.Make(max, 0))
            || (TEntity.VersionMask != 0 && TEntity.SameVersion(TEntity.Make(1, 0), TEntity.Make(1, 1))))
        {
            throw new InvalidOperationException($"{typeof(TEntity).Name} breaks the IEntity layout rules");
        }
    }

    public EntityHandle<TEntity> Create() => new(this, CreateEntityNow());

    private TEntity CreateEntity()
    {
        if (_freeCount > 0)
        {
            var index = _free.TakeLowest(ref _freeHint);
            _freeCount--;
            return TEntity.Make(index, _versions[index]);
        }
        if (_created > TEntity.MaxIndex)
        {
            ThrowIndexSpaceExhausted();
        }

        if (_created == _versions.Length)
        {
            Array.Resize(ref _versions, _created * 2);
        }

        return TEntity.Make(_created++, 0);
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowIndexSpaceExhausted()
    {
        var message = $"Entity index space exhausted: {typeof(TEntity).Name} allows {TEntity.MaxIndex:N0} " +
            "live entities, and every index is in use.";
        if (typeof(TEntity) == typeof(Entity32))
        {
            message += $" Registry<Entity64> allows {int.MaxValue:N0}, at 8 bytes a handle instead of 4.";
        }

        throw new InvalidOperationException(message);
    }

    // Recreates hint's id if its index is free, else a plain Create; compare with hint.
    // A far index allocates up to it, so bound untrusted input.
    public EntityHandle<TEntity> CreateAt(TEntity hint) => new(this, CreateEntityAt(hint));

    private TEntity CreateEntityAt(TEntity hint)
    {
        var index = hint.Index;
        if (index == 0)
        {
            return CreateEntity();
        }

        // Unsigned: catches indices past Array.MaxLength and negative Entity64 ones.
        if ((uint)index >= (uint)Array.MaxLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(hint), $"Index {index} is outside 1 to {Array.MaxLength - 1}");
        }

        if (index < _created)
        {
            if (!_free.Test(index))
            {
                return CreateEntity();
            }

            // Clearing a bit keeps _freeHint's invariant, so it stays put.
            _free.Unset(index);
            _freeCount--;
        }
        else
        {
            // Allocate before any bookkeeping, so OOM leaves the registry intact.
            if (index >= _versions.Length)
            {
                var grown = (int)Math.Min(Array.MaxLength, Math.Max(index + 1L, _versions.Length * 2L));
                Array.Resize(ref _versions, grown);
            }

            _free.SetRange(_created, index);
            _freeCount += index - _created;
            _freeHint = Math.Min(_freeHint, _created >> 6);
            _created = index + 1;
        }

        _versions[index] = hint.Version;
        return TEntity.Make(index, hint.Version);
    }

    // Pool.Add's cheap check: no free-bit test, so a fabricated handle can pass. Loaders use IsAlive.
    internal bool IsCurrent(TEntity e)
    {
        var index = e.Index;
        return (uint)(index - 1) < (uint)(_created - 1) && _versions[index] == e.Version
            && (_destroying == 0 || !_dying.Test(index));
    }

    public bool IsAlive(TEntity e) =>
        (uint)(e.Index - 1) < (uint)(_created - 1) && _versions[e.Index] == e.Version && !_free.Test(e.Index);

    // Current versions by index, for walks over tag bits.
    internal uint[] Versions => _versions;

    // Not mid-Destroy, so an OnRemove handler can't attach something that outlives e.
    internal bool CanAttachTo(TEntity e) => IsAlive(e) && (_destroying == 0 || !_dying.Test(e.Index));

    public void Destroy(TEntity e)
    {
        if (!IsAlive(e))
        {
            return;
        }

        if (_deferDepth > 0)
        {
            Deferred.Destroy(e);
            return;
        }

        // No handlers means nothing can re-enter, so skip the guard.
        if (_destroySignals == 0)
        {
            DestroyParts(e);
        }
        else if (!DestroyGuarded(e))
        {
            return;
        }

        if (_observerList.Count > 0)
        {
            foreach (var observer in CollectionsMarshal.AsSpan(_observerList))
            {
                observer.EntityDestroyed(e);
            }
        }

        _versions[e.Index] = (_versions[e.Index] + 1) & TEntity.VersionMask;
        _free.Set(e.Index);
        _freeCount++;
        _freeHint = Math.Min(_freeHint, e.Index >> 6);
    }

    // False if e is already mid-Destroy up the stack.
    private bool DestroyGuarded(TEntity e)
    {
        var index = e.Index;
        if (_destroying > 0 && _dying.Test(index))
        {
            return false;
        }

        _dying.Set(index);
        _destroying++;
        try
        {
            DestroyParts(e);
        }
        finally
        {
            _dying.Unset(index);
            _destroying--;
        }
        return true;
    }

    private void DestroyParts(TEntity e)
    {
        // Unlinking e as a source first breaks cycles: cascades only follow source links.
        // Spans are safe if handlers add stores: a new store can't hold a dying e.
        if (_relationList.Count > 0)
        {
            foreach (var r in CollectionsMarshal.AsSpan(_relationList))
            {
                r.SourceDestroyed(e);
            }

            foreach (var r in CollectionsMarshal.AsSpan(_relationList))
            {
                r.TargetDestroyed(e, this); // may recurse
            }
        }
        foreach (var pool in CollectionsMarshal.AsSpan(_poolList))
        {
            pool.Remove(e); // fires OnRemove
        }

        foreach (var s in CollectionsMarshal.AsSpan(_stores)[_poolList.Count..])
        {
            s.EntityDestroyed(e); // relation stores were done above
        }
    }

    // Rare, so a scan and a copy are fine.
    private void AddStore(IStore<TEntity> store)
    {
        var order = store.DestroyOrder;
        var old = CollectionsMarshal.AsSpan(_stores);
        var at = old.Length;
        while (at > 0 && old[at - 1].DestroyOrder > order)
        {
            at--;
        }

        var stores = new List<IStore<TEntity>>(old.Length + 1);
        stores.AddRange(old[..at]);
        stores.Add(store);
        stores.AddRange(old[at..]);
        _stores = stores;
    }

    public Pool<TEntity, T> Storage<T>()
        where T : IComponent
    {
        ref var slot = ref _pools.Slot(TypeId<T>.Value);
        if (slot is null)
        {
            Markers.RequireOne(typeof(T));
            var pool = new Pool<TEntity, T> { OwnerRegistry = this };
            slot = pool;
            AddStore(pool);
            _poolList.Add(pool);
        }
        return (Pool<TEntity, T>)slot;
    }

    // A second pool of T under a name, made on first use; Add/Get/Has use only the default.
    // Every pool costs each Destroy a probe: keep names few, and keep the returned pool.
    public Pool<TEntity, T> Storage<T>(string name)
        where T : IComponent
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var key = (TypeId<T>.Value, name);
        if (!_namedPools.TryGetValue(key, out var pool))
        {
            Markers.RequireOne(typeof(T));
            pool = new Pool<TEntity, T> { OwnerRegistry = this, Name = name };
            _namedPools.Add(key, pool);
            AddStore(pool);
            _poolList.Add(pool);
        }

        return (Pool<TEntity, T>)pool;
    }

    // Clears and forgets a named pool, firing OnRemove; it stays readable but empty.
    // Not during Destroy or a walk over the pools.
    public bool DropStorage<T>(string name)
        where T : IComponent
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (_destroying > 0)
        {
            throw new InvalidOperationException("Can't drop a pool while an entity is being destroyed");
        }

        // Free the name first, so a handler below can reuse it.
        if (!_namedPools.Remove((TypeId<T>.Value, name), out var pool))
        {
            return false;
        }

        // Cleared while still listed, so a handler's Destroy of another member removes it here.
        pool.Dropped = true;
        try
        {
            ((Pool<TEntity, T>)pool).Clear();
        }
        finally
        {
            _stores.Remove(pool);
            _poolList.Remove(pool);
            _destroySignals -= pool.DestroyHandlers; // the dropped pool no longer reports removals
            pool.OwnerRegistry = null;
        }

        pool.ShrinkToFit();
        return true;
    }

    // Never creates.
    public Pool<TEntity, T>? FindStorage<T>() where T : IComponent => FindPool<T>();

    public Pool<TEntity, T>? FindStorage<T>(string name)
        where T : IComponent
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _namedPools.TryGetValue((TypeId<T>.Value, name), out var pool) ? (Pool<TEntity, T>)pool : null;
    }

    // Walks every pool; keep the result.
    public SparseSet<TEntity>? FindStorage(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return FindStorageIn(type, null);
    }

    public SparseSet<TEntity>? FindStorage(Type type, string name)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return FindStorageIn(type, name);
    }

    private SparseSet<TEntity>? FindStorageIn(Type type, string? name)
    {
        foreach (var s in CollectionsMarshal.AsSpan(_stores))
        {
            // A pool mid-drop is still listed beside its replacement.
            if (s is SparseSet<TEntity> pool && pool.ComponentType == type && pool.Name == name && !pool.Dropped)
            {
                return pool;
            }
        }

        return null;
    }

    // Default and named pools in creation order, including ones made mid-walk.
    public StorageEnumerator Storages => new(this);

    // Queries use this so they never create a pool Destroy would walk forever.
    private Pool<TEntity, T>? FindPool<T>()
        where T : IComponent
    {
        return (Pool<TEntity, T>?)_pools.Find(TypeId<T>.Value);
    }

    // Never creates storage; called per Each, so storage made after the view is seen.
    internal Filter<TEntity> FilterFor(ViewFilter<TEntity> filter)
    {
        var id = filter.TypeId;
        return filter.Kind switch
        {
            ViewFilterKind.Component => new(_pools.Find(id), null),
            ViewFilterKind.Tag => new(null, _tags.Find(id)),
            ViewFilterKind.LiveTag => new(null, _tags.Find(id), live: true),
            ViewFilterKind.Pool => new(filter.Pool, null),
            ViewFilterKind.Inbox => new(FindInboxes(id)?.Owners, null),
            ViewFilterKind.AttachmentKey => new(null, null, key: filter.Key),
            ViewFilterKind.RelationTargets => new(FindRelationStore(id)?.HasTargets, null),
            ViewFilterKind.RelationSources => new(FindRelationStore(id)?.HasSources, null),
            ViewFilterKind.RelationTarget => new(FindRelationStore(id)?.HasTargets, null, key: filter.Key),
            ViewFilterKind.LinkTargets => new(FindLinkStore(id)?.HasTargets, null),
            ViewFilterKind.LinkSources => new(FindLinkStore(id)?.HasSources, null),
            ViewFilterKind.LinkTarget => new(FindLinkStore(id)?.HasTargets, null, key: filter.Key),
            _ => new(FindAttachments(id)?.Owners, null),
        };
    }

    // Another registry's ids would mean different entities here.
    internal void CheckOwns(SparseSet<TEntity> pool)
    {
        ArgumentNullException.ThrowIfNull(pool);
        if (pool.OwnerRegistry != this || pool.Dropped)
        {
            throw new ArgumentException("The pool belongs to another registry, or was dropped", nameof(pool));
        }
    }

    // Returned refs go stale on the next structural change: grows, removes and sorts move slots.
    public ref T Add<T>(TEntity e, T value)
        where T : IComponent
    {
        if (_deferDepth > 0)
        {
            return ref Record(CommandOp.Add, e, value);
        }

        return ref Storage<T>().Add(e, value);
    }

    // Also finds pending components of an entity created in a defer scope.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T Get<T>(TEntity e)
        where T : IComponent
    {
        if (_bornCount > 0)
        {
            return ref GetBorn<T>(e);
        }

        return ref RequirePool<T>(e).Get(e);
    }

    // Fires OnUpdate.
    public ref T Replace<T>(TEntity e, T value) where T : IComponent => ref RequirePool<T>(e).Replace(e, value);
    public ref T AddOrReplace<T>(TEntity e, T value)
        where T : IComponent
    {
        if (_deferDepth > 0)
        {
            return ref Record(CommandOp.AddOrReplace, e, value);
        }

        return ref Storage<T>().AddOrReplace(e, value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private ref T Record<T>(CommandOp op, TEntity e, T value)
        where T : IComponent =>
        ref Deferred.Pending(op, e, value);
    public void Patch<T>(TEntity e, RefAction<T> edit) where T : IComponent => RequirePool<T>(e).Patch(e, edit);
    public void Patch<T>(TEntity e) where T : IComponent => RequirePool<T>(e).Patch(e);

    private Pool<TEntity, T> RequirePool<T>(TEntity e) where T : IComponent
    {
        var pool = FindPool<T>();
        if (pool is null)
        {
            Throw.NoComponent<TEntity, T>(e);
        }

        return pool;
    }

    // A group-owned pool throws; sort it through the group.
    public void Sort<T>(Comparison<T> compare) where T : IComponent => FindPool<T>()?.Sort(compare);
    public void SortEntities<T>(Comparison<TEntity> compare)
        where T : IComponent =>
        FindPool<T>()?.SortEntities(compare);

    // Entities shared with TOther come first, in TOther's order.
    public void SortAs<T, TOther>()
        where T : IComponent
        where TOther : IComponent
    {
        if (FindPool<T>() is not { } pool)
        {
            return;
        }

        if (FindPool<TOther>() is { } other)
        {
            pool.SortAs(other);
        }
        else
        {
            pool.ThrowIfOwned(); // nothing to follow, but owned pools throw either way
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Has<T>(TEntity e)
        where T : IComponent =>
        _bornCount > 0 ? HasBorn<T>(e) : FindPool<T>()?.Has(e) ?? false;
    public bool Remove<T>(TEntity e)
        where T : IComponent
    {
        if (_deferDepth > 0)
        {
            var had = Has<T>(e); // before recording, which Has would then see
            Deferred.Remove<T>(e);
            return had;
        }

        return FindPool<T>()?.Remove(e) ?? false;
    }

    // Pools created later aren't sized; +1 since index 0 is unused.
    public void Reserve(int entities)
    {
        if (entities + 1 > _versions.Length)
        {
            Array.Resize(ref _versions, entities + 1);
        }

        foreach (var s in CollectionsMarshal.AsSpan(_stores))
        {
            s.Reserve(entities);
        }
    }

    // Removes never give memory back; Compact and ShrinkToFit do, when the caller chooses.

    // Skips slabs whose gap fraction is under minWaste; pools are always dense.
    public void Compact(double minWaste = 0)
    {
        foreach (var s in CollectionsMarshal.AsSpan(_stores))
        {
            s.Compact(minWaste);
        }
    }

    // Not during a walk.
    public void ShrinkToFit()
    {
        var versions = Math.Max(64, _created);
        if (versions < _versions.Length)
        {
            Array.Resize(ref _versions, versions);
        }

        _free.ShrinkToFit();
        _dying.ShrinkToFit();
        foreach (var observer in CollectionsMarshal.AsSpan(_observerList))
        {
            observer.ShrinkToFit();
        }

        foreach (var group in CollectionsMarshal.AsSpan(_nonOwningGroups))
        {
            group.ShrinkToFit();
        }

        foreach (var s in CollectionsMarshal.AsSpan(_stores))
        {
            s.ShrinkToFit();
        }
    }
}
