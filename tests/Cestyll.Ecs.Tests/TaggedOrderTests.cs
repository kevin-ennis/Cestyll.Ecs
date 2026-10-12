using Cestyll.Ecs;
using Cestyll.Ecs.Tags;

namespace Cestyll.Ecs.Tests;

public class TaggedOrderTests
{
    private struct Pilot : IComponent { public int Slot; }
    private struct Player : ITag;

    private static List<Entity> Tagged(Registry registry)
    {
        var order = new List<Entity>();
        foreach (var e in registry.View().WithTag<Player>().Rows())
        {
            order.Add(e);
        }

        return order;
    }

    [Fact]
    public void Order_IsByIndex_NotByCreation()
    {
        var registry = new Registry();
        Entity filler = registry.Create();
        Entity first = registry.Create(new Pilot { Slot = 0 }).Tag<Player>();
        registry.Destroy(filler);
        Entity second = registry.Create(new Pilot { Slot = 1 }).Tag<Player>(); // takes filler's lower index

        Assert.Equal([second, first], Tagged(registry));
    }
}
