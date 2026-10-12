using System.Buffers;
using Cestyll.Ecs.Collections;
using Cestyll.Ecs.Storage;
using Cestyll.Ecs.Tags;

namespace Cestyll.Ecs.Views;

// The loops a view runs; ViewQuery picks one and supplies the tag mask.
internal interface IViewWalker<TEntity>
    where TEntity : unmanaged, IEntity<TEntity>
{
    int MinCount { get; }

    // driven: a group's members or a target's sources when the query is Driven, else empty.
    void Unfiltered(ReadOnlySpan<TEntity> driven);

    // Drive from driven, else the smallest or pinned pool, last slot first; tags tested as one word mask.
    void PoolDriven(
        ReadOnlySpan<TEntity> driven, ReadOnlySpan<ulong> mask, MaskMode mode, ViewFilters<TEntity> filters,
        bool componentFilters);

    // Walk the mask's set bits, probing by index; mask bits are live entities only, so that's exact.
    void Masked(ReadOnlySpan<ulong> mask, ViewFilters<TEntity> filters, bool componentFilters);
}

// The filters resolved for one Each.
internal readonly ref struct ViewFilters<TEntity>(
    ReadOnlySpan<Filter<TEntity>> with, ReadOnlySpan<Filter<TEntity>> without)
    where TEntity : unmanaged, IEntity<TEntity>
{
    public readonly ReadOnlySpan<Filter<TEntity>> With = with;
    public readonly ReadOnlySpan<Filter<TEntity>> Without = without;

    // Per-entity filters only; other tags go through the word mask.
    public bool PassComponents(TEntity e)
    {
        foreach (var f in With)
        {
            if (f.PerEntity && !f.Has(e))
            {
                return false;
            }
        }

        foreach (var f in Without)
        {
            if (f.PerEntity && f.Has(e))
            {
                return false;
            }
        }

        return true;
    }
}

// A view's With/Without filters; drives from the smallest pool, or a tag mask when more selective.
// Order depends only on the registry calls made; replays rely on it, so changing it is breaking.
internal readonly struct ViewQuery<TEntity>(
    Registry<TEntity> registry, ViewFilter<TEntity>[] with, ViewFilter<TEntity>[] without, int driver,
    SparseSet<TEntity>? driveSet = null, TargetKey<TEntity>? driveKey = null)
    where TEntity : unmanaged, IEntity<TEntity>
{
    // Type ids, resolved at every Each so storage created after the view still counts.
    private readonly ViewFilter<TEntity>[] _with = with;
    private readonly ViewFilter<TEntity>[] _without = without;

    // Pinned pool index, -1 for the smallest, or TagDriver (UseTag).
    public readonly int Driver = driver;

    public const int TagDriver = -2;

    // Use(group) or WithRelationTarget(target): the members or the sources drive, every pool probed.
    public const int SetDriver = -3;

    public readonly SparseSet<TEntity>? DriveSet = driveSet;
    public readonly TargetKey<TEntity>? DriveKey = driveKey;

    public bool Driven => Driver == SetDriver;

    // Resolved once per walk, since the members and sources change.
    private ReadOnlySpan<TEntity> DriveEntities =>
        !Driven ? default : DriveSet is { } set ? set.Entities : DriveKey!.Sources;

    public ViewQuery(Registry<TEntity> registry) : this(registry, [], [], -1) { }

    public Registry<TEntity> Registry => registry;

    public ViewQuery<TEntity> With<T>(ViewFilterKind kind, KeyTest<TEntity>? key = null) =>
        new(registry, [.. _with, new(TypeId<T>.Value, kind, Type: typeof(T), Key: key)], _without, Driver, DriveSet, DriveKey);

    public ViewQuery<TEntity> Without<T>(ViewFilterKind kind, KeyTest<TEntity>? key = null) =>
        new(registry, _with, [.. _without, new(TypeId<T>.Value, kind, Type: typeof(T), Key: key)], Driver, DriveSet, DriveKey);

    // The first target drives unless something else already does; later ones only test.
    public ViewQuery<TEntity> WithTargetKey<T>(ViewFilterKind kind, TargetKey<TEntity> key)
    {
        ViewFilter<TEntity>[] with = [.. _with, new(TypeId<T>.Value, kind, Type: typeof(T), Key: key)];
        if (Driver == -1)
        {
            return new(registry, with, _without, SetDriver, null, key);
        }

        return new(registry, with, _without, Driver, DriveSet, DriveKey);
    }

    // A live tag filter makes T's tag set, so a tag first used mid-walk is still seen.
    public ViewQuery<TEntity> WithTag<T>(bool live) where T : struct, ITag
    {
        if (!live)
        {
            return With<T>(ViewFilterKind.Tag);
        }

        registry.Tags<T>();
        return With<T>(ViewFilterKind.LiveTag);
    }

    public ViewQuery<TEntity> WithoutTag<T>(bool live) where T : struct, ITag
    {
        if (!live)
        {
            return Without<T>(ViewFilterKind.Tag);
        }

        registry.Tags<T>();
        return Without<T>(ViewFilterKind.LiveTag);
    }

    // Tests this pool itself (e.g. a named one), not the type's default.
    public ViewQuery<TEntity> With(SparseSet<TEntity> pool)
    {
        registry.CheckOwns(pool);
        return new(registry, [.. _with, new(-1, ViewFilterKind.Pool, pool)], _without, Driver, DriveSet, DriveKey);
    }

    public ViewQuery<TEntity> Without(SparseSet<TEntity> pool)
    {
        registry.CheckOwns(pool);
        return new(registry, _with, [.. _without, new(-1, ViewFilterKind.Pool, pool)], Driver, DriveSet, DriveKey);
    }

    // No pin. Requiring what's excluded throws here; excluding a fetched pool throws at Each.
    public ViewQuery<TEntity> Join(ViewQuery<TEntity> other)
    {
        if (registry is null || registry != other.Registry)
        {
            throw new ArgumentException("The views belong to different registries", nameof(other));
        }

        var joined = new ViewQuery<TEntity>(
            registry, Union(_with, other._with), Union(_without, other._without), -1);
        joined.RejectContradictions([]);
        return joined;
    }

    public ViewQuery<TEntity> Unpinned() => new(registry, _with, _without, -1);

    // Reuses an input array when nothing is added.
    private static ViewFilter<TEntity>[] Union(ViewFilter<TEntity>[] a, ViewFilter<TEntity>[] b)
    {
        if (b.Length == 0)
        {
            return a;
        }

        if (a.Length == 0)
        {
            return b;
        }

        var extra = 0;
        foreach (var f in b)
        {
            if (Array.IndexOf(a, f) < 0)
            {
                extra++;
            }
        }

        if (extra == 0)
        {
            return a;
        }

        var union = new ViewFilter<TEntity>[a.Length + extra];
        a.CopyTo(union, 0);
        var n = a.Length;
        foreach (var f in b)
        {
            if (Array.IndexOf(a, f) < 0)
            {
                union[n++] = f;
            }
        }

        return union;
    }

    internal void CheckJoin(scoped ReadOnlySpan<SparseSet<TEntity>> pools) => RejectContradictions(pools);

    // Throws if nothing could match. Compared by resolved storage, so by-type and With(pool) forms meet.
    private void RejectContradictions(scoped ReadOnlySpan<SparseSet<TEntity>> pools)
    {
        foreach (var excluded in _without)
        {
            var set = registry.FilterFor(excluded).Set;
            if (set is not null && pools.IndexOf(set) >= 0)
            {
                throw new InvalidOperationException(
                    $"The joined view both walks and excludes {Describe(excluded)}, so it would never match");
            }

            foreach (var required in _with)
            {
                if (SameStorage(required, excluded))
                {
                    throw new InvalidOperationException(
                        $"The joined view both requires and excludes {Describe(excluded)}, so it would never match");
                }
            }
        }
    }

    private bool SameStorage(ViewFilter<TEntity> a, ViewFilter<TEntity> b)
    {
        if (a == b)
        {
            return true;
        }

        var x = registry.FilterFor(a);
        var y = registry.FilterFor(b);
        return (x.Set is not null && x.Set == y.Set) || (x.Tags is not null && x.Tags == y.Tags);
    }

    private static string Describe(ViewFilter<TEntity> filter) =>
        filter.Pool is { Name: { } name } pool ? $"{pool.ComponentType!.Name} \"{name}\""
        : (filter.Type ?? filter.Pool!.ComponentType!).Name;

    // Rows() with no components: a group, tag pin or With filter must drive. Live tags and keys only test.
    public bool HasDriver
    {
        get
        {
            if (DriveSet is not null || Driver == TagDriver)
            {
                return true;
            }

            foreach (var w in _with)
            {
                if (w.Kind is not (ViewFilterKind.LiveTag or ViewFilterKind.AttachmentKey))
                {
                    return true;
                }
            }

            return false;
        }
    }

    // The smallest required store with a set, or null when only tags can drive.
    public SparseSet<TEntity>? SmallestRequiredSet()
    {
        SparseSet<TEntity>? best = null;
        foreach (var w in _with)
        {
            if (registry.FilterFor(w).Set is { } set && (best is null || set.Count < best.Count))
            {
                best = set;
            }
        }

        return best;
    }

    // Walks the tag mask whatever the pool sizes.
    public ViewQuery<TEntity> UseTag<T>() where T : struct, ITag
    {
        var filter = new ViewFilter<TEntity>(TypeId<T>.Value, ViewFilterKind.Tag, Type: typeof(T));
        var with = Array.IndexOf(_with, filter) >= 0 ? _with : [.. _with, filter];
        return new(registry, with, _without, TagDriver);
    }

    // Walks the group's members, last first, probing every pool.
    public ViewQuery<TEntity> Use(Groups.NonOwningGroup<TEntity> group)
    {
        ArgumentNullException.ThrowIfNull(group);
        if (group.Disposed)
        {
            throw new ObjectDisposedException(nameof(Groups.NonOwningGroup<TEntity>));
        }

        if (group.Registry != registry)
        {
            throw new ArgumentException("The group belongs to another registry", nameof(group));
        }

        return new(registry, _with, _without, SetDriver, group.Members);
    }

    // pools are the view's pools in order; pool must be one of them.
    public ViewQuery<TEntity> Use(SparseSet<TEntity> pool, params ReadOnlySpan<SparseSet<TEntity>> pools)
    {
        ArgumentNullException.ThrowIfNull(pool);
        for (var d = 0; d < pools.Length; d++)
        {
            if (pools[d] == pool)
            {
                return new(registry, _with, _without, d);
            }
        }

        throw new InvalidOperationException("This view doesn't walk that pool");
    }

    // With AutoDefer the walk is a defer scope; pools release it before the scope's commands run.
    public void Run<TWalker>(ref TWalker walker, params ReadOnlySpan<SparseSet<TEntity>> pools)
        where TWalker : struct, IViewWalker<TEntity>, allows ref struct
    {
        var driven = DriveEntities;
#if !DEBUG
        // Nothing to close afterwards: no scope, and Release counts no pools.
        if (!registry.AutoDefer)
        {
            RunWalk(ref walker, driven, pools);
            return;
        }
#endif

        using var scope = WalkScope<TEntity>.Enter(registry, pools, DriveSet);
        RunWalk(ref walker, driven, pools);
    }

    private void RunWalk<TWalker>(
        ref TWalker walker, ReadOnlySpan<TEntity> driven, scoped ReadOnlySpan<SparseSet<TEntity>> pools)
        where TWalker : struct, IViewWalker<TEntity>, allows ref struct
    {
        if (_without.Length == 0 && AllWalked(_with, pools))
        {
            walker.Unfiltered(driven); // the common case pays nothing for filters
            return;
        }

        var plan = Plan(walker.MinCount, pools);
        try
        {
            switch (plan.Kind)
            {
                case ViewPlanKind.PoolDriven:
                    walker.PoolDriven(driven, plan.Mask, plan.Mode, plan.Filters, plan.ComponentFilters);
                    break;
                case ViewPlanKind.Masked:
                    walker.Masked(plan.Mask, plan.Filters, plan.ComponentFilters);
                    break;
            }
        }
        finally
        {
            plan.Dispose();
        }
    }

    // Every With is a walked pool or the driving target, so the walk already tests it.
    private bool AllWalked(ReadOnlySpan<ViewFilter<TEntity>> with, scoped ReadOnlySpan<SparseSet<TEntity>> pools)
    {
        foreach (var w in with)
        {
            if (w.Key is not null && w.Key == DriveKey)
            {
                continue;
            }

            if (w.Kind is not (ViewFilterKind.Component or ViewFilterKind.Pool)
                || registry.FilterFor(w).Set is not { } set || pools.IndexOf(set) < 0)
            {
                return false;
            }
        }

        return true;
    }

    // minCount is the driving pool's size, weighed against a tag mask unless pinned.
    public ViewPlan<TEntity> Plan(int minCount, scoped ReadOnlySpan<SparseSet<TEntity>> walked = default)
    {
        var plan = new ViewPlan<TEntity>();
        if (_with.Length == 0 && _without.Length == 0)
        {
            plan.Kind = ViewPlanKind.Unfiltered;
            return plan;
        }

        // Rented per call so nested and parallel walks are safe; cleared on return so it roots nothing.
        var rented = ArrayPool<Filter<TEntity>>.Shared.Rent(_with.Length + _without.Length);
        plan.RentedFilters = rented;
        // A With on a walked pool, or the driving target, is dropped: the walk already holds it.
        var kept = 0;
        foreach (var w in _with)
        {
            if (w.Key is not null && w.Key == DriveKey)
            {
                continue;
            }

            var f = registry.FilterFor(w);
            if (f.Set is null || walked.IndexOf(f.Set) < 0)
            {
                rented[kept++] = f;
            }
        }

        var with = rented.AsSpan(0, kept);
        var without = rented.AsSpan(kept, _without.Length);
        for (var i = 0; i < without.Length; i++)
        {
            without[i] = registry.FilterFor(_without[i]);
        }

        plan.Filters = new ViewFilters<TEntity>(with, without);
        if (with.Length == 0 && without.Length == 0)
        {
            plan.Kind = ViewPlanKind.PoolDriven;
            return plan;
        }

        // A pinned pool or group always drives; a pinned tag always masks.
        PlanFiltered(ref plan, Driver >= 0 || Driver == SetDriver ? 0 : Driver == TagDriver ? int.MaxValue : minCount);
        return plan;
    }

    private static void PlanFiltered(ref ViewPlan<TEntity> plan, int minCount)
    {
        var filters = plan.Filters;
        TagSet<TEntity>? onlyTag = null;
        int requiredTags = 0, excludedTags = 0, words = int.MaxValue;
        var componentFilters = false;
        foreach (var f in filters.With)
        {
            if (!f.Exists)
            {
                return; // nobody has a required type: stays Empty
            }

            if (f.PerEntity)
            {
                componentFilters = true;
            }
            else
            {
                requiredTags++;
                onlyTag = f.Tags;
                words = Math.Min(words, f.Tags!.Words.Length);
            }
        }
        foreach (var f in filters.Without)
        {
            if (!f.PerEntity)
            {
                excludedTags++;
            }
            else if (f.Exists)
            {
                componentFilters = true;
            }
        }

        plan.ComponentFilters = componentFilters;
        if (requiredTags == 0)
        {
            plan.Kind = ViewPlanKind.PoolDriven;
            if (excludedTags == 0)
            {
                return;
            }

            // Only exclusions: drive from the pool and reject the excluded tags' union.
            plan.Mode = MaskMode.Exclude;
            TagSet<TEntity>? onlyExcluded = null;
            var exclWords = 0;
            foreach (var f in filters.Without)
            {
                if (!f.PerEntity && f.Tags is { } t)
                {
                    onlyExcluded = t;
                    exclWords = Math.Max(exclWords, t.Words.Length);
                }
            }

            if (excludedTags == 1)
            {
                plan.Mask = onlyExcluded!.Words;
                plan.MaskArray = onlyExcluded.WordsArray;
                return;
            }

            var exclRented = ArrayPool<ulong>.Shared.Rent(exclWords);
            plan.RentedMask = exclRented;
            var excl = exclRented.AsSpan(0, exclWords);
            excl.Clear();
            foreach (var f in filters.Without)
            {
                if (!f.PerEntity && f.Tags is { } t)
                {
                    WordOps.Or(excl, t.Words);
                }
            }

            plan.Mask = excl;
            plan.MaskArray = exclRented;
            return;
        }

        // A single required tag and no exclusions: its own words are the mask, no copy.
        if (requiredTags == 1 && excludedTags == 0)
        {
            PlanMasked(ref plan, onlyTag!.Words, onlyTag.WordsArray, onlyTag.Count, minCount);
            return;
        }

        // AND required tags, ANDN excluded; rented per call so nested walks are safe.
        var rented = ArrayPool<ulong>.Shared.Rent(words);
        plan.RentedMask = rented;
        var mask = rented.AsSpan(0, words);
        var first = true;
        foreach (var f in filters.With)
        {
            if (f.PerEntity || f.Tags is not { } t)
            {
                continue;
            }

            if (first)
            {
                t.Words[..words].CopyTo(mask);
                first = false;
            }
            else
            {
                WordOps.And(mask, t.Words);
            }
        }
        foreach (var f in filters.Without)
        {
            if (!f.PerEntity && f.Tags is { } t)
            {
                WordOps.AndNot(mask, t.Words);
            }
        }

        PlanMasked(ref plan, mask, rented, WordOps.PopCount(mask), minCount);
    }

    // Drive from the mask when smaller, else from the pool with one bit test per entity.
    private static void PlanMasked(ref ViewPlan<TEntity> plan, ReadOnlySpan<ulong> mask, ulong[] array, int maskCount, int minCount)
    {
        plan.Mask = mask;
        plan.MaskArray = array;
        if (maskCount >= minCount)
        {
            plan.Kind = ViewPlanKind.PoolDriven;
            plan.Mode = MaskMode.Include;
            return;
        }

        plan.Kind = ViewPlanKind.Masked;
    }
}

internal enum ViewPlanKind : byte { Empty, Unfiltered, PoolDriven, Masked }

// How one walk runs. Owns its rented arrays until Dispose.
internal ref struct ViewPlan<TEntity>
    where TEntity : unmanaged, IEntity<TEntity>
{
    public ViewPlanKind Kind;
    public ReadOnlySpan<ulong> Mask;
    public ulong[]? MaskArray; // the array behind Mask, for a cursor that outlives the span
    public MaskMode Mode;
    public ViewFilters<TEntity> Filters;
    public bool ComponentFilters;
    public Filter<TEntity>[]? RentedFilters;
    public ulong[]? RentedMask;

    public void Dispose()
    {
        if (RentedFilters is not null)
        {
            ArrayPool<Filter<TEntity>>.Shared.Return(RentedFilters, clearArray: true);
        }

        if (RentedMask is not null)
        {
            ArrayPool<ulong>.Shared.Return(RentedMask);
        }

        this = default; // Empty, and a second Dispose returns nothing twice
    }
}

// One foreach's state. A class so the enumerator holds one reference the JIT keeps in a
// register; a bigger struct gets spilled every step. Reused per thread.
internal class ViewCursor<TEntity>
    where TEntity : unmanaged, IEntity<TEntity>
{
    // The driving set at walk start; Next's slot indexes Packed.
    public TEntity[] Packed = [];
    public int Count;

    // Pinned, else smallest; pool 1 on masked walks; -1 when a group's members drive.
    public int Driver;

    // No per-entity test: the enumerator counts down Packed and never calls Next.
    public bool Simple;

    // Simple, and pool 1 drives: a View1 walk then needs no probe, so its step is a countdown.
    public bool Plain;

    // No sets: a tags-only Rows() walks Mask itself rather than calling Next.
    public bool TagsOnly;

    // Probe slots for enumerators too wide to keep them in fields (4-16 pools, View3's pool 1).
    public int J1, J2, J3, J4, J5, J6, J7, J8, J9, J10, J11, J12, J13, J14, J15, J16;

    private ViewPlanKind _kind;
    private MaskMode _mode;
    private bool _componentFilters;
    private Filter<TEntity>[]? _rentedFilters;
    private int _withCount;
    private int _withoutCount;
    private ulong[]? _mask;
    private int _maskWords;
    private ulong[]? _rentedMask;
    private TEntity[]? _rentedSources; // a target's sources, copied at Bind
    private TEntity[] _sparse = []; // pool 1's, for masked walks
    private bool _masked;
    private int _wordEnd;
    private int _i; // pool-driven: slots left to visit; masked: next word
    private ulong _bits;
    private Registry<TEntity>? _registry;
    private bool _deferred; // this walk opened a defer scope, which Dispose ends
#if DEBUG
    private WalkedPools<TEntity> _walked; // the pools counting this walk, until Dispose
    private int _walkedCount;
#endif

    public void Bind(ViewQuery<TEntity> query, scoped ReadOnlySpan<SparseSet<TEntity>> sets)
    {
        var d = query.Driver;
        var driving = query.DriveSet;
        if (query.Driven || sets.Length == 0)
        {
            d = -1;
        }
        else if (d < 0)
        {
            d = 0;
            for (var k = 1; k < sets.Length; k++)
            {
                if (sets[k].Count < sets[d].Count)
                {
                    d = k;
                }
            }
        }

        if (!query.Driven && sets.Length > 0)
        {
            driving = sets[d];
        }

        // Packed indexes from zero, so a target's sources, which sit mid-slab, are copied.
        var packed = driving?.PackedArray ?? [];
        var count = driving?.Count ?? 0;
        if (driving is null && query.Driven)
        {
            var sources = query.DriveKey!.Sources;
            if (sources.Length > 0)
            {
                packed = _rentedSources = ArrayPool<TEntity>.Shared.Rent(sources.Length);
                sources.CopyTo(packed);
            }

            count = sources.Length;
        }

        // No sets means a tags-only Rows(): the plan masks, or is Empty.
        var plan = query.Plan(driving?.Count ?? int.MaxValue, sets);
        _kind = plan.Kind;
        _mode = plan.Mode;
        _componentFilters = plan.ComponentFilters;
        _rentedFilters = plan.RentedFilters;
        _withCount = plan.Filters.With.Length;
        _withoutCount = plan.Filters.Without.Length;
        _mask = plan.MaskArray;
        _maskWords = plan.Mask.Length;
        _rentedMask = plan.RentedMask;
        _masked = false;
        TagsOnly = false;
        _bits = 0;
        switch (_kind)
        {
            case ViewPlanKind.Masked when sets.Length == 0:
                TagsOnly = true;
                break;
            case ViewPlanKind.Masked:
                d = 0;
                driving = sets[0];
                packed = driving.PackedArray;
                count = driving.Count;
                _masked = true;
                _sparse = sets[0].SparseArray;
                _wordEnd = Math.Min(_maskWords, (_sparse.Length + 63) >> 6);
                _i = 0;
                break;
            case ViewPlanKind.Empty:
                _i = 0;
                break;
            default:
                _i = count;
                break;
        }

        Simple = _kind == ViewPlanKind.Unfiltered
            || (_kind == ViewPlanKind.PoolDriven && _mode == MaskMode.None && !_componentFilters);
        Driver = d;
        Plain = Simple && d == 0;
        Packed = packed;
        Count = count;
        _registry = query.Registry;
        _deferred = _registry.EnterWalk();
#if DEBUG
        foreach (var set in sets)
        {
            set.EnterWalk();
            _walked[_walkedCount++] = set;
        }

        if (query.DriveSet is { } members)
        {
            members.EnterWalk();
            _walked[_walkedCount++] = members;
        }
#endif
    }

    // Next passing slot in Packed, or -1. Out of line to keep the Simple loop lean.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public int Next()
    {
        if (_masked)
        {
            return NextMasked();
        }

        var include = _mode == MaskMode.Include;
        var mask = _mask.AsSpan(0, _maskWords);
        while (_i > 0)
        {
            var i = --_i;
            var e = Packed[i];
            if (_mode != MaskMode.None && WordOps.Test(mask, e.Index) != include)
            {
                continue;
            }

            if (_componentFilters && !PassComponents(e))
            {
                continue;
            }

            return i;
        }

        return -1;
    }

    private int NextMasked()
    {
        var mask = _mask.AsSpan(0, _maskWords);
        while (true)
        {
            while (_bits == 0)
            {
                if (_i >= _wordEnd)
                {
                    return -1;
                }

                _bits = mask[_i++];
            }

            var index = ((_i - 1) << 6) + BitOperations.TrailingZeroCount(_bits);
            _bits &= _bits - 1;

            var j = SparseSet<TEntity>.Slot(_sparse, index);
            if (j < 0)
            {
                continue;
            }

            if (!_componentFilters || PassComponents(Packed[j]))
            {
                return j;
            }
        }
    }

    public ReadOnlySpan<ulong> Mask => _mask.AsSpan(0, _maskWords);
    public bool ComponentFilters => _componentFilters;

    public bool PassComponents(TEntity e)
    {
        var filters = new ViewFilters<TEntity>(
            _rentedFilters.AsSpan(0, _withCount), _rentedFilters.AsSpan(_withCount, _withoutCount));
        return filters.PassComponents(e);
    }

    // A second call does nothing.
    public void Dispose()
    {
        if (_rentedFilters is not null)
        {
            ArrayPool<Filter<TEntity>>.Shared.Return(_rentedFilters, clearArray: true);
            _rentedFilters = null;
        }

        if (_rentedMask is not null)
        {
            ArrayPool<ulong>.Shared.Return(_rentedMask);
            _rentedMask = null;
        }

        if (_rentedSources is not null)
        {
            ArrayPool<TEntity>.Shared.Return(_rentedSources);
            _rentedSources = null;
        }

        _mask = null;
        _sparse = [];
        Packed = [];
        Count = 0;
        Simple = false;
        Plain = false;
        TagsOnly = false;
        _i = 0;
        _bits = 0;
#if DEBUG
        while (_walkedCount > 0)
        {
            _walked[--_walkedCount]!.ExitWalk();
        }
#endif
        if (_deferred)
        {
            _deferred = false; // a second Dispose ends nothing twice
            _registry!.ExitWalk(true);
        }

        _registry = null;
    }
}

#if DEBUG
// Walked pools, held without allocating so Dispose can release them.
[InlineArray(17)] // sixteen pools and a group's members
internal struct WalkedPools<TEntity>
    where TEntity : unmanaged, IEntity<TEntity>
{
    private SparseSet<TEntity>? _pool;
}
#endif

// Which pool drives; typeof(TDrive) folds per instantiation, so each driver gets its own loop.
internal struct Drive1 { }
internal struct Drive2 { }
internal struct Drive3 { }
internal struct Drive4 { }
internal struct Drive5 { }
internal struct Drive6 { }
internal struct Drive7 { }
internal struct Drive8 { }
internal struct Drive9 { }
internal struct Drive10 { }
internal struct Drive11 { }
internal struct Drive12 { }
internal struct Drive13 { }
internal struct Drive14 { }
internal struct Drive15 { }
internal struct Drive16 { }

// Use(group): no pool drives, so every pool is probed.
internal struct DriveNone { }

// One With/Without rule. A Pool filter names its pool outright, for pools a type id can't find.
internal readonly record struct ViewFilter<TEntity>(
    int TypeId, ViewFilterKind Kind, SparseSet<TEntity>? Pool = null, Type? Type = null, KeyTest<TEntity>? Key = null)
    where TEntity : unmanaged, IEntity<TEntity>;

internal enum ViewFilterKind : byte
{
    Component, Tag, Attachment, AttachmentKey, Inbox, Pool, LiveTag, RelationTargets, RelationSources, LinkTargets,
    LinkSources, RelationTarget, LinkTarget,
}
