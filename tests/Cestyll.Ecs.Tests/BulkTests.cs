using Cestyll.Ecs;
using Cestyll.Ecs.Groups;
using Cestyll.Ecs.Tags;

namespace Cestyll.Ecs.Tests;

public class BulkTests
{
    private struct Position : IComponent { public float X; }
    private struct Velocity : IComponent;
    private struct Name : IComponent { public string Value; }
    private struct Marked : ITag;
    private struct Other : ITag;

    [Fact]
    public void CreateMany_RecyclesLowestFirst_ThenFresh()
    {
        var registry = new Registry();
        var first = registry.CreateMany(4);
        registry.Destroy(first[2]);
        registry.Destroy(first[0]);

        var more = registry.CreateMany(3);

        Assert.Equal([1, 3, 5], more.Select(e => e.Index));
        Assert.All(more, e => Assert.True(registry.IsAlive(e)));
        Assert.Equal(5, registry.AliveCount);
    }

    [Fact]
    public void CreateMany_FillsSpan()
    {
        var registry = new Registry();
        Span<Entity> entities = stackalloc Entity[3];
        registry.CreateMany(entities);

        Assert.Equal(3, registry.AliveCount);
        foreach (var e in entities)
        {
            Assert.True(registry.IsAlive(e));
        }
    }

    [Fact]
    public void AddMany_SameValue_ReachesEveryEntity()
    {
        var registry = new Registry();
        var entities = registry.CreateMany(100);

        registry.Add(entities, new Position { X = 7 });

        Assert.Equal(100, registry.ComponentCount<Position>());
        Assert.All(entities, e => Assert.Equal(7, registry.Get<Position>(e).X));
    }

    [Fact]
    public void AddValues_PairsByIndex()
    {
        var registry = new Registry();
        var entities = registry.CreateMany(3);
        var values = new[] { new Position { X = 1 }, new Position { X = 2 }, new Position { X = 3 } };

        registry.AddValues(entities, values);

        Assert.Equal([1f, 2f, 3f], entities.Select(e => registry.Get<Position>(e).X));
    }

    [Fact]
    public void AddValues_LengthMismatch_Throws()
    {
        var registry = new Registry();
        var entities = registry.CreateMany(2);

        Assert.Throws<ArgumentException>(() => registry.AddValues(entities, new Position[1]));
        Assert.Equal(0, registry.ComponentCount<Position>());
    }

    [Fact]
    public void AddMany_DeadEntity_Throws()
    {
        var registry = new Registry();
        var entities = registry.CreateMany(2);
        registry.Destroy(entities[1]);

        Assert.Throws<InvalidOperationException>(() => registry.Add(entities, new Position()));
    }

    [Fact]
    public void AddMany_FiresOnAddPerEntity_AndFeedsGroups()
    {
        var registry = new Registry();
        var entities = registry.CreateMany(4);
        var group = registry.Group().Own<Position>().Own<Velocity>().Build();
        var constructed = new List<Entity>();
        registry.Storage<Position>().OnAdd += constructed.Add;

        registry.Add(entities, new Velocity());
        registry.Add(entities.AsSpan(0, 3), new Position());

        Assert.Equal(entities[..3], constructed);
        Assert.Equal(3, group.Count);
    }

    [Fact]
    public void Destroy_Span_SkipsDeadAndDuplicates()
    {
        var registry = new Registry();
        var entities = registry.CreateMany(4);
        registry.Destroy(entities[1]);

        registry.Destroy([entities[0], entities[1], entities[0], entities[3]]);

        Assert.Equal(1, registry.AliveCount);
        Assert.True(registry.IsAlive(entities[2]));
    }

    [Fact]
    public void Destroy_PoolsOwnEntities_DestroysAll()
    {
        var registry = new Registry();
        var entities = registry.CreateMany(10);
        registry.Add(entities, new Position());
        Entity keep = registry.Create();

        registry.Destroy(registry.Storage<Position>().Entities);

        Assert.Equal(0, registry.ComponentCount<Position>());
        Assert.Equal(1, registry.AliveCount);
        Assert.True(registry.IsAlive(keep));
    }

    [Fact]
    public void Destroy_GroupsOwnEntities_DestroysAll()
    {
        var registry = new Registry();
        var entities = registry.CreateMany(10);
        registry.Add(entities, new Position());
        registry.Add(entities.AsSpan(0, 6), new Velocity());
        var group = registry.Group().Own<Position>().Own<Velocity>().Build();

        registry.Destroy(group.Entities);

        Assert.Equal(0, group.Count);
        Assert.Equal(4, registry.AliveCount);
        Assert.Equal(4, registry.ComponentCount<Position>());
    }

    [Fact]
    public void Remove_Span_SkipsMissingAndDuplicates()
    {
        var registry = new Registry();
        var entities = registry.CreateMany(6);
        registry.Add(entities.AsSpan(0, 4), new Position());

        var removed = registry.Remove<Position>([entities[0], entities[5], entities[0], entities[2]]);

        Assert.Equal(2, removed);
        Assert.Equal(6, registry.AliveCount);
    }

    [Fact]
    public void Remove_PoolsOwnEntities_RemovesAll()
    {
        var registry = new Registry();
        var entities = registry.CreateMany(6);
        registry.Add(entities.AsSpan(0, 4), new Position());

        var removed = registry.Remove<Position>(registry.Storage<Position>().Entities);

        Assert.Equal(4, removed);
        Assert.Equal(0, registry.ComponentCount<Position>());
    }

    [Fact]
    public void Remove_AbsentPool_ReturnsZero()
    {
        var registry = new Registry();
        var entities = registry.CreateMany(6);

        var removed = registry.Remove<Velocity>(entities);

        Assert.Equal(0, removed);
    }

    [Fact]
    public void ClearT_RemovesComponentOnly_AndFiresOnRemove()
    {
        var registry = new Registry();
        var entities = registry.CreateMany(5);
        registry.Add(entities, new Position());
        registry.Add(entities, new Velocity());
        var destroyed = new List<Entity>();
        registry.Storage<Position>().OnRemove += e =>
        {
            Assert.True(registry.Has<Position>(e)); // still readable in the handler
            destroyed.Add(e);
        };

        registry.Clear<Position>();

        Assert.Equal(0, registry.ComponentCount<Position>());
        Assert.Equal(5, registry.ComponentCount<Velocity>());
        Assert.Equal(5, registry.AliveCount);
        Assert.Equal(entities.OrderBy(e => e.Index), destroyed.OrderBy(e => e.Index));
    }

    [Fact]
    public void ClearT_Unhooked_DropsAll()
    {
        var registry = new Registry();
        var entities = registry.CreateMany(5);
        registry.Add(entities, new Name { Value = "x" });

        registry.Clear<Name>();

        Assert.Equal(0, registry.ComponentCount<Name>());
        Assert.All(entities, e => Assert.False(registry.Has<Name>(e)));
    }

    [Fact]
    public void ClearT_Unhooked_PoolIsReusable()
    {
        var registry = new Registry();
        var entities = registry.CreateMany(5);
        registry.Add(entities, new Name { Value = "x" });
        registry.Clear<Name>();

        registry.Add(entities[3], new Name { Value = "y" });

        Assert.Equal("y", registry.Get<Name>(entities[3]).Value);
    }

    [Fact]
    public void ClearT_OwnedPool_EmptiesGroup()
    {
        var registry = new Registry();
        var entities = registry.CreateMany(5);
        registry.Add(entities, new Position());
        registry.Add(entities, new Velocity());
        var group = registry.Group().Own<Position>().Own<Velocity>().Build();

        registry.Clear<Velocity>();

        Assert.Equal(0, group.Count);
        Assert.Equal(5, registry.ComponentCount<Position>());
    }

    [Fact]
    public void Clear_DestroysEverything()
    {
        var registry = new Registry();
        var entities = registry.CreateMany(8);
        registry.Add(entities, new Position());
        registry.Tag<Marked>(entities[2]);

        registry.Clear();

        Assert.Equal(0, registry.AliveCount);
        Assert.Equal(0, registry.ComponentCount<Position>());
        Assert.Equal(0, registry.TagCount<Marked>());
        Assert.All(entities, e => Assert.False(registry.IsAlive(e)));
    }

    [Fact]
    public void Clear_RecycledIndex_GetsNewVersion()
    {
        var registry = new Registry();
        var entities = registry.CreateMany(8);
        registry.Add(entities, new Position());
        registry.Tag<Marked>(entities[2]);
        registry.Clear();

        Entity next = registry.Create();

        Assert.Equal(entities[0].Index, next.Index);
        Assert.NotEqual(entities[0], next);
    }

    [Fact]
    public void Clear_HandlerDestroyingOthers_IsFine()
    {
        var registry = new Registry();
        var entities = registry.CreateMany(6);
        registry.Add(entities, new Position());
        registry.Storage<Position>().OnRemove += _ =>
        {
            foreach (var other in entities)
            {
                registry.Destroy(other);
            }
        };

        registry.Clear();

        Assert.Equal(0, registry.AliveCount);
        Assert.Equal(0, registry.ComponentCount<Position>());
    }

    [Fact]
    public void ClearTags_Unhooked_ClearsOnlyThatTag()
    {
        var registry = new Registry();
        var entities = registry.CreateMany(100);
        foreach (var e in entities)
        {
            registry.Tag<Marked>(e);
            registry.Tag<Other>(e);
        }

        registry.ClearTags<Marked>();

        Assert.Equal(0, registry.TagCount<Marked>());
        Assert.Equal(100, registry.TagCount<Other>());
        Assert.All(entities, e => Assert.False(registry.HasTag<Marked>(e)));
    }

    [Fact]
    public void ClearTags_Unhooked_SetIsReusable()
    {
        var registry = new Registry();
        var entities = registry.CreateMany(100);
        foreach (var e in entities)
        {
            registry.Tag<Marked>(e);
            registry.Tag<Other>(e);
        }

        registry.ClearTags<Marked>();

        registry.Tag<Marked>(entities[5]);

        Assert.Equal(1, registry.TagCount<Marked>());
    }

    [Fact]
    public void ClearTags_FiresOnUntagWithCurrentHandles()
    {
        var registry = new Registry();
        var entities = registry.CreateMany(4);
        registry.Destroy(entities[1]);
        var recycled = registry.Create().Entity; // index 2 again, newer version
        Entity[] tagged = [entities[0], recycled, entities[3]];
        foreach (var e in tagged)
        {
            registry.Tag<Marked>(e);
        }

        var untagged = new List<Entity>();
        registry.Tags<Marked>().OnUntag += e =>
        {
            Assert.True(registry.HasTag<Marked>(e)); // still set in the handler
            untagged.Add(e);
        };

        registry.ClearTags<Marked>();

        Assert.Equal(tagged, untagged);
        Assert.Equal(0, registry.TagCount<Marked>());
        Assert.Equal(4, registry.AliveCount);
    }

    [Fact]
    public void ClearTags_HandlerUntaggingOthers_IsFine()
    {
        var registry = new Registry();
        var entities = registry.CreateMany(6);
        foreach (var e in entities)
        {
            registry.Tag<Marked>(e);
        }

        var calls = 0;
        registry.Tags<Marked>().OnUntag += _ =>
        {
            calls++;
            registry.Untag<Marked>(entities[^1]);
        };

        registry.ClearTags<Marked>();

        Assert.Equal(6, calls);
        Assert.Equal(0, registry.TagCount<Marked>());
    }

    [Fact]
    public void ClearTags_EmptiesGroupRequiringTag()
    {
        var registry = new Registry();
        var entities = registry.CreateMany(5);
        registry.Add(entities, new Position());
        registry.Add(entities, new Velocity());
        foreach (var e in entities)
        {
            registry.Tag<Marked>(e);
        }

        var group = registry.Group().Own<Position>().Own<Velocity>().WithTag<Marked>().Build();

        registry.ClearTags<Marked>();

        Assert.Equal(0, group.Count);
    }

    [Fact]
    public void DestroyTagged_DestroysOnlyTagged()
    {
        var registry = new Registry();
        var entities = registry.CreateMany(130);
        registry.Add(entities, new Position());
        for (var i = 0; i < entities.Length; i += 3)
        {
            registry.Tag<Marked>(entities[i]);
        }

        registry.DestroyTagged<Marked>();

        for (var i = 0; i < entities.Length; i++)
        {
            Assert.Equal(i % 3 != 0, registry.IsAlive(entities[i]));
        }

        Assert.Equal(0, registry.TagCount<Marked>());
        Assert.Equal(86, registry.ComponentCount<Position>());
    }

    [Fact]
    public void DestroyTagged_NoTagSet_CreatesNone()
    {
        var registry = new Registry();
        Entity e = registry.Create();

        registry.DestroyTagged<Marked>();

        Assert.True(registry.IsAlive(e));
        Assert.Null(registry.FindTags<Marked>());
    }

    [Fact]
    public void DestroyTagged_HandlerDestroyingAnotherTagged_DestroysEachOnce()
    {
        var registry = new Registry();
        var entities = registry.CreateMany(4);
        registry.Add(entities, new Position());
        foreach (var e in entities)
        {
            registry.Tag<Marked>(e);
        }

        var destroyed = new List<Entity>();
        registry.OnRemove<Position>(e =>
        {
            destroyed.Add(e);
            registry.Destroy(entities[^1]);
        });

        registry.DestroyTagged<Marked>();

        Assert.Equal(entities.Length, destroyed.Distinct().Count());
        Assert.Equal(entities.Length, destroyed.Count);
        Assert.Equal(0, registry.AliveCount);
    }

    [Fact]
    public void DestroyTagged_InDeferScope_AppliesAtEnd()
    {
        var registry = new Registry();
        var entities = registry.CreateMany(3);
        registry.Tag<Marked>(entities[0]);
        registry.Tag<Marked>(entities[2]);

        using (registry.Defer())
        {
            registry.DestroyTagged<Marked>();
            Assert.Equal(3, registry.AliveCount);
        }

        Assert.False(registry.IsAlive(entities[0]));
        Assert.True(registry.IsAlive(entities[1]));
        Assert.False(registry.IsAlive(entities[2]));
    }
}
