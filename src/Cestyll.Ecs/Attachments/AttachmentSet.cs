using System.Diagnostics;
using Cestyll.Ecs.Collections;
using Cestyll.Ecs.Storage;

namespace Cestyll.Ecs.Attachments;

// Each entity's T attachments, contiguous in a shared slab; key lookups scan linearly.
// Unordered (Detach swap-removes). Refs and spans go stale on any attach or detach in this set.
public sealed class AttachmentSet<TEntity, T, TKey> : AttachmentStore<TEntity, T>
    where TEntity : unmanaged, IEntity<TEntity>
    where T : struct, IAttachment<TKey>
    where TKey : notnull
{
    private readonly SlabLists<TEntity, T> _lists;
    private readonly Registry<TEntity> _registry;

    internal AttachmentSet(Registry<TEntity> registry)
    {
        _registry = registry;
        _lists = new(registry.TakeAttachmentOwners<T>());
    }

    // OnDetach fires before removal, so Get still reads the item. Writes through refs fire nothing.
    public event AttachmentHandler<TEntity, TKey>? OnAttach;
    public event AttachmentHandler<TEntity, TKey>? OnUpdate;
    public event AttachmentHandler<TEntity, TKey>? OnDetach
    {
        add
        {
            _detach += value;
            _registry.DestroySignalConnected();
        }
        remove
        {
            var before = _detach;
            _detach -= value;
            if (_detach != before)
            {
                _registry.DestroySignalDisconnected();
            }
        }
    }

    private AttachmentHandler<TEntity, TKey>? _detach;
    private InFlight<(TEntity, TKey)>? _removing;

    // Open All walks, DEBUG only.
    private int _walks;

    [Conditional("DEBUG")]
    private void CheckNotWalked(string change)
    {
        if (_walks > 0)
        {
            throw new InvalidOperationException(
                $"Can't {change} a {typeof(T).Name} while a walk over All is open: record the change in a "
                + "CommandBuffer, or make it inside Registry.Defer()");
        }
    }

    // Every entity with at least one T, in packed order.
    public ReadOnlySpan<TEntity> Entities => _lists.Owners.Entities;

    internal override SparseSet<TEntity> Owners => _lists.Owners;
    internal override Type StoredType => typeof(T);
    public override int SlabUsed => _lists.Used;
    public override int SlabGaps => _lists.Gaps;
    public override int SlabCapacity => _lists.Length;

    public override void Compact(double minWaste = 0) => _lists.Compact(minWaste);
    public override void ShrinkToFit() => _lists.ShrinkToFit();

    public int ItemCount(TEntity e) => _lists.Count(e);

    public bool Has(TEntity e) => _lists.IndexOf(e) >= 0;

    // In a defer scope, keyed reads also see recorded items.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Has(TEntity e, TKey key) => PendingReads ? HasPending(e, key) : IndexOf(_lists.Span(e), key) >= 0;

    // Out of line to keep the usual paths small. The latest record wins over applied items.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool HasPending(TEntity e, TKey key)
    {
        var pending = _registry.PendingAttachmentSlot<T, TKey>(e, key);
        return pending >= 0 || (pending == Registry<TEntity>.NoPending && IndexOf(_lists.Span(e), key) >= 0);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private ref T GetPending(TEntity e, TKey key)
    {
        var pending = _registry.PendingAttachmentSlot<T, TKey>(e, key);
        if (pending >= 0)
        {
            return ref _registry.PendingAttachment<T, TKey>(pending);
        }

        var items = _lists.Span(e);
        var i = pending == Registry<TEntity>.NoPending ? IndexOf(items, key) : -1;
        if (i < 0)
        {
            Throw.NoAttachment<TEntity, T, TKey>(e, key);
        }

        return ref items[i];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool TryGetPending(TEntity e, TKey key, out T item)
    {
        var pending = _registry.PendingAttachmentSlot<T, TKey>(e, key);
        if (pending >= 0)
        {
            item = _registry.PendingAttachment<T, TKey>(pending);
            return true;
        }

        var items = _lists.Span(e);
        var i = pending == Registry<TEntity>.NoPending ? IndexOf(items, key) : -1;
        item = i >= 0 ? items[i] : default;
        return i >= 0;
    }

    // Attached for real, not only recorded.
    internal bool HasApplied(TEntity e, TKey key) => IndexOf(_lists.Span(e), key) >= 0;

    public override Span<T> GetAll(TEntity e) => _lists.Span(e);

    // Replaces the attachment with the same key.
    public ref T Attach(TEntity e, T item)
    {
        CheckNotWalked("attach");
        if (!_registry.CanAttachTo(e))
        {
            Throw.NotAlive(e);
        }

        var items = _lists.Span(e);
        var i = IndexOf(items, item.Key);
        if (i >= 0)
        {
            items[i] = item;
            if (OnUpdate is not { } update)
            {
                return ref items[i];
            }

            return ref Fire(update, e, item.Key);
        }

        ref var added = ref _lists.Append(e, item, out _);
        if (OnAttach is not { } attached)
        {
            return ref added;
        }

        return ref Fire(attached, e, item.Key);
    }

    // Out of line so Attach stays small when nothing listens.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private ref T Fire(AttachmentHandler<TEntity, TKey> handler, TEntity e, TKey key)
    {
        handler(e, key);
        return ref GetAfterSignal(e, key);
    }

    // False if the item's OnDetach is already running further up the stack.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool FireDetach(TEntity e, TKey key)
    {
        if (!(_removing ??= new()).TryEnter((e, key), out var guard))
        {
            return false;
        }

        using (guard)
        {
            _detach?.Invoke(e, key);
        }
        return true;
    }

    // Merges item into the existing one under its key, else attaches it.
    public ref T AttachOrPatch(TEntity e, T item, MergeAction<T> merge)
    {
        ArgumentNullException.ThrowIfNull(merge);
        var i = IndexOf(_lists.Span(e), item.Key);
        if (i < 0)
        {
            return ref Attach(e, item);
        }

        merge(ref _lists.Span(e)[i], in item);
        if (OnUpdate is not { } update)
        {
            return ref GetAfterSignal(e, item.Key);
        }

        return ref Fire(update, e, item.Key);
    }

    public void Patch(TEntity e, TKey key, RefAction<T> edit)
    {
        edit(ref Get(e, key));
        OnUpdate?.Invoke(e, key);
    }

    // For an item already changed through a ref.
    public void Patch(TEntity e, TKey key)
    {
        if (!Has(e, key))
        {
            Throw.NoAttachment<TEntity, T, TKey>(e, key);
        }

        OnUpdate?.Invoke(e, key);
    }

    public bool Detach(TEntity e, TKey key)
    {
        CheckNotWalked("detach");
        var i = IndexOf(_lists.Span(e), key);
        if (i < 0)
        {
            return false;
        }

        if (_detach is not null)
        {
            if (!FireDetach(e, key))
            {
                return false; // its OnDetach is already running
            }

            i = IndexOf(_lists.Span(e), key); // the handler may have moved it
        }

        _lists.RemoveAt(e, i, out _);
        return true;
    }

    // Returns how many there were.
    public int DetachItems(TEntity e)
    {
        if (_detach is null)
        {
            return _lists.RemoveAll(e);
        }

        // From the end, re-reading the list since handlers may change it.
        var count = _lists.Count(e);
        for (var k = count; k > 0; k--)
        {
            var items = _lists.Span(e);
            if (items.Length == 0)
            {
                break;
            }

            Detach(e, items[Math.Min(k, items.Length) - 1].Key);
        }

        return count;
    }

    internal override void EntityDestroyed(TEntity e) => DetachItems(e);

    // A handler may move the item's block, so look it up again; it must not have detached it.
    private ref T GetAfterSignal(TEntity e, TKey key)
    {
        var items = _lists.Span(e);
        var i = IndexOf(items, key);
        if (i < 0)
        {
            throw new InvalidOperationException($"A signal handler detached {typeof(T).Name} {key} from {e}");
        }

        return ref items[i];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T Get(TEntity e, TKey key)
    {
        if (PendingReads)
        {
            return ref GetPending(e, key);
        }

        var items = _lists.Span(e);
        var i = IndexOf(items, key);
        if (i < 0)
        {
            Throw.NoAttachment<TEntity, T, TKey>(e, key);
        }

        return ref items[i];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGet(TEntity e, TKey key, out T item)
    {
        if (PendingReads)
        {
            return TryGetPending(e, key, out item);
        }

        var items = _lists.Span(e);
        var i = IndexOf(items, key);
        item = i >= 0 ? items[i] : default;
        return i >= 0;
    }

    // Every attachment of every entity. No attach or detach during the walk; Compact is fine.
    public AllEnumerator All => new(this);

    public readonly ref struct Attached
    {
        public readonly TEntity Entity;
        public readonly ref T Item;

        internal Attached(TEntity entity, ref T item)
        {
            Entity = entity;
            Item = ref item;
        }
    }

    public ref struct AllEnumerator
    {
        private readonly AttachmentSet<TEntity, T, TKey> _set;
        private readonly SlabLists<TEntity, T> _lists;
        private bool _open; // counted in the set's _walks
        private int _h;      // owner's slot in the heads pool
        private int _i;      // item within the owner's block
        private int _count;
        private int _offset;
        private int _moves;

        internal AllEnumerator(AttachmentSet<TEntity, T, TKey> set)
        {
            _set = set;
            var lists = set._lists;
            _lists = lists;
            _h = -1;
            _moves = lists.Moves;
        }

        public readonly Attached Current =>
            new(_lists.Owners.EntityAt(_h), ref _lists.Items[_offset + _i]);

        public readonly AllEnumerator GetEnumerator() => this;

        public bool MoveNext()
        {
            Open();

            // Blocks moved; owners keep their order, so re-read _h's offset.
            if (_lists.Moves != _moves)
            {
                _moves = _lists.Moves;
                if (_h >= 0)
                {
                    _offset = _lists.At(_h).Offset;
                }
            }

            if (++_i < _count)
            {
                return true;
            }

            if (++_h >= _lists.Owners.Count)
            {
                Dispose();
                return false;
            }

            ref readonly var head = ref _lists.At(_h); // every listed owner has at least one
            _offset = head.Offset;
            _count = head.Count;
            _i = 0;
            return true;
        }

        public void Dispose() => Close();

        [Conditional("DEBUG")]
        private void Open()
        {
            if (!_open)
            {
                _open = true;
                _set._walks++;
            }
        }

        [Conditional("DEBUG")]
        private void Close()
        {
            if (_open)
            {
                _open = false;
                _set._walks--;
            }
        }
    }

    private static int IndexOf(Span<T> items, TKey key)
    {
        for (var i = 0; i < items.Length; i++)
        {
            if (EqualityComparer<TKey>.Default.Equals(items[i].Key, key))
            {
                return i;
            }
        }

        return -1;
    }
}
