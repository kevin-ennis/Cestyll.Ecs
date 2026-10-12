using Cestyll.Ecs;
using Cestyll.Ecs.Attachments;
using Cestyll.Ecs.Groups;
using Cestyll.Ecs.Relations;
using Cestyll.Ecs.Tags;

namespace Cestyll.Ecs.Tests;

// Signals on relations, link sets, attachment sets and tags.
public class StoreSignalTests
{
    private struct Owns : IRelation
    {
        public static OnTargetDestroyed Policy => OnTargetDestroyed.Unrelate;
    }

    private struct Parent : IRelation
    {
        public static OnTargetDestroyed Policy => OnTargetDestroyed.DestroySources;
    }

    private struct Attacks : ILink<Entity>
    {
        public Entity Target { get; init; }
        public int Damage;
    }

    private enum ItemType { Coin, Gem }

    private struct Item : IAttachment<ItemType>
    {
        public ItemType Type;
        public int Count;
        public readonly ItemType Key => Type;
    }

    private struct Selected : ITag;
    private struct Hidden : ITag;
    private struct Position : IComponent;
    private struct Velocity : IComponent;

    private static Attacks Hit(Entity target, int damage = 1) => new() { Target = target, Damage = damage };

    [Fact]
    public void Relation_LinkRelinkUnlink_FireInOrder()
    {
        var registry = new Registry();
        var owns = registry.Relations<Owns>();
        var (s, a, b) = ((Entity)registry.Create(), (Entity)registry.Create(), (Entity)registry.Create());
        var log = new List<string>();
        owns.OnRelate += (x, t) => log.Add($"link {x == s} {t == a}{t == b}");
        owns.OnUnrelate += (x, t) => log.Add($"unlink {x == s} {t == a}{t == b} {owns.GetTarget(x) == t}");

        registry.Relate<Owns>(s, a);
        registry.Relate<Owns>(s, a); // same pair: no-op
        registry.Relate<Owns>(s, b);
        registry.Unrelate<Owns>(s);

        Assert.Equal(
            ["link True TrueFalse", "unlink True TrueFalse True", "link True FalseTrue", "unlink True FalseTrue True"],
            log);
    }

    [Fact]
    public void Relation_DestroyTarget_FiresUnlinkForEachSource()
    {
        var registry = new Registry();
        Entity target = registry.Create();
        var sources = new[] { (Entity)registry.Create(), (Entity)registry.Create(), (Entity)registry.Create() };
        foreach (var s in sources)
        {
            registry.Relate<Owns>(s, target);
        }

        var unlinked = new List<(Entity, Entity)>();
        registry.Relations<Owns>().OnUnrelate += (s, t) => unlinked.Add((s, t));

        registry.Destroy(target);

        Assert.Equivalent(sources.Select(s => (s, target)).ToArray(), unlinked, strict: true);
    }

    [Fact]
    public void Relation_Cascade_FiresUnlinkForEachDestroyedChild()
    {
        var registry = new Registry();
        Entity root = registry.Create();
        Entity child = registry.Create();
        Entity grandchild = registry.Create();
        registry.Relate<Parent>(child, root);
        registry.Relate<Parent>(grandchild, child);
        var unlinked = new List<(Entity, Entity)>();
        registry.Relations<Parent>().OnUnrelate += (s, t) => unlinked.Add((s, t));

        registry.Destroy(root);

        Assert.Equivalent(new[] { (child, root), (grandchild, child) }, unlinked, strict: true);
        Assert.False(registry.IsAlive(grandchild));
    }

    [Fact]
    public void Relation_OnUnrelate_DestroyingTheDyingSource_FreesOnce()
    {
        var registry = new Registry();
        Entity s = registry.Create();
        registry.Relate<Owns>(s, registry.Create());
        registry.Relations<Owns>().OnUnrelate += (x, _) => registry.Destroy(x);

        registry.Destroy(s);

        Assert.False(registry.IsAlive(s));
        Assert.NotEqual(registry.Create().Entity.Index, registry.Create().Entity.Index);
    }

    [Fact]
    public void Links_LinkUpdatePatchUnlink_Fire()
    {
        var registry = new Registry();
        var set = registry.Links<Attacks>();
        var (s, t) = ((Entity)registry.Create(), (Entity)registry.Create());
        var log = new List<string>();
        set.OnLink += (_, _) => log.Add("link");
        set.OnUpdate += (x, y) => log.Add($"update {set.Get(x, y).Damage}");
        set.OnUnlink += (x, y) => log.Add($"unlink {set.Get(x, y).Damage}");

        set.Link(s, Hit(t, 1));
        set.Link(s, Hit(t, 3));
        set.Patch(s, t, static (ref Attacks a) => a.Damage *= 10);
        set.Get(s, t).Damage = 7;
        set.Patch(s, t);
        set.Unlink(s, t);

        Assert.Equal(["link", "update 3", "update 30", "update 7", "unlink 7"], log);
    }

    [Fact]
    public void Links_LinkReturnedRef_StaysValidAfterOnLink()
    {
        var registry = new Registry();
        var set = registry.Links<Attacks>();
        var (s, t) = ((Entity)registry.Create(), (Entity)registry.Create());
        var log = new List<string>();
        set.OnLink += (_, _) => log.Add("link");

        ref var link = ref set.Link(s, Hit(t, 1));
        link.Damage = 2;

        Assert.Equal(2, set.Get(s, t).Damage);
    }

    [Fact]
    public void Links_BulkUnlinks_FireForEachPair()
    {
        var registry = new Registry();
        var set = registry.Links<Attacks>();
        var (a, b, c) = ((Entity)registry.Create(), (Entity)registry.Create(), (Entity)registry.Create());
        set.Link(a, Hit(b));
        set.Link(a, Hit(c));
        set.Link(b, Hit(c));
        var unlinked = new List<(Entity, Entity)>();
        var readable = new List<bool>();
        set.OnUnlink += (x, y) =>
        {
            readable.Add(set.Has(x, y));
            unlinked.Add((x, y));
        };

        var fromTargets = set.UnlinkTargets(a);
        var fromSources = set.UnlinkSources(c);

        Assert.Equal(2, fromTargets);
        Assert.Equal(1, fromSources);
        Assert.All(readable, r => Assert.True(r)); // still readable
        Assert.Equivalent(new[] { (a, b), (a, c), (b, c) }, unlinked, strict: true);
        Assert.Equal(0, set.Sources.Length);
        Assert.Equal(0, set.Targets.Length);
    }

    [Fact]
    public void Links_Destroy_FiresBothDirections()
    {
        var registry = new Registry();
        var set = registry.Links<Attacks>();
        var (a, b, c) = ((Entity)registry.Create(), (Entity)registry.Create(), (Entity)registry.Create());
        set.Link(a, Hit(b));
        set.Link(b, Hit(c));
        set.Link(c, Hit(b));
        var unlinked = new List<(Entity, Entity)>();
        set.OnUnlink += (x, y) => unlinked.Add((x, y));

        registry.Destroy(b);

        Assert.Equivalent(new[] { (a, b), (b, c), (c, b) }, unlinked, strict: true);
        Assert.Equal(0, set.Sources.Length);
    }

    [Fact]
    public void Links_OnUnlink_MayUnlinkSiblingPairs()
    {
        var registry = new Registry();
        var set = registry.Links<Attacks>();
        var (a, b, c, d) = ((Entity)registry.Create(), (Entity)registry.Create(), (Entity)registry.Create(), (Entity)registry.Create());
        set.Link(a, Hit(b));
        set.Link(a, Hit(c));
        set.Link(a, Hit(d));
        var count = 0;
        set.OnUnlink += (x, _) =>
        {
            count++;
            set.Unlink(x, b);
        };

        set.UnlinkTargets(a);

        Assert.Equal(0, set.TargetCount(a));
        Assert.Equal(0, set.SourceCount(b));
        Assert.Equal(3, count);
    }

    [Fact]
    public void Attachments_AttachUpdatePatchDetach_Fire()
    {
        var registry = new Registry();
        var items = registry.Attachments<Item, ItemType>();
        Entity e = registry.Create();
        var log = new List<string>();
        items.OnAttach += (_, k) => log.Add($"attach {k}");
        items.OnUpdate += (x, k) => log.Add($"update {k} {items.Get(x, k).Count}");
        items.OnDetach += (x, k) => log.Add($"detach {k} {items.Get(x, k).Count}");

        items.Attach(e, new Item { Type = ItemType.Coin, Count = 1 });
        items.Attach(e, new Item { Type = ItemType.Coin, Count = 2 });
        items.Patch(e, ItemType.Coin, static (ref Item i) => i.Count += 10);
        items.Detach(e, ItemType.Coin);
        items.Detach(e, ItemType.Coin); // absent: no signal

        Assert.Equal(["attach Coin", "update Coin 2", "update Coin 12", "detach Coin 12"], log);
    }

    [Fact]
    public void Attachments_DetachItemsAndDestroy_FireForEachItem()
    {
        var registry = new Registry();
        var items = registry.Attachments<Item, ItemType>();
        var (a, b) = ((Entity)registry.Create(), (Entity)registry.Create());
        foreach (var e in new[] { a, b })
        {
            items.Attach(e, new Item { Type = ItemType.Coin });
            items.Attach(e, new Item { Type = ItemType.Gem });
        }

        var detached = new List<(Entity, ItemType)>();
        var readable = new List<bool>();
        items.OnDetach += (e, k) =>
        {
            readable.Add(items.Has(e, k));
            detached.Add((e, k));
        };

        var detachedCount = items.DetachItems(a);
        registry.Destroy(b);

        Assert.Equal(2, detachedCount);
        Assert.All(readable, r => Assert.True(r));
        Assert.Equivalent(
            new[] { (a, ItemType.Coin), (a, ItemType.Gem), (b, ItemType.Coin), (b, ItemType.Gem) },
            detached, strict: true);
        Assert.Equal(0, items.Entities.Length);
    }

    [Fact]
    public void Tags_TagAndUntag_Fire()
    {
        var registry = new Registry();
        var tags = registry.Tags<Selected>();
        var (a, b) = ((Entity)registry.Create(), (Entity)registry.Create());
        var log = new List<string>();
        tags.OnTag += x => log.Add($"tag {x == a} {registry.HasTag<Selected>(x)}");
        tags.OnUntag += x => log.Add($"untag {x == a} {registry.HasTag<Selected>(x)}");

        registry.Tag<Selected>(a);
        registry.Tag<Selected>(a); // already tagged: no signal
        registry.Tag<Selected>(b);
        registry.Untag<Selected>(a);
        registry.Destroy(b);

        Assert.Equal(["tag True True", "tag False True", "untag True True", "untag False True"], log);
    }

    [Fact]
    public void Tags_OnTag_SeesGroupMembership()
    {
        var registry = new Registry();
        var group = registry.Group().Own<Position>().Own<Velocity>().WithTag<Selected>().Build();
        Entity e = registry.Create();
        registry.Add(e, new Position());
        registry.Add(e, new Velocity());
        var grouped = false;
        registry.Tags<Selected>().OnTag += _ => grouped = group.Count == 1;

        registry.Tag<Selected>(e);

        Assert.True(grouped);
    }

    [Fact]
    public void Tags_OnUntag_DestroyingTheDyingEntity_FreesOnce()
    {
        var registry = new Registry();
        Entity e = registry.Create();
        registry.Tag<Selected>(e);
        registry.Tags<Selected>().OnUntag += registry.Destroy;

        registry.Destroy(e);

        Assert.False(registry.IsAlive(e));
        Assert.NotEqual(registry.Create().Entity.Index, registry.Create().Entity.Index);
    }

    [Fact]
    public void Observer_CollectsTaggedEntities()
    {
        var registry = new Registry();
        using var observer = registry.Observer().OnTag<Selected>();
        var (a, b) = ((Entity)registry.Create(), (Entity)registry.Create());

        registry.Tag<Selected>(a);
        registry.Tag<Selected>(b);
        registry.Untag<Selected>(a);
        registry.Destroy(b);

        Assert.Equal([a], observer.Entities.ToArray());
    }

    [Fact]
    public void Observer_TagFilters()
    {
        var registry = new Registry();
        using var observer = registry.Observer().OnAdd<Position>().WithTag<Selected>().WithoutTag<Hidden>();
        var (a, b, c) = ((Entity)registry.Create(), (Entity)registry.Create(), (Entity)registry.Create());
        registry.Tag<Selected>(b);
        registry.Tag<Selected>(c);
        registry.Tag<Hidden>(c);

        registry.Add(a, new Position());
        registry.Add(b, new Position());
        registry.Add(c, new Position());

        Assert.Equal([b], observer.Entities.ToArray());
    }

    [Fact]
    public void Observer_RequiringAndExcludingSameTag_Throws()
    {
        var registry = new Registry();
        using var observer = registry.Observer().OnAdd<Position>();
        observer.WithTag<Selected>();
        observer.WithoutTag<Hidden>();

        Assert.Throws<InvalidOperationException>(() => observer.WithoutTag<Selected>());
        Assert.Throws<InvalidOperationException>(() => observer.WithTag<Hidden>());
    }

    [Fact]
    public void Observer_RejectedTagFilter_AddsNothing()
    {
        var registry = new Registry();
        using var observer = registry.Observer().OnAdd<Position>();
        observer.WithTag<Selected>();
        Record.Exception(() => observer.WithoutTag<Selected>());
        observer.WithoutTag<Hidden>();
        Record.Exception(() => observer.WithTag<Hidden>());
        Entity e = registry.Create();

        registry.Tag<Selected>(e);
        registry.Add(e, new Position());

        Assert.Equal([e], observer.Entities.ToArray());
    }

    [Fact]
    public void Observer_WatchingAndExcludingSameTag_Throws()
    {
        var registry = new Registry();
        using var observer = registry.Observer().OnUntag<Selected>();
        observer.WithoutTag<Hidden>();
        observer.WithTag<Selected>(); // watching and requiring is fine

        Assert.Throws<InvalidOperationException>(() => observer.WithoutTag<Selected>());
        Assert.Throws<InvalidOperationException>(() => observer.OnTag<Hidden>());
    }

    [Fact]
    public void Observer_Disposed_RejectsTagFilters()
    {
        var registry = new Registry();
        var observer = registry.Observer().OnUntag<Selected>();
        observer.Dispose();

        Assert.Throws<ObjectDisposedException>(() => observer.WithoutTag<Hidden>());
    }

    [Fact]
    public void Observer_RelationWatches_CollectSources()
    {
        var registry = new Registry();
        using var related = registry.Observer().OnRelate<Owns>();
        using var unrelated = registry.Observer().OnUnrelate<Owns>();
        var (a, b, t) = ((Entity)registry.Create(), (Entity)registry.Create(), (Entity)registry.Create());

        registry.Relate<Owns>(a, t);
        registry.Relate<Owns>(b, t);
        registry.Unrelate<Owns>(a);
        registry.Destroy(t); // unlinks b

        Assert.Equal([a, b], related.Entities.ToArray());
        Assert.Equal([a, b], unrelated.Entities.ToArray());
    }

    [Fact]
    public void Observer_RelationWatch_DropsDestroyedSources()
    {
        var registry = new Registry();
        using var observer = registry.Observer().OnUnrelate<Parent>();
        var (child, parent) = ((Entity)registry.Create(), (Entity)registry.Create());
        registry.Relate<Parent>(child, parent);

        registry.Destroy(parent); // destroys child, which unlinks it

        Assert.Equal(0, observer.Count);
    }

    [Fact]
    public void Observer_LinkWatches_CollectSources()
    {
        var registry = new Registry();
        var set = registry.Links<Attacks>();
        using var linked = registry.Observer().OnLink<Attacks>();
        using var updated = registry.Observer().OnLinkUpdate<Attacks>();
        using var unlinked = registry.Observer().OnUnlink<Attacks>();
        var (a, b, t) = ((Entity)registry.Create(), (Entity)registry.Create(), (Entity)registry.Create());

        set.Link(a, Hit(t));
        set.Link(b, Hit(t));
        set.Patch(b, t, static (ref Attacks x) => x.Damage++);
        set.Unlink(a, t);

        Assert.Equal([a, b], linked.Entities.ToArray());
        Assert.Equal([b], updated.Entities.ToArray());
        Assert.Equal([a], unlinked.Entities.ToArray());
    }

    [Fact]
    public void Observer_UnlinkWatch_CollectsWhenTargetDestroyed()
    {
        var registry = new Registry();
        var set = registry.Links<Attacks>();
        using var unlinked = registry.Observer().OnUnlink<Attacks>();
        var (a, b, t) = ((Entity)registry.Create(), (Entity)registry.Create(), (Entity)registry.Create());
        set.Link(a, Hit(t));
        set.Link(b, Hit(t));
        set.Unlink(a, t);

        registry.Destroy(t);

        Assert.Equal([a, b], unlinked.Entities.ToArray());
    }

    [Fact]
    public void Observer_AttachmentWatches_CollectOwners()
    {
        var registry = new Registry();
        var items = registry.Attachments<Item, ItemType>();
        using var attached = registry.Observer().OnAttach<Item, ItemType>();
        using var updated = registry.Observer().OnAttachmentUpdate<Item, ItemType>();
        using var detached = registry.Observer().OnDetach<Item, ItemType>();
        var (a, b) = ((Entity)registry.Create(), (Entity)registry.Create());

        items.Attach(a, new Item { Type = ItemType.Coin });
        items.Attach(a, new Item { Type = ItemType.Gem });
        items.Attach(b, new Item { Type = ItemType.Coin });
        items.Attach(b, new Item { Type = ItemType.Coin, Count = 2 });
        items.Detach(a, ItemType.Gem);
        registry.Destroy(b);

        Assert.Equal([a], attached.Entities.ToArray()); // b dropped on destroy
        Assert.Equal(0, updated.Count);
        Assert.Equal([a], detached.Entities.ToArray());
    }

    [Fact]
    public void Observer_StoreWatches_ApplyFilters()
    {
        var registry = new Registry();
        using var observer = registry.Observer().OnRelate<Owns>().OnLink<Attacks>().WithTag<Selected>();
        var (a, b, t) = ((Entity)registry.Create(), (Entity)registry.Create(), (Entity)registry.Create());
        registry.Tag<Selected>(b);

        registry.Relate<Owns>(a, t);
        registry.Relate<Owns>(b, t);
        registry.Links<Attacks>().Link(a, Hit(t));

        Assert.Equal([b], observer.Entities.ToArray());
    }

    [Fact]
    public void Observer_StoreWatches_DisposeDisconnects()
    {
        var registry = new Registry();
        var observer = registry.Observer().OnRelate<Owns>().OnLink<Attacks>().WithTag<Selected>();
        var (a, b, t) = ((Entity)registry.Create(), (Entity)registry.Create(), (Entity)registry.Create());
        registry.Tag<Selected>(b);
        registry.Relate<Owns>(a, t);
        registry.Relate<Owns>(b, t);
        registry.Links<Attacks>().Link(a, Hit(t));

        observer.Dispose();
        registry.Links<Attacks>().Link(b, Hit(t));
        var thrown = Record.Exception(() => observer.OnAttach<Item, ItemType>());

        Assert.Equal(0, observer.Count);
        Assert.IsType<ObjectDisposedException>(thrown);
    }

    // Removing the item whose removal signal is running is a no-op, not a recursion.
    [Fact]
    public void RemovingSelfFromOwnRemovalHandler_IsNoOp()
    {
        var registry = new Registry();
        Entity e = registry.Create();
        Entity t = registry.Create();
        var fired = new List<string>();
        var nested = new List<bool>();
        registry.Add(e, new Position());
        registry.Storage<Position>().OnRemove += x =>
        {
            fired.Add("component");
            nested.Add(registry.Remove<Position>(x));
        };
        registry.Tag<Selected>(e);
        registry.Tags<Selected>().OnUntag += x =>
        {
            fired.Add("tag");
            nested.Add(registry.Untag<Selected>(x));
        };
        registry.Relate<Owns>(e, t);
        registry.Relations<Owns>().OnUnrelate += (x, _) =>
        {
            fired.Add("relation");
            nested.Add(registry.Unrelate<Owns>(x));
        };
        var links = registry.Links<Attacks>();
        links.Link(e, Hit(t));
        links.OnUnlink += (x, y) =>
        {
            fired.Add("link");
            nested.Add(links.Unlink(x, y));
        };
        var items = registry.Attachments<Item, ItemType>();
        items.Attach(e, new Item { Type = ItemType.Gem });
        items.OnDetach += (x, k) =>
        {
            fired.Add("attachment");
            nested.Add(items.Detach(x, k));
        };

        var removed = registry.Remove<Position>(e);
        var untagged = registry.Untag<Selected>(e);
        var unrelated = registry.Unrelate<Owns>(e);
        var unlinked = links.Unlink(e, t);
        var detached = items.Detach(e, ItemType.Gem);

        Assert.True(removed);
        Assert.True(untagged);
        Assert.True(unrelated);
        Assert.True(unlinked);
        Assert.True(detached);
        Assert.Equal(["component", "tag", "relation", "link", "attachment"], fired);
        Assert.All(nested, Assert.False);
        Assert.False(registry.Has<Position>(e));
        Assert.False(registry.HasTag<Selected>(e));
        Assert.Equal(Entity.Null, registry.GetTarget<Owns>(e));
        Assert.False(links.Has(e, t));
        Assert.False(items.Has(e));
    }

    [Fact]
    public void Relation_RelinkFromOwnOnUnrelate_Throws_AndLeavesSourceUnrelated()
    {
        var registry = new Registry();
        var owns = registry.Relations<Owns>();
        var (s, a, b) = ((Entity)registry.Create(), (Entity)registry.Create(), (Entity)registry.Create());
        registry.Relate<Owns>(s, a);
        Exception? thrown = null;
        owns.OnUnrelate += (x, _) => thrown = Record.Exception(() => registry.Relate<Owns>(x, b));

        registry.Unrelate<Owns>(s);

        Assert.IsType<InvalidOperationException>(thrown);
        Assert.Equal(Entity.Null, owns.GetTarget(s));
        Assert.Equal(0, owns.SourceCount(a));
        Assert.Equal(0, owns.SourceCount(b));
    }

    [Fact]
    public void Links_UnlinkTargetsFromOwnOnUnlink_StillClearsTheRest()
    {
        var registry = new Registry();
        var set = registry.Links<Attacks>();
        var (a, b, c, d) = ((Entity)registry.Create(), (Entity)registry.Create(), (Entity)registry.Create(), (Entity)registry.Create());
        set.Link(a, Hit(b));
        set.Link(a, Hit(c));
        set.Link(a, Hit(d));
        set.OnUnlink += (x, y) =>
        {
            if (y == d)
            {
                set.UnlinkTargets(x);
            }
        };

        set.Unlink(a, d);

        Assert.Equal(0, set.TargetCount(a));
        Assert.Equal(0, set.Targets.Length);
    }

    [Fact]
    public void OnRemoveHandler_CannotLinkOrAttachToDyingEntity()
    {
        var registry = new Registry();
        Entity e = registry.Create();
        Entity other = registry.Create();
        registry.Add(e, new Position());
        var errors = new List<Exception?>();
        registry.Storage<Position>().OnRemove += x =>
        {
            errors.Add(Record.Exception(() => registry.Relate<Owns>(other, x)));
            errors.Add(Record.Exception(() => registry.Links<Attacks>().Link(x, Hit(other))));
            errors.Add(Record.Exception(
                () => registry.Attach<Item, ItemType>(x, new Item { Type = ItemType.Gem })));
        };

        registry.Destroy(e);

        Assert.All(errors, ex => Assert.IsType<InvalidOperationException>(ex));
        Assert.Equal(Entity.Null, registry.GetTarget<Owns>(other));
        Assert.Equal(0, registry.Links<Attacks>().Sources.Length);
        Assert.Equal(0, registry.Attachments<Item, ItemType>().Entities.Length);
    }
}
