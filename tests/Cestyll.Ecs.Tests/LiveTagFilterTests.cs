using Cestyll.Ecs;
using Cestyll.Ecs.Tags;

namespace Cestyll.Ecs.Tests;

public class LiveTagFilterTests
{
    private struct Position : IComponent { public int X; }
    private struct Dead : ITag;
    private struct Enemy : ITag;

    private static (Registry Registry, List<Entity> All) Enemies(int count)
    {
        var registry = new Registry();
        var all = new List<Entity>();
        for (var i = 0; i < count; i++)
        {
            Entity e = registry.Create(new Position { X = i });
            registry.Tag<Enemy>(e);
            all.Add(e);
        }

        return (registry, all);
    }

    // The first entity visited kills everyone; a live exclusion sees it at once.
    [Fact]
    public void WithoutTagLive_SkipsEntitiesTaggedEarlierInTheWalk()
    {
        var (registry, all) = Enemies(8);
        var visited = 0;

        registry.View().WithTag<Enemy>().WithoutLiveTag<Dead>().Each((Entity e, ref Position _) =>
        {
            visited++;
            foreach (var other in all)
            {
                registry.Tag<Dead>(other);
            }
        });

        Assert.Equal(1, visited);
    }

    [Fact]
    public void WithTagLive_MatchesTheMaskedFilter_InEachAndForeach()
    {
        var (registry, all) = Enemies(6);
        registry.Tag<Dead>(all[1]);
        registry.Tag<Dead>(all[4]);

        var masked = new List<Entity>();
        registry.View().WithTag<Enemy>().WithTag<Dead>().Each((Entity e, ref Position _) => masked.Add(e));
        var live = new List<Entity>();
        foreach (var row in registry.View().WithTag<Enemy>().WithLiveTag<Dead>().Rows<Position>())
        {
            live.Add(row.Entity);
        }

        // A mask may drive the masked walk in index order; the live one runs over the pool.
        Assert.Equal(masked.OrderBy(e => e.Index), live.OrderBy(e => e.Index));
        Assert.Equal(2, live.Count);
    }

    [Fact]
    public void Live_MakesTheTagSet()
    {
        var registry = new Registry();

        registry.View().WithoutLiveTag<Dead>().Rows<Position>();

        Assert.Contains(registry.Stores(), s => s.Type == typeof(Dead));
        Assert.Equal(0, registry.TagCount<Dead>());
    }
}
