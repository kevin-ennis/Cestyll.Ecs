using Cestyll.Ecs.Storage;
using Cestyll.Ecs.Tags;

namespace Cestyll.Ecs.Groups;

// Keeps its members in its own set and reorders no pool, so it coexists with any owning group.
// Needs at least one Get. Entities goes stale on any membership change.
internal sealed class NonOwningGroup<TEntity> : IDisposable
    where TEntity : unmanaged, IEntity<TEntity>
{
    private readonly EntitySet<TEntity> _members = new();
    private readonly SparseSet<TEntity>[] _gets;
    private readonly SparseSet<TEntity>[] _excludePools;
    private readonly TagSet<TEntity>[] _require;
    private readonly TagSet<TEntity>[] _exclude;
    private readonly int[] _ruleKeys;
    private readonly List<Action> _unhook = [];
    private int[] _sortSlots = []; // Sort's scratch, owned so a repeat sort never allocates

    // So building again returns the same Group.
    internal Group<TEntity>? Handle;
    private Action<NonOwningGroup<TEntity>>? _release;

    internal NonOwningGroup(Registry<TEntity> registry, GroupRules<TEntity> rules, Action<NonOwningGroup<TEntity>> release)
    {
        Registry = registry;
        if (rules.Get.Length == 0)
        {
            throw new ArgumentException("A group needs at least one Own<T>() or With filter");
        }

        GroupFilter<TEntity>.Validate([], rules);
        _gets = rules.Get;
        _excludePools = rules.ExcludePools;
        _require = rules.Require;
        _exclude = rules.Exclude;
        _ruleKeys = rules.Keys;
        _release = release;

        var smallest = _gets[0];
        foreach (var p in _gets)
        {
            if (p.Count < smallest.Count)
            {
                smallest = p;
            }
        }

        foreach (var e in smallest.Entities)
        {
            TryAdd(e);
        }

        foreach (var s in _gets)
        {
            Hook(s, TryAdd, Leave, after: false);
        }

        foreach (var s in _excludePools)
        {
            Hook(s, Leave, TryAdd, after: true);
        }

        foreach (var t in _require)
        {
            Hook(t, TryAddIndex, LeaveIndex);
        }

        foreach (var t in _exclude)
        {
            Hook(t, LeaveIndex, TryAddIndex);
        }
    }

    // For View.Use(group).
    internal SparseSet<TEntity> Members => _members;
    internal Registry<TEntity> Registry { get; }
    internal bool Disposed => _release is null;

    public int Count => _members.Count;
    public ReadOnlySpan<TEntity> Entities => _members.Entities;
    public bool Contains(TEntity e) => _members.Has(e);

    internal bool HasRules(ReadOnlySpan<GroupRule> rules) => GroupRules<TEntity>.Match(rules, _ruleKeys);

    public Pool<TEntity, T> Storage<T>() where T : IComponent => GroupFilter<TEntity>.Find<T>([], _gets);

    // slots[i] = Entities[i]'s slot in T's pool; never -1.
    public void GetSlots<T>(Span<int> slots) where T : IComponent => Storage<T>().IndexOfMany(Entities, slots);

    // Reorder members only, no pool moves. Unstable; holds until membership changes.
    public void Sort<T>(Comparison<T> compare)
        where T : IComponent
    {
        var pool = Storage<T>();
        if (_sortSlots.Length < Count)
        {
            _sortSlots = new int[Math.Max(Count, _sortSlots.Length * 2)];
        }

        pool.IndexOfMany(Entities, _sortSlots);
        SparseSet<TEntity>.SortSlots([_members], 0, Count, pool.Sorter(compare, reverse: false, _sortSlots));
    }

    public void SortEntities(Comparison<TEntity> compare) =>
        SparseSet<TEntity>.SortSlots([_members], 0, Count, _members.EntitySorter(compare, reverse: false));

    public void ShrinkToFit() => _members.ShrinkToFit();

    public void Dispose()
    {
        if (_release is not { } release)
        {
            return;
        }

        foreach (var unhook in _unhook)
        {
            unhook();
        }

        _unhook.Clear();
        _members.Clear();
        _release = null;
        release(this);
    }

    private void Hook(SparseSet<TEntity> s, Action<TEntity> onAdd, Action<TEntity> onRemove, bool after)
    {
        s.OnAdded += onAdd;
        if (after)
        {
            s.OnRemoved += onRemove;
        }
        else
        {
            s.OnRemoving += onRemove;
        }

        _unhook.Add(() =>
        {
            s.OnAdded -= onAdd;
            if (after)
            {
                s.OnRemoved -= onRemove;
            }
            else
            {
                s.OnRemoving -= onRemove;
            }
        });
    }

    private void Hook(TagSet<TEntity> t, Action<int> onSet, Action<int> onClear)
    {
        t.OnAdded += onSet;
        t.OnRemoved += onClear;
        _unhook.Add(() =>
        {
            t.OnAdded -= onSet;
            t.OnRemoved -= onClear;
        });
    }

    private void TryAdd(TEntity e)
    {
        if (!_members.Has(e) && GroupFilter<TEntity>.Passes(e, _gets, _excludePools, _require, _exclude))
        {
            _members.Add(e);
        }
    }

    private void Leave(TEntity e) => _members.Remove(e);

    // Tag hooks deliver a bare index; any Get pool resolves it.
    private void TryAddIndex(int index)
    {
        var slot = _gets[0].Slot(index);
        if (slot >= 0)
        {
            TryAdd(_gets[0].EntityAt(slot));
        }
    }

    private void LeaveIndex(int index)
    {
        var slot = _members.Slot(index);
        if (slot >= 0)
        {
            _members.Remove(_members.EntityAt(slot));
        }
    }
}
