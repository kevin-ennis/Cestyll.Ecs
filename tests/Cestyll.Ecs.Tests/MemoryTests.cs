using Cestyll.Ecs;
using Cestyll.Ecs.Attachments;
using Cestyll.Ecs.Tags;

namespace Cestyll.Ecs.Tests;

// Removes never give memory back; Compact and ShrinkToFit do. Slab stores are tested elsewhere.
public class MemoryTests
{
    private struct Position : IComponent { public int X; }
    private struct Velocity : IComponent { public int Dx; }
    private struct Name : IComponent { public string Value; }
    private struct Marked : ITag;

    private struct Slot : IAttachment<int>
    {
        public int Id;
        public object? Payload; // managed, so moves must clear what they leave behind

        public readonly int Key => Id;
    }

    // count entities with `per` slots each; every other one is destroyed, leaving gaps.
    private static (Registry registry, AttachmentSet<Entity, Slot, int> slots, List<Entity> kept)
        Fragmented(int count, int per)
    {
        var registry = new Registry();
        var slots = registry.Attachments<Slot, int>();
        var entities = registry.CreateMany(count);
        foreach (var e in entities)
        {
            for (var i = 0; i < per; i++)
            {
                slots.Attach(e, new Slot { Id = i, Payload = e.Index * 100 + i });
            }
        }

        registry.Destroy(entities.Where(e => e.Index % 2 == 0).ToArray());
        return (registry, slots, entities.Where(e => e.Index % 2 != 0).ToList());
    }

    [Fact]
    public void Compact_UnderMinWaste_Skips()
    {
        var (_, slots, _) = Fragmented(100, 1); // 50 one-slot gaps in a 128-slot slab

        slots.Compact(minWaste: 0.5);

        Assert.Equal(128, slots.SlabCapacity);
        Assert.Equal(50, slots.SlabUsed);
        Assert.Equal(50, slots.SlabGaps);
    }

    [Fact]
    public void Compact_OverMinWaste_ClosesGaps_KeepsItems()
    {
        var (_, slots, kept) = Fragmented(100, 1);

        slots.Compact(minWaste: 0.25);

        Assert.Equal(0, slots.SlabGaps);
        Assert.Equal(128, slots.SlabCapacity);
        Assert.All(kept, e => Assert.Equal(e.Index * 100, slots.Get(e, 0).Payload));
    }

    [Fact]
    public void Compact_MinWasteOutOfRange_Throws()
    {
        var (_, slots, _) = Fragmented(100, 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => slots.Compact(1.5));
        Assert.Throws<ArgumentOutOfRangeException>(() => slots.Compact(double.NaN));
    }

    [Fact]
    public void Registry_Compact_UnderMinWaste_Skips()
    {
        var (registry, slots, _) = Fragmented(100, 1);

        registry.Compact(minWaste: 0.5);

        Assert.Equal(50, slots.SlabGaps);
    }

    [Fact]
    public void Registry_Compact_Default_ClosesGaps()
    {
        var (registry, slots, _) = Fragmented(100, 1);

        registry.Compact();

        Assert.Equal(0, slots.SlabGaps);
    }

    [Fact]
    public void AttachmentAll_FollowsBlocksThroughCompactAndShrink()
    {
        var (_, slots, kept) = Fragmented(60, 3);
        var seen = new List<object?>();
        var step = 0;
        foreach (var a in slots.All)
        {
            seen.Add(a.Item.Payload);
            step++;
            if (step == 1)
            {
                slots.Compact();
            }
            else if (step == 40)
            {
                slots.ShrinkToFit();
            }
        }

        Assert.Equal(0, slots.SlabGaps);
        var expected = kept.SelectMany(e => Enumerable.Range(0, 3).Select(i => (object?)(e.Index * 100 + i)));
        Assert.Equal(expected.ToHashSet(), seen.ToHashSet());
        Assert.Equal(kept.Count * 3, seen.Count);
    }

    [Fact]
    public void Registry_ShrinkToFit_TrimsTagsAndObservers()
    {
        var registry = new Registry();
        using var observer = registry.Observer().OnAdd<Position>();
        var entities = registry.CreateMany(5000);
        foreach (var e in entities)
        {
            registry.Tag<Marked>(e);
            registry.Add(e, new Position { X = e.Index });
        }

        var tags = registry.Tags<Marked>();
        var wordsBefore = tags.Words.Length;
        var sparseBefore = observer.SparseCapacity;
        var gone = entities[10..];
        registry.Destroy(gone);

        registry.ShrinkToFit();

        Assert.True(wordsBefore >= 5000 / 64);
        Assert.True(sparseBefore > 5000);
        Assert.Equal(16, tags.Words.Length);
        Assert.Equal(64, observer.SparseCapacity);
        Assert.Equal(10, observer.Count);
        Assert.All(entities[..10], e => Assert.True(registry.IsAlive(e)));
        Assert.All(entities[..10], e => Assert.True(registry.HasTag<Marked>(e)));
        Assert.All(gone, e => Assert.False(registry.IsAlive(e)));
    }

    // Recycled indices get new versions; fresh ones start past the old high-water mark.
    [Fact]
    public void Registry_ShrinkToFit_ThenRegrow_KeepsTagsAndObservers()
    {
        var registry = new Registry();
        using var observer = registry.Observer().OnAdd<Position>();
        var entities = registry.CreateMany(5000);
        foreach (var e in entities)
        {
            registry.Tag<Marked>(e);
            registry.Add(e, new Position { X = e.Index });
        }

        var tags = registry.Tags<Marked>();
        var gone = entities[10..];
        registry.Destroy(gone);
        registry.ShrinkToFit();

        var again = registry.CreateMany(6000);
        foreach (var e in again)
        {
            registry.Tag<Marked>(e);
            registry.Add(e, new Position { X = e.Index });
        }

        Assert.Equal(6010, tags.Count);
        Assert.Equal(6010, observer.Count);
        Assert.Equal(6010, registry.AliveCount);
        Assert.All(gone, e => Assert.False(registry.IsAlive(e)));
        Assert.All(again, e => Assert.Equal(e.Index, registry.Get<Position>(e).X));
    }

    [Fact]
    public void Pool_Remove_KeepsCapacity()
    {
        var registry = new Registry();
        var entities = registry.CreateMany(1000);
        foreach (var e in entities)
        {
            registry.Add(e, new Position { X = e.Index });
        }

        var pool = registry.Storage<Position>();
        var full = pool.Capacity;
        var keep = entities.Take(10).Append(entities[199]).ToList();

        foreach (var e in entities.Except(keep))
        {
            registry.Destroy(e);
        }

        Assert.Equal(full, pool.Capacity);
        Assert.True(pool.SparseCapacity > 1000);
    }

    [Fact]
    public void Pool_ShrinkToFit_ReturnsCapacity_KeepsValues()
    {
        var registry = new Registry();
        var entities = registry.CreateMany(1000);
        foreach (var e in entities)
        {
            registry.Add(e, new Position { X = e.Index });
        }

        var pool = registry.Storage<Position>();
        var keep = entities.Take(10).Append(entities[199]).ToList(); // highest index 200
        foreach (var e in entities.Except(keep))
        {
            registry.Destroy(e);
        }

        pool.ShrinkToFit();

        Assert.Equal(64, pool.Capacity);
        Assert.Equal(201, pool.SparseCapacity);
        Assert.Equal(11, pool.Count);
        Assert.All(keep, e => Assert.Equal(e.Index, registry.Get<Position>(e).X));
        Assert.All(entities.Except(keep), e => Assert.False(pool.Has(e)));
    }

    [Fact]
    public void Pool_GrowsAgainAfterShrink()
    {
        var registry = new Registry();
        var pool = registry.Storage<Name>();
        var entities = registry.CreateMany(300);
        foreach (var e in entities)
        {
            registry.Add(e, new Name { Value = $"n{e.Index}" });
        }

        pool.Clear();
        pool.ShrinkToFit();

        foreach (var e in entities)
        {
            registry.Add(e, new Name { Value = $"m{e.Index}" });
        }

        Assert.Equal(300, pool.Count);
        Assert.All(entities, e => Assert.Equal($"m{e.Index}", registry.Get<Name>(e).Value));
    }

    [Fact]
    public void Registry_ShrinkToFit_KeepsOwningGroupIntact()
    {
        var registry = new Registry();
        var group = registry.Group().Own<Position>().Own<Velocity>().Build();
        var entities = registry.CreateMany(500);
        foreach (var e in entities)
        {
            registry.Add(e, new Position { X = e.Index });
            if (e.Index % 2 == 0)
            {
                registry.Add(e, new Velocity { Dx = e.Index });
            }
        }

        foreach (var e in entities.Where(e => e.Index > 40))
        {
            registry.Destroy(e);
        }

        registry.Compact();

        registry.ShrinkToFit();

        var prefix = registry.Storage<Velocity>().Entities[..group.Count].ToArray();
        Assert.Equal(20, group.Count);
        Assert.Equal(64, registry.Storage<Position>().Capacity);
        Assert.Equal(64, registry.Storage<Velocity>().Capacity);
        Assert.Equal(20, prefix.Length);
        Assert.All(prefix, e => Assert.Equal(0, e.Index % 2));
        Assert.All(prefix, e => Assert.Equal(e.Index, registry.Get<Position>(e).X));
        Assert.All(prefix, e => Assert.Equal(e.Index, registry.Get<Velocity>(e).Dx));
    }

    [Fact]
    public void OwningGroup_AfterShrinkToFit_StillTracksAdds()
    {
        var registry = new Registry();
        var group = registry.Group().Own<Position>().Own<Velocity>().Build();
        var entities = registry.CreateMany(500);
        foreach (var e in entities)
        {
            registry.Add(e, new Position { X = e.Index });
            if (e.Index % 2 == 0)
            {
                registry.Add(e, new Velocity { Dx = e.Index });
            }
        }

        foreach (var e in entities.Where(e => e.Index > 40))
        {
            registry.Destroy(e);
        }

        registry.Compact();
        registry.ShrinkToFit();
        Entity late = registry.Create();

        registry.Add(late, new Position());
        registry.Add(late, new Velocity());

        Assert.Equal(21, group.Count);
    }
}
