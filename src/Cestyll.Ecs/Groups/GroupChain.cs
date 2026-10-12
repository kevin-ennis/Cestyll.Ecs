using Cestyll.Ecs.Storage;
using Cestyll.Ecs.Tags;

namespace Cestyll.Ecs.Groups;

// Nested owning groups over shared pools, outermost first; each sits in the front of the prefix before it.
// A change affects a suffix of the chain: joins run outer to inner, leaves inner to outer.
internal sealed class GroupChain<TEntity>
    where TEntity : unmanaged, IEntity<TEntity>
{
    private readonly List<OwningGroup<TEntity>> _groups = [];
    private readonly List<Action> _unhook = [];

    internal List<OwningGroup<TEntity>> Groups => _groups;

    // Pools under different chains can't be joined: groups that nest already share one.
    internal static GroupChain<TEntity> For(SparseSet<TEntity>[] pools)
    {
        GroupChain<TEntity>? chain = null;
        foreach (var p in pools)
        {
            if (p.Owner is not GroupChain<TEntity> c)
            {
                continue;
            }

            if (chain is not null && chain != c)
            {
                throw new InvalidOperationException(
                    "This group would own pools of two unrelated groups; groups nest in a single line");
            }

            chain = c;
        }

        return chain ?? new();
    }

    // After every group that encloses it. Throws unless it nests with all.
    internal int PlaceFor(OwningGroup<TEntity> group)
    {
        var at = 0;
        foreach (var g in _groups)
        {
            var outer = g.Encloses(group);
            var inner = group.Encloses(g);
            if (outer && inner)
            {
                throw new InvalidOperationException(
                    "A group over these pools with the same rules exists; ask for it with the same type order");
            }
            if (!outer && !inner)
            {
                throw NotNested(group, g);
            }

            if (outer)
            {
                at++;
            }
        }

        return at;
    }

    // Members already in front of a group placed at `at`.
    internal int CountAt(int at) => at < _groups.Count ? _groups[at].Count : 0;

    // Members of the next group inside, in front of this one's prefix.
    internal int CountInside(OwningGroup<TEntity> group) => CountAt(_groups.IndexOf(group) + 1);

    internal void Insert(int at, OwningGroup<TEntity> group)
    {
        _groups.Insert(at, group);
        foreach (var p in group.Owned)
        {
            p.Owner = this;
        }

        Rewire();
    }

    // The rest still nest.
    internal void Remove(OwningGroup<TEntity> group)
    {
        _groups.Remove(group);
        foreach (var p in group.Owned)
        {
            var stillOwned = false;
            foreach (var g in _groups)
            {
                stillOwned |= g.Owns(p);
            }

            if (!stillOwned)
            {
                p.Owner = null;
            }
        }

        Rewire();
    }

    private void Rewire()
    {
        foreach (var unhook in _unhook)
        {
            unhook();
        }

        _unhook.Clear();

        var sets = new List<SparseSet<TEntity>>();
        var excludedSets = new List<SparseSet<TEntity>>();
        var require = new List<TagSet<TEntity>>();
        var exclude = new List<TagSet<TEntity>>();
        foreach (var g in _groups)
        {
            AddNew(sets, g.Owned);
            AddNew(sets, g.Gets);
            AddNew(excludedSets, g.ExcludedPools);
            AddNew(require, g.Required);
            AddNew(exclude, g.Excluded);
        }

        foreach (var s in sets)
        {
            var (add, remove) = Handlers(FirstUser(g => g.Uses(s)));
            s.OnAdded += add;
            s.OnRemoving += remove;
            _unhook.Add(() =>
            {
                s.OnAdded -= add;
                s.OnRemoving -= remove;
            });
        }

        // Excluded: adding leaves, removing joins.
        foreach (var s in excludedSets)
        {
            var (add, remove) = Handlers(FirstUser(g => g.Excludes(s)));
            s.OnAdded += remove;
            s.OnRemoved += add;
            _unhook.Add(() =>
            {
                s.OnAdded -= remove;
                s.OnRemoved -= add;
            });
        }

        // Required tags join when set; excluded ones the reverse.
        foreach (var t in require)
        {
            HookTag(t, FirstUser(g => g.Requires(t)), join: true);
        }

        foreach (var t in exclude)
        {
            HookTag(t, FirstUser(g => g.Excludes(t)), join: false);
        }
    }

    private (Action<TEntity> Add, Action<TEntity> Remove) Handlers(int from)
    {
        if (from == _groups.Count - 1)
        {
            var g = _groups[from];
            return (g.Join, g.TryRemove);
        }

        var suffix = new Suffix(_groups.GetRange(from, _groups.Count - from));
        return (suffix.Add, suffix.Remove);
    }

    private void HookTag(TagSet<TEntity> t, int from, bool join)
    {
        Action<int> add, remove;
        if (from == _groups.Count - 1)
        {
            var g = _groups[from];
            add = g.JoinIndex;
            remove = g.TryRemoveIndex;
        }
        else
        {
            var suffix = new Suffix(_groups.GetRange(from, _groups.Count - from));
            add = suffix.AddIndex;
            remove = suffix.RemoveIndex;
        }

        Action<int> onSet = join ? add : remove, onClear = join ? remove : add;
        t.OnAdded += onSet;
        t.OnRemoved += onClear;
        _unhook.Add(() =>
        {
            t.OnAdded -= onSet;
            t.OnRemoved -= onClear;
        });
    }

    // First group that depends on something; every group after it does too.
    private int FirstUser(Func<OwningGroup<TEntity>, bool> uses)
    {
        for (var i = 0; i < _groups.Count; i++)
        {
            if (uses(_groups[i]))
            {
                return i;
            }
        }

        throw new InvalidOperationException("Unreachable: nothing in the chain uses this");
    }

    private static void AddNew<T>(List<T> into, ReadOnlySpan<T> items)
    {
        foreach (var item in items)
        {
            if (!into.Contains(item))
            {
                into.Add(item);
            }
        }
    }

    private static InvalidOperationException NotNested(OwningGroup<TEntity> group, OwningGroup<TEntity> existing)
    {
        const string Rule = "Groups can share owned pools only if they nest: one must own every pool the other " +
            "owns, and require and exclude everything the other does.";

        // Common near miss: stricter rules without owning all the looser group owns.
        if (!group.OwnsSuperset(existing) && !existing.OwnsSuperset(group))
        {
            return new(Rule + " Neither owns all of the other's pools: make the stricter one own the rest " +
                "too, or require them with With<T>() instead of owning them.");
        }

        return new(Rule + " These own compatible pools, but their rules differ each way: one must require " +
            "and exclude at least what the other does. Use With<T>() to require a component " +
            "without owning it.");
    }

    // The groups a change can affect, outermost first.
    private sealed class Suffix(List<OwningGroup<TEntity>> groups)
    {
        private readonly OwningGroup<TEntity>[] _groups = [.. groups];

        public void Add(TEntity e)
        {
            foreach (var g in _groups)
            {
                if (!g.TryAdd(e))
                {
                    return; // no group inside it can take e either
                }
            }
        }

        public void Remove(TEntity e)
        {
            for (var i = _groups.Length - 1; i >= 0; i--)
            {
                _groups[i].TryRemove(e);
            }
        }

        public void AddIndex(int index)
        {
            foreach (var g in _groups)
            {
                if (!g.TryAddIndex(index))
                {
                    return;
                }
            }
        }

        public void RemoveIndex(int index)
        {
            for (var i = _groups.Length - 1; i >= 0; i--)
            {
                _groups[i].TryRemoveIndex(index);
            }
        }
    }
}
