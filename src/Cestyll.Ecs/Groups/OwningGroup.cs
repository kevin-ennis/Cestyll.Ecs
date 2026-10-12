using Cestyll.Ecs.Storage;
using Cestyll.Ecs.Tags;

namespace Cestyll.Ecs.Groups;

// The first Count slots of every owned pool hold the same entities in order, so spans align.
// With<T>() pools are required but never reordered. Groups share owned pools only if they nest.
internal sealed partial class OwningGroup<TEntity> : IDisposable
    where TEntity : unmanaged, IEntity<TEntity>
{
    private readonly SparseSet<TEntity>[] _pools; // owned
    private readonly SparseSet<TEntity>[] _gets;
    private readonly SparseSet<TEntity>[] _probe; // owned past the first, then gets
    private readonly SparseSet<TEntity>[] _excludePools;
    private readonly TagSet<TEntity>[] _require;
    private readonly TagSet<TEntity>[] _exclude;
    private readonly int[] _ruleKeys;
    private GroupChain<TEntity>? _chain;

    public int Count { get; private set; }
    public ReadOnlySpan<TEntity> Entities => _pools[0].Entities[..Count];

    internal OwningGroup(SparseSet<TEntity>[] pools, GroupRules<TEntity> rules)
    {
        GroupFilter<TEntity>.Validate(pools, rules);
        _pools = pools;
        _gets = rules.Get;
        _probe = [.. pools.AsSpan(1), .. rules.Get];
        _excludePools = rules.ExcludePools;
        _require = rules.Require;
        _exclude = rules.Exclude;
        _ruleKeys = rules.Keys;

        // Throws before touching anything unless it nests. Inner group members are already in front.
        var chain = GroupChain<TEntity>.For(pools);
        var at = chain.PlaceFor(this);
        Count = chain.CountAt(at);

        // Swaps only touch slots already visited, so walking by index stays valid.
        var smallest = pools[0];
        foreach (var p in _probe)
        {
            if (p.Count < smallest.Count)
            {
                smallest = p;
            }
        }

        for (var i = 0; i < smallest.Count; i++)
        {
            TryAdd(smallest.Entities[i]);
        }

        chain.Insert(at, this);
        _chain = chain;
    }

    internal ReadOnlySpan<SparseSet<TEntity>> Owned => _pools;
    internal ReadOnlySpan<SparseSet<TEntity>> Gets => _gets;
    internal ReadOnlySpan<SparseSet<TEntity>> ExcludedPools => _excludePools;
    internal ReadOnlySpan<TagSet<TEntity>> Required => _require;
    internal ReadOnlySpan<TagSet<TEntity>> Excluded => _exclude;

    internal bool HasRules(ReadOnlySpan<GroupRule> rules) => GroupRules<TEntity>.Match(rules, _ruleKeys);

    // Inner can live in the front of this group's prefix.
    internal bool Encloses(OwningGroup<TEntity> inner)
    {
        foreach (var p in _pools)
        {
            if (Array.IndexOf(inner._pools, p) < 0)
            {
                return false;
            }
        }

        foreach (var p in _gets)
        {
            if (!inner.Uses(p))
            {
                return false;
            }
        }

        foreach (var p in _excludePools)
        {
            if (!inner.Excludes(p))
            {
                return false;
            }
        }

        foreach (var t in _require)
        {
            if (!inner.Requires(t))
            {
                return false;
            }
        }

        foreach (var t in _exclude)
        {
            if (!inner.Excludes(t))
            {
                return false;
            }
        }

        return true;
    }

    internal bool OwnsSuperset(OwningGroup<TEntity> other)
    {
        foreach (var p in other._pools)
        {
            if (Array.IndexOf(_pools, p) < 0)
            {
                return false;
            }
        }

        return true;
    }

    internal bool Owns(SparseSet<TEntity> set) => Array.IndexOf(_pools, set) >= 0;
    internal bool Uses(SparseSet<TEntity> set) => Array.IndexOf(_pools, set) >= 0 || Array.IndexOf(_gets, set) >= 0;
    internal bool Excludes(SparseSet<TEntity> set) => Array.IndexOf(_excludePools, set) >= 0;
    internal bool Requires(TagSet<TEntity> tag) => Array.IndexOf(_require, tag) >= 0;
    internal bool Excludes(TagSet<TEntity> tag) => Array.IndexOf(_exclude, tag) >= 0;

    // A Get pool's slots don't line up with Entities: use GetSlots.
    public Pool<TEntity, T> Storage<T>() where T : IComponent => GroupFilter<TEntity>.Find<T>(_pools, _gets);

    // slots[i] = Entities[i]'s slot in T's pool; never -1.
    public void GetSlots<T>(Span<int> slots) where T : IComponent => Storage<T>().IndexOfMany(Entities, slots);

    // Moves every owned pool. Unstable; holds until membership changes. An inner group's members stay in front.
    public void Sort<T>(Comparison<T> compare)
        where T : IComponent
    {
        var start = SortStart();
        foreach (var p in _pools)
        {
            if (p is Pool<TEntity, T> pool)
            {
                SparseSet<TEntity>.SortSlots(_pools, start, Count, pool.Sorter(compare, reverse: false));
                return;
            }
        }

        throw new InvalidOperationException($"This group doesn't own {typeof(T).Name}");
    }

    public void SortEntities(Comparison<TEntity> compare) =>
        SparseSet<TEntity>.SortSlots(_pools, SortStart(), Count, _pools[0].EntitySorter(compare, reverse: false));

    private int SortStart() =>
        (_chain ?? throw new ObjectDisposedException(GetType().Name)).CountInside(this);

    public void Dispose()
    {
        if (_chain is not { } chain)
        {
            return;
        }

        chain.Remove(this);
        _chain = null;
        Count = 0;
    }

    // Hook targets for a group alone in its chain.
    internal void Join(TEntity e) => TryAdd(e);
    internal void JoinIndex(int index) => TryAddIndex(index);

    // False stops a chain: every group inside requires at least as much.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryAdd(TEntity e)
    {
        var first = _pools[0].IndexOf(e);
        if (first < 0)
        {
            return false;
        }

        if (first < Count)
        {
            return true;
        }

        if (!GroupFilter<TEntity>.Passes(e, _probe, _excludePools, _require, _exclude))
        {
            return false;
        }

        // Empty in Release, yet dropping it measured GroupChurn Without<Job> 17% slower (JIT layout).
        foreach (var p in _pools)
        {
            p.CheckNotWalked("pull an entity into a group that owns");
        }

        foreach (var p in _pools)
        {
            p.SwapSlots(p.IndexOf(e), Count);
        }

        Count++;
        return true;
    }

    internal void TryRemove(TEntity e)
    {
        var first = _pools[0].IndexOf(e);
        if (first < 0 || first >= Count)
        {
            return; // not grouped
        }

        Count--;
        foreach (var p in _pools)
        {
            p.SwapSlots(p.IndexOf(e), Count);
        }
    }

    // Tag hooks deliver a bare index; Destroy removes pools before tags, so pools[0] resolves it.
    internal bool TryAddIndex(int index)
    {
        var slot = _pools[0].Slot(index);
        return slot >= 0 && (slot < Count || TryAdd(_pools[0].EntityAt(slot)));
    }

    internal void TryRemoveIndex(int index)
    {
        var slot = _pools[0].Slot(index);
        if (slot >= 0 && slot < Count)
        {
            TryRemove(_pools[0].EntityAt(slot));
        }
    }
}
