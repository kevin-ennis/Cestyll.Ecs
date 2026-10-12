using Cestyll.Ecs;
using Cestyll.Ecs.Snapshots;

namespace Cestyll.Ecs.Tests;

// Loads into a fresh registry whose world wiring is already in place.
public class SnapshotLoaderSetupTests
{
    private struct Position : IComponent { public int X; }
    private struct Velocity : IComponent { public int X; }
    private struct Clock { public int Tick; }

    private static readonly SnapshotSchema<Entity> _schema =
        new SnapshotSchema<Entity>().Component<Position>().Component<Velocity>().Context<Clock>();

    private static Archive Saved()
    {
        var source = new Registry();
        source.Create(new Position { X = 1 }, new Velocity { X = 2 });
        source.Create(new Position { X = 3 });
        source.AddOrReplaceContext(new Clock { Tick = 50 });
        var archive = new Archive();
        _schema.Save(source, ref archive);
        return archive.Reopen();
    }

    [Fact]
    public void WiredRegistry_LoadsAndItsWiringSeesTheLoad()
    {
        var world = new Registry();
        var movers = world.Group().Own<Position>().Own<Velocity>().Build();
        var spawned = world.Observer().OnAdd<Position>();
        var constructed = 0;
        world.Storage<Position>().OnAdd += _ => constructed++;
        world.AddOrReplaceContext(new Clock { Tick = 0 });

        var reader = Saved();
        _schema.Load(world, ref reader);

        Assert.Equal(1, movers.Count);
        Assert.Equal(2, spawned.Count);
        Assert.Equal(2, constructed);
        Assert.Equal(50, world.GetContext<Clock>().Tick);
    }

    [Fact]
    public void DestroyedRegistry_IsNotEmpty()
    {
        var destroyed = new Registry();
        destroyed.Destroy(destroyed.Create());
        var reader = Saved();

        Assert.Throws<InvalidOperationException>(() => _schema.Load(destroyed, ref reader));
    }

    [Fact]
    public void ClearedRegistry_IsNotEmpty()
    {
        var cleared = new Registry();
        cleared.Create(new Position());
        cleared.Clear();
        var reader = Saved();

        Assert.Throws<InvalidOperationException>(() => _schema.Load(cleared, ref reader));
    }
}
