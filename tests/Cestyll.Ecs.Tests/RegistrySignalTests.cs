using Cestyll.Ecs;
using Cestyll.Ecs.Attachments;
using Cestyll.Ecs.Relations;
using Cestyll.Ecs.Tags;

namespace Cestyll.Ecs.Tests;

// The registry's signal shortcuts and their Connections.
public class RegistrySignalTests
{
    private struct Health : IComponent { public int Value; }

    private struct Owns : IRelation
    {
        public static OnTargetDestroyed Policy => OnTargetDestroyed.Unrelate;
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

    [Fact]
    public void ComponentSignals_Fire_UntilDisposed()
    {
        var registry = new Registry();
        Entity e = registry.Create();
        var log = new List<string>();
        var constructed = registry.OnAdd<Health>(_ => log.Add("construct"));
        var updated = registry.OnUpdate<Health>(_ => log.Add("update"));
        var destroyed = registry.OnRemove<Health>(_ => log.Add("destroy"));

        registry.Add(e, new Health { Value = 1 });
        registry.Replace(e, new Health { Value = 2 });
        registry.Remove<Health>(e);

        constructed.Dispose();
        updated.Dispose();
        destroyed.Dispose();
        registry.Add(e, new Health { Value = 3 });
        registry.Replace(e, new Health { Value = 4 });
        registry.Remove<Health>(e);

        Assert.Equal(["construct", "update", "destroy"], log);
    }

    [Fact]
    public void ComponentSignals_OnNamedPool_LeaveTheDefaultPoolAlone()
    {
        var registry = new Registry();
        Entity e = registry.Create();
        var log = new List<string>();
        using var constructed = registry.OnAdd<Health>("previous", _ => log.Add("construct"));
        using var updated = registry.OnUpdate<Health>("previous", _ => log.Add("update"));
        using var destroyed = registry.OnRemove<Health>("previous", _ => log.Add("destroy"));

        registry.Add(e, new Health()); // default pool: no signal
        var previous = registry.Storage<Health>("previous");
        previous.Add(e, new Health());
        previous.Replace(e, new Health { Value = 1 });
        previous.Remove(e);

        Assert.Equal(["construct", "update", "destroy"], log);
    }

    [Fact]
    public void TagSignals_Fire_UntilDisposed()
    {
        var registry = new Registry();
        Entity e = registry.Create();
        var log = new List<string>();
        var tagged = registry.OnTag<Selected>(_ => log.Add("tag"));
        var untagged = registry.OnUntag<Selected>(_ => log.Add("untag"));

        registry.Tag<Selected>(e);
        registry.Untag<Selected>(e);
        tagged.Dispose();
        untagged.Dispose();
        registry.Tag<Selected>(e);
        registry.Untag<Selected>(e);

        Assert.Equal(["tag", "untag"], log);
    }

    [Fact]
    public void RelationSignals_Fire_UntilDisposed()
    {
        var registry = new Registry();
        var (s, t) = ((Entity)registry.Create(), (Entity)registry.Create());
        var log = new List<string>();
        var related = registry.OnRelate<Owns>((x, y) => log.Add($"relate {x == s} {y == t}"));
        var unrelated = registry.OnUnrelate<Owns>((x, y) => log.Add($"unrelate {x == s} {y == t}"));

        registry.Relate<Owns>(s, t);
        registry.Unrelate<Owns>(s);
        related.Dispose();
        unrelated.Dispose();
        registry.Relate<Owns>(s, t);
        registry.Unrelate<Owns>(s);

        Assert.Equal(["relate True True", "unrelate True True"], log);
    }

    [Fact]
    public void LinkSetSignals_Fire_UntilDisposed()
    {
        var registry = new Registry();
        var (s, t) = ((Entity)registry.Create(), (Entity)registry.Create());
        var log = new List<string>();
        var linked = registry.OnLink<Attacks>((_, _) => log.Add("link"));
        var updated = registry.OnLinkUpdate<Attacks>((_, _) => log.Add("update"));
        var unlinked = registry.OnUnlink<Attacks>((_, _) => log.Add("unlink"));

        registry.Link(s, new Attacks { Target = t, Damage = 1 });
        registry.Link(s, new Attacks { Target = t, Damage = 2 });
        registry.Unlink<Attacks>(s, t);
        linked.Dispose();
        updated.Dispose();
        unlinked.Dispose();
        registry.Link(s, new Attacks { Target = t, Damage = 1 });
        registry.Link(s, new Attacks { Target = t, Damage = 2 });
        registry.Unlink<Attacks>(s, t);

        Assert.Equal(["link", "update", "unlink"], log);
    }

    [Fact]
    public void AttachmentSignals_Fire_UntilDisposed()
    {
        var registry = new Registry();
        Entity e = registry.Create();
        var log = new List<string>();
        var attached = registry.OnAttach<Item, ItemType>((_, k) => log.Add($"attach {k}"));
        var updated = registry.OnAttachmentUpdate<Item, ItemType>((_, k) => log.Add($"update {k}"));
        var detached = registry.OnDetach<Item, ItemType>((_, k) => log.Add($"detach {k}"));

        registry.Attach<Item, ItemType>(e, new Item { Type = ItemType.Gem, Count = 1 });
        registry.Attach<Item, ItemType>(e, new Item { Type = ItemType.Gem, Count = 2 });
        registry.Detach<Item, ItemType>(e, ItemType.Gem);
        attached.Dispose();
        updated.Dispose();
        detached.Dispose();
        registry.Attach<Item, ItemType>(e, new Item { Type = ItemType.Gem, Count = 1 });
        registry.Attach<Item, ItemType>(e, new Item { Type = ItemType.Gem, Count = 2 });
        registry.Detach<Item, ItemType>(e, ItemType.Gem);

        Assert.Equal(["attach Gem", "update Gem", "detach Gem"], log);
    }

    [Fact]
    public void Connection_DisposingTwice_IsANoOp()
    {
        var registry = new Registry();
        Entity e = registry.Create();
        var count = 0;
        var first = registry.OnTag<Selected>(_ => count++);
        first.Dispose();
        first.Dispose();

        registry.Tag<Selected>(e);

        Assert.Equal(0, count);
    }

    [Fact]
    public void Connection_SameHandlerReconnected_Fires()
    {
        var registry = new Registry();
        Entity e = registry.Create();
        var count = 0;
        Action<Entity> handler = _ => count++;
        var first = registry.OnTag<Selected>(handler);
        first.Dispose();
        first.Dispose();
        using var second = registry.OnTag<Selected>(handler);

        registry.Tag<Selected>(e);

        Assert.Equal(1, count);
    }

    [Fact]
    public void Connection_Default_DisposeIsANoOp()
    {
        var connection = default(Connection);

        var thrown = Record.Exception(() => connection.Dispose());

        Assert.Null(thrown);
    }

    [Fact]
    public void OnRemove_Disposed_LeavesNoDestroySignal()
    {
        var registry = new Registry();
        var connection = registry.OnRemove<Health>(_ => { });

        connection.Dispose();

        Assert.Equal(0, registry.DestroySignals);
    }

    [Fact]
    public void Destroy_AfterOnRemoveDisposed_KillsEntity()
    {
        var registry = new Registry();
        registry.OnRemove<Health>(_ => { }).Dispose();
        Entity e = registry.Create();
        registry.Add(e, new Health());

        registry.Destroy(e);

        Assert.False(registry.IsAlive(e));
    }

    [Fact]
    public void OnRemove_Reconnected_CountsDestroySignal()
    {
        var registry = new Registry();
        registry.OnRemove<Health>(_ => { }).Dispose();

        using var again = registry.OnRemove<Health>(x => registry.Destroy(x));

        Assert.Equal(1, registry.DestroySignals);
    }

    [Fact]
    public void OnRemove_ReconnectedReentrantDestroy_FreesEntityOnce()
    {
        var registry = new Registry();
        registry.OnRemove<Health>(_ => { }).Dispose();
        Entity f = registry.Create();
        registry.Add(f, new Health());
        using var again = registry.OnRemove<Health>(x => registry.Destroy(x)); // re-enters

        registry.Destroy(f);
        Entity next = registry.Create();

        Assert.Equal(f.Index, next.Index);
        Assert.Equal(f.Version + 1, next.Version); // freed once
    }

    [Fact]
    public void NullHandler_Throws()
    {
        var registry = new Registry();

        Assert.Throws<ArgumentNullException>(() => registry.OnAdd<Health>(null!));
        Assert.Throws<ArgumentNullException>(() => registry.OnRemove<Health>("previous", null!));
        Assert.Throws<ArgumentNullException>(() => registry.OnTag<Selected>(null!));
        Assert.Throws<ArgumentNullException>(() => registry.OnRelate<Owns>(null!));
        Assert.Throws<ArgumentNullException>(() => registry.OnUnlink<Attacks>(null!));
        Assert.Throws<ArgumentNullException>(() => registry.OnDetach<Item, ItemType>(null!));
    }
}
