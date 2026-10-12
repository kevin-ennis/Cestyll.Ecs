using Cestyll.Ecs;
using Cestyll.Ecs.Attachments;
using Cestyll.Ecs.Relations;
using Cestyll.Ecs.Tags;

namespace Cestyll.Ecs.Tests;

public class EntityHandleTests
{
    private struct C1 : IComponent { public int V; }
    private struct C2 : IComponent { public int V; }

    private struct Selected : ITag;

    private struct Owns : IRelation
    {
        public static OnTargetDestroyed Policy => OnTargetDestroyed.Unrelate;
    }

    private enum ItemType { Coin, Gem }

    private struct Item : IAttachment<ItemType>
    {
        public ItemType Type;
        public int Count;
        public readonly ItemType Key => Type;
    }

    private struct Aims : ILink<Entity>
    {
        public Entity Target { get; init; }
        public int Power;
    }

    [Fact]
    public void Patch_Touch_FiresOnUpdate()
    {
        var registry = new Registry();
        var updates = 0;
        registry.Storage<C1>().OnUpdate += _ => updates++;
        var e = registry.Create().Add(new C1());

        e.Get<C1>().V = 4;
        e.Patch<C1>();

        Assert.Equal(1, updates);
    }

    [Fact]
    public void PatchLinkAndAttachment_EditsAndFires()
    {
        var registry = new Registry();
        Entity target = registry.Create();
        var linkUpdates = 0;
        var itemUpdates = 0;
        registry.Links<Aims>().OnUpdate += (_, _) => linkUpdates++;
        registry.Attachments<Item, ItemType>().OnUpdate += (_, _) => itemUpdates++;

        var e = registry.Create()
            .Link(new Aims { Target = target, Power = 1 })
            .Attach<Item, ItemType>(new Item { Type = ItemType.Coin, Count = 1 })
            .Patch<Aims>(target, (ref Aims a) => a.Power = 7)
            .Patch<Aims>(target)
            .Patch<Item, ItemType>(ItemType.Coin, (ref Item i) => i.Count = 9)
            .Patch<Item, ItemType>(ItemType.Coin);

        Assert.Equal(7, e.GetLink<Aims>(target).Power);
        Assert.Equal(9, e.GetAttachments<Item, ItemType>()[0].Count);
        Assert.Equal(2, linkUpdates);
        Assert.Equal(2, itemUpdates);
    }

    [Fact]
    public void Patch_MissingStore_ThrowsWithoutMakingIt()
    {
        var registry = new Registry();
        Entity a = registry.Create();
        Entity b = registry.Create();

        Assert.Throws<KeyNotFoundException>(() => registry.Patch<Aims>(a, b));
        Assert.Throws<KeyNotFoundException>(() => registry.GetLink<Aims>(a, b));
        Assert.Throws<KeyNotFoundException>(() => registry.Patch<Item, ItemType>(a, ItemType.Gem));
        Assert.DoesNotContain(registry.Stores(), s => s.Type == typeof(Aims) || s.Type == typeof(Item));
    }

    [Fact]
    public void LinkQueries_MirrorTheRegistry()
    {
        var registry = new Registry();
        Entity t1 = registry.Create();
        Entity t2 = registry.Create();
        var e = registry.Create()
            .Link(new Aims { Target = t1, Power = 1 })
            .Link(new Aims { Target = t2, Power = 2 });

        var handleCount = e.TargetCount<Aims>();
        var registryCount = registry.TargetCount<Aims>(e);
        var found = e.TryGetLink<Aims>(t2, out var link);
        var foundBetweenTargets = registry.TryGetLink<Aims>(t1, t2, out _);
        var sum = 0;
        foreach (ref var l in e.GetTargets<Aims>())
        {
            sum += l.Power;
        }
        var targetCount = registry.Handle(t1).TargetCount<Aims>();

        Assert.Equal(2, handleCount);
        Assert.Equal(2, registryCount);
        Assert.True(found);
        Assert.Equal(2, link.Power);
        Assert.False(foundBetweenTargets);
        Assert.Equal(3, sum);
        Assert.Equal(0, targetCount);
    }

    [Fact]
    public void RelationSources_MirrorTheRegistry()
    {
        var registry = new Registry();
        var owner = registry.Create();
        Entity a = registry.Create().Relate<Owns>(owner);
        Entity b = registry.Create().Relate<Owns>(owner);

        var sourceCount = owner.SourceCount<Owns>();
        var sources = new List<Entity>();
        foreach (var s in owner.GetSources<Owns>())
        {
            sources.Add(s);
        }

        Assert.Equal(2, sourceCount);
        Assert.Equal(2, sources.Count);
        Assert.Contains(a, sources);
        Assert.Contains(b, sources);
    }

    [Fact]
    public void Create_ChainsEdits()
    {
        var registry = new Registry();
        var owner = registry.Create();

        var e = registry.Create()
            .Add(new C1 { V = 1 })
            .Tag<Selected>()
            .Relate<Owns>(owner)
            .Attach<Item, ItemType>(new Item { Type = ItemType.Gem, Count = 3 });

        Assert.True(e.IsAlive);
        Assert.Equal(1, e.Get<C1>().V);
        Assert.True(e.HasTag<Selected>());
        Assert.Equal(owner.Entity, e.GetTarget<Owns>());
        Assert.Equal(3, e.GetAttachments<Item, ItemType>()[0].Count);

        Entity id = e;
        Assert.Equal(1, registry.Get<C1>(id).V);
        Assert.True(registry.HasTag<Selected>(id));
    }

    [Fact]
    public void Handle_AddReplaceAndPatch_EditExistingEntity()
    {
        var registry = new Registry();
        Entity id = registry.Create();

        var e = registry.Handle(id).Add(new C1 { V = 1 }).Replace(new C1 { V = 2 }).AddOrReplace(new C2 { V = 3 });
        e.Patch((ref C2 c) => c.V++);

        Assert.Equal(2, registry.Get<C1>(id).V);
        Assert.Equal(4, registry.Get<C2>(id).V);
    }

    [Fact]
    public void Handle_RemoveAndUntag_ReportSuccess()
    {
        var registry = new Registry();
        Entity id = registry.Create();
        var e = registry.Handle(id).Add(new C1 { V = 1 });

        var removed = e.Remove<C1>();
        var untagged = e.Tag<Selected>().Untag<Selected>();

        Assert.True(removed);
        Assert.False(e.Has<C1>());
        Assert.True(untagged);
    }

    [Fact]
    public void Handle_Destroy_KillsEntity()
    {
        var registry = new Registry();
        Entity id = registry.Create();
        var e = registry.Handle(id).Add(new C1 { V = 1 });

        e.Destroy();

        Assert.False(registry.IsAlive(id));
    }

    [Fact]
    public void Create_WithOneComponent()
    {
        var registry = new Registry();
        var e = registry.Create(new C1 { V = 1 });

        Assert.Equal(1, e.Get<C1>().V);
    }

    [Fact]
    public void Create_WithComponents_ReusesFreedIndex()
    {
        var registry = new Registry();
        Entity dead = registry.Create(new C1 { V = 1 });
        registry.Destroy(dead);

        Entity e = registry.Create(new C1 { V = 2 }, new C2 { V = 3 });

        Assert.Equal(dead.Index, e.Index);
        Assert.NotEqual(dead.Version, e.Version);
        Assert.Equal(2, registry.Get<C1>(e).V);
        Assert.False(registry.Has<C1>(dead));
    }
}
