using Cestyll.Ecs.Collections;
using Cestyll.Ecs.Storage;

namespace Cestyll.Ecs.Tags;

// One bit per entity index; the Registry checks liveness. Walks cost O(highest index / 64).
public sealed class TagSet<TEntity> : IStore<TEntity>
    where TEntity : unmanaged, IEntity<TEntity>
{
    private readonly EntityBitset _bits = new();
    private readonly Registry<TEntity> _registry;

    internal TagSet(Registry<TEntity> registry, Type tagType)
    {
        _registry = registry;
        TagType = tagType;
    }

    public Type TagType { get; }

    public int Count { get; private set; }

    // Raw words for custom bitwise queries.
    public ReadOnlySpan<ulong> Words => _bits.Words;
    internal ulong[] WordsArray => _bits.WordsArray;

    internal bool Test(int index) => _bits.Test(index);

    // Applied tags only, even in a defer scope.
    public bool Contains(TEntity e) => _registry.IsAlive(e) && Test(e.Index);

    // Untagging never shrinks the bitset; this trims it. Words spans go stale.
    public void ShrinkToFit() => _bits.ShrinkToFit();

    StoreKind IStore<TEntity>.Kind => StoreKind.Tag;
    Type IStore<TEntity>.StoredType => TagType;
    string? IStore<TEntity>.Name => null;
    SparseSet<TEntity>? IStore<TEntity>.Owners => null;
    bool IStore<TEntity>.Contains(TEntity e) => Contains(e);
    void IStore<TEntity>.EntityDestroyed(TEntity e) => Remove(e);
    void IStore<TEntity>.Compact(double minWaste) { }
    void IStore<TEntity>.Reserve(int entities) { }

    internal BitEnumerator Indices => _bits.GetEnumerator();

    // Group hooks, fired after the bit changes. Any number of groups may subscribe.
    internal Action<int>? OnAdded;
    internal Action<int>? OnRemoved;

    // OnTag fires after groups react; OnUntag before the bit clears, so HasTag is still true.
    public event Action<TEntity>? OnTag;
    public event Action<TEntity>? OnUntag
    {
        add
        {
            _untag += value;
            _registry.DestroySignalConnected();
        }
        remove
        {
            var before = _untag;
            _untag -= value;
            if (_untag != before)
            {
                _registry.DestroySignalDisconnected();
            }
        }
    }

    private Action<TEntity>? _untag;
    private InFlight<TEntity>? _removing;

    internal bool Add(TEntity e)
    {
        var index = e.Index;
        if (_bits.Test(index))
        {
            return false;
        }

        _bits.Set(index);
        Count++;
        if (OnAdded is not null || OnTag is not null)
        {
            FireAdded(e);
        }
        return true;
    }

    // Out of line so Add stays small when nothing listens.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void FireAdded(TEntity e)
    {
        OnAdded?.Invoke(e.Index);
        OnTag?.Invoke(e);
    }

    // False if e's OnUntag is already running further up the stack.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool FireUntag(TEntity e)
    {
        if (!(_removing ??= new()).TryEnter(e, out var guard))
        {
            return false;
        }

        using (guard)
        {
            _untag?.Invoke(e);
        }
        return true;
    }

    // Clears all bits at once if nothing listens; otherwise false, and the caller removes each.
    internal bool TryClearQuietly()
    {
        if (_untag is not null || OnRemoved is not null)
        {
            return false;
        }

        _bits.Clear();
        Count = 0;
        return true;
    }

    internal bool Remove(TEntity e)
    {
        var index = e.Index;
        if (!_bits.Test(index))
        {
            return false;
        }

        if (_untag is not null && !FireUntag(e))
        {
            return false; // its OnUntag is already running
        }

        _bits.Unset(index);
        Count--;
        OnRemoved?.Invoke(index);
        return true;
    }
}
