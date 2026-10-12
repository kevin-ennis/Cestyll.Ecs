using Cestyll.Ecs;
using Cestyll.Ecs.Attachments;
using Cestyll.Ecs.Inboxes;
using Cestyll.Ecs.Relations;
using Cestyll.Ecs.Tags;
using Cestyll.Ecs.Views;

namespace Cestyll.Ecs.Tests;

// View().Rows() with no components: filters alone pick the entities.
public class EntityRowsTests
{
    private struct Pos : IComponent { public int X; }
    private struct Never : IComponent { }
    private struct Red : ITag;
    private struct Big : ITag;
    private struct Dead : ITag;
    private struct Unused : ITag;

    private struct Parent : IRelation
    {
        public static OnTargetDestroyed Policy => OnTargetDestroyed.Unrelate;
    }

    private struct Attacks : ILink<Entity>
    {
        public Entity Target { get; init; }
    }

    private enum Slot { Head, Hand }

    private struct Gear : IAttachment<Slot>
    {
        public Slot Slot;
        public readonly Slot Key => Slot;
    }

    private struct Hit : IMessage { }

    // Red every 2nd, Big every 3rd, Dead every 5th; Pos on odd ones.
    private static (Registry Registry, List<Entity> All) World()
    {
        var registry = new Registry();
        var all = new List<Entity>();
        for (var i = 0; i < 300; i++)
        {
            Entity e = i % 2 == 1 ? registry.Create(new Pos { X = i }) : registry.Create();
            if (i % 2 == 0)
            {
                registry.Tag<Red>(e);
            }

            if (i % 3 == 0)
            {
                registry.Tag<Big>(e);
            }

            if (i % 5 == 0)
            {
                registry.Tag<Dead>(e);
            }

            all.Add(e);
        }

        return (registry, all);
    }

    private static List<Entity> Walk(ViewRows<Entity> rows)
    {
        var seen = new List<Entity>();
        foreach (var e in rows)
        {
            seen.Add(e);
        }

        return seen;
    }

    private static List<Entity> Sorted(List<Entity> list)
    {
        list.Sort((a, b) => a.Index.CompareTo(b.Index));
        return list;
    }

    [Fact]
    public void Tags_IntersectAndExclude_InIndexOrder()
    {
        var (registry, all) = World();

        var seen = Walk(registry.View().WithTag<Red>().WithTag<Big>().WithoutTag<Dead>().Rows());

        var expected = all.Where((_, i) => i % 6 == 0 && i % 5 != 0).ToList();
        Assert.Equal(expected, seen);
    }

    [Fact]
    public void Tags_WithRelationFilter()
    {
        var (registry, all) = World();
        registry.Relate<Parent>(all[7], all[6]);
        registry.Relate<Parent>(all[9], all[12]);
        registry.Relate<Parent>(all[11], all[1]);

        var seen = Sorted(Walk(registry.View().WithTag<Red>().WithRelationSources<Parent>().Rows()));

        Assert.Equal([all[6], all[12]], seen);
    }

    [Fact]
    public void ComponentFilter_DrivesWithoutReading()
    {
        var (registry, all) = World();

        var seen = Sorted(Walk(registry.View().With<Pos>().WithoutTag<Dead>().Rows()));

        Assert.Equal(all.Where((_, i) => i % 2 == 1 && i % 5 != 0).ToList(), seen);
    }

    [Fact]
    public void Group_Drives()
    {
        var (registry, all) = World();
        var group = registry.Group().With<Pos>().Build();

        var seen = Sorted(Walk(registry.View().Use(group).WithTag<Big>().Rows()));

        Assert.Equal(all.Where((_, i) => i % 2 == 1 && i % 3 == 0).ToList(), seen);
    }

    [Fact]
    public void Pin_DrivesInPoolOrder()
    {
        var (registry, _) = World();
        registry.Sort<Pos>((a, b) => b.X.CompareTo(a.X));

        var seen = Walk(registry.View().Use<Pos>().WithTag<Big>().Rows());

        var xs = seen.Select(e => registry.Get<Pos>(e).X).ToList();
        Assert.Equal(xs.OrderDescending().ToList(), xs);
        Assert.Equal(50, xs.Count);
    }

    [Fact]
    public void Join_CombinesFilters()
    {
        var (registry, all) = World();

        var seen = Walk(registry.View().WithTag<Red>().Join(registry.View().WithTag<Big>()).Rows());

        Assert.Equal(all.Where((_, i) => i % 6 == 0).ToList(), seen);
    }

    [Fact]
    public void MissingStores_WalkNothing()
    {
        var (registry, _) = World();

        Assert.Empty(Walk(registry.View().WithTag<Unused>().Rows()));
        Assert.Empty(Walk(registry.View().With<Never>().WithTag<Red>().Rows()));
    }

    [Fact]
    public void StoreMadeAfterRows_IsSeen()
    {
        var registry = new Registry();
        var rows = registry.View().WithTag<Unused>().Rows();
        Entity e = registry.Create().Tag<Unused>();

        Assert.Equal([e], Walk(rows));
    }

    [Fact]
    public void Walk_MayDestroyTheCurrentEntity()
    {
        var (registry, all) = World();

        foreach (var e in registry.View().WithTag<Red>().WithTag<Big>().Rows())
        {
            registry.Destroy(e);
        }

        foreach (var e in registry.View().WithTag<Dead>().Rows())
        {
            registry.Destroy(e);
        }

        Assert.Empty(Walk(registry.View().WithTag<Red>().WithTag<Big>().Rows()));
        Assert.Empty(Walk(registry.View().WithTag<Dead>().Rows()));
        Assert.Equal(
            all.Where((_, i) => i % 3 == 0 && i % 6 != 0 && i % 5 != 0).ToList(),
            Walk(registry.View().WithTag<Big>().Rows()));
    }

    [Fact]
    public void NothingToDrive_Throws()
    {
        var registry = new Registry();

        Assert.Throws<InvalidOperationException>(() => registry.View().Rows());
        Assert.Throws<InvalidOperationException>(() => registry.View().WithoutTag<Dead>().Rows());
        Assert.Throws<InvalidOperationException>(() => registry.View().WithLiveTag<Red>().Rows());
        Assert.Throws<InvalidOperationException>(() => registry.View().WithAttachment<Gear, Slot>(Slot.Head).Rows());
    }

    // No components anywhere: each store's own set drives the walk.
    [Fact]
    public void Relation_Drives()
    {
        var registry = new Registry();
        Entity ship = registry.Create();
        Entity turret = registry.Create();
        Entity gun = registry.Create();
        registry.Create();
        registry.Relate<Parent>(turret, ship);
        registry.Relate<Parent>(gun, ship);

        Assert.Equal([turret, gun], Sorted(Walk(registry.View().WithRelationTarget<Parent>().Rows())));
        Assert.Equal([ship], Walk(registry.View().WithRelationSources<Parent>().Rows()));
    }

    [Fact]
    public void Relation_DrivesUnderTagExclusion()
    {
        var registry = new Registry();
        Entity ship = registry.Create();
        Entity turret = registry.Create();
        Entity gun = registry.Create();
        registry.Create();
        registry.Relate<Parent>(turret, ship);
        registry.Relate<Parent>(gun, ship);
        registry.Tag<Red>(turret);

        Assert.Equal([gun], Walk(registry.View().WithRelationTarget<Parent>().WithoutTag<Red>().Rows()));
    }

    [Fact]
    public void Links_Drive()
    {
        var registry = new Registry();
        Entity archer = registry.Create();
        Entity orc = registry.Create();
        Entity troll = registry.Create();
        registry.Link(archer, new Attacks { Target = orc });
        registry.Link(archer, new Attacks { Target = troll });

        Assert.Equal([archer], Walk(registry.View().WithLinkTargets<Attacks>().Rows()));
        Assert.Equal([orc, troll], Sorted(Walk(registry.View().WithLinkSources<Attacks>().Rows())));
        Assert.Empty(Walk(registry.View().WithLinkTargets<Attacks>().WithLinkSources<Attacks>().Rows()));
    }

    [Fact]
    public void Inbox_Drives()
    {
        var registry = new Registry();
        Entity a = registry.Create();
        Entity b = registry.Create();
        Entity c = registry.Create().Tag<Dead>();
        registry.Post(a, new Hit());
        registry.Post(c, new Hit());

        Assert.Equal([a, c], Sorted(Walk(registry.View().WithInbox<Hit>().Rows())));
        Assert.Equal([a], Walk(registry.View().WithInbox<Hit>().WithoutTag<Dead>().Rows()));
        Assert.DoesNotContain(b, Walk(registry.View().WithInbox<Hit>().Rows()));
    }

    [Fact]
    public void Attachment_Drives_AndKeysFilterOnTop()
    {
        var registry = new Registry();
        Entity a = registry.Create();
        Entity b = registry.Create();
        registry.Create();
        registry.Attach<Gear, Slot>(a, new Gear { Slot = Slot.Head });
        registry.Attach<Gear, Slot>(b, new Gear { Slot = Slot.Hand });

        Assert.Equal([a, b], Sorted(Walk(registry.View().WithAttachment<Gear>().Rows())));
        Assert.Equal([b], Walk(registry.View().WithAttachment<Gear>().WithAttachment<Gear, Slot>(Slot.Hand).Rows()));
        Assert.Equal([a], Walk(registry.View().WithAttachment<Gear>().WithoutAttachment<Gear, Slot>(Slot.Hand).Rows()));
    }
}
