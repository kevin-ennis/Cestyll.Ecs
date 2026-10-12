using Cestyll.Ecs;
using Cestyll.Ecs.Attachments;
using Cestyll.Ecs.Tags;
using Cestyll.Ecs.Views;

namespace Cestyll.Ecs.Tests;

public class ViewTests
{
    private struct Pos : IComponent { }
    private struct Vel : IComponent { }
    private struct Frozen : IComponent { }
    private struct Selected : ITag { }
    private struct Hidden : ITag { }

    private enum Slot { Head }

    private struct Gear : IAttachment<Slot>
    {
        public Slot Slot;
        public readonly Slot Key => Slot;
    }

    private static (Registry, Entity, Entity) Moving()
    {
        var registry = new Registry();
        Entity a = registry.Create(new Pos(), new Vel());
        Entity b = registry.Create(new Pos(), new Vel());
        return (registry, a, b);
    }

    private static List<Entity> Collect(View<Entity> view)
    {
        var seen = new List<Entity>();
        view.Each((Entity e, ref Pos _, ref Vel _) => seen.Add(e));
        return seen;
    }

    [Fact]
    public void With_TagCreatedAfterView_IsSeen()
    {
        var (registry, a, _) = Moving();
        var view = registry.View().WithTag<Selected>();
        var before = Collect(view);

        registry.Tag<Selected>(a);

        Assert.Empty(before);
        Assert.Equal([a], Collect(view));
    }

    [Fact]
    public void Without_TagCreatedAfterView_IsSeen()
    {
        var (registry, a, b) = Moving();
        var view = registry.View().WithoutTag<Selected>();

        registry.Tag<Selected>(a);

        Assert.Equal([b], Collect(view));
    }

    [Fact]
    public void With_ComponentCreatedAfterView_IsSeen()
    {
        var (registry, a, _) = Moving();
        var view = registry.View().With<Frozen>();
        var before = Collect(view);

        registry.Add(a, new Frozen());

        Assert.Empty(before);
        Assert.Equal([a], Collect(view));
    }

    [Fact]
    public void Without_ComponentCreatedAfterView_IsSeen()
    {
        var (registry, a, b) = Moving();
        var view = registry.View().Without<Frozen>();

        registry.Add(a, new Frozen());

        Assert.Equal([b], Collect(view));
    }

    [Fact]
    public void With_AttachmentCreatedAfterView_IsSeen()
    {
        var (registry, a, _) = Moving();
        var with = registry.View().WithAttachment<Gear>();

        registry.Attachments<Gear, Slot>().Attach(a, new Gear { Slot = Slot.Head });

        Assert.Equal([a], Collect(with));
    }

    [Fact]
    public void Without_AttachmentCreatedAfterView_IsSeen()
    {
        var (registry, a, b) = Moving();
        var without = registry.View().WithoutAttachment<Gear>();

        registry.Attachments<Gear, Slot>().Attach(a, new Gear { Slot = Slot.Head });

        Assert.Equal([b], Collect(without));
    }

    [Fact]
    public void MixedFilters_CreatedAfterView_AreSeen()
    {
        var (registry, a, b) = Moving();
        Entity c = registry.Create(new Pos(), new Vel());
        var view = registry.View().WithTag<Selected>().WithoutTag<Hidden>().Without<Frozen>();

        registry.Tag<Selected>(a);
        registry.Tag<Selected>(b);
        registry.Tag<Selected>(c);
        registry.Tag<Hidden>(b);
        registry.Add(c, new Frozen());

        Assert.Equal([a], Collect(view));
    }
}
