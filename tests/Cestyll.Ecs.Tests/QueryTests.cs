using Cestyll.Ecs;
using Cestyll.Ecs.Attachments;
using Cestyll.Ecs.Groups;
using Cestyll.Ecs.Relations;
using Cestyll.Ecs.Tags;

namespace Cestyll.Ecs.Tests;

public class QueryTests
{
    private struct Position : IComponent { public int X; }
    private struct Velocity : IComponent;
    private struct Health : IComponent { public int V; }

    private struct Selected : ITag;
    private struct Hidden : ITag;
    private struct Frozen : ITag;

    private struct Parent : IRelation
    {
        public static OnTargetDestroyed Policy => OnTargetDestroyed.Unrelate;
    }

    private struct Likes(Entity target) : ILink<Entity>
    {
        public readonly Entity Target => target;
    }

    private struct Item : IAttachment<int>
    {
        public int Id;
        public readonly int Key => Id;
    }

    [Fact]
    public void TryGet_CopiesWhenPresent()
    {
        var registry = new Registry();
        var e = registry.Create(new Position { X = 3 });

        Assert.True(registry.TryGet<Position>(e, out var p));
        Assert.Equal(3, p.X);
        Assert.False(registry.TryGet<Velocity>(e, out var v));
        Assert.Equal(default, v);
        Assert.True(e.TryGet<Position>(out _));
    }

    [Fact]
    public void TryGet_MissesStaleHandle()
    {
        var registry = new Registry();
        Entity e = registry.Create(new Position { X = 3 });
        registry.Destroy(e);
        registry.Create(new Position { X = 9 }); // recycles the index

        Assert.False(registry.TryGet<Position>(e, out _));
    }

    [Fact]
    public void TryGet_NeverCreatesPool()
    {
        var registry = new Registry();
        Entity e = registry.Create();

        var found = registry.TryGet<Health>(e, out _);

        Assert.False(found);
        Assert.Null(registry.FindStorage<Health>());
    }

    [Fact]
    public void GetOrAdd_AddsOnceThenReturnsExisting()
    {
        var registry = new Registry();
        var constructed = 0;
        registry.Storage<Health>().OnAdd += _ => constructed++;
        Entity e = registry.Create();
        registry.GetOrAdd(e, new Health { V = 5 }).V++;

        var value = registry.GetOrAdd(e, new Health { V = 100 }).V;

        Assert.Equal(6, value);
        Assert.Equal(1, constructed);
    }

    [Fact]
    public void Handle_GetOrAdd_AddsDefault()
    {
        var registry = new Registry();
        var e = registry.Create();

        var x = e.GetOrAdd<Position>().X;

        Assert.Equal(0, x);
        Assert.True(e.Has<Position>());
    }

    [Fact]
    public void GetOrAdd_RejectsDeadEntity()
    {
        var registry = new Registry();
        Entity e = registry.Create();
        registry.Destroy(e);

        Assert.Throws<InvalidOperationException>(() => registry.GetOrAdd<Health>(e));
    }

    [Fact]
    public void HasAllAndHasAny()
    {
        var registry = new Registry();
        var both = registry.Create(new Position(), new Velocity());
        var one = registry.Create(new Position());
        var none = registry.Create();

        Assert.True(registry.HasAll<Position, Velocity>(both));
        Assert.False(registry.HasAll<Position, Velocity>(one));
        Assert.False(registry.HasAll<Position, Velocity, Health>(both));
        Assert.True(registry.HasAny<Velocity, Health>(both));
        Assert.True(registry.HasAny<Health, Position>(one));
        Assert.False(registry.HasAny<Position, Velocity>(none));
        Assert.True(both.HasAll<Velocity, Position>());
        Assert.False(one.HasAny<Velocity, Health>());
    }

    [Fact]
    public void HasAll_FalseForDeadEntity()
    {
        var registry = new Registry();
        Entity e = registry.Create(new Position(), new Velocity());
        registry.Destroy(e);

        Assert.False(registry.HasAll<Position, Velocity>(e));
        Assert.False(registry.HasAny<Position, Velocity>(e));
    }

    [Fact]
    public void IsOrphan_TrueForFreshEntity()
    {
        var registry = new Registry();
        var e = registry.Create();

        var orphan = e.IsOrphan;

        Assert.True(orphan);
    }

    [Fact]
    public void IsOrphan_FalseWithComponent()
    {
        var registry = new Registry();
        var e = registry.Create();

        e.Add(new Position());

        Assert.False(e.IsOrphan);
    }

    [Fact]
    public void IsOrphan_FalseWithTag()
    {
        var registry = new Registry();
        var e = registry.Create();

        e.Tag<Selected>();

        Assert.False(e.IsOrphan);
    }

    [Fact]
    public void IsOrphan_FalseWithAttachment()
    {
        var registry = new Registry();
        var e = registry.Create();

        e.Attach<Item, int>(new Item { Id = 1 });

        Assert.False(e.IsOrphan);
    }

    // Being a target counts too.
    [Fact]
    public void IsOrphan_FalseAsRelationSourceOrTarget()
    {
        var registry = new Registry();
        Entity e = registry.Create();
        Entity other = registry.Create();

        registry.Relate<Parent>(e, other);

        Assert.False(registry.IsOrphan(e));
        Assert.False(registry.IsOrphan(other));
    }

    [Fact]
    public void IsOrphan_FalseAsLinkTarget()
    {
        var registry = new Registry();
        Entity e = registry.Create();
        Entity other = registry.Create();

        registry.Links<Likes>().Link(other, new Likes(e));

        Assert.False(registry.IsOrphan(e));
    }

    [Fact]
    public void IsOrphan_TrueOnceEveryStoreIsEmptied()
    {
        var registry = new Registry();
        Entity e = registry.Create(new Position());
        Entity other = registry.Create();
        registry.Tag<Selected>(e);
        registry.Attach<Item, int>(e, new Item { Id = 1 });
        registry.Relate<Parent>(e, other);
        registry.Links<Likes>().Link(other, new Likes(e));

        registry.Remove<Position>(e);
        registry.Untag<Selected>(e);
        registry.Detach<Item, int>(e, 1);
        registry.Unrelate<Parent>(e);
        registry.Links<Likes>().Unlink(other, e);

        Assert.True(registry.IsOrphan(e));
        Assert.True(registry.IsOrphan(other));
    }

    [Fact]
    public void IsOrphan_FalseForDeadOrNull()
    {
        var registry = new Registry();
        Entity e = registry.Create();
        registry.Destroy(e);

        Assert.False(registry.IsOrphan(e));
        Assert.False(registry.IsOrphan(default));
    }

    [Fact]
    public void AliveCount_TracksCreateAndDestroy()
    {
        var registry = new Registry();
        var empty = registry.AliveCount;
        Entity a = registry.Create(new Position());
        registry.Create(new Position(), new Velocity());
        registry.Create();
        var created = registry.AliveCount;

        registry.Destroy(a);

        Assert.Equal(0, empty);
        Assert.Equal(3, created);
        Assert.Equal(2, registry.AliveCount);
    }

    [Fact]
    public void ComponentCount_TracksCreateAndDestroy()
    {
        var registry = new Registry();
        Entity a = registry.Create(new Position());
        registry.Create(new Position(), new Velocity());
        registry.Create();
        var positions = registry.ComponentCount<Position>();
        var velocities = registry.ComponentCount<Velocity>();
        var healths = registry.ComponentCount<Health>();

        registry.Destroy(a);

        Assert.Equal(2, positions);
        Assert.Equal(1, velocities);
        Assert.Equal(0, healths);
        Assert.Equal(1, registry.ComponentCount<Position>());
    }

    [Fact]
    public void Entities_WalksLiveEntitiesInIndexOrder()
    {
        var registry = new Registry();
        Entity a = registry.Create();
        Entity b = registry.Create();
        Entity c = registry.Create();
        Entity d = registry.Create();
        registry.Destroy(b);
        registry.Destroy(d);
        var seen = new List<Entity>();

        foreach (var e in registry.Entities)
        {
            seen.Add(e);
        }

        Assert.Equal([a, c], seen);
    }

    [Fact]
    public void Each_WalksLiveEntitiesInIndexOrder()
    {
        var registry = new Registry();
        Entity a = registry.Create();
        Entity b = registry.Create();
        Entity c = registry.Create();
        Entity d = registry.Create();
        registry.Destroy(b);
        registry.Destroy(d);
        var seen = new List<Entity>();

        registry.Each(seen.Add);

        Assert.Equal([a, c], seen);
    }

    [Fact]
    public void Entities_YieldsCurrentVersions()
    {
        var registry = new Registry();
        Entity old = registry.Create();
        registry.Destroy(old);
        Entity recycled = registry.Create();

        var seen = new List<Entity>();
        registry.Each(seen.Add);
        Assert.Equal([recycled], seen);
        Assert.NotEqual(old, recycled);
    }

    [Fact]
    public void Entities_AllowsDestroyDuringWalk()
    {
        var registry = new Registry();
        var all = new List<Entity>();
        for (var i = 0; i < 100; i++)
        {
            all.Add(registry.Create());
        }

        // Destroy the current entity and the one after it: every other entity is visited.
        var seen = new List<Entity>();
        foreach (var e in registry.Entities)
        {
            seen.Add(e);
            var next = all.IndexOf(e) + 1;
            registry.Destroy(e);
            if (next < all.Count)
            {
                registry.Destroy(all[next]);
            }
        }
        Assert.Equal(all.Where((_, i) => i % 2 == 0), seen);
        Assert.Equal(0, registry.AliveCount);
    }

    [Fact]
    public void Entities_EmptyRegistry()
    {
        var registry = new Registry();
        var count = 0;
        registry.Each(_ => count++);
        Assert.Equal(0, count);
    }

    [Fact]
    public void GetComponentTypes_ListsOnlyComponents()
    {
        var registry = new Registry();
        registry.Storage<Health>(); // a pool e isn't in
        var e = registry.Create(new Position(), new Velocity());
        e.Tag<Selected>();

        var types = GetComponentTypes(registry, e);

        Assert.Equal([typeof(Position), typeof(Velocity)], types);
    }

    [Fact]
    public void Handle_ComponentTypes_ListsOnlyComponents()
    {
        var registry = new Registry();
        registry.Storage<Health>(); // a pool e isn't in
        var e = registry.Create(new Position(), new Velocity());
        e.Tag<Selected>();
        var viaHandle = new List<Type>();

        foreach (var t in e.ComponentTypes)
        {
            viaHandle.Add(t);
        }

        Assert.Equal([typeof(Position), typeof(Velocity)], viaHandle);
    }

    [Fact]
    public void GetComponentTypes_EmptyForDeadEntity()
    {
        var registry = new Registry();
        Entity e = registry.Create(new Position());
        registry.Destroy(e);

        Assert.Empty(GetComponentTypes(registry, e));
    }

    [Fact]
    public void HasAllTagsAndHasAnyTags()
    {
        var registry = new Registry();
        var both = registry.Create().Tag<Selected>().Tag<Hidden>();
        var one = registry.Create().Tag<Selected>();
        var none = registry.Create();

        Assert.True(registry.HasAllTags<Selected, Hidden>(both));
        Assert.False(registry.HasAllTags<Selected, Hidden>(one));
        Assert.False(registry.HasAllTags<Selected, Hidden, Frozen>(both));
        Assert.True(registry.HasAnyTags<Frozen, Selected>(one));
        Assert.False(registry.HasAnyTags<Selected, Hidden>(none));
        Assert.True(both.HasAllTags<Hidden, Selected>());
        Assert.False(one.HasAnyTags<Hidden, Frozen>());
    }

    [Fact]
    public void HasAllTags_FalseForDeadEntityOnRecycledIndex()
    {
        var registry = new Registry();
        Entity e = registry.Create().Tag<Selected>().Tag<Hidden>();
        registry.Destroy(e);
        registry.Create().Tag<Selected>().Tag<Hidden>(); // same index, new version

        Assert.False(registry.HasAllTags<Selected, Hidden>(e));
        Assert.False(registry.HasAnyTags<Selected, Hidden>(e));
    }

    [Fact]
    public void TagCount_ZeroForAnUnusedTag()
    {
        var registry = new Registry();
        registry.Create().Tag<Selected>();

        var count = registry.TagCount<Frozen>();

        Assert.Equal(0, count);
        Assert.Null(registry.FindTags<Frozen>());
    }

    [Fact]
    public void TagCount_TracksTagAndDestroy()
    {
        var registry = new Registry();
        Entity a = registry.Create().Tag<Selected>();
        registry.Create().Tag<Selected>();
        var tagged = registry.TagCount<Selected>();

        registry.Destroy(a);

        Assert.Equal(2, tagged);
        Assert.Equal(1, registry.TagCount<Selected>());
    }

    [Fact]
    public void GetTagTypes_ListsOnlyTags()
    {
        var registry = new Registry();
        registry.Tags<Frozen>(); // a tag set e isn't in
        var e = registry.Create(new Position()).Tag<Selected>().Tag<Hidden>();

        var types = GetTagTypes(registry, e);

        Assert.Equal([typeof(Selected), typeof(Hidden)], types);
    }

    [Fact]
    public void Handle_TagTypes_ListsOnlyTags()
    {
        var registry = new Registry();
        registry.Tags<Frozen>(); // a tag set e isn't in
        var e = registry.Create(new Position()).Tag<Selected>().Tag<Hidden>();
        var viaHandle = new List<Type>();

        foreach (var t in e.TagTypes)
        {
            viaHandle.Add(t);
        }

        Assert.Equal([typeof(Selected), typeof(Hidden)], viaHandle);
    }

    [Fact]
    public void GetTagTypes_NamesTagSetsCreatedByGroups()
    {
        var registry = new Registry();
        registry.Group().Own<Position>().Own<Velocity>().WithoutTag<Frozen>().Build();
        var e = registry.Create().Tag<Frozen>();

        Assert.Equal([typeof(Frozen)], GetTagTypes(registry, e));
    }

    [Fact]
    public void GetTagTypes_EmptyForDeadEntityOnRecycledIndex()
    {
        var registry = new Registry();
        Entity e = registry.Create().Tag<Selected>();
        registry.Destroy(e);
        registry.Create().Tag<Selected>(); // same index, new version

        Assert.Empty(GetTagTypes(registry, e));
    }

    [Fact]
    public void GetTagTypes_StoresMadeMidWalk_RepeatNoTag()
    {
        var registry = new Registry();
        var e = registry.Create().Tag<Selected>().Tag<Hidden>();

        var types = new List<Type>();
        foreach (var t in registry.GetTagTypes(e))
        {
            types.Add(t);
            registry.Storage<Health>(); // goes before the tag sets
            registry.Tags<Frozen>();
        }

        Assert.Equal([typeof(Selected), typeof(Hidden)], types);
    }

    private static List<Type> GetComponentTypes(Registry registry, Entity e)
    {
        var types = new List<Type>();
        foreach (var t in registry.GetComponentTypes(e))
        {
            types.Add(t);
        }
        return types;
    }

    private static List<Type> GetTagTypes(Registry registry, Entity e)
    {
        var types = new List<Type>();
        foreach (var t in registry.GetTagTypes(e))
        {
            types.Add(t);
        }
        return types;
    }
}
