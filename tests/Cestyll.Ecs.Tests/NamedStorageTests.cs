using Cestyll.Ecs;
using Cestyll.Ecs.Snapshots;
using Cestyll.Ecs.Tags;

namespace Cestyll.Ecs.Tests;

public class NamedStorageTests
{
    private struct Position : IComponent { public int X; }
    private struct Velocity : IComponent { public int X; }
    private struct Marked : ITag;
    private struct Frozen : IComponent, ITag;

    // Holds an Entity, for the ContinuousLoader remap path.
    private struct Target : IComponent { public Entity Who; }

    [Fact]
    public void Named_IsApartFromDefaultAndOtherNames()
    {
        var registry = new Registry();
        Entity e = registry.Create();
        var previous = registry.Storage<Position>("previous");

        registry.Add(e, new Position { X = 2 });
        previous.Add(e, new Position { X = 1 });

        Assert.Same(previous, registry.Storage<Position>("previous"));
        Assert.NotSame(previous, registry.Storage<Position>());
        Assert.NotSame(previous, registry.Storage<Position>("spawn"));
        Assert.False(registry.Storage<Position>("spawn").Has(e));
        Assert.Equal(2, registry.Get<Position>(e).X);
        Assert.Equal(1, previous.Get(e).X);
        Assert.Equal("previous", previous.Name);
        Assert.Null(registry.Storage<Position>().Name);
    }

    [Fact]
    public void Remove_DefaultPool_LeavesNamedPool()
    {
        var registry = new Registry();
        Entity e = registry.Create();
        var previous = registry.Storage<Position>("previous");
        registry.Add(e, new Position { X = 2 });
        previous.Add(e, new Position { X = 1 });

        registry.Remove<Position>(e);

        Assert.True(previous.Has(e));
    }

    [Fact]
    public void Named_NullName_Throws()
    {
        var registry = new Registry();

        Assert.Throws<ArgumentNullException>(() => registry.Storage<Position>(null!));
    }

    [Fact]
    public void Named_TwoMarkers_Throws()
    {
        var registry = new Registry();

        Assert.Throws<ArgumentException>(() => registry.Storage<Frozen>("x"));
    }

    [Fact]
    public void Destroy_RemovesFromNamedPools_AndFiresOnRemove()
    {
        var registry = new Registry();
        Entity e = registry.Create();
        var previous = registry.Storage<Position>("previous");
        previous.Add(e, new Position { X = 1 });
        var destroyed = 0;
        previous.OnRemove += _ => destroyed++;

        registry.Destroy(e);

        Assert.Equal(0, previous.Count);
        Assert.Equal(1, destroyed);
    }

    [Fact]
    public void GetComponentTypes_ListsDefaultPoolsOnly()
    {
        var registry = new Registry();
        Entity e = registry.Create();
        registry.Storage<Position>("previous").Add(e, default);
        registry.Add(e, new Velocity());

        var types = new List<Type>();
        foreach (var t in registry.GetComponentTypes(e))
        {
            types.Add(t);
        }

        Assert.Equal([typeof(Velocity)], types);
    }

    [Fact]
    public void View_PairsDefaultWithNamed()
    {
        var registry = new Registry();
        var previous = registry.Storage<Position>("previous");
        var entities = new Entity[5];
        for (var i = 0; i < entities.Length; i++)
        {
            entities[i] = registry.Create();
            registry.Add(entities[i], new Position { X = i * 10 });
            if (i % 2 == 0)
            {
                previous.Add(entities[i], new Position { X = i });
            }
        }

        var seen = new Dictionary<Entity, (int Now, int Before)>();
        var rows = new List<(int Before, int Now)>();

        registry.View().Rows(registry.Storage<Position>(), previous).Each((Entity e, ref Position now, ref Position before) =>
            seen.Add(e, (now.X, before.X)));
        foreach (var row in registry.View().Rows(previous, registry.Storage<Position>()))
        {
            rows.Add((row.C1.X, row.C2.X));
        }

        Assert.Equal(3, seen.Count);
        foreach (var i in (int[])[0, 2, 4])
        {
            Assert.Equal((i * 10, i), seen[entities[i]]);
        }

        Assert.Equal(3, rows.Count);
        Assert.All(rows, r => Assert.Equal(r.Before * 10, r.Now));
    }

    // The previous-frame case: copy this frame's value out before moving it.
    [Fact]
    public void View_WritesNamedPoolThroughRef()
    {
        var registry = new Registry();
        var previous = registry.Storage<Position>("previous");
        Entity e = registry.Create();
        registry.Add(e, new Position { X = 5 });
        registry.Add(e, new Velocity { X = 3 });
        previous.Add(e, default);

        registry.View().Rows(registry.Storage<Position>(), registry.Storage<Velocity>(), previous)
            .Each((Entity _, ref Position p, ref Velocity v, ref Position before) =>
            {
                before = p;
                p.X += v.X;
            });

        Assert.Equal(8, registry.Get<Position>(e).X);
        Assert.Equal(5, previous.Get(e).X);
    }

    // Use<T> names T's default pool, so beside a named pool of T it pins the default one.
    [Fact]
    public void Use_FollowsThePinnedPoolsOrder()
    {
        var registry = new Registry();
        var previous = registry.Storage<Position>("previous");
        for (var i = 0; i < 6; i++)
        {
            Entity e = registry.Create();
            registry.Add(e, new Position { X = i });
            previous.Add(e, new Position { X = (i * 5) % 6 });
        }

        previous.Sort((a, b) => a.X.CompareTo(b.X));
        registry.Sort<Position>((a, b) => b.X.CompareTo(a.X));
        var current = registry.Storage<Position>();

        var byPrevious = new List<int>();
        registry.View().Use<Position>("previous").Rows(current, previous)
            .Each((Entity _, ref Position _, ref Position before) => byPrevious.Add(before.X));
        var byDefault = new List<int>();
        registry.View().Use<Position>().Rows(current, previous)
            .Each((Entity _, ref Position now, ref Position _) => byDefault.Add(now.X));

        Assert.Equal([0, 1, 2, 3, 4, 5], byPrevious);
        Assert.Equal([5, 4, 3, 2, 1, 0], byDefault);
    }

    [Fact]
    public void Use_ByName_NotInView_Throws()
    {
        var registry = new Registry();
        var view = registry.View().Use<Position>("previous");

        Assert.Throws<InvalidOperationException>(() => view.Rows<Position>());
    }

    [Fact]
    public void WithAndWithout_FilterOnNamedPool()
    {
        var registry = new Registry();
        var spawned = registry.Storage<Position>("spawn");
        Entity a = registry.Create();
        Entity b = registry.Create();
        Entity c = registry.Create();
        foreach (var e in (Entity[])[a, b, c])
        {
            registry.Add(e, new Velocity());
        }

        spawned.Add(b, default);
        registry.Tag<Marked>(c);
        spawned.Add(c, default);

        Assert.Equal([b, c], Walk(registry.View().With<Position>("spawn")));
        Assert.Equal([a], Walk(registry.View().Without<Position>("spawn")));
        Assert.Equal([c], Walk(registry.View().With<Position>("spawn").WithTag<Marked>()));
        Assert.Equal([b], Walk(registry.View().WithoutTag<Marked>().With<Position>("spawn")));
    }

    [Fact]
    public void PoolFromAnotherRegistry_Throws()
    {
        var registry = new Registry();
        var other = new Registry().Storage<Position>("previous");

        Assert.Throws<ArgumentException>(() => registry.View().Rows(other));
    }

    [Fact]
    public void Snapshot_RoundTripsNamedPools()
    {
        var source = new Registry();
        Entity a = source.Create();
        Entity b = source.Create();
        source.Add(a, new Position { X = 1 });
        source.Storage<Position>("previous").Add(a, new Position { X = 2 });
        source.Storage<Position>("previous").Add(b, new Position { X = 3 });
        source.Storage<Velocity>("spawn").Add(b, new Velocity { X = 4 });

        var archive = new Archive();
        source.Snapshot<Archive>()
            .Entities(ref archive)
            .Component<Position>(ref archive)
            .Component<Position>(ref archive, "previous")
            .Component<Velocity>(ref archive, "spawn");
        var loaded = new Registry();
        var reader = archive.Reopen();
        loaded.SnapshotLoader<Archive>()
            .Entities(ref reader)
            .Component<Position>(ref reader)
            .Component<Position>(ref reader, "previous")
            .Component<Velocity>(ref reader, "spawn");

        Assert.Equal(1, loaded.Get<Position>(a).X);
        Assert.False(loaded.Has<Position>(b));
        Assert.Equal(2, loaded.Storage<Position>("previous").Get(a).X);
        Assert.Equal(3, loaded.Storage<Position>("previous").Get(b).X);
        Assert.Equal(4, loaded.Storage<Velocity>("spawn").Get(b).X);
        Assert.False(loaded.Has<Velocity>(b));
    }

    [Fact]
    public void ContinuousLoader_ReplacesNamedPoolAndRemaps()
    {
        var remote = new Registry();
        Entity hero = remote.Create();
        Entity foe = remote.Create();
        remote.Storage<Target>("last").Add(hero, new Target { Who = foe });

        var local = new Registry();
        Entity bystander = local.Create();
        local.Add(bystander, new Target());
        var loader = local.ContinuousLoader<Archive>();

        var archive = new Archive();
        remote.Snapshot<Archive>().Entities(ref archive).Component<Target>(ref archive, "last");
        var reader = archive.Reopen();
        loader.Entities(ref reader)
            .Component<Target>(ref reader, "last", (t, map) => new Target { Who = map(t.Who) });

        var last = local.Storage<Target>("last");
        Assert.Equal(1, last.Count);
        Assert.Equal(loader.Map(foe), last.Get(loader.Map(hero)).Who);
        Assert.True(local.Has<Target>(bystander));
    }

    [Fact]
    public void ShrinkToFit_TrimsNamedPools()
    {
        var registry = new Registry();
        var previous = registry.Storage<Position>("previous");
        var entities = new Entity[1000];
        for (var i = 0; i < entities.Length; i++)
        {
            entities[i] = registry.Create();
            previous.Add(entities[i], default);
        }

        foreach (var e in entities)
        {
            registry.Destroy(e);
        }

        registry.ShrinkToFit();

        Assert.Equal(64, previous.Capacity);
    }

    [Fact]
    public void Storages_ListsDefaultAndNamedPools()
    {
        var registry = new Registry();
        Entity e = registry.Create();
        registry.Add(e, new Position());
        registry.Storage<Position>("previous").Add(e, default);
        registry.Storage<Velocity>("spawn");
        registry.Tag<Marked>(e);

        var pools = new List<(Type?, string?)>();
        foreach (var pool in registry.Storages)
        {
            pools.Add((pool.ComponentType, pool.Name));
        }

        Assert.Equal([(typeof(Position), null), (typeof(Position), "previous"), (typeof(Velocity), "spawn")], pools);
    }

    [Fact]
    public void Storages_SeesPoolsMadeMidWalk()
    {
        var registry = new Registry();
        registry.Storage<Position>();

        var pools = new List<(Type?, string?)>();
        foreach (var pool in registry.Storages)
        {
            pools.Add((pool.ComponentType, pool.Name));
            if (pools.Count == 1)
            {
                registry.Tags<Marked>(); // a store of another kind made mid-walk too
                registry.Storage<Velocity>("spawn");
            }
        }

        Assert.Equal([(typeof(Position), null), (typeof(Velocity), "spawn")], pools);
    }

    [Fact]
    public void FindStorage_NeverCreates()
    {
        var registry = new Registry();
        var previous = registry.Storage<Position>("previous");

        Assert.Same(previous, registry.FindStorage<Position>("previous"));
        Assert.Null(registry.FindStorage<Position>());
        Assert.Null(registry.FindStorage<Position>("spawn"));
        Assert.Null(registry.FindStorage<Velocity>("previous"));
        Assert.Same(previous, registry.FindStorage(typeof(Position), "previous"));
        Assert.Null(registry.FindStorage(typeof(Position)));
        Assert.Single(Pools(registry));
    }

    [Fact]
    public void FindStorage_FindsDefaultPoolOnceMade()
    {
        var registry = new Registry();
        registry.Storage<Position>("previous");

        var pool = registry.Storage<Position>();

        Assert.Same(pool, registry.FindStorage(typeof(Position)));
        Assert.Same(pool, registry.FindStorage<Position>());
    }

    [Fact]
    public void Observer_WatchesNamedPoolOnly()
    {
        var registry = new Registry();
        var previous = registry.Storage<Position>("previous");
        using var constructed = registry.Observer().OnAdd<Position>("previous");
        using var updated = registry.Observer().OnUpdate<Position>("previous");
        using var removed = registry.Observer().OnRemove<Position>("previous");
        Entity a = registry.Create();
        Entity b = registry.Create();

        registry.Add(a, new Position());
        registry.Patch<Position>(a);
        registry.Remove<Position>(a);
        previous.Add(b, default);
        previous.Patch(b);
        previous.Remove(b);

        Assert.Equal([b], constructed.Entities.ToArray());
        Assert.Equal([b], updated.Entities.ToArray());
        Assert.Equal([b], removed.Entities.ToArray());
    }

    [Fact]
    public void Observer_FiltersOnNamedPool()
    {
        var registry = new Registry();
        var spawned = registry.Storage<Position>("spawn");
        using var with = registry.Observer().OnAdd<Velocity>().With<Position>("spawn");
        using var without = registry.Observer().OnAdd<Velocity>().Without<Position>("spawn");
        Entity a = registry.Create();
        Entity b = registry.Create();
        registry.Add(b, new Position()); // the default pool doesn't count
        spawned.Add(a, default);

        registry.Add(a, new Velocity());
        registry.Add(b, new Velocity());

        Assert.Equal([a], with.Entities.ToArray());
        Assert.Equal([b], without.Entities.ToArray());
    }

    [Fact]
    public void Observer_NamedContradictions_Throw()
    {
        var registry = new Registry();

        Assert.Throws<InvalidOperationException>(() => registry.Observer().OnAdd<Position>("previous").Without<Position>("previous"));
        Assert.Throws<InvalidOperationException>(() => registry.Observer().Without<Position>("previous").OnUpdate<Position>("previous"));
        Assert.Throws<InvalidOperationException>(() => registry.Observer().With<Position>("previous").Without<Position>("previous"));
    }

    // Watching the default pool and excluding a named one of the same type can collect.
    [Fact]
    public void Observer_DefaultPoolWithNamedExclusion_Collects()
    {
        var registry = new Registry();
        using var observer = registry.Observer().OnAdd<Position>().Without<Position>("previous");
        Entity e = registry.Create();

        registry.Add(e, new Position());

        Assert.True(observer.Contains(e));
    }

    [Fact]
    public void Observer_NullName_Throws()
    {
        var registry = new Registry();

        Assert.Throws<ArgumentNullException>(() => registry.Observer().OnAdd<Position>(null!));
    }

    [Fact]
    public void View_NullName_Throws()
    {
        var registry = new Registry();

        Assert.Throws<ArgumentNullException>(() => registry.View().With<Position>(null!));
    }

    [Fact]
    public void Observer_Dispose_DisconnectsNamedPool()
    {
        var registry = new Registry();
        var previous = registry.Storage<Position>("previous");
        var observer = registry.Observer().OnAdd<Position>("previous");
        observer.Dispose();
        Entity e = registry.Create();

        previous.Add(e, default);

        Assert.Equal(0, observer.Count);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Names_EmptyOrWhitespace_Throw(string name)
    {
        var registry = new Registry();

        Assert.Throws<ArgumentException>(() => registry.Storage<Position>(name));
        Assert.Throws<ArgumentException>(() => registry.FindStorage<Position>(name));
        Assert.Throws<ArgumentException>(() => registry.FindStorage(typeof(Position), name));
        Assert.Throws<ArgumentException>(() => registry.DropStorage<Position>(name));
        Assert.Throws<ArgumentException>(() => registry.Observer().OnAdd<Position>(name));
        Assert.Throws<ArgumentException>(() => registry.View().With<Position>(name));
        Assert.Throws<ArgumentException>(() => registry.View().Without<Position>(name));
        Assert.Throws<ArgumentException>(() => registry.View().Use<Position>(name));
        var archive = new Archive();
        Assert.Throws<ArgumentException>(() => registry.Snapshot<Archive>().Component<Position>(ref archive, name));
        Assert.Throws<ArgumentException>(() => registry.ContinuousLoader<Archive>().Component<Position>(ref archive, name));
        Assert.Null(registry.FindStorage<Position>());
    }

    [Fact]
    public void Snapshot_Writer_NeverMakesPools()
    {
        var source = new Registry();
        source.Create();
        var archive = new Archive();

        source.Snapshot<Archive>()
            .Entities(ref archive)
            .Component<Position>(ref archive)
            .Component<Position>(ref archive, "previous")
            .Component<Velocity>(ref archive, "spawn");

        Assert.Empty(Pools(source));
    }

    [Fact]
    public void SnapshotLoader_EmptySections_ReadCleanly()
    {
        var source = new Registry();
        Entity e = source.Create();
        var archive = new Archive();
        source.Snapshot<Archive>()
            .Entities(ref archive)
            .Component<Position>(ref archive)
            .Component<Position>(ref archive, "previous")
            .Component<Velocity>(ref archive, "spawn");
        var loaded = new Registry();
        var reader = archive.Reopen();

        loaded.SnapshotLoader<Archive>()
            .Entities(ref reader)
            .Component<Position>(ref reader)
            .Component<Position>(ref reader, "previous")
            .Component<Velocity>(ref reader, "spawn");

        Assert.True(loaded.IsAlive(e));
        Assert.Equal(0, loaded.Storage<Position>("previous").Count);
        Assert.True(reader.AllRead);
    }

    [Fact]
    public void DropStorage_FiresOnRemove_AndForgetsPool()
    {
        var registry = new Registry();
        var previous = registry.Storage<Position>("previous");
        Entity a = registry.Create();
        Entity b = registry.Create();
        previous.Add(a, default);
        previous.Add(b, default);
        var destroyed = new List<Entity>();
        previous.OnRemove += destroyed.Add;

        var dropped = registry.DropStorage<Position>("previous");

        Assert.True(dropped);
        Assert.Equal([a, b], destroyed.OrderBy(x => x.Index).ToArray());
        Assert.Equal(0, previous.Count);
        Assert.Empty(Pools(registry));
        Assert.Null(registry.FindStorage<Position>("previous"));
        Assert.True(registry.IsOrphan(a));
    }

    [Fact]
    public void DropStorage_Twice_ReturnsFalse()
    {
        var registry = new Registry();
        var previous = registry.Storage<Position>("previous");
        Entity a = registry.Create();
        previous.Add(a, default);
        registry.DropStorage<Position>("previous");

        var droppedAgain = registry.DropStorage<Position>("previous");

        Assert.False(droppedAgain);
    }

    [Fact]
    public void DropStorage_DroppedPool_RejectsAdd()
    {
        var registry = new Registry();
        var previous = registry.Storage<Position>("previous");
        Entity a = registry.Create();
        previous.Add(a, default);
        registry.DropStorage<Position>("previous");

        Assert.Throws<InvalidOperationException>(() => previous.Add(a, default));
    }

    [Fact]
    public void DropStorage_ExistingView_WalksNothing()
    {
        var registry = new Registry();
        var previous = registry.Storage<Position>("previous");
        Entity a = registry.Create();
        Entity b = registry.Create();
        previous.Add(a, default);
        previous.Add(b, default);
        var view = registry.View().Rows(previous);
        registry.DropStorage<Position>("previous");

        var walked = 0;
        view.Each((Entity _, ref Position _) => walked++);

        Assert.Equal(0, walked);
    }

    [Fact]
    public void DropStorage_SameName_MakesFreshPool()
    {
        var registry = new Registry();
        var previous = registry.Storage<Position>("previous");
        Entity a = registry.Create();
        previous.Add(a, default);
        registry.DropStorage<Position>("previous");

        var fresh = registry.Storage<Position>("previous");

        Assert.NotSame(previous, fresh);
        Assert.Equal(0, fresh.Count);
    }

    [Fact]
    public void DropStorage_DestroySkipsDroppedPool()
    {
        var registry = new Registry();
        var previous = registry.Storage<Position>("previous");
        Entity a = registry.Create();
        previous.Add(a, default);
        registry.DropStorage<Position>("previous");
        registry.Storage<Position>("previous");

        registry.Destroy(a); // no longer walks the dropped pool

        Assert.False(registry.IsAlive(a));
    }

    [Fact]
    public void DropStorage_FromOnRemoveHandler_Throws()
    {
        var registry = new Registry();
        registry.Storage<Position>("previous");
        Entity e = registry.Create();
        registry.Add(e, new Velocity());
        registry.Storage<Velocity>().OnRemove += _ => registry.DropStorage<Position>("previous");

        Assert.Throws<InvalidOperationException>(() => registry.Destroy(e));
    }

    [Fact]
    public void DropStorage_AddFromItsOwnHandler_Throws()
    {
        var registry = new Registry();
        var previous = registry.Storage<Position>("previous");
        Entity e = registry.Create();
        previous.Add(e, default);
        previous.OnRemove += x => previous.Add(x, default);

        Assert.Throws<InvalidOperationException>(() => registry.DropStorage<Position>("previous"));
    }

    [Fact]
    public void GetStorages_ListsNamedAndDefaultPools()
    {
        var registry = new Registry();
        Entity e = registry.Create();
        Entity other = registry.Create();
        var previous = registry.Storage<Position>("previous");
        registry.Storage<Velocity>("spawn").Add(other, default);
        previous.Add(e, default);
        var namedOnly = Pools(registry, e);
        var orphan = registry.IsOrphan(e);

        registry.Add(e, new Position());

        Assert.Equal([previous], namedOnly);
        Assert.False(orphan);
        Assert.Equal([previous, registry.Storage<Position>()], Pools(registry, e));
    }

    [Fact]
    public void GetStorages_DeadEntity_IsEmpty()
    {
        var registry = new Registry();
        Entity e = registry.Create();
        registry.Storage<Position>("previous").Add(e, default);
        registry.Add(e, new Position());

        registry.Destroy(e);

        Assert.Empty(Pools(registry, e));
    }

    // The drop must never fire on an entity a handler already destroyed; it would poison an observer.
    [Fact]
    public void DropStorage_HandlerDestroysAnotherMember_FiresOnlyOnLiveEntities()
    {
        var registry = new Registry();
        var previous = registry.Storage<Position>("previous");
        var entities = new Entity[4];
        for (var i = 0; i < entities.Length; i++)
        {
            entities[i] = registry.Create();
            previous.Add(entities[i], default);
        }

        using var observer = registry.Observer().OnRemove<Position>("previous").OnAdd<Velocity>();
        var firedAlive = new List<bool>();
        previous.OnRemove += e =>
        {
            firedAlive.Add(registry.IsAlive(e));
            foreach (var other in entities)
            {
                registry.Destroy(other);
            }
        };

        registry.DropStorage<Position>("previous");

        Assert.Equal(entities.Length, firedAlive.Count);
        Assert.DoesNotContain(false, firedAlive);
        Assert.Equal(0, observer.Count);
        Assert.All(entities, e => Assert.False(registry.IsAlive(e)));
    }

    [Fact]
    public void DropStorage_HandlerDestroysMembers_ObserverStaysUsable()
    {
        var registry = new Registry();
        var previous = registry.Storage<Position>("previous");
        var entities = new Entity[4];
        for (var i = 0; i < entities.Length; i++)
        {
            entities[i] = registry.Create();
            previous.Add(entities[i], default);
        }

        using var observer = registry.Observer().OnRemove<Position>("previous").OnAdd<Velocity>();
        previous.OnRemove += _ =>
        {
            foreach (var other in entities)
            {
                registry.Destroy(other);
            }
        };
        registry.DropStorage<Position>("previous");
        Entity recycled = registry.Create();

        registry.Add(recycled, new Velocity());

        Assert.Equal([recycled], observer.Entities.ToArray());
    }

    [Fact]
    public void DropStorage_HandlerRemakesName_FindStorageSeesNewPool()
    {
        var registry = new Registry();
        var previous = registry.Storage<Position>("previous");
        Entity e = registry.Create();
        previous.Add(e, default);
        Cestyll.Ecs.Storage.SparseSet<Entity>? found = null;
        Cestyll.Ecs.Storage.Pool<Entity, Position>? remade = null;
        previous.OnRemove += _ =>
        {
            remade = registry.Storage<Position>("previous");
            found = registry.FindStorage(typeof(Position), "previous");
        };

        registry.DropStorage<Position>("previous");

        Assert.NotNull(remade);
        Assert.NotSame(previous, remade);
        Assert.Same(remade, found);
        Assert.Same(remade, registry.FindStorage(typeof(Position), "previous"));
        Assert.Equal([remade], Pools(registry));
    }

    private static List<object> Pools(Registry registry)
    {
        var list = new List<object>();
        foreach (var pool in registry.Storages)
        {
            list.Add(pool);
        }

        return list;
    }

    private static List<object> Pools(Registry registry, Entity e)
    {
        var list = new List<object>();
        foreach (var pool in registry.GetStorages(e))
        {
            list.Add(pool);
        }

        return list;
    }

    private static List<Entity> Walk(Cestyll.Ecs.Views.View<Entity> view)
    {
        var list = new List<Entity>();
        view.Each((Entity e, ref Velocity _) => list.Add(e));
        list.Sort((x, y) => x.Index.CompareTo(y.Index));
        return list;
    }
}
