using System.Buffers;
using System.Diagnostics;
using Cestyll.Ecs.Collections;

namespace Cestyll.Ecs.Storage;

// Type-erased sparse set: entity -> packed slot. Public for Pool<T>, but closed to derivation.
public abstract partial class SparseSet<TEntity> : IStore<TEntity>
    where TEntity : unmanaged, IEntity<TEntity>
{
    private protected SparseSet() { }

    // Sparse entry: the entity with its index replaced by slot + 1, so probes check versions without _packed.
    private protected const int MinCapacity = 64;

    private TEntity[] _sparse = new TEntity[MinCapacity];
    private TEntity[] _packed = new TEntity[MinCapacity];

    public int Count { get; private set; }
    public ReadOnlySpan<TEntity> Entities => _packed.AsSpan(0, Count);
    internal ReadOnlySpan<TEntity> SparseSpan => _sparse;
    internal TEntity[] SparseArray => _sparse;
    internal TEntity[] PackedArray => _packed;

    public int Capacity => _packed.Length;
    public int SparseCapacity => _sparse.Length;

    // Group hooks. OnRemoved is for groups that exclude this set.
    internal Action<TEntity>? OnAdded;
    internal Action<TEntity>? OnRemoving;
    internal Action<TEntity>? OnRemoved;
    internal object? Owner; // the GroupChain of the groups that own this set, if any

    // Backs Pool<T>.OnRemove. Fires before OnRemoving, while e is still grouped.
    private protected Action<TEntity>? Destroying;
    internal int DestroyHandlers => Destroying?.GetInvocationList().Length ?? 0;
    private InFlight<TEntity>? _removing;

    // Lets Pool.Add reject stale handles. Null for internal and dropped sets.
    internal Registry<TEntity>? OwnerRegistry;

    // Set as Registry.DropStorage starts; OwnerRegistry clears when it's done.
    internal bool Dropped;

    // Open walks, DEBUG only: reordering the set under a walk throws.
    internal int Walks;

    [Conditional("DEBUG")]
    internal void EnterWalk() => Walks++;

    [Conditional("DEBUG")]
    internal void ExitWalk() => Walks--;

    [Conditional("DEBUG")]
    internal void CheckNotWalked(string change)
    {
        if (Walks > 0)
        {
            throw new InvalidOperationException(
                $"Can't {change} {Describe()} while a walk over it is open: record the change in a "
                + "CommandBuffer, or make it inside Registry.Defer() or an AutoDefer walk (see IEach)");
        }
    }

    // The loop lives here so Release drops it with the calls.
    [Conditional("DEBUG")]
    internal static void CheckNotWalked(ReadOnlySpan<SparseSet<TEntity>> sets, string change)
    {
        foreach (var s in sets)
        {
            s.CheckNotWalked(change);
        }
    }

    private string Describe() =>
        ComponentType is not { } type ? "this set"
        : Name is null ? $"the {type.Name} pool"
        : $"the {type.Name} pool \"{Name}\"";

    // Null for internal sets.
    public virtual Type? ComponentType => null;

    // Null for the default pool and internal sets.
    public string? Name { get; internal init; }

    StoreKind IStore<TEntity>.Kind => StoreKind.Component;
    Type IStore<TEntity>.StoredType => ComponentType!; // internal sets are never registered
    SparseSet<TEntity>? IStore<TEntity>.Owners => this;
    void IStore<TEntity>.EntityDestroyed(TEntity e) => Remove(e); // fires OnDestroy
    void IStore<TEntity>.Compact(double minWaste) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static TEntity Pack(TEntity e, int slot) => TEntity.Make(slot + 1, e.Version);

    // Stale handles miss on the sparse entry alone, without touching _packed.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int Probe(ReadOnlySpan<TEntity> sparse, TEntity e)
    {
        var index = e.Index;
        if ((uint)index >= (uint)sparse.Length)
        {
            return -1;
        }

        var entry = sparse[index];
        var slot1 = entry.Index;
        return slot1 != 0 && TEntity.SameVersion(entry, e) ? slot1 - 1 : -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int IndexOf(TEntity e) => Probe(_sparse, e);

    public bool Has(TEntity e) => IndexOf(e) >= 0;
    bool IStore<TEntity>.Contains(TEntity e) => Has(e);

    // No version check: only for indices known to be live.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int Slot(int index) => Slot(_sparse, index);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int Slot(ReadOnlySpan<TEntity> sparse, int index) =>
        (uint)index < (uint)sparse.Length ? sparse[index].Index - 1 : -1;

    internal TEntity EntityAt(int slot) => _packed[slot];

    // Sparse gets one extra slot: indices start at 1 (0 is Null's).
    public void Reserve(int capacity)
    {
        if (capacity > _packed.Length)
        {
            Array.Resize(ref _packed, capacity);
            ResizeStorage(capacity);
        }
        if (capacity + 1 > _sparse.Length)
        {
            Array.Resize(ref _sparse, capacity + 1);
        }
    }

    // Removes never shrink a set; this trims to Count and the highest index, min 64.
    public void ShrinkToFit()
    {
        CheckNotWalked("shrink");
        var capacity = Math.Max(MinCapacity, Count);
        if (capacity < _packed.Length)
        {
            Array.Resize(ref _packed, capacity);
            ResizeStorage(capacity);
        }

        var top = 0;
        foreach (var e in Entities)
        {
            top = Math.Max(top, e.Index);
        }

        var sparse = Math.Max(MinCapacity, top + 1);
        if (sparse < _sparse.Length)
        {
            Array.Resize(ref _sparse, sparse);
        }
    }

    // Single probe: the duplicate check is the sparse entry itself.
    private protected int Emplace(TEntity e)
    {
        CheckNotWalked("add to");
        var index = e.Index;
        if (index >= _sparse.Length)
        {
            Array.Resize(ref _sparse, Math.Max(index + 1, _sparse.Length * 2));
        }

        ref var entry = ref _sparse[index];
        if (!entry.IsNull)
        {
            var held = _packed[entry.Index - 1];
            throw new InvalidOperationException(
                held == e ? $"{e} is already in this set" : $"Index {index} is held by {held}");
        }
        // slot + 1 must fit the index field.
        if (Count >= TEntity.MaxIndex)
        {
            throw new InvalidOperationException("Set is full");
        }

        if (Count == _packed.Length)
        {
            Array.Resize(ref _packed, Count * 2);
            ResizeStorage(_packed.Length);
        }
        var slot = Count++;
        _packed[slot] = e;
        entry = Pack(e, slot);
        return slot;
    }

    // Swap-and-pop.
    public bool Remove(TEntity e)
    {
        var slot = IndexOf(e);
        if (slot < 0)
        {
            return false;
        }

        if (Destroying is not null)
        {
            if (!FireDestroying(e))
            {
                return false; // its OnRemove is already running
            }

            slot = IndexOf(e);
        }
        if (OnRemoving is { } removing)
        {
            removing(e);        // a group may move e out of its prefix first
            slot = IndexOf(e);
        }

        var last = Count - 1;
        var moved = _packed[last];
        _packed[slot] = moved;
        _sparse[moved.Index] = Pack(moved, slot);
        _sparse[e.Index] = default;
        MoveStorage(last, slot);
        Count--;
        OnRemoved?.Invoke(e);
        return true;
    }

    // Out of line to keep Remove small. False if e's OnRemove is already running.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool FireDestroying(TEntity e)
    {
        if (!(_removing ??= new()).TryEnter(e, out var guard))
        {
            return false;
        }

        using (guard)
        {
            Destroying?.Invoke(e);
        }
        return true;
    }

    // Fires no hooks.
    private protected void ClearSet()
    {
        foreach (var e in Entities)
        {
            _sparse[e.Index] = default;
        }

        Count = 0;
    }

    // For owning groups' shared prefix.
    internal void SwapSlots(int i, int j)
    {
        if (i == j)
        {
            return;
        }

        TEntity ei = _packed[i], ej = _packed[j];
        _packed[i] = ej;
        _packed[j] = ei;
        _sparse[ej.Index] = Pack(ej, i);
        _sparse[ei.Index] = Pack(ei, j);
        SwapStorage(i, j);
    }

    // Sorts pack in reverse, since views walk from the last slot down. Unstable, no signals.

    // Reused so a per-frame sort doesn't allocate.
    private SlotComparer<TEntity>? _entitySorter;

    public void SortEntities(Comparison<TEntity> compare)
    {
        ThrowIfOwned();
        SortSlots([this], 0, Count, EntitySorter(compare, reverse: true));
    }

    internal SlotComparer<TEntity> EntitySorter(Comparison<TEntity> compare, bool reverse) =>
        (_entitySorter ??= new()).Set(_packed, compare, reverse);

    // Shared entities first, in other's view order, then the rest.
    public void SortAs(SparseSet<TEntity> other)
    {
        ThrowIfOwned();
        if (other == this)
        {
            return;
        }

        var rented = ArrayPool<int>.Shared.Rent(Count);
        try
        {
            // Walks run high slot to low, so shared entities go last in other's slot order.
            var order = rented.AsSpan(0, Count);
            var n = 0;
            for (var slot = 0; slot < Count; slot++)
            {
                if (!other.Has(_packed[slot]))
                {
                    order[n++] = slot;
                }
            }
            foreach (var e in other.Entities)
            {
                var slot = IndexOf(e);
                if (slot >= 0)
                {
                    order[n++] = slot;
                }
            }

            Permute([this], order);
        }
        finally
        {
            ArrayPool<int>.Shared.Return(rented);
        }
    }

    // Sorting would break an owning group's prefix.
    internal void ThrowIfOwned()
    {
        if (Owner is not null)
        {
            throw new InvalidOperationException("This pool is owned by a group; sort it through the group");
        }
    }

    // Applies the sorted order of start..end to every set.
    internal static void SortSlots<TItem>(
        ReadOnlySpan<SparseSet<TEntity>> sets, int start, int end, SlotComparer<TItem> comparer)
    {
        var count = end - start;
        var rented = ArrayPool<int>.Shared.Rent(count);
        try
        {
            var order = rented.AsSpan(0, count);
            for (var k = 0; k < count; k++)
            {
                order[k] = start + k;
            }

            order.Sort(comparer.Slots); // a throwing comparison leaves the sets untouched
            Permute(sets, order, start);
        }
        finally
        {
            comparer.Release();
            ArrayPool<int>.Shared.Return(rented);
        }
    }

    // Moves slot order[k] to start + k in every set, cycle by cycle: park one, shift the rest.
    private static void Permute(ReadOnlySpan<SparseSet<TEntity>> sets, Span<int> order, int start = 0)
    {
        CheckNotWalked(sets, "sort");

        for (var p = 0; p < order.Length; p++)
        {
            if (order[p] == start + p)
            {
                continue;
            }

            foreach (var s in sets)
            {
                s.Park(start + p);
            }

            var q = p;
            while (order[q] != start + p)
            {
                var next = order[q];
                foreach (var s in sets)
                {
                    s.MoveSlot(next, start + q);
                }

                order[q] = start + q;
                q = next - start;
            }

            foreach (var s in sets)
            {
                s.Unpark(start + q);
            }

            order[q] = start + q;
        }
    }

    private TEntity _parked;

    private void Park(int slot)
    {
        _parked = _packed[slot];
        ParkStorage(slot);
    }

    private void MoveSlot(int from, int to)
    {
        var e = _packed[from];
        _packed[to] = e;
        _sparse[e.Index] = Pack(e, to);
        MoveStorage(from, to);
    }

    private void Unpark(int slot)
    {
        var e = _parked;
        _packed[slot] = e;
        _sparse[e.Index] = Pack(e, slot);
        UnparkStorage(slot);
    }

    private protected abstract void ResizeStorage(int capacity);
    private protected abstract void MoveStorage(int from, int to);
    private protected abstract void SwapStorage(int i, int j);

    // Holds one slot aside while a sort shifts a cycle.
    private protected abstract void ParkStorage(int slot);
    private protected abstract void UnparkStorage(int slot);
}

// Compares slots by their items. Reused across sorts.
internal sealed class SlotComparer<TItem>
{
    private TItem[] _items = [];
    private int[]? _map;
    private Comparison<TItem>? _compare;
    private bool _reverse;

    // Cached: an IComparer would get wrapped in a fresh delegate every sort.
    public readonly Comparison<int> Slots;

    public SlotComparer() => Slots = Compare;

    // map sorts one set by items stored in another.
    public SlotComparer<TItem> Set(TItem[] items, Comparison<TItem> compare, bool reverse, int[]? map = null)
    {
        (_items, _compare, _reverse, _map) = (items, compare, reverse, map);
        return this;
    }

    public void Release() => (_items, _compare, _map) = ([], null, null);

    private int Compare(int i, int j)
    {
        if (_map is { } map)
        {
            (i, j) = (map[i], map[j]);
        }

        return _reverse ? _compare!(_items[j], _items[i]) : _compare!(_items[i], _items[j]);
    }
}
