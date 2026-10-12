using System.Buffers;

namespace Cestyll.Ecs.Storage;

// Per-owner lists of T, each in a power-of-two block of one shared array (the slab).
// Blocks double when full and halve at a quarter; freed blocks are reused per size class.
// Blocks move on append, remove, Compact and ShrinkToFit: Moves bumps, re-read Items.
internal sealed class SlabLists<TEntity, T>
    where TEntity : unmanaged, IEntity<TEntity>
{
    public const int MinLength = 64;
    private const int Classes = 32;

    public struct Head : IComponent
    {
        public int Offset, Count;
        public int Class; // block capacity is 1 << Class
    }

    private readonly Pool<TEntity, Head> _heads;
    public T[] Items = new T[MinLength];

    // Optional int per item, e.g. a twin list's slot; moves with Items.
    public int[]? Twins;
    private int _top;                                   // high-water mark of carved blocks
    private int _live;                                  // slots in allocated blocks
    private readonly List<int>?[] _free = new List<int>?[Classes];

    public int Moves;

    public int Used => _live;
    public int Length => Items.Length;
    public int Gaps => _top - _live; // freed slots only Compact reclaims

    // heads: a prebuilt owner set, which a group may already watch.
    public SlabLists(Pool<TEntity, Head>? heads = null, bool twins = false)
    {
        _heads = heads ?? new();
        Twins = twins ? new int[MinLength] : null;
    }

    // Every owner with a non-empty list, in packed order.
    public SparseSet<TEntity> Owners => _heads;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int IndexOf(TEntity owner) => _heads.IndexOf(owner);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref Head At(int h) => ref _heads.At(h);

    public int Count(TEntity owner)
    {
        var h = _heads.IndexOf(owner);
        return h < 0 ? 0 : _heads.At(h).Count;
    }

    public Span<T> Span(TEntity owner)
    {
        var h = _heads.IndexOf(owner);
        if (h < 0)
        {
            return default;
        }

        ref var head = ref _heads.At(h);
        return Items.AsSpan(head.Offset, head.Count);
    }

    public Span<int> TwinSpan(TEntity owner)
    {
        var h = _heads.IndexOf(owner);
        if (h < 0)
        {
            return default;
        }

        ref var head = ref _heads.At(h);
        return Twins.AsSpan(head.Offset, head.Count);
    }

    // By head slot from IndexOf, skipping the owner probe.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> SpanAt(int h)
    {
        ref var head = ref _heads.At(h);
        return Items.AsSpan(head.Offset, head.Count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<int> TwinSpanAt(int h)
    {
        ref var head = ref _heads.At(h);
        return Twins.AsSpan(head.Offset, head.Count);
    }

    // slot is the new item's position in owner's list.
    public ref T Append(TEntity owner, T item, out int slot, int twin = 0)
    {
        var h = _heads.IndexOf(owner);
        ref var head = ref h >= 0
            ? ref _heads.At(h)
            : ref _heads.Add(owner, new Head { Offset = Alloc(0), Class = 0 });

        if (head.Count == 1 << head.Class)
        {
            Move(ref head, head.Class + 1);
        }

        slot = head.Count++;
        if (Twins is { } twins)
        {
            twins[head.Offset + slot] = twin;
        }

        var items = Items;
        items[head.Offset + slot] = item;
        return ref items[head.Offset + slot];
    }

    // Swap-remove. Returns true when the last item moved into slot.
    public bool RemoveAt(TEntity owner, int slot, out T moved) => RemoveAt(_heads.IndexOf(owner), slot, out moved);

    // Frees head h if the list empties, so h is only still valid after a true return.
    public bool RemoveAt(int h, int slot, out T moved)
    {
        ref var head = ref _heads.At(h);
        var items = Items;
        var last = --head.Count;
        var didMove = slot != last;
        moved = didMove ? items[head.Offset + last] : default!;
        if (didMove)
        {
            items[head.Offset + slot] = moved;
            if (Twins is { } twins)
            {
                twins[head.Offset + slot] = twins[head.Offset + last];
            }
        }

        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            items[head.Offset + last] = default!; // don't keep the GC's view of it alive
        }

        if (head.Count == 0)
        {
            var (offset, sizeClass) = (head.Offset, head.Class);
            _heads.Remove(_heads.EntityAt(h));
            Release(offset, sizeClass);
        }
        // Only halve when it needs no grow or compaction, keeping removes O(1).
        else if (head.Count <= (1 << head.Class) >> 2 && FitsInPlace(head.Class - 1))
        {
            Move(ref head, head.Class - 1); // half size leaves room, so a hovering count won't thrash
        }

        return didMove;
    }

    // Returns how many items owner's list held.
    public int RemoveAll(TEntity owner)
    {
        var h = _heads.IndexOf(owner);
        return h < 0 ? 0 : RemoveAllAt(h);
    }

    // Frees head h.
    public int RemoveAllAt(int h)
    {
        var (offset, count, sizeClass) = (_heads.At(h).Offset, _heads.At(h).Count, _heads.At(h).Class);
        _heads.Remove(_heads.EntityAt(h));
        Release(offset, sizeClass);
        return count;
    }

    // Keeps the slab, so clearing every frame doesn't allocate.
    public void Clear()
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            Array.Clear(Items, 0, _top);
        }

        _heads.Clear();
        _top = 0;
        _live = 0;
        foreach (var free in _free)
        {
            free?.Clear();
        }

        Moves++;
    }

    // Moves an owner's items to a block of another size class.
    private void Move(ref Head head, int sizeClass)
    {
        var offset = Alloc(sizeClass);
        Array.Copy(Items, head.Offset, Items, offset, head.Count);
        if (Twins is { } twins)
        {
            Array.Copy(twins, head.Offset, twins, offset, head.Count);
        }

        var (oldOffset, oldClass) = (head.Offset, head.Class);
        head.Offset = offset;
        head.Class = sizeClass;
        Moves++;
        Release(oldOffset, oldClass);
    }

    private int Alloc(int sizeClass)
    {
        var size = 1 << sizeClass;
        if (_free[sizeClass] is { Count: > 0 } free)
        {
            _live += size;
            var reused = free[^1];
            free.RemoveAt(free.Count - 1);
            return reused;
        }

        if (_top + size > Items.Length)
        {
            // Compact instead of growing if that leaves it half free; amortized O(1) like doubling.
            if ((_live + size) * 2 <= Items.Length)
            {
                Compact();
            }
            else
            {
                var length = Math.Max(_top + size, Items.Length * 2);
                Array.Resize(ref Items, length);
                if (Twins is not null)
                {
                    Array.Resize(ref Twins, length);
                }
            }
        }

        _live += size;
        var offset = _top;
        _top += size;
        return offset;
    }

    // A block of sizeClass is free without growing or compacting.
    private bool FitsInPlace(int sizeClass) =>
        _free[sizeClass] is { Count: > 0 } || _top + (1 << sizeClass) <= Items.Length;

    private void Release(int offset, int sizeClass)
    {
        var size = 1 << sizeClass;
        _live -= size;
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            Array.Clear(Items, offset, size);
        }

        (_free[sizeClass] ??= []).Add(offset);
    }

    // Slides live blocks down over free ones, in place. No-op while gaps are under minWaste (0 to 1).
    public void Compact(double minWaste = 0)
    {
        if (minWaste is < 0 or > 1 || double.IsNaN(minWaste))
        {
            throw new ArgumentOutOfRangeException(nameof(minWaste), minWaste, "Must be between 0 and 1");
        }

        var gaps = _top - _live;
        if (gaps == 0 || gaps < minWaste * Items.Length)
        {
            return;
        }

        // Mark each block's first slot (live: head + 1, free: ~sizeClass) to hop block to block.
        var starts = ArrayPool<int>.Shared.Rent(_top);
        try
        {
            for (var h = 0; h < _heads.Count; h++)
            {
                starts[_heads.At(h).Offset] = h + 1;
            }

            for (var sizeClass = 0; sizeClass < Classes; sizeClass++)
            {
                if (_free[sizeClass] is { } free)
                {
                    foreach (var offset in CollectionsMarshal.AsSpan(free))
                    {
                        starts[offset] = ~sizeClass;
                    }
                }
            }

            // In offset order each block only moves down, onto space already passed.
            var items = Items;
            var top = 0;
            for (var at = 0; at < _top;)
            {
                var mark = starts[at];
                if (mark < 0)
                {
                    at += 1 << ~mark;
                    continue;
                }

                ref var head = ref _heads.At(mark - 1);
                var size = 1 << head.Class;
                if (at != top)
                {
                    Array.Copy(items, at, items, top, head.Count);
                    if (Twins is { } twins)
                    {
                        Array.Copy(twins, at, twins, top, head.Count);
                    }

                    if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                    {
                        Array.Clear(items, top + head.Count, size - head.Count);
                    }

                    head.Offset = top;
                }

                at += size;
                top += size;
            }

            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                Array.Clear(items, top, _top - top);
            }

            _top = top;
        }
        finally
        {
            ArrayPool<int>.Shared.Return(starts);
        }

        foreach (var free in _free)
        {
            free?.Clear();
        }

        Moves++;
    }

    // Repacks into a fresh gap-free slab, each block at its smallest fitting class.
    public void ShrinkToFit()
    {
        var live = 0;
        foreach (ref var head in _heads.Components)
        {
            live += 1 << FitClass(head.Count);
        }

        var length = Math.Max(MinLength, live);
        if (live < _top || length < Items.Length)
        {
            var old = Items;
            var items = new T[length];
            var oldTwins = Twins;
            var twins = oldTwins is null ? null : new int[length];
            var top = 0;
            foreach (ref var head in _heads.Components)
            {
                Array.Copy(old, head.Offset, items, top, head.Count);
                if (twins is not null)
                {
                    Array.Copy(oldTwins!, head.Offset, twins, top, head.Count);
                }

                head.Offset = top;
                head.Class = FitClass(head.Count);
                top += 1 << head.Class;
            }

            Items = items;
            Twins = twins;
            _top = top;
            _live = top;
            Moves++;
        }

        Array.Fill(_free, null);
        _heads.ShrinkToFit();
    }

    // count is never 0.
    private static int FitClass(int count) => BitOperations.Log2((uint)count - 1) + (count > 1 ? 1 : 0);
}
