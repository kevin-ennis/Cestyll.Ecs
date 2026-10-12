using Cestyll.Ecs.Collections;
using Cestyll.Ecs.Snapshots;
using Cestyll.Ecs.Storage;

namespace Cestyll.Ecs.Relations;

// Many-target links: out lists per source, in lists per target.
// Each entry holds its twin's slot, so unlink swap-removes both sides in O(1).
public sealed class LinkSet<TEntity, T> : RelationStore<TEntity>
    where TEntity : unmanaged, IEntity<TEntity>
    where T : struct, ILink<TEntity>
{
    internal struct Out
    {
        public T Link;
        public int InSlot;
    }

    private readonly SlabLists<TEntity, Out> _out = new();
    // Sources per target, as plain entities for view walks; Twins holds each one's slot in its out-list.
    private readonly SlabLists<TEntity, TEntity> _in = new(twins: true);
    private readonly Registry<TEntity> _registry;

    internal LinkSet(Registry<TEntity> registry) => _registry = registry;

    // OnUnlink fires before removal, so Get still reads the link. Writes through refs fire nothing.
    public event PairHandler<TEntity>? OnLink;
    public event PairHandler<TEntity>? OnUpdate;
    public event PairHandler<TEntity>? OnUnlink
    {
        add
        {
            _unlink += value;
            _registry.DestroySignalConnected();
        }
        remove
        {
            var before = _unlink;
            _unlink -= value;
            if (_unlink != before)
            {
                _registry.DestroySignalDisconnected();
            }
        }
    }

    private PairHandler<TEntity>? _unlink;
    private InFlight<(TEntity, TEntity)>? _removing;

    public ReadOnlySpan<TEntity> Sources => _out.Owners.Entities;
    public ReadOnlySpan<TEntity> Targets => _in.Owners.Entities;

    // Both slabs together.
    public override int SlabUsed => _out.Used + _in.Used;
    public override int SlabGaps => _out.Gaps + _in.Gaps;
    public override int SlabCapacity => _out.Length + _in.Length;

    public override void Compact(double minWaste = 0)
    {
        _out.Compact(minWaste);
        _in.Compact(minWaste);
    }

    public override void ShrinkToFit()
    {
        _out.ShrinkToFit();
        _in.ShrinkToFit();
    }

    public int TargetCount(TEntity source) => _out.Count(source);
    public int SourceCount(TEntity target) => _in.Count(target);

    internal override bool Involves(TEntity e) => _out.IndexOf(e) >= 0 || _in.IndexOf(e) >= 0;
    internal override Type StoredType => typeof(T);
    internal override bool IsLinkSet => true;
    internal override SparseSet<TEntity> HasTargets => _out.Owners;
    internal override SparseSet<TEntity> HasSources => _in.Owners;

    // In a defer scope, keyed reads also see recorded links; counts and walks don't.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Has(TEntity source, TEntity target) =>
        PendingReads ? HasPending(source, target) : HasApplied(source, target);

    // Out of line to keep the usual paths small. The latest record wins over applied links.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool HasPending(TEntity source, TEntity target)
    {
        var pending = _registry.PendingLinkSlot<T>(source, target);
        return pending >= 0 || (pending == Registry<TEntity>.NoPending && HasApplied(source, target));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private ref T GetPending(TEntity source, TEntity target)
    {
        var pending = _registry.PendingLinkSlot<T>(source, target);
        if (pending >= 0)
        {
            return ref _registry.PendingLinkValue<T>(pending);
        }

        var outs = _out.Span(source);
        var o = pending == Registry<TEntity>.NoPending ? IndexOf(outs, target) : -1;
        if (o < 0)
        {
            Throw.NoLink<TEntity, T>(source, target);
        }

        return ref outs[o].Link;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool TryGetPending(TEntity source, TEntity target, out T link)
    {
        var pending = _registry.PendingLinkSlot<T>(source, target);
        if (pending >= 0)
        {
            link = _registry.PendingLinkValue<T>(pending);
            return true;
        }

        var outs = _out.Span(source);
        var o = pending == Registry<TEntity>.NoPending ? IndexOf(outs, target) : -1;
        link = o >= 0 ? outs[o].Link : default;
        return o >= 0;
    }

    // Linked for real, not only recorded.
    internal bool HasApplied(TEntity source, TEntity target)
    {
        var outs = _out.Span(source);
        var ins = _in.Span(target);
        if (outs.Length <= ins.Length)
        {
            return IndexOf(outs, target) >= 0;
        }

        return ins.IndexOf(source) >= 0;
    }

    // Replaces the data of an existing link to link.Target.
    public ref T Link(TEntity source, T link)
    {
        var target = link.Target;
        if (!_registry.CanAttachTo(source))
        {
            Throw.NotAlive(source);
        }

        if (!_registry.CanAttachTo(target))
        {
            Throw.NotAlive(target);
        }

        var outs = _out.Span(source);
        var o = IndexOf(outs, target);
        if (o >= 0)
        {
            ref var existing = ref outs[o].Link;
            existing = link;
            if (OnUpdate is not { } update)
            {
                return ref existing;
            }

            return ref Fire(update, source, target);
        }

        _in.Append(target, source, out var inSlot, twin: outs.Length);
        ref var added = ref _out.Append(source, new Out { Link = link, InSlot = inSlot }, out _).Link;
        if (OnLink is not { } linked)
        {
            return ref added;
        }

        return ref Fire(linked, source, target);
    }

    // Out of line so Link stays small when nothing listens.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private ref T Fire(PairHandler<TEntity> handler, TEntity source, TEntity target)
    {
        handler(source, target);
        return ref GetAfterSignal(source, target);
    }

    // False if the pair's OnUnlink is already running further up the stack.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool FireUnlink(TEntity source, TEntity target)
    {
        if (!(_removing ??= new()).TryEnter((source, target), out var guard))
        {
            return false;
        }

        using (guard)
        {
            _unlink?.Invoke(source, target);
        }
        return true;
    }

    public void Patch(TEntity source, TEntity target, RefAction<T> edit)
    {
        edit(ref Get(source, target));
        OnUpdate?.Invoke(source, target);
    }

    // For a link already changed through a ref.
    public void Patch(TEntity source, TEntity target)
    {
        if (!Has(source, target))
        {
            Throw.NoLink<TEntity, T>(source, target);
        }

        OnUpdate?.Invoke(source, target);
    }

    public bool Unlink(TEntity source, TEntity target)
    {
        var h = _out.IndexOf(source);
        var o = h < 0 ? -1 : IndexOf(_out.SpanAt(h), target);
        if (o < 0)
        {
            return false;
        }

        if (_unlink is not null)
        {
            if (!FireUnlink(source, target))
            {
                return false; // its OnUnlink is already running
            }

            h = _out.IndexOf(source); // the handler may have moved it
            o = IndexOf(_out.SpanAt(h), target);
        }

        RemoveOut(h, o);
        return true;
    }

    // Returns how many there were.
    public int UnlinkTargets(TEntity source)
    {
        if (_unlink is not null)
        {
            return UnlinkEach(source, outgoing: true);
        }

        var h = _out.IndexOf(source);
        if (h < 0)
        {
            return 0;
        }

        foreach (var o in _out.SpanAt(h))
        {
            RemoveIn(_in.IndexOf(o.Link.Target), o.InSlot);
        }

        return _out.RemoveAllAt(h);
    }

    // Returns how many there were.
    public int UnlinkSources(TEntity target)
    {
        if (_unlink is not null)
        {
            return UnlinkEach(target, outgoing: false);
        }

        var h = _in.IndexOf(target);
        if (h < 0)
        {
            return 0;
        }

        var ins = _in.SpanAt(h);
        var outSlots = _in.TwinSpanAt(h);
        for (var k = 0; k < ins.Length; k++)
        {
            RemoveOutOnly(_out.IndexOf(ins[k]), outSlots[k]);
        }

        return _in.RemoveAllAt(h);
    }

    // With handlers: one at a time from the end, skipping pairs mid-OnUnlink; the start count bounds re-links.
    private int UnlinkEach(TEntity e, bool outgoing)
    {
        var count = outgoing ? _out.Count(e) : _in.Count(e);
        for (var k = count; k > 0; k--)
        {
            if (outgoing)
            {
                var outs = _out.Span(e);
                if (outs.Length == 0)
                {
                    break;
                }

                Unlink(e, outs[Math.Min(k, outs.Length) - 1].Link.Target);
            }
            else
            {
                var ins = _in.Span(e);
                if (ins.Length == 0)
                {
                    break;
                }

                Unlink(ins[Math.Min(k, ins.Length) - 1], e);
            }
        }

        return count;
    }

    // A handler may move the link's block, so look it up again; it must not have unlinked it.
    private ref T GetAfterSignal(TEntity source, TEntity target)
    {
        var outs = _out.Span(source);
        var o = IndexOf(outs, target);
        if (o < 0)
        {
            throw new InvalidOperationException(
                $"A signal handler unlinked the {typeof(T).Name} link from {source} to {target}");
        }

        return ref outs[o].Link;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T Get(TEntity source, TEntity target)
    {
        if (PendingReads)
        {
            return ref GetPending(source, target);
        }

        var outs = _out.Span(source);
        var o = IndexOf(outs, target);
        if (o < 0)
        {
            Throw.NoLink<TEntity, T>(source, target);
        }

        return ref outs[o].Link;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGet(TEntity source, TEntity target, out T link)
    {
        if (PendingReads)
        {
            return TryGetPending(source, target, out link);
        }

        var outs = _out.Span(source);
        var o = IndexOf(outs, target);
        link = o >= 0 ? outs[o].Link : default;
        return o >= 0;
    }

    // Stale after any link, unlink, Compact or ShrinkToFit. Use GetSources to unlink while walking.
    public ReadOnlySpan<TEntity> SourceSpan(TEntity target) => _in.Span(target);

    public TargetEnumerator GetTargets(TEntity source) => new(_out, source);
    public SourceEnumerator GetSources(TEntity target) => new(_in, target);

    internal override void SourceDestroyed(TEntity e) => UnlinkTargets(e);
    internal override void TargetDestroyed(TEntity e, Registry<TEntity> registry) => UnlinkSources(e);

    // h is the source's out head; removing out items leaves in heads put, so t stays valid.
    private void RemoveOut(int h, int o)
    {
        var entry = _out.SpanAt(h)[o];
        var t = _in.IndexOf(entry.Link.Target);
        RemoveOutOnly(h, o);
        RemoveIn(t, entry.InSlot);
    }

    // Repairs the moved item's twin, always in another entity's list, so callers can walk their own span.
    private void RemoveOutOnly(int h, int o)
    {
        if (_out.RemoveAt(h, o, out var moved))
        {
            _in.TwinSpan(moved.Link.Target)[moved.InSlot] = o;
        }
    }

    // A move means the list didn't empty, so head h is still valid after RemoveAt.
    private void RemoveIn(int h, int i)
    {
        if (_in.RemoveAt(h, i, out var moved))
        {
            _out.Span(moved)[_in.TwinSpanAt(h)[i]].InSlot = i;
        }
    }

    private static int IndexOf(Span<Out> outs, TEntity target)
    {
        for (var o = 0; o < outs.Length; o++)
        {
            if (outs[o].Link.Target == target)
            {
                return o;
            }
        }

        return -1;
    }

    // Count, one source per link, then each link's data (which holds the target).
    internal void Write<TWriter>(ref TWriter w) where TWriter : ISnapshotWriter<TEntity>, allows ref struct
    {
        var total = 0;
        foreach (var source in Sources)
        {
            total += _out.Count(source);
        }

        w.WriteCount(total);
        foreach (var source in Sources)
        {
            for (var k = _out.Count(source); k > 0; k--)
            {
                w.WriteEntity(source);
            }
        }

        foreach (var source in Sources)
        {
            foreach (var o in _out.Span(source))
            {
                w.Write(in o.Link);
            }
        }
    }

    // Last first, so unlinking Current is safe: the swap fills its slot with a visited link.
    public ref struct TargetEnumerator
    {
        private readonly SlabLists<TEntity, Out> _lists;
        private readonly TEntity _source;
        private int _offset;
        private int _i;
        private int _moves;

        internal TargetEnumerator(SlabLists<TEntity, Out> lists, TEntity source)
        {
            _lists = lists;
            _source = source;
            var h = lists.IndexOf(source);
            _offset = h < 0 ? 0 : lists.At(h).Offset;
            _i = h < 0 ? 0 : lists.At(h).Count;
            _moves = lists.Moves;
        }

        public readonly ref T Current => ref _lists.Items[_offset + _i].Link;
        public readonly TargetEnumerator GetEnumerator() => this;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (--_i < 0)
            {
                return false;
            }

            if (_lists.Moves != _moves)
            {
                var h = _lists.IndexOf(_source);
                if (h < 0)
                {
                    return false;
                }

                _offset = _lists.At(h).Offset;
                _moves = _lists.Moves;
            }

            return true;
        }
    }

    // Last first, so unlinking Current is safe.
    public ref struct SourceEnumerator
    {
        private readonly SlabLists<TEntity, TEntity> _lists;
        private readonly TEntity _target;
        private int _offset;
        private int _i;
        private int _moves;

        internal SourceEnumerator(SlabLists<TEntity, TEntity> lists, TEntity target)
        {
            _lists = lists;
            _target = target;
            var h = lists.IndexOf(target);
            _offset = h < 0 ? 0 : lists.At(h).Offset;
            _i = h < 0 ? 0 : lists.At(h).Count;
            _moves = lists.Moves;
        }

        public TEntity Current { get; private set; }
        public readonly SourceEnumerator GetEnumerator() => this;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (--_i < 0)
            {
                return false;
            }

            if (_lists.Moves != _moves)
            {
                var h = _lists.IndexOf(_target);
                if (h < 0)
                {
                    return false;
                }

                _offset = _lists.At(h).Offset;
                _moves = _lists.Moves;
            }

            Current = _lists.Items[_offset + _i];
            return true;
        }
    }
}
