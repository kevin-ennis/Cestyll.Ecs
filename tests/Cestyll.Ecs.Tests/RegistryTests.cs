using Cestyll.Ecs;
using Cestyll.Ecs.Attachments;
using Cestyll.Ecs.Snapshots;
using Cestyll.Ecs.Tags;

namespace Cestyll.Ecs.Tests;

public class RegistryTests
{
    private struct Position : IComponent { public float X, Y; }
    private struct Marked : ITag;
    private struct Frozen : IComponent, ITag;
    private struct Loot : IComponent, IAttachment<int> { public int Key { get; init; } }

    [Fact]
    public void Create_ReturnsLiveEntity()
    {
        var registry = new Registry();
        Entity e = registry.Create();

        Assert.False(e.IsNull);
        Assert.True(registry.IsAlive(e));
    }

    [Fact]
    public void Default_IsNull()
    {
        Assert.Equal(Entity.Null, default);
        Assert.True(default(Entity).IsNull);
        Assert.Equal(0, Entity.Null.Index);
    }

    [Fact]
    public void Create_NeverIssuesIndexZero()
    {
        var registry = new Registry();
        Entity first = registry.Create();
        registry.Destroy(first);
        Entity recycled = registry.Create();

        Assert.Equal(1, first.Index);
        Assert.Equal(1, recycled.Index);
        Assert.NotEqual(Entity.Null, first);
        Assert.NotEqual(Entity.Null, recycled);
    }

    [Fact]
    public void Null_IsNeverAlive()
    {
        var registry = new Registry();
        registry.Create();
        Entity uninitialized = default;

        var alive = registry.IsAlive(uninitialized);
        var versionedAlive = registry.IsAlive(Entity.Make(0, 1));
        var has = registry.Has<Position>(uninitialized);

        Assert.False(alive);
        Assert.False(versionedAlive);
        Assert.False(has);
    }

    [Fact]
    public void Null_AddOrTag_Throws()
    {
        var registry = new Registry();
        registry.Create();
        Entity uninitialized = default;

        Assert.Throws<InvalidOperationException>(() => registry.Add(uninitialized, new Position()));
        Assert.Throws<InvalidOperationException>(() => registry.Tag<Marked>(uninitialized));
    }

    [Fact]
    public void Destroy_Null_IsANoOp()
    {
        var registry = new Registry();
        registry.Create();
        Entity uninitialized = default;

        registry.Destroy(uninitialized); // must not free index 0
        Entity next = registry.Create();

        Assert.Equal(2, next.Index);
    }

    [Fact]
    public void TwoMarkers_RejectedWhenStorageIsFirstMade()
    {
        var registry = new Registry();
        Entity e = registry.Create();

        Assert.Throws<ArgumentException>(() => registry.Add(e, new Frozen()));
        Assert.Throws<ArgumentException>(() => registry.Tag<Frozen>(e));
        Assert.Throws<ArgumentException>(() => registry.Add(e, new Loot()));
        Assert.Throws<ArgumentException>(() => registry.Attach<Loot, int>(e, new Loot()));
        Assert.False(registry.Has<Frozen>(e)); // no pool was left behind
    }

    [Fact]
    public void Snapshot_FreeIndexZero_Rejected()
    {
        var archive = new Archive();
        archive.BeginSection(SnapshotSection.Entities, typeof(Entity), null);
        archive.WriteCount(1);                  // index 1 issued
        archive.WriteEntity(Entity.Make(1, 0));
        archive.WriteCount(1);                  // one free
        archive.WriteEntity(Entity.Null);       // index 0
        archive.EndSection();

        var reader = archive.Reopen();
        var loader = new Registry().SnapshotLoader<Archive>();

        Assert.Throws<InvalidDataException>(() => loader.Entities(ref reader));
    }

    [Fact]
    public void Snapshot_RoundTrip_KeepsIndexZeroReserved()
    {
        var source = new Registry();
        Entity a = source.Create();
        Entity b = source.Create();
        source.Destroy(a);

        var archive = new Archive();
        source.Snapshot<Archive>().Entities(ref archive);
        var loaded = new Registry();
        var reader = archive.Reopen();
        loaded.SnapshotLoader<Archive>().Entities(ref reader);

        Assert.False(loaded.IsAlive(a));
        Assert.True(loaded.IsAlive(b));
        Assert.False(loaded.IsAlive(Entity.Null));
        Assert.Equal(a.Index, loaded.Create().Entity.Index);
        Assert.Equal(3, loaded.Create().Entity.Index);
    }

    [Fact]
    public void Destroy_KillsEntity()
    {
        var registry = new Registry();
        Entity e = registry.Create();

        registry.Destroy(e);

        Assert.False(registry.IsAlive(e));
    }

    [Fact]
    public void Add_ThenGet_RoundTripsComponent()
    {
        var registry = new Registry();
        Entity e = registry.Create();

        registry.Add(e, new Position { X = 1, Y = 2 });

        Assert.True(registry.Has<Position>(e));
        Assert.Equal(1, registry.Get<Position>(e).X);
        Assert.Equal(2, registry.Get<Position>(e).Y);
    }

    [Fact]
    public void Remove_ClearsComponent()
    {
        var registry = new Registry();
        Entity e = registry.Create();
        registry.Add(e, new Position { X = 1, Y = 2 });

        var removed = registry.Remove<Position>(e);

        Assert.True(removed);
        Assert.False(registry.Has<Position>(e));
    }
}
