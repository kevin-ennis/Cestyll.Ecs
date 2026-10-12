using Cestyll.Ecs;
using Cestyll.Ecs.Tags;

namespace Cestyll.Ecs.Tests;

public class UseTagTests
{
    private struct Position : IComponent { public int X; }
    private struct Pilot : IComponent { public int Slot; }
    private struct Player : ITag;
    private struct Downed : ITag;

    // Two players among many pilots, created so pool order and index order disagree.
    private static (Registry Registry, Entity Low, Entity High) World()
    {
        var registry = new Registry();
        var filler = registry.Create();
        Entity high = registry.Create(new Pilot { Slot = 1 }, new Position { X = 10 }).Tag<Player>();
        registry.Destroy(filler);
        Entity low = registry.Create(new Pilot { Slot = 0 }, new Position { X = 20 }).Tag<Player>();
        for (var i = 0; i < 20; i++)
        {
            registry.Create(new Pilot(), new Position());
        }

        return (registry, low, high);
    }

    [Fact]
    public void UseTag_WalksTaggedEntitiesInIndexOrder()
    {
        var (registry, low, high) = World();
        var tagged = new List<Entity>();
        foreach (var e in registry.View().WithTag<Player>().Rows())
        {
            tagged.Add(e);
        }

        var seen = new List<Entity>();
        registry.View().UseTag<Player>().Each((Entity e, ref Position _, ref Pilot _) => seen.Add(e));

        Assert.Equal([low, high], seen);
        Assert.Equal(tagged, seen);
    }

    [Fact]
    public void UseTag_EvenWhenTheTagIsTheLargerSide()
    {
        var registry = new Registry();
        Entity a = registry.Create(new Pilot()).Tag<Player>();
        Entity b = registry.Create(new Pilot()).Tag<Player>();
        for (var i = 0; i < 10; i++)
        {
            registry.Create(new Position()).Tag<Player>();
        }

        var seen = new List<Entity>();
        foreach (var row in registry.View().UseTag<Player>().Rows<Pilot>())
        {
            seen.Add(row.Entity);
        }

        Assert.Equal([a, b], seen); // a plain WithTag would drive from the 2-entity pool, b first
    }

    [Fact]
    public void UseTag_KeepsOtherFilters_AndReadsComponents()
    {
        var (registry, low, high) = World();
        registry.Tag<Downed>(high);

        var xs = new List<int>();
        registry.View().UseTag<Player>().WithoutTag<Downed>()
            .Each((Entity _, ref Position p, ref Pilot _) => xs.Add(p.X));

        Assert.Equal([20], xs);
    }

    [Fact]
    public void UseTag_WithNoTagSet_WalksNothing()
    {
        var registry = new Registry();
        registry.Create(new Pilot());

        var count = 0;
        registry.View().UseTag<Player>().Each((Entity _, ref Pilot _) => count++);

        Assert.Equal(0, count);
    }
}
