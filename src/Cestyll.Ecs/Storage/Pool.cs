namespace Cestyll.Ecs.Storage;

// Component storage parallel to the packed entity array. Refs and spans go stale on any change.
public sealed class Pool<TEntity, T> : SparseSet<TEntity>
    where TEntity : unmanaged, IEntity<TEntity>
    where T : IComponent
{
    private T[] _data = new T[MinCapacity];

    internal Pool() { }

    public override Type ComponentType => typeof(T);

    // OnRemove fires before removal. No removing T inside its OnAdd. Ref writes fire nothing: Patch.
    public event Action<TEntity>? OnAdd;
    public event Action<TEntity>? OnUpdate;
    public event Action<TEntity>? OnRemove
    {
        // SparseSet fires it from Remove.
        add
        {
            Destroying += value;
            OwnerRegistry?.DestroySignalConnected();
        }
        remove
        {
            var before = Destroying;
            Destroying -= value;
            if (Destroying != before)
            {
                OwnerRegistry?.DestroySignalDisconnected();
            }
        }
    }

    public Span<T> Components => _data.AsSpan(0, Count);
    internal T[] DataArray => _data;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T At(int slot) => ref _data[slot];

    public ref T Add(TEntity e, T value)
    {
        // A stale handle would block its recycled index. Dropped first: mid-drop still has a registry.
        if (Dropped)
        {
            throw new InvalidOperationException($"The {typeof(T).Name} pool \"{Name}\" was dropped");
        }

        if (OwnerRegistry is { } registry && !registry.IsCurrent(e))
        {
            Throw.NotAlive(e);
        }

        var slot = Emplace(e); // throws if e already has T
        _data[slot] = value;
        if (OnAdded is null && OnAdd is null)
        {
            return ref _data[slot];
        }

        return ref _data[Constructed(e)];
    }

    // Not atomic: entities before a throw keep T.
    public void Add(ReadOnlySpan<TEntity> entities, T value)
    {
        ReserveMore(entities.Length);
        foreach (var e in entities)
        {
            Add(e, value);
        }
    }

    // Not an Add overload: it would be ambiguous on Registry.
    public void AddValues(ReadOnlySpan<TEntity> entities, ReadOnlySpan<T> values)
    {
        if (values.Length != entities.Length)
        {
            throw new ArgumentException($"{entities.Length} entities but {values.Length} values", nameof(values));
        }

        ReserveMore(entities.Length);
        for (var i = 0; i < entities.Length; i++)
        {
            Add(entities[i], values[i]);
        }
    }

    // Out of line to keep the unhooked Add small.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private int Constructed(TEntity e)
    {
        OnAdded?.Invoke(e); // a group may move it
        OnAdd?.Invoke(e);
        return SlotAfterSignal(e);
    }

    public ref T Get(TEntity e)
    {
        var slot = IndexOf(e);
        if (slot < 0)
        {
            Throw.NoComponent<TEntity, T>(e);
        }

        return ref _data[slot];
    }

    public ref T Replace(TEntity e, T value)
    {
        Get(e) = value;
        return ref Updated(e);
    }

    public ref T AddOrReplace(TEntity e, T value)
    {
        var slot = IndexOf(e);
        if (slot < 0)
        {
            return ref Add(e, value);
        }

        _data[slot] = value;
        return ref Updated(e);
    }

    public void Patch(TEntity e, RefAction<T> edit)
    {
        edit(ref Get(e));
        Updated(e);
    }

    // For a T already changed through a ref.
    public void Patch(TEntity e)
    {
        if (IndexOf(e) < 0)
        {
            Throw.NoComponent<TEntity, T>(e);
        }

        // Unhooked, Updated would only re-probe for a ref we drop.
        if (OnUpdate is not null)
        {
            Updated(e);
        }
    }

    private ref T Updated(TEntity e)
    {
        if (OnUpdate is not { } update)
        {
            return ref _data[IndexOf(e)];
        }

        update(e);
        return ref _data[SlotAfterSignal(e)];
    }

    // A handler may reorder slots, so re-probe; it must not have removed T from e.
    private int SlotAfterSignal(TEntity e)
    {
        var slot = IndexOf(e);
        return slot >= 0
            ? slot
            : throw new InvalidOperationException($"A signal handler removed {typeof(T).Name} from {e}");
    }

    // Drops all at once unless hooked, then one by one so signals fire.
    public void Clear()
    {
        if (Destroying is not null || OnRemoving is not null || OnRemoved is not null)
        {
            RemoveAll();
            return;
        }

        var count = Count;
        ClearSet();
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            Array.Clear(_data, 0, count);
        }
    }

    private SlotComparer<T>? _sorter;
    private T _parked = default!;

    public void Sort(Comparison<T> compare)
    {
        ThrowIfOwned();
        SortSlots([this], 0, Count, Sorter(compare, reverse: true));
    }

    internal SlotComparer<T> Sorter(Comparison<T> compare, bool reverse, int[]? map = null) =>
        (_sorter ??= new()).Set(_data, compare, reverse, map);

    private protected override void ResizeStorage(int capacity) => Array.Resize(ref _data, capacity);

    private protected override void MoveStorage(int from, int to)
    {
        _data[to] = _data[from];
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            _data[from] = default!;
        }
    }

    private protected override void SwapStorage(int i, int j) => (_data[i], _data[j]) = (_data[j], _data[i]);

    private protected override void ParkStorage(int slot) => _parked = _data[slot];

    private protected override void UnparkStorage(int slot)
    {
        _data[slot] = _parked;
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            _parked = default!;
        }
    }
}
