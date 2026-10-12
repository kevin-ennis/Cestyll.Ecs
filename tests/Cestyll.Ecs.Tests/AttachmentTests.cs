using Cestyll.Ecs;
using Cestyll.Ecs.Attachments;
using Cestyll.Ecs.Snapshots;

namespace Cestyll.Ecs.Tests;

public class AttachmentTests
{
    private enum ItemType { Coin, Axe, Potion, Gem }

    private struct Item : IAttachment<ItemType>
    {
        public ItemType Type;
        public int Count;

        public readonly ItemType Key => Type;
    }

    // String keys and a reference field, to cover managed items and clearing on removal.
    private struct Note : IAttachment<string>
    {
        public string Topic;
        public object? Payload;

        public readonly string Key => Topic;
    }

    // Holds an Entity, for the ContinuousLoader remap path.
    private struct Grip : IAttachment<int>
    {
        public int Hand;
        public Entity Held;

        public readonly int Key => Hand;
    }

    private struct Pos : IComponent { }
    private struct Vel : IComponent { }

    private static Item Coin(int n) => new() { Type = ItemType.Coin, Count = n };
    private static Item Axe(int n) => new() { Type = ItemType.Axe, Count = n };

    [Fact]
    public void Attach_AddsItemsWithDistinctKeys()
    {
        var registry = new Registry();
        Entity e = registry.Create();
        var items = registry.Attachments<Item, ItemType>();

        items.Attach(e, Coin(42));
        items.Attach(e, Axe(1));

        Assert.Equal(2, items.ItemCount(e));
        Assert.True(items.Has(e));
        Assert.True(items.Has(e, ItemType.Axe));
        Assert.False(items.Has(e, ItemType.Gem));
        Assert.Equal(42, items.Get(e, ItemType.Coin).Count);
        Assert.Equal([e], items.Entities.ToArray());
    }

    [Fact]
    public void Attach_SameKey_Replaces()
    {
        var registry = new Registry();
        Entity e = registry.Create();

        registry.Attach<Item, ItemType>(e, Coin(1));
        registry.Attach<Item, ItemType>(e, Coin(7));

        var all = registry.GetAttachments<Item, ItemType>(e);
        Assert.Equal(1, all.Length);
        Assert.Equal(7, all[0].Count);
    }

    [Fact]
    public void RefsAndSpans_WriteInPlace()
    {
        var registry = new Registry();
        Entity e = registry.Create();
        var items = registry.Attachments<Item, ItemType>();

        items.Attach(e, Coin(1)).Count += 10;
        items.Get(e, ItemType.Coin).Count++;
        items.Attach(e, Axe(1));
        foreach (ref var item in items.GetAll(e))
        {
            item.Count *= 2;
        }

        Assert.Equal(24, items.Get(e, ItemType.Coin).Count);
        Assert.Equal(2, items.Get(e, ItemType.Axe).Count);
    }

    [Fact]
    public void Detach_RemovesOnlyThatKey()
    {
        var registry = new Registry();
        Entity e = registry.Create();
        var items = registry.Attachments<Item, ItemType>();
        items.Attach(e, Coin(1));
        items.Attach(e, Axe(2));
        items.Attach(e, new Item { Type = ItemType.Gem, Count = 3 });

        var first = registry.Detach<Item, ItemType>(e, ItemType.Coin);
        var second = items.Detach(e, ItemType.Coin);

        Assert.True(first);
        Assert.False(second);
        Assert.Equal(
            new HashSet<ItemType> { ItemType.Axe, ItemType.Gem },
            items.GetAll(e).ToArray().Select(i => i.Type).ToHashSet());
        Assert.Equal(3, items.Get(e, ItemType.Gem).Count);
    }

    [Fact]
    public void DetachLast_EntityLeavesSet()
    {
        var registry = new Registry();
        Entity e = registry.Create();
        var items = registry.Attachments<Item, ItemType>();
        items.Attach(e, Coin(1));

        items.Detach(e, ItemType.Coin);

        Assert.False(items.Has(e));
        Assert.Equal(0, items.ItemCount(e));
        Assert.True(items.GetAll(e).IsEmpty);
        Assert.Empty(items.Entities.ToArray());
    }

    [Fact]
    public void DetachItems_ReturnsCount()
    {
        var registry = new Registry();
        Entity e = registry.Create();
        var items = registry.Attachments<Item, ItemType>();
        items.Attach(e, Coin(1));
        items.Attach(e, Axe(1));

        var first = items.DetachItems(e);
        var second = items.DetachItems(e);

        Assert.Equal(2, first);
        Assert.Equal(0, second);
        Assert.False(items.Has(e));
    }

    [Fact]
    public void GetAndTryGet_MissingKey()
    {
        var registry = new Registry();
        Entity e = registry.Create();
        var items = registry.Attachments<Item, ItemType>();
        items.Attach(e, Coin(1));

        Assert.Throws<KeyNotFoundException>(() => items.Get(e, ItemType.Axe));
        Assert.False(items.TryGet(e, ItemType.Axe, out _));
        Assert.True(items.TryGet(e, ItemType.Coin, out var coin));
        Assert.Equal(1, coin.Count);
    }

    [Fact]
    public void Attach_DeadEntity_Throws()
    {
        var registry = new Registry();
        Entity e = registry.Create();
        registry.Destroy(e);

        Assert.Throws<InvalidOperationException>(() => registry.Attach<Item, ItemType>(e, Coin(1)));
        Assert.Throws<InvalidOperationException>(() => registry.Attach<Item, ItemType>(Entity.Null, Coin(1)));
    }

    [Fact]
    public void Destroy_DetachesEverything_AndStaleHandlesMiss()
    {
        var registry = new Registry();
        Entity e = registry.Create();
        Entity other = registry.Create();
        var items = registry.Attachments<Item, ItemType>();
        items.Attach(e, Coin(1));
        items.Attach(e, Axe(1));
        items.Attach(other, Coin(5));

        registry.Destroy(e);
        Entity recycled = registry.Create(); // takes e's index

        Assert.Equal(e.Index, recycled.Index);
        Assert.False(items.Has(e));
        Assert.False(items.Has(recycled));
        Assert.Equal([other], items.Entities.ToArray());
        Assert.Equal(5, items.Get(other, ItemType.Coin).Count);
    }

    [Fact]
    public void ManyItems_AllReadable()
    {
        var registry = new Registry();
        Entity e = registry.Create();
        var notes = registry.Attachments<Note, string>();

        for (var i = 0; i < 1000; i++)
        {
            notes.Attach(e, new Note { Topic = $"t{i}", Payload = i });
        }

        Assert.Equal(1000, notes.ItemCount(e));
        Assert.True(notes.SlabCapacity >= 1000);
        for (var i = 0; i < 1000; i++)
        {
            Assert.Equal(i, notes.Get(e, $"t{i}").Payload);
        }
    }

    [Fact]
    public void ShrinkToFit_AfterDetach_KeepsRemainingItems()
    {
        var registry = new Registry();
        Entity e = registry.Create();
        var notes = registry.Attachments<Note, string>();
        for (var i = 0; i < 1000; i++)
        {
            notes.Attach(e, new Note { Topic = $"t{i}", Payload = i });
        }

        var detached = 0;
        for (var i = 5; i < 1000; i++)
        {
            if (notes.Detach(e, $"t{i}"))
            {
                detached++;
            }
        }

        var beforeShrink = notes.SlabCapacity;

        notes.ShrinkToFit();

        Assert.Equal(995, detached);
        Assert.True(beforeShrink >= 1000, "detach gave memory back");
        Assert.True(notes.SlabCapacity <= 64, $"slab still holds {notes.SlabCapacity} entries");
        Assert.Equal(
            Enumerable.Range(0, 5).Select(i => $"t{i}").ToHashSet(),
            notes.GetAll(e).ToArray().Select(n => n.Topic).ToHashSet());
    }

    [Fact]
    public void Memory_ChurnAcrossSizeClasses_ReusesFreeSpace()
    {
        // Freed blocks only serve their own class; without reclaim-on-grow each phase would pile up.
        var registry = new Registry();
        var notes = registry.Attachments<Note, string>();
        for (var round = 0; round < 3; round++)
        {
            for (var sizeClass = 0; sizeClass <= 8; sizeClass++)
            {
                var perEntity = 1 << sizeClass;
                var entities = registry.CreateMany(256 / perEntity);
                foreach (var e in entities)
                {
                    for (var i = 0; i < perEntity; i++)
                    {
                        notes.Attach(e, new Note { Topic = $"t{i}", Payload = i });
                    }
                }

                registry.Destroy(entities);
            }
        }

        Assert.True(notes.SlabCapacity <= 1024, $"slab {notes.SlabCapacity} for 256 live slots");
    }

    [Fact]
    public void Destroy_KeepsSlabCapacity()
    {
        var registry = new Registry();
        var entities = AttachThreeToThousand(registry);
        var items = registry.Attachments<Item, ItemType>();
        var full = items.SlabCapacity;

        DestroyNineInTen(registry, entities);

        Assert.Equal(full, items.SlabCapacity); // destroy only frees blocks
    }

    [Fact]
    public void Compact_ReusesFreedBlocksWithoutGrowing()
    {
        var registry = new Registry();
        DestroyNineInTen(registry, AttachThreeToThousand(registry));
        var items = registry.Attachments<Item, ItemType>();
        var full = items.SlabCapacity;
        items.Compact();

        // Compact keeps the length; 500 more 4-slot blocks then fit without a grow.
        var more = Enumerable.Range(0, 500).Select(_ => (Entity)registry.Create()).ToList();
        foreach (var e in more)
        {
            items.Attach(e, Coin(2));
            items.Attach(e, Axe(2));
            items.Attach(e, new Item { Type = ItemType.Gem });
        }

        Assert.Equal(full, items.SlabCapacity);
        Assert.All(items.Entities.ToArray(), e => Assert.Equal(3, items.ItemCount(e)));
    }

    [Fact]
    public void ShrinkToFit_KeepsLiveItems()
    {
        var registry = new Registry();
        DestroyNineInTen(registry, AttachThreeToThousand(registry));
        var items = registry.Attachments<Item, ItemType>();

        items.ShrinkToFit();

        // 100 entities x 3 items, each fitted to a 4-slot block.
        Assert.Equal(400, items.SlabCapacity);
        Assert.Equal(100, items.Entities.Length);
        Assert.All(items.Entities.ToArray(), e =>
        {
            Assert.Equal(3, items.ItemCount(e));
            Assert.Equal(1, items.Get(e, ItemType.Coin).Count);
            Assert.True(items.Has(e, ItemType.Potion));
        });
    }

    private static List<Entity> AttachThreeToThousand(Registry registry)
    {
        var entities = Enumerable.Range(0, 1000).Select(_ => (Entity)registry.Create()).ToList();
        var items = registry.Attachments<Item, ItemType>();
        foreach (var e in entities)
        {
            items.Attach(e, Coin(1));
            items.Attach(e, Axe(1));
            items.Attach(e, new Item { Type = ItemType.Potion });
        }

        return entities;
    }

    private static void DestroyNineInTen(Registry registry, List<Entity> entities)
    {
        foreach (var e in entities.Where((_, k) => k % 10 != 0))
        {
            registry.Destroy(e);
        }
    }

    [Fact]
    public void All_WalksEveryItemOnce_AndEditsInPlace()
    {
        var registry = new Registry();
        var items = registry.Attachments<Item, ItemType>();
        var expected = new HashSet<(Entity, ItemType)>();
        for (var i = 0; i < 50; i++)
        {
            Entity e = registry.Create();
            for (var k = 0; k < i % 4; k++) // some entities hold nothing
            {
                items.Attach(e, new Item { Type = (ItemType)k, Count = 1 });
                expected.Add((e, (ItemType)k));
            }
        }

        var seen = new List<(Entity, ItemType)>();
        foreach (var a in items.All)
        {
            seen.Add((a.Entity, a.Item.Type));
            a.Item.Count = 5;
        }

        Assert.Equal(expected.Count, seen.Count);
        Assert.Equal(expected, seen.ToHashSet());
        Assert.All(expected, p => Assert.Equal(5, items.Get(p.Item1, p.Item2).Count));
    }

    [Fact]
    public void All_EmptySet_WalksNothing()
    {
        var registry = new Registry();
        var notes = registry.Attachments<Note, string>();

        var walked = 0;
        foreach (var _ in notes.All)
        {
            walked++;
        }

        Assert.Equal(0, walked);
    }

    [Fact]
    public void View_WithAndWithout_UseHasAny()
    {
        var registry = new Registry();
        Entity holder = registry.Create();
        Entity emptied = registry.Create();
        Entity never = registry.Create();
        foreach (var e in new[] { holder, emptied, never })
        {
            registry.Add(e, new Pos());
            registry.Add(e, new Vel());
        }

        var items = registry.Attachments<Item, ItemType>();
        items.Attach(holder, Coin(1));
        items.Attach(emptied, Coin(1));
        items.Detach(emptied, ItemType.Coin);

        var with = new List<Entity>();
        registry.View().WithAttachment<Item>().Each((Entity e, ref Pos _, ref Vel _) => with.Add(e));
        var without = new List<Entity>();
        registry.View().WithoutAttachment<Item>().Each((Entity e, ref Pos _, ref Vel _) => without.Add(e));

        Assert.Equal([holder], with);
        Assert.Equal(new HashSet<Entity> { emptied, never }, without.ToHashSet());
    }

    // Losing its last item takes an entity out of a WithAttachment walk that hasn't reached it.
    [Fact]
    public void View_WithAttachment_DropsAnEntityWhoseLastItemGoesMidWalk()
    {
        var registry = new Registry();
        var items = registry.Attachments<Item, ItemType>();
        Entity visitedLast = registry.Create(new Pos(), new Vel()); // walks run last slot first
        Entity visitedFirst = registry.Create(new Pos(), new Vel());
        items.Attach(visitedLast, Coin(1));
        items.Attach(visitedFirst, Coin(1));

        var seen = new List<Entity>();
        registry.View().WithAttachment<Item>().Each((Entity e, ref Pos _, ref Vel _) =>
        {
            seen.Add(e);
            items.Detach(visitedLast, ItemType.Coin);
        });

        Assert.Equal([visitedFirst], seen);
    }

    [Fact]
    public void Snapshot_RoundTrip_Blit()
    {
        var source = new Registry();
        Entity a = source.Create();
        Entity b = source.Create();
        Entity c = source.Create();
        var items = source.Attachments<Item, ItemType>();
        items.Attach(a, Coin(3));
        items.Attach(a, Axe(1));
        items.Attach(c, new Item { Type = ItemType.Gem, Count = 9 });
        source.Destroy(b);

        var archive = new Archive();
        source.Snapshot<Archive>().Entities(ref archive).Attachment<Item, ItemType>(ref archive);

        var loaded = new Registry();
        var reader = archive.Reopen();
        loaded.SnapshotLoader<Archive>().Entities(ref reader).Attachment<Item, ItemType>(ref reader);

        var restored = loaded.Attachments<Item, ItemType>();
        Assert.Equal(2, restored.ItemCount(a));
        Assert.Equal(3, restored.Get(a, ItemType.Coin).Count);
        Assert.Equal(1, restored.Get(a, ItemType.Axe).Count);
        Assert.Equal(9, restored.Get(c, ItemType.Gem).Count);
        Assert.False(restored.Has(b));
    }

    [Fact]
    public void Snapshot_RoundTrip_ReferenceFields()
    {
        var source = new Registry();
        Entity e = source.Create();
        var notes = source.Attachments<Note, string>();
        notes.Attach(e, new Note { Topic = "quest", Payload = "find the axe" });
        notes.Attach(e, new Note { Topic = "lore" });

        var archive = new Archive();
        source.Snapshot<Archive>().Entities(ref archive).Attachment<Note, string>(ref archive);

        var loaded = new Registry();
        var reader = archive.Reopen();
        loaded.SnapshotLoader<Archive>().Entities(ref reader).Attachment<Note, string>(ref reader);

        var restored = loaded.Attachments<Note, string>();
        Assert.Equal(2, restored.ItemCount(e));
        Assert.Equal("find the axe", restored.Get(e, "quest").Payload?.ToString()); // object comes back as a JsonElement
    }

    [Fact]
    public void SnapshotLoader_BadCount_ThrowsBeforeAttaching()
    {
        var archive = new Archive();
        var source = new Registry();
        Entity e = source.Create();
        source.Snapshot<Archive>().Entities(ref archive);
        archive.BeginSection(SnapshotSection.Attachment, typeof(Item), null);
        archive.WriteCount(1);         // one owner
        archive.WriteEntity(e);
        archive.WriteCount(1_000_000); // claims a million items, the archive has none
        archive.EndSection();

        var loaded = new Registry();
        var reader = archive.Reopen();
        var loader = loaded.SnapshotLoader<Archive>().Entities(ref reader);

        Assert.Throws<InvalidDataException>(() => loader.Attachment<Item, ItemType>(ref reader));
        Assert.False(loaded.Attachments<Item, ItemType>().Has(e));
    }

    [Fact]
    public void ContinuousLoader_RemapsEntityFields()
    {
        var remote = new Registry();
        Entity hero = remote.Create();
        Entity sword = remote.Create();
        remote.Attach<Grip, int>(hero, new Grip { Hand = 0, Held = sword });
        remote.Attach<Item, ItemType>(hero, Coin(10));
        var local = new Registry();
        var loader = local.ContinuousLoader<Archive>();
        var archive = new Archive();
        remote.Snapshot<Archive>().Entities(ref archive)
            .Attachment<Grip, int>(ref archive).Attachment<Item, ItemType>(ref archive);
        var reader = archive.Reopen();

        loader.Entities(ref reader)
            .Attachment<Grip, int>(ref reader, (g, map) => g with { Held = map(g.Held) })
            .Attachment<Item, ItemType>(ref reader)
            .Orphans();

        var localHero = loader.Map(hero);
        Assert.Equal(loader.Map(sword), local.Attachments<Grip, int>().Get(localHero, 0).Held);
        Assert.Equal(10, local.Attachments<Item, ItemType>().Get(localHero, ItemType.Coin).Count);
    }

    // Random attach / detach / detach-all / destroy against a dictionary model.
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void RandomOperations_MatchModel(int seed)
    {
        var rng = new Random(seed);
        var registry = new Registry();
        var items = registry.Attachments<Item, ItemType>();
        var model = new Dictionary<Entity, Dictionary<ItemType, int>>();
        var alive = new List<Entity>();
        var types = Enum.GetValues<ItemType>();

        for (var step = 0; step < 20_000; step++)
        {
            var op = rng.Next(100);
            if (alive.Count < 10 || op < 5)
            {
                Entity e = registry.Create();
                alive.Add(e);
                model[e] = [];
                continue;
            }

            var target = alive[rng.Next(alive.Count)];
            var type = types[rng.Next(types.Length)];
            if (op < 60)
            {
                var count = rng.Next(1000);
                items.Attach(target, new Item { Type = type, Count = count });
                model[target][type] = count;
            }
            else if (op < 90)
            {
                Assert.Equal(model[target].Remove(type), items.Detach(target, type));
            }
            else if (op < 95)
            {
                Assert.Equal(model[target].Count, items.DetachItems(target));
                model[target].Clear();
            }
            else
            {
                registry.Destroy(target);
                alive.Remove(target);
                model.Remove(target);
                Assert.False(items.Has(target));
            }
        }

        var owners = model.Where(kv => kv.Value.Count > 0).Select(kv => kv.Key).ToHashSet();
        Assert.Equal(owners, items.Entities.ToArray().ToHashSet());
        foreach (var (e, expected) in model)
        {
            Assert.Equal(expected, items.GetAll(e).ToArray().ToDictionary(i => i.Type, i => i.Count));
        }
    }
}
