using Cestyll.Ecs.Collections;

namespace Cestyll.Ecs.Snapshots;

// Shared section reads and writes. Reads grow lists as values arrive rather than trust a count.
internal static class SnapshotIo<TEntity>
    where TEntity : unmanaged, IEntity<TEntity>
{
    private const int MaxPresize = 1024;

    public static void WriteEntityList<TWriter>(ref TWriter w, ReadOnlySpan<TEntity> entities)
        where TWriter : ISnapshotWriter<TEntity>, allows ref struct
    {
        w.WriteCount(entities.Length);
        foreach (var e in entities)
        {
            w.WriteEntity(e);
        }
    }

    public static void WriteValues<TWriter, T>(ref TWriter w, ReadOnlySpan<T> values)
        where TWriter : ISnapshotWriter<TEntity>, allows ref struct
    {
        foreach (ref readonly var value in values)
        {
            w.Write(in value);
        }
    }

    // Entity lists use TEntity.MaxIndex; link sources Array.MaxLength, since a source can have many links.
    public static Span<TEntity> ReadEntities<TReader>(ref TReader r, int max, string what)
        where TReader : ISnapshotReader<TEntity>, allows ref struct
    {
        var count = ReadCount(ref r, max, what);
        var entities = new List<TEntity>(Math.Min(count, MaxPresize));
        for (var i = 0; i < count; i++)
        {
            entities.Add(r.ReadEntity());
        }

        return CollectionsMarshal.AsSpan(entities);
    }

    public static Span<T> ReadValues<TReader, T>(ref TReader r, int count)
        where TReader : ISnapshotReader<TEntity>, allows ref struct
    {
        var values = new List<T>(Math.Min(count, MaxPresize));
        for (var i = 0; i < count; i++)
        {
            values.Add(r.Read<T>());
        }

        return CollectionsMarshal.AsSpan(values);
    }

    // count is already backed by that many entities read, so presizing is safe.
    public static T[] ReadComponents<TReader, T>(ref TReader r, int count)
        where TReader : ISnapshotReader<TEntity>, allows ref struct
    {
        var values = new T[count];
        r.ReadComponents<T>(values);
        return values;
    }

    // One error for negative or huge counts, rather than overflow or OOM later.
    public static int ReadCount<TReader>(ref TReader r, int max, string what)
        where TReader : ISnapshotReader<TEntity>, allows ref struct
    {
        var count = r.ReadCount();
        if ((uint)count > (uint)max)
        {
            throw new InvalidDataException($"Bad {what} count {count}");
        }

        return count;
    }

    // By index alone; rejected up front so a second Add can't throw mid-section.
    public static void RejectDuplicates(ReadOnlySpan<TEntity> entities)
    {
        var seen = new EntityBitset();
        foreach (var e in entities)
        {
            if (seen.Test(e.Index))
            {
                throw new InvalidDataException($"Snapshot lists {e} twice");
            }

            seen.Set(e.Index);
        }
    }

    // Pair count is bounded by entity indices.
    public static Span<(TEntity Source, TEntity Target)> ReadRelationPairs<TReader>(ref TReader r)
        where TReader : ISnapshotReader<TEntity>, allows ref struct
    {
        var count = ReadCount(ref r, TEntity.MaxIndex, "relation");
        var pairs = new List<(TEntity, TEntity)>(Math.Min(count, MaxPresize));
        for (var i = 0; i < count; i++)
        {
            pairs.Add((r.ReadEntity(), r.ReadEntity()));
        }

        return CollectionsMarshal.AsSpan(pairs);
    }

    // Per-owner item counts, checked before the items are read.
    public static int[] ReadAttachmentCounts<TReader>(ref TReader r, int owners, out int total)
        where TReader : ISnapshotReader<TEntity>, allows ref struct
    {
        var counts = new int[owners]; // owners were read one by one, so this is backed
        long sum = 0;
        for (var i = 0; i < counts.Length; i++)
        {
            counts[i] = r.ReadCount();
            if (counts[i] <= 0)
            {
                throw new InvalidDataException($"Bad attachment count {counts[i]}");
            }

            sum += counts[i];
        }

        if (sum > Array.MaxLength)
        {
            throw new InvalidDataException($"Bad attachment total {sum}");
        }

        total = (int)sum;
        return counts;
    }

    // Validated fully here: a bad free list or version only fails much later, in Create.
    // versions is indexed by entity index; versions[0] is unused.
    public static (int Created, List<uint> Versions, int[] Free) ReadEntitySection<TReader>(ref TReader r)
        where TReader : ISnapshotReader<TEntity>, allows ref struct
    {
        r.BeginSection(SnapshotSection.Entities, typeof(TEntity), null);
        var created = ReadCount(ref r, TEntity.MaxIndex, "entity") + 1;
        var versions = new List<uint>(Math.Min(created, MaxPresize)) { 0 };
        for (var i = 1; i < created; i++)
        {
            var e = r.ReadEntity();
            if (e.Index != i || e.Version > TEntity.VersionMask)
            {
                throw new InvalidDataException($"Bad entity {e} at index {i}");
            }

            versions.Add(e.Version);
        }

        var freeCount = ReadCount(ref r, created - 1, "free");
        var free = new int[freeCount]; // bounded by created, which the reads above backed
        var seen = new EntityBitset();
        for (var i = 0; i < free.Length; i++)
        {
            var e = r.ReadEntity();
            var index = e.Index;
            if ((uint)(index - 1) >= (uint)(created - 1) || seen.Test(index) || e.Version != versions[index])
            {
                throw new InvalidDataException($"Bad free entity {e}");
            }

            seen.Set(index);
            free[i] = index;
        }

        r.EndSection();
        return (created, versions, free);
    }
}
