using System.Buffers;

namespace Cestyll.Ecs.Storage;

public abstract partial class SparseSet<TEntity>
    where TEntity : unmanaged, IEntity<TEntity>
{
    // A separate probe pass lets the CPU overlap the random loads. Vector256 measured no better.
    public void IndexOfMany(ReadOnlySpan<TEntity> entities, Span<int> slots)
    {
        var sparse = SparseSpan;
        slots = slots[..entities.Length];
        for (var i = 0; i < entities.Length; i++)
        {
            slots[i] = Probe(sparse, entities[i]);
        }
    }

    // Walks a copy, so entities may be a span the removals reorder (e.g. this set's Entities).
    public int Remove(ReadOnlySpan<TEntity> entities)
    {
        var removed = 0;
        var buffer = ArrayPool<TEntity>.Shared.Rent(entities.Length);
        try
        {
            entities.CopyTo(buffer);
            foreach (var e in buffer.AsSpan(0, entities.Length))
            {
                if (Remove(e))
                {
                    removed++;
                }
            }
        }
        finally
        {
            ArrayPool<TEntity>.Shared.Return(buffer);
        }
        return removed;
    }

    // Doubles like Emplace, so repeated small batches don't resize each time.
    private protected void ReserveMore(int extra)
    {
        var needed = Count + extra;
        if (needed > _packed.Length)
        {
            Reserve(Math.Max(needed, _packed.Length * 2));
        }
    }

    // For a bulk create whose indices are all below indexBound.
    internal void ReserveBatch(int extra, int indexBound)
    {
        ReserveMore(extra);
        if (indexBound > _sparse.Length)
        {
            Array.Resize(ref _sparse, Math.Max(indexBound, _sparse.Length * 2));
        }
    }

    // Fires signals. Tolerates handlers removing others; entities they add stay.
    private protected void RemoveAll()
    {
        for (var slot = Count - 1; slot >= 0; slot = Math.Min(slot, Count) - 1)
        {
            Remove(_packed[slot]);
        }
    }
}
