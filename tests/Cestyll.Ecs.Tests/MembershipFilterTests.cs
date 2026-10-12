using Cestyll.Ecs;
using Cestyll.Ecs.Attachments;
using Cestyll.Ecs.Groups;
using Cestyll.Ecs.Inboxes;
using Cestyll.Ecs.Relations;
using Cestyll.Ecs.Views;

namespace Cestyll.Ecs.Tests;

// The attachment, inbox, relation and link filters of views and groups.
public class MembershipFilterTests
{
    private struct Pos : IComponent { public int X; }

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

    private static List<Entity> Walk(View<Entity> view)
    {
        var seen = new List<Entity>();
        view.Each((Entity e, ref Pos _) => seen.Add(e));
        seen.Sort((a, b) => a.Index.CompareTo(b.Index));
        return seen;
    }

    private static List<Entity> Members(Group<Entity> group)
    {
        var members = group.Entities.ToArray().ToList();
        members.Sort((a, b) => a.Index.CompareTo(b.Index));
        return members;
    }

    // ship <- turret, ship <- gun; rock alone.
    private static (Registry, Entity ship, Entity turret, Entity gun, Entity rock) Hierarchy()
    {
        var registry = new Registry();
        Entity ship = registry.Create(new Pos());
        Entity turret = registry.Create(new Pos());
        Entity gun = registry.Create(new Pos());
        Entity rock = registry.Create(new Pos());
        registry.Relate<Parent>(turret, ship);
        registry.Relate<Parent>(gun, ship);
        return (registry, ship, turret, gun, rock);
    }

    [Fact]
    public void View_RelationFilters_PickTheEnd()
    {
        var (registry, ship, turret, gun, rock) = Hierarchy();

        Assert.Equal([turret, gun], Walk(registry.View().WithRelationTarget<Parent>()));
        Assert.Equal([ship, rock], Walk(registry.View().WithoutRelationTarget<Parent>()));
        Assert.Equal([ship], Walk(registry.View().WithRelationSources<Parent>()));
        Assert.Equal([turret, gun, rock], Walk(registry.View().WithoutRelationSources<Parent>()));
        Assert.Equal([rock], Walk(registry.View().WithoutRelationTarget<Parent>().WithoutRelationSources<Parent>()));
    }

    [Fact]
    public void View_LinkFilters_PickTheEnd()
    {
        var registry = new Registry();
        Entity archer = registry.Create(new Pos());
        Entity orc = registry.Create(new Pos());
        Entity troll = registry.Create(new Pos());
        registry.Link(archer, new Attacks { Target = orc });
        registry.Link(archer, new Attacks { Target = troll });

        Assert.Equal([archer], Walk(registry.View().WithLinkTargets<Attacks>()));
        Assert.Equal([orc, troll], Walk(registry.View().WithoutLinkTargets<Attacks>()));
        Assert.Equal([orc, troll], Walk(registry.View().WithLinkSources<Attacks>()));
        Assert.Equal([archer], Walk(registry.View().WithoutLinkSources<Attacks>()));
    }

    [Fact]
    public void View_RelationFilter_BeforeAnyStore_MatchesNothing()
    {
        var registry = new Registry();
        Entity a = registry.Create(new Pos());
        Entity b = registry.Create(new Pos());
        var parented = registry.View().WithRelationTarget<Parent>();
        var roots = registry.View().WithoutRelationTarget<Parent>();

        var parentedSeen = Walk(parented);
        var rootsSeen = Walk(roots);

        Assert.Empty(parentedSeen);
        Assert.Equal([a, b], rootsSeen);
    }

    [Fact]
    public void View_StoreMadeAfterTheView_IsSeen()
    {
        var registry = new Registry();
        Entity a = registry.Create(new Pos());
        Entity b = registry.Create(new Pos());
        var parented = registry.View().WithRelationTarget<Parent>();
        var roots = registry.View().WithoutRelationTarget<Parent>();
        Walk(parented); // walked once while no store exists
        Walk(roots);

        registry.Relate<Parent>(a, b);

        Assert.Equal([a], Walk(parented));
        Assert.Equal([b], Walk(roots));
    }

    [Fact]
    public void View_InboxFilters()
    {
        var registry = new Registry();
        Entity a = registry.Create(new Pos());
        Entity b = registry.Create(new Pos());
        registry.Post(b, new Hit());

        var with = Walk(registry.View().WithInbox<Hit>());
        var without = Walk(registry.View().WithoutInbox<Hit>());

        Assert.Equal([b], with);
        Assert.Equal([a], without);
    }

    [Fact]
    public void Group_RelationFilters_TrackRelate()
    {
        var (registry, ship, turret, gun, rock) = Hierarchy();
        var parented = registry.Group().With<Pos>().WithRelationTarget<Parent>().Build();
        var parents = registry.Group().With<Pos>().WithRelationSources<Parent>().Build();
        var loners = registry.Group().With<Pos>().WithoutRelationTarget<Parent>().WithoutRelationSources<Parent>().Build();

        registry.Relate<Parent>(rock, turret);

        Assert.Equal([turret, gun, rock], Members(parented));
        Assert.Equal([ship, turret], Members(parents));
        Assert.Empty(Members(loners));
    }

    [Fact]
    public void Group_RelationFilters_TrackUnrelate()
    {
        var (registry, ship, turret, gun, rock) = Hierarchy();
        var parented = registry.Group().With<Pos>().WithRelationTarget<Parent>().Build();
        var loners = registry.Group().With<Pos>().WithoutRelationTarget<Parent>().WithoutRelationSources<Parent>().Build();
        registry.Relate<Parent>(rock, turret);

        registry.Unrelate<Parent>(gun);

        Assert.Equal([turret, rock], Members(parented));
        Assert.Equal([gun], Members(loners));
    }

    [Fact]
    public void Group_RelationFilters_TrackDestroy()
    {
        var (registry, ship, turret, gun, rock) = Hierarchy();
        var parented = registry.Group().With<Pos>().WithRelationTarget<Parent>().Build();
        var parents = registry.Group().With<Pos>().WithRelationSources<Parent>().Build();
        registry.Relate<Parent>(rock, turret);
        registry.Unrelate<Parent>(gun);

        registry.Destroy(ship); // Unrelate policy: turret loses its parent and stays

        Assert.Equal([rock], Members(parented));
        Assert.Equal([turret], Members(parents));
    }

    [Fact]
    public void OwningGroup_WithSources_PacksParentsOnly()
    {
        var (registry, ship, turret, gun, rock) = Hierarchy();

        var parents = registry.Group().Own<Pos>().WithRelationSources<Parent>().Build();

        Assert.Equal([ship], Members(parents));
        Assert.Equal(registry.Get<Pos>(ship).X, parents.Span<Pos>()[0].X);
    }

    [Fact]
    public void OwningGroup_WithSources_TracksRelateAndUnrelate()
    {
        var (registry, ship, turret, gun, rock) = Hierarchy();
        var parents = registry.Group().Own<Pos>().WithRelationSources<Parent>().Build();
        registry.Relate<Parent>(rock, gun);
        var related = Members(parents);
        var seen = new List<Entity>();
        parents.Each((Entity e, ref Pos p) => seen.Add(e));

        registry.Unrelate<Parent>(rock);

        Assert.Equal([ship, gun], related);
        Assert.Equal(2, seen.Count);
        Assert.Equal([ship], Members(parents));
    }

    [Fact]
    public void Group_LinkFilters_TrackLink()
    {
        var registry = new Registry();
        Entity archer = registry.Create(new Pos());
        Entity orc = registry.Create(new Pos());
        var attackers = registry.Group().With<Pos>().WithLinkTargets<Attacks>().Build();
        var targets = registry.Group().Own<Pos>().WithLinkSources<Attacks>().Build();
        var before = Members(attackers);

        registry.Link(archer, new Attacks { Target = orc });

        Assert.Empty(before);
        Assert.Equal([archer], Members(attackers));
        Assert.Equal([orc], Members(targets));
    }

    [Fact]
    public void Group_LinkFilters_TrackUnlink()
    {
        var registry = new Registry();
        Entity archer = registry.Create(new Pos());
        Entity orc = registry.Create(new Pos());
        var attackers = registry.Group().With<Pos>().WithLinkTargets<Attacks>().Build();
        var targets = registry.Group().Own<Pos>().WithLinkSources<Attacks>().Build();
        registry.Link(archer, new Attacks { Target = orc });

        registry.Unlink<Attacks>(archer, orc);

        Assert.Empty(Members(attackers));
        Assert.Empty(Members(targets));
    }

    [Fact]
    public void Group_AttachmentFilter_BuiltBeforeAnyStore_TracksAttach()
    {
        var registry = new Registry();
        Entity a = registry.Create(new Pos());
        Entity b = registry.Create(new Pos());
        var geared = registry.Group().With<Pos>().WithAttachment<Gear>().Build();
        var bare = registry.Group().Own<Pos>().WithoutAttachment<Gear>().Build();
        var gearedBefore = Members(geared);
        var bareBefore = Members(bare);

        registry.Attach<Gear, Slot>(a, new Gear { Slot = Slot.Head });
        registry.Attach<Gear, Slot>(a, new Gear { Slot = Slot.Hand });

        Assert.Empty(gearedBefore);
        Assert.Equal([a, b], bareBefore);
        Assert.Equal([a], Members(geared));
        Assert.Equal([b], Members(bare));
    }

    [Fact]
    public void Group_AttachmentFilter_LeavesOnlyWhenLastItemDetached()
    {
        var registry = new Registry();
        Entity a = registry.Create(new Pos());
        Entity b = registry.Create(new Pos());
        var geared = registry.Group().With<Pos>().WithAttachment<Gear>().Build();
        var bare = registry.Group().Own<Pos>().WithoutAttachment<Gear>().Build();
        registry.Attach<Gear, Slot>(a, new Gear { Slot = Slot.Head });
        registry.Attach<Gear, Slot>(a, new Gear { Slot = Slot.Hand });

        registry.Detach<Gear, Slot>(a, Slot.Head);
        var afterFirst = Members(geared); // still holds one
        registry.Detach<Gear, Slot>(a, Slot.Hand);

        Assert.Equal([a], afterFirst);
        Assert.Empty(Members(geared));
        Assert.Equal([a, b], Members(bare));
    }

    [Fact]
    public void Group_InboxFilter_TracksPost()
    {
        var registry = new Registry();
        Entity a = registry.Create(new Pos());
        Entity b = registry.Create(new Pos());
        var hit = registry.Group().With<Pos>().WithInbox<Hit>().Build();
        var calm = registry.Group().With<Pos>().WithoutInbox<Hit>().Build();

        registry.Post(a, new Hit());
        registry.Post(a, new Hit());

        Assert.Equal([a], Members(hit));
        Assert.Equal([b], Members(calm));
    }

    [Fact]
    public void Group_InboxFilter_TracksClear()
    {
        var registry = new Registry();
        Entity a = registry.Create(new Pos());
        Entity b = registry.Create(new Pos());
        var hit = registry.Group().With<Pos>().WithInbox<Hit>().Build();
        var calm = registry.Group().With<Pos>().WithoutInbox<Hit>().Build();
        registry.Post(a, new Hit());
        registry.Post(a, new Hit());

        registry.Inboxes<Hit>().Clear();

        Assert.Empty(Members(hit));
        Assert.Equal([a, b], Members(calm));
    }

    [Fact]
    public void Group_SameRulesInAnyOrder_IsTheSameGroup()
    {
        var registry = new Registry();
        var group = registry.Group().With<Pos>().WithRelationTarget<Parent>().WithoutAttachment<Gear>().Build();

        Assert.Same(group, registry.Group().WithoutAttachment<Gear>().WithRelationTarget<Parent>().With<Pos>().Build());
        Assert.NotSame(group, registry.Group().With<Pos>().WithRelationSources<Parent>().WithoutAttachment<Gear>().Build());
    }

    [Fact]
    public void Group_OnlyARelationRule_IsNonOwning_AndViewsCanUseIt()
    {
        var (registry, ship, turret, gun, rock) = Hierarchy();
        var parented = registry.Group().WithRelationTarget<Parent>().Build();

        var walked = Walk(registry.View().Use(parented));

        Assert.False(parented.Owning);
        Assert.Equal([turret, gun], Members(parented));
        Assert.Equal([turret, gun], walked);
    }

    [Fact]
    public void Group_OnlyARelationRule_Each_Throws()
    {
        var (registry, _, _, _, _) = Hierarchy();
        var parented = registry.Group().WithRelationTarget<Parent>().Build();

        Assert.Throws<InvalidOperationException>(() => parented.Each((Entity e, ref Pos p) => { }));
    }

    [Fact]
    public void Join_RequiresAndExcludesTheSameEnd_Throws()
    {
        var registry = new Registry();

        Assert.Throws<InvalidOperationException>(() =>
            registry.View().WithRelationTarget<Parent>().Join(registry.View().WithoutRelationTarget<Parent>()).Each((Entity e, ref Pos _) => { }));
    }

    [Fact]
    public void View_KeyedAttachmentFilters_BeforeAnyStore()
    {
        var registry = new Registry();
        Entity a = registry.Create(new Pos());
        Entity b = registry.Create(new Pos());
        Entity c = registry.Create(new Pos());
        var heads = registry.View().WithAttachment<Gear, Slot>(Slot.Head);
        var noHead = registry.View().WithoutAttachment<Gear, Slot>(Slot.Head);

        var headsSeen = Walk(heads);
        var noHeadSeen = Walk(noHead);

        Assert.Empty(headsSeen);
        Assert.Equal([a, b, c], noHeadSeen);
    }

    [Fact]
    public void View_KeyedAttachmentFilters_MatchTheKey()
    {
        var registry = new Registry();
        Entity a = registry.Create(new Pos());
        Entity b = registry.Create(new Pos());
        Entity c = registry.Create(new Pos());
        var heads = registry.View().WithAttachment<Gear, Slot>(Slot.Head);
        var noHead = registry.View().WithoutAttachment<Gear, Slot>(Slot.Head);
        Walk(heads); // walked once while no store exists
        Walk(noHead);

        registry.Attach<Gear, Slot>(a, new Gear { Slot = Slot.Head });
        registry.Attach<Gear, Slot>(b, new Gear { Slot = Slot.Hand });

        Assert.Equal([a], Walk(heads));
        Assert.Equal([b, c], Walk(noHead));
        Assert.Equal([b], Walk(registry.View().WithAttachment<Gear, Slot>(Slot.Hand).WithoutAttachment<Gear, Slot>(Slot.Head)));
    }

    [Fact]
    public void View_KeyedAttachmentFilter_TracksDetach()
    {
        var registry = new Registry();
        Entity a = registry.Create(new Pos());
        Entity b = registry.Create(new Pos());
        var heads = registry.View().WithAttachment<Gear, Slot>(Slot.Head);
        registry.Attach<Gear, Slot>(a, new Gear { Slot = Slot.Head });
        registry.Attach<Gear, Slot>(b, new Gear { Slot = Slot.Hand });
        Walk(heads);

        registry.Detach<Gear, Slot>(a, Slot.Head);

        Assert.Empty(Walk(heads));
    }

    [Fact]
    public void Join_SameKeyRequiredAndExcluded_Throws()
    {
        var registry = new Registry();
        var head = registry.View().WithAttachment<Gear, Slot>(Slot.Head);

        Assert.Throws<InvalidOperationException>(() =>
            head.Join(registry.View().WithoutAttachment<Gear, Slot>(Slot.Head)));
    }

    [Fact]
    public void Join_OtherKeyExcluded_IsAllowed()
    {
        var registry = new Registry();
        var head = registry.View().WithAttachment<Gear, Slot>(Slot.Head);

        var error = Record.Exception(() => head.Join(registry.View().WithoutAttachment<Gear, Slot>(Slot.Hand)));

        Assert.Null(error);
    }
}
