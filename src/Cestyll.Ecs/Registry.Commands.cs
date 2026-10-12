using Cestyll.Ecs.Attachments;
using Cestyll.Ecs.Commands;
using Cestyll.Ecs.Relations;
using Cestyll.Ecs.Storage;

namespace Cestyll.Ecs;

// Defer scopes: structural registry calls are recorded and run in order when the outermost scope ends.
// Reads see the pre-scope state, except keyed reads of entities created in the scope, which see their records.
// Not recorded: Create itself, Replace, Patch, bulk calls, and calls made straight on a store.
public partial class Registry<TEntity>
    where TEntity : unmanaged, IEntity<TEntity>
{
    private int _deferDepth;
    private CommandBuffer<TEntity>? _deferred;

    // Entities created in the scope -> pending count at creation, so reads look back no further.
    private Dictionary<TEntity, int>? _born;
    private int _bornCount;

    // Sets with PendingReads raised, lowered when the scope ends.
    private readonly List<IPendingReads> _pendingReads = [];

    public CommandBuffer<TEntity> CommandBuffer() => new(this);

    // Every view walk is a defer scope; groups and tag/attachment/relation walks need Defer().
    public bool AutoDefer { get; set; }

    public bool IsDeferring => _deferDepth > 0;

    public int PendingCount => _deferred?.Count ?? 0;

    // Scopes nest; the outermost end applies everything recorded.
    public DeferScope<TEntity> Defer()
    {
        _deferDepth++;
        return new(this);
    }

    internal void EndDefer()
    {
        if (_deferDepth == 0)
        {
            throw new InvalidOperationException("No defer scope is open");
        }

        if (--_deferDepth > 0)
        {
            return;
        }

        if (_bornCount > 0)
        {
            _born!.Clear();
            _bornCount = 0;
        }

        foreach (var set in CollectionsMarshal.AsSpan(_pendingReads))
        {
            set.PendingReads = false;
        }

        _pendingReads.Clear();

        if (_deferred is { IsEmpty: false } pending)
        {
            pending.FlushPending();
        }
    }

    // Result goes back to ExitWalk, so toggling AutoDefer mid-walk can't unbalance the depth.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool EnterWalk()
    {
        if (!AutoDefer)
        {
            return false;
        }

        _deferDepth++;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void ExitWalk(bool deferred)
    {
        if (deferred)
        {
            EndDefer();
        }
    }

    private CommandBuffer<TEntity> Deferred => _deferred ??= new(this);

    internal TEntity CreateEntityNow()
    {
        var e = CreateEntity();
        if (_deferDepth > 0)
        {
            (_born ??= []).TryAdd(e, Deferred.Count);
            _bornCount++;
        }

        return e;
    }

    // Pending*Slot results other than a value index.
    internal const int PendingRemoved = -1;
    internal const int NoPending = -2;

    // Index of e's latest pending T, or PendingRemoved, or NoPending.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private int PendingSlot<T>(TEntity e)
        where T : IComponent =>
        _born is not null && _born.TryGetValue(e, out var since) && Deferred.TryFindPending<T>(e, since, out var value)
            ? value
            : NoPending;

    // Reads of born entities: the latest record decides; the pool answers only with no record.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool HasBorn<T>(TEntity e)
        where T : IComponent
    {
        var pending = PendingSlot<T>(e);
        return pending >= 0 || (pending == NoPending && (FindPool<T>()?.Has(e) ?? false));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool TryGetBorn<T>(TEntity e, out T value)
        where T : IComponent
    {
        var pending = PendingSlot<T>(e);
        if (pending >= 0)
        {
            value = Deferred.PendingValue<T>(pending);
            return true;
        }

        var pool = pending == NoPending ? FindPool<T>() : null;
        var slot = pool?.IndexOf(e) ?? -1;
        value = slot >= 0 ? pool!.At(slot) : default!;
        return slot >= 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private ref T GetOrAddBorn<T>(TEntity e, T value)
        where T : IComponent
    {
        var pending = PendingSlot<T>(e);
        if (pending >= 0)
        {
            return ref Deferred.PendingValue<T>(pending);
        }

        if (pending == NoPending)
        {
            var pool = Storage<T>();
            var slot = pool.IndexOf(e);
            if (slot >= 0)
            {
                return ref pool.At(slot);
            }
        }

        return ref Add(e, value); // recorded, after any pending Remove
    }

    // Raises PendingReads so the set's keyed reads see records for born entities.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private ref T RecordAttach<T, TKey>(TEntity e, T item)
        where T : struct, IAttachment<TKey>
        where TKey : notnull
    {
        RaisePendingReads(Attachments<T, TKey>());
        return ref Deferred.PendingAttach<T, TKey>(e, item);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool RecordDetach<T, TKey>(TEntity e, TKey key)
        where T : struct, IAttachment<TKey>
        where TKey : notnull
    {
        var set = Attachments<T, TKey>();
        var has = set.Has(e, key); // before recording: whether it would detach something
        RaisePendingReads(set);
        Deferred.Detach<T, TKey>(e, key);
        return has;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private ref T RecordLink<T>(TEntity source, T link)
        where T : struct, ILink<TEntity>
    {
        RaisePendingReads(Links<T>());
        return ref Deferred.PendingLink(source, link);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool RecordUnlink<T>(TEntity source, TEntity target)
        where T : struct, ILink<TEntity>
    {
        var set = Links<T>();
        var had = set.Has(source, target); // before recording, which Has would then see
        RaisePendingReads(set);
        Deferred.Unlink<T>(source, target);
        return had;
    }

    private void RaisePendingReads(IPendingReads set)
    {
        if (!set.PendingReads)
        {
            set.PendingReads = true;
            _pendingReads.Add(set);
        }
    }

    // As PendingSlot, for source's T link to target.
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal int PendingLinkSlot<T>(TEntity source, TEntity target)
        where T : struct, ILink<TEntity> =>
        _born is not null && _born.TryGetValue(source, out var since)
            && Deferred.TryFindPendingLink<T>(source, target, since, out var link)
            ? link
            : NoPending;

    internal ref T PendingLinkValue<T>(int link)
        where T : struct, ILink<TEntity> =>
        ref Deferred.PendingLinkValue<T>(link);

    // As PendingSlot, for e's item under key.
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal int PendingAttachmentSlot<T, TKey>(TEntity e, TKey key)
        where T : struct, IAttachment<TKey>
        where TKey : notnull =>
        _born is not null && _born.TryGetValue(e, out var since)
            && Deferred.TryFindPendingAttachment<T, TKey>(e, key, since, out var item)
            ? item
            : NoPending;

    internal ref T PendingAttachment<T, TKey>(int item)
        where T : struct, IAttachment<TKey>
        where TKey : notnull =>
        ref Deferred.PendingAttachment<T, TKey>(item);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private ref T GetBorn<T>(TEntity e)
        where T : IComponent
    {
        var pending = PendingSlot<T>(e);
        if (pending >= 0)
        {
            return ref Deferred.PendingValue<T>(pending);
        }

        if (pending == PendingRemoved)
        {
            Throw.NoComponent<TEntity, T>(e);
        }

        return ref RequirePool<T>(e).Get(e);
    }

    // Out of line for the Create overloads' rare path.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private CommandBuffer<TEntity> DeferredBuffer() => Deferred;

}

// Ends a Registry.Defer scope. Dispose once.
public readonly ref struct DeferScope<TEntity>
    where TEntity : unmanaged, IEntity<TEntity>
{
    private readonly Registry<TEntity> _registry;

    internal DeferScope(Registry<TEntity> registry) => _registry = registry;

    public void Dispose() => _registry.EndDefer();
}
