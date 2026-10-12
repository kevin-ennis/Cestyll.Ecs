using Cestyll.Ecs;
using Cestyll.Ecs.Groups;

namespace Cestyll.Ecs.Tests;

// CreateMany with components: one pass, pools sized once.
public class CreateManyTests
{
    private struct Position : IComponent { public float X; }
    private struct Velocity : IComponent { public float Dx; }
    private struct Health : IComponent { public int Hp; }
    private struct C4 : IComponent { public int V; }
    private struct C5 : IComponent { public int V; }
    private struct C6 : IComponent { public int V; }
    private struct C7 : IComponent { public int V; }
    private struct C8 : IComponent { public int V; }

    [Fact]
    public void GivesEachEntityTheComponents()
    {
        var registry = new Registry();
        var made = registry.CreateMany(1000, new Position { X = 1 }, new Velocity { Dx = 2 }, new Health { Hp = 3 });

        Assert.Equal(1000, made.Length);
        Assert.Equal(1000, made.Distinct().Count());
        Assert.All(made, e =>
        {
            Assert.True(registry.IsAlive(e));
            Assert.Equal(1, registry.Get<Position>(e).X);
            Assert.Equal(2, registry.Get<Velocity>(e).Dx);
            Assert.Equal(3, registry.Get<Health>(e).Hp);
        });
        Assert.Equal(1000, registry.Storage<Health>().Count);
    }

    [Fact]
    public void SameEntitiesAndOrderAsCreate()
    {
        var bulk = new Registry();
        var one = new Registry();
        foreach (var r in new[] { bulk, one })
        {
            var first = r.CreateMany(10);
            r.Destroy(first[7]);
            r.Destroy(first[2]);
        }

        Span<Entity> made = stackalloc Entity[20];
        bulk.CreateMany(made, new Position { X = 5 }, new Velocity());
        var expected = new Entity[20];
        for (var i = 0; i < 20; i++)
        {
            expected[i] = one.Create(new Position { X = 5 }, new Velocity());
        }

        Assert.Equal(expected, made.ToArray());
        Assert.Equal(one.Storage<Position>().Entities.ToArray(), bulk.Storage<Position>().Entities.ToArray());
        Assert.Equal([3, 8], made[..2].ToArray().Select(e => e.Index));
    }

    [Fact]
    public void FiresSignalsAndFillsGroups()
    {
        var registry = new Registry();
        var group = registry.Group().With<Position>().With<Velocity>().Build();
        var owning = registry.Group().Own<Health>().Build();
        var constructed = 0;
        registry.Storage<Velocity>().OnAdd += _ => constructed++;

        registry.CreateMany(300, new Position(), new Velocity(), new Health { Hp = 9 });

        Assert.Equal(300, constructed);
        Assert.Equal(300, group.Count);
        Assert.Equal(300, owning.Span<Health>().Length);
    }

    // Alive at once, components at the scope's end.
    [Fact]
    public void DefersInsideAScope()
    {
        var registry = new Registry();
        Entity[] made;
        bool[] aliveInScope;
        int countInScope;
        using (registry.Defer())
        {
            made = registry.CreateMany(5, new Position { X = 4 });
            aliveInScope = made.Select(e => registry.IsAlive(e)).ToArray();
            countInScope = registry.Storage<Position>().Count;
        }

        Assert.All(aliveInScope, alive => Assert.True(alive));
        Assert.Equal(0, countInScope);
        Assert.Equal(5, registry.Storage<Position>().Count);
        Assert.All(made, e => Assert.Equal(4, registry.Get<Position>(e).X));
    }

    [Fact]
    public void OneAndEightComponents_ReachEveryEntity()
    {
        var registry = new Registry();

        var one = registry.CreateMany(3, new Position());
        var eight = registry.CreateMany(3, new Position(), new Velocity(), new Health(), new C4 { V = 4 },
            new C5(), new C6(), new C7(), new C8 { V = 8 });

        Assert.All(one, e => Assert.True(registry.Has<Position>(e)));
        Assert.All(eight, e =>
        {
            Assert.Equal(4, registry.Get<C4>(e).V);
            Assert.Equal(8, registry.Get<C8>(e).V);
        });
        Assert.Equal(6, registry.Storage<Position>().Count);
    }

    [Fact]
    public void ZeroCount_MakesNothing()
    {
        var registry = new Registry();

        var made = registry.CreateMany(0, new Position());

        Assert.Empty(made);
    }
}
