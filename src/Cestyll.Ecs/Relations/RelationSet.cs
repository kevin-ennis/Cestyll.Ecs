using Cestyll.Ecs.Collections;
using Cestyll.Ecs.Snapshots;
using Cestyll.Ecs.Storage;

namespace Cestyll.Ecs.Relations;

// One target per source, with a reverse index and a cleanup policy.
// Forward: Pool source -> Edge; reverse: SlabLists target -> sources, Edge.Slot indexing it.
public sealed class RelationSet<TEntity, TRelation> : RelationStore<TEntity>
    where TEntity : unmanaged, IEntity<TEntity>
    where TRelation : struct, IRelation
{
    private struct Edge : IComponent
    {
        public TEntity Target;
        public int Slot;
    }

    private readonly Pool<TEntity, Edge> _targets = new();
    private readonly SlabLists<TEntity, TEntity> _sources = new();
    private readonly Registry<TEntity> _registry;

    internal RelationSet(Registry<TEntity> registry) => _registry = registry;

    // OnUnrelate fires before removal, so GetTarget still works. Relating that source again from it throws.
    public event PairHandler<TEntity>? OnRelate;
    public event PairHandler<TEntity>? OnUnrelate
    {
        add
        {
            _unrelate += value;
            _registry.DestroySignalConnected();
        }
        remove
        {
            var before = _unrelate;
            _unrelate -= value;
            if (_unrelate != before)
            {
                _registry.DestroySignalDisconnected();
            }
        }
    }

    private PairHandler<TEntity>? _unrelate;
    private InFlight<TEntity>? _removing;

    // Every entity with a target, in packed order.
    public ReadOnlySpan<TEntity> Sources => _targets.Entities;

    // Forward index capacity.
    public int SourceCapacity => _targets.Capacity;

    public override int SlabUsed => _sources.Used;
    public override int SlabGaps => _sources.Gaps;
    public override int SlabCapacity => _sources.Length;

    public override void Compact(double minWaste = 0) => _sources.Compact(minWaste);

    public override void ShrinkToFit()
    {
        _targets.ShrinkToFit();
        _sources.ShrinkToFit();
    }

    internal override Type StoredType => typeof(TRelation);
    internal override bool IsLinkSet => false;
    internal override SparseSet<TEntity> HasTargets => _targets;
    internal override SparseSet<TEntity> HasSources => _sources.Owners;

    public TEntity GetTarget(TEntity source)
    {
        var s = _targets.IndexOf(source);
        return s < 0 ? default(TEntity) : _targets.At(s).Target;
    }

    public bool TryGetTarget(TEntity source, out TEntity target)
    {
        target = GetTarget(source); // Relate rejects Null, so Null means no target
        return !target.IsNull;
    }

    public int SourceCount(TEntity target) => _sources.Count(target);

    internal override bool Involves(TEntity e) => _targets.Has(e) || _sources.IndexOf(e) >= 0;

    public SourceEnumerator GetSources(TEntity target)
    {
        var h = _sources.IndexOf(target);
        return h < 0
            ? new(this, target, 0, 0)
            : new(this, target, _sources.At(h).Offset, _sources.At(h).Count);
    }

    // Stale after any Relate, Unrelate, Compact or ShrinkToFit. Use GetSources to unrelate while walking.
    public ReadOnlySpan<TEntity> SourceSpan(TEntity target) => _sources.Span(target);

    internal void Relate(TEntity source, TEntity target)
    {
        if (GetTarget(source) == target)
        {
            return;
        }

        // Unrelate no-ops inside source's own OnUnrelate, leaving it related; catch that first.
        if (!Unrelate(source) && _unrelate is not null && _targets.Has(source))
        {
            throw new InvalidOperationException($"{source} can't be related again from its own OnUnrelate handler");
        }

        _sources.Append(target, source, out var slot);
        _targets.Add(source, new Edge { Target = target, Slot = slot });
        OnRelate?.Invoke(source, target);
    }

    // False if source's OnUnrelate is already running further up the stack.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool FireUnrelate(TEntity source, TEntity target)
    {
        if (!(_removing ??= new()).TryEnter(source, out var guard))
        {
            return false;
        }

        using (guard)
        {
            _unrelate?.Invoke(source, target);
        }
        return true;
    }

    internal bool Unrelate(TEntity source)
    {
        var s = _targets.IndexOf(source);
        if (s < 0)
        {
            return false;
        }

        if (_unrelate is not null)
        {
            if (!FireUnrelate(source, _targets.At(s).Target))
            {
                return false; // its OnUnrelate is already running
            }

            s = _targets.IndexOf(source);
            if (s < 0)
            {
                return true; // defensive: nested Unrelate calls on source are no-ops
            }
        }

        var edge = _targets.At(s);
        if (_sources.RemoveAt(edge.Target, edge.Slot, out var moved))
        {
            _targets.Get(moved).Slot = edge.Slot;
        }

        _targets.Remove(source);
        return true;
    }

    internal override void SourceDestroyed(TEntity e) => Unrelate(e);

    internal override void TargetDestroyed(TEntity target, Registry<TEntity> registry)
    {
        if (TRelation.Policy == OnTargetDestroyed.Unrelate && _unrelate is null)
        {
            DropSources(target);
            return;
        }

        // Re-probe the head each pass: Unrelate calls may move or free its block.
        for (int h; (h = _sources.IndexOf(target)) >= 0;)
        {
            ref readonly var head = ref _sources.At(h);
            var last = _sources.Items[head.Offset + head.Count - 1];
            if (TRelation.Policy == OnTargetDestroyed.DestroySources)
            {
                registry.Destroy(last);
            }

            Unrelate(last); // no-op after Destroy; guarantees progress either way
        }
    }

    // No handler can touch the lists: drop the whole in-list, in the loop's order (last first, head before first).
    private void DropSources(TEntity target)
    {
        var h = _sources.IndexOf(target);
        if (h < 0)
        {
            return;
        }

        var sources = _sources.SpanAt(h);
        for (var k = sources.Length - 1; k > 0; k--)
        {
            _targets.Remove(sources[k]);
        }

        var first = sources[0];
        _sources.RemoveAllAt(h);
        _targets.Remove(first);
    }

    internal void Write<TWriter>(ref TWriter w) where TWriter : ISnapshotWriter<TEntity>, allows ref struct
    {
        var sources = _targets.Entities;
        var edges = _targets.Components;
        w.WriteCount(sources.Length);
        for (var i = 0; i < sources.Length; i++)
        {
            w.WriteEntity(sources[i]);
            w.WriteEntity(edges[i].Target);
        }
    }

    // Last first, so unrelating Current is safe: the swap fills its slot with a visited source.
    // Relating to this target, or destroying its other sources, mid-walk is not.
    public ref struct SourceEnumerator
    {
        private readonly SlabLists<TEntity, TEntity> _sources;
        private readonly TEntity _target;
        private int _offset;
        private int _i;
        private int _moves;

        internal SourceEnumerator(RelationSet<TEntity, TRelation> relation, TEntity target, int offset, int count)
        {
            _sources = relation._sources;
            _target = target;
            _offset = offset;
            _i = count;
            _moves = _sources.Moves;
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

            var sources = _sources;
            if (sources.Moves != _moves)
            {
                var h = sources.IndexOf(_target);
                if (h < 0)
                {
                    return false;
                }

                _offset = sources.At(h).Offset;
                _moves = sources.Moves;
            }

            Current = sources.Items[_offset + _i];
            return true;
        }
    }
}
