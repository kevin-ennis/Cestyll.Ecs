using Cestyll.Ecs;
using Cestyll.Ecs.Attachments;
using Cestyll.Ecs.Relations;
using Cestyll.Ecs.Snapshots;
using Cestyll.Ecs.Tags;

namespace Cestyll.Ecs.Tests;

// Repeated passes diff against live state instead of replacing it.
public class ContinuousLoaderTests
{
    private struct Position : IComponent { public int X; }
    private struct Velocity : IComponent { public int X; }
    private struct Marked : ITag;
    private struct Parent : IRelation
    {
        public static OnTargetDestroyed Policy => OnTargetDestroyed.Unrelate;
    }

    private struct Attacks : ILink<Entity>
    {
        public Entity Target { get; init; }
        public int Damage;
    }

    private struct Slot : IAttachment<int>
    {
        public int Key { get; init; }
        public int Value;
    }

    private struct Leader
    {
        public Entity Who;
        public int Term;
    }

    private static void LoadContext(Registry remote, ContinuousLoader<Entity, Archive> loader)
    {
        var archive = new Archive();
        remote.Snapshot<Archive>().Entities(ref archive).Context<Leader>(ref archive);
        var reader = archive.Reopen();
        loader.Entities(ref reader).Context<Leader>(ref reader, (l, map) => l with { Who = map(l.Who) });
    }

    [Fact]
    public void Context_RemapsEntityFields()
    {
        var remote = new Registry();
        remote.Create();
        Entity chief = remote.Create();
        remote.AddOrReplaceContext(new Leader { Who = chief, Term = 3 });
        var local = new Registry();
        local.Create(); // locals differ from remotes
        var loader = local.ContinuousLoader<Archive>();

        LoadContext(remote, loader);

        Assert.Equal(loader.Map(chief), local.GetContext<Leader>().Who);
        Assert.Equal(3, local.GetContext<Leader>().Term);
    }

    [Fact]
    public void Context_RemovedRemotely_RemovedLocally()
    {
        var remote = new Registry();
        remote.Create();
        Entity chief = remote.Create();
        remote.AddOrReplaceContext(new Leader { Who = chief, Term = 3 });
        var local = new Registry();
        local.Create(); // locals differ from remotes
        var loader = local.ContinuousLoader<Archive>();
        LoadContext(remote, loader);
        remote.RemoveContext<Leader>();

        LoadContext(remote, loader);

        Assert.False(local.HasContext<Leader>());
    }

    private sealed class Counts
    {
        public int Construct, Update, Destroy;

        public static Counts On<T>(Registry registry) where T : IComponent
        {
            var counts = new Counts();
            var pool = registry.Storage<T>();
            pool.OnAdd += _ => counts.Construct++;
            pool.OnUpdate += _ => counts.Update++;
            pool.OnRemove += _ => counts.Destroy++;
            return counts;
        }
    }

    private static (Registry Remote, Entity A, Entity B, Entity C) Remote()
    {
        var remote = new Registry();
        Entity a = remote.Create();
        Entity b = remote.Create();
        Entity c = remote.Create();
        foreach (var (e, x) in new[] { (a, 1), (b, 2), (c, 3) })
        {
            remote.Add(e, new Position { X = x });
        }

        remote.Add(a, new Velocity { X = 10 });
        remote.Add(b, new Velocity { X = 20 });
        return (remote, a, b, c);
    }

    private static void Load(Registry remote, ContinuousLoader<Entity, Archive> loader)
    {
        var archive = new Archive();
        remote.Snapshot<Archive>().Entities(ref archive).Component<Position>(ref archive).Component<Velocity>(ref archive);
        var reader = archive.Reopen();
        loader.Entities(ref reader).Component<Position>(ref reader).Component<Velocity>(ref reader).Orphans();
    }

    [Fact]
    public void UnchangedPass_FiresOnlyUpdates()
    {
        var (remote, _, _, _) = Remote();
        var local = new Registry();
        var loader = local.ContinuousLoader<Archive>();
        Load(remote, loader);

        var position = Counts.On<Position>(local);
        var velocity = Counts.On<Velocity>(local);
        Load(remote, loader);

        Assert.Equal((0, 3, 0), (position.Construct, position.Update, position.Destroy));
        Assert.Equal((0, 2, 0), (velocity.Construct, velocity.Update, velocity.Destroy));
    }

    [Fact]
    public void ChangedPass_AddsRemovesAndWritesComponents()
    {
        var (remote, a, b, c) = Remote();
        var local = new Registry();
        var loader = local.ContinuousLoader<Archive>();
        Load(remote, loader);
        var velocity = Counts.On<Velocity>(local);
        remote.Remove<Velocity>(b);
        remote.Add(c, new Velocity { X = 30 });
        remote.Get<Velocity>(a).X = 11;

        Load(remote, loader);

        Assert.Equal((1, 1, 1), (velocity.Construct, velocity.Update, velocity.Destroy));
        Assert.False(local.Has<Velocity>(loader.Map(b)));
        Assert.Equal(11, local.Get<Velocity>(loader.Map(a)).X);
        Assert.Equal(30, local.Get<Velocity>(loader.Map(c)).X);
    }

    [Fact]
    public void DroppedComponent_RemovedOnlyOnce()
    {
        var (remote, _, b, _) = Remote();
        var local = new Registry();
        var loader = local.ContinuousLoader<Archive>();
        Load(remote, loader);
        var velocity = Counts.On<Velocity>(local);
        remote.Remove<Velocity>(b);
        Load(remote, loader);

        Load(remote, loader);

        Assert.Equal(1, velocity.Destroy);
    }

    [Fact]
    public void UnchangedPass_KeepsOwningGroupOrder()
    {
        var (remote, _, _, _) = Remote();
        var local = new Registry();
        var group = local.Group().Own<Position>().Own<Velocity>().Build();
        var loader = local.ContinuousLoader<Archive>();
        Load(remote, loader);

        var before = group.Entities.ToArray();
        var positions = local.Storage<Position>().Entities.ToArray();
        Load(remote, loader);

        Assert.Equal(2, before.Length);
        Assert.Equal(before, group.Entities.ToArray());
        Assert.Equal(positions, local.Storage<Position>().Entities.ToArray());
    }

    [Fact]
    public void Orphans_DestroysMissingAndRecycledRemotes()
    {
        var (remote, a, b, c) = Remote();
        var local = new Registry();
        var loader = local.ContinuousLoader<Archive>();
        Load(remote, loader);
        var (localA, localB, localC) = (loader.Map(a), loader.Map(b), loader.Map(c));

        remote.Destroy(b);
        remote.Destroy(c);
        Entity d = remote.Create(); // may reuse an index under a new version
        remote.Add(d, new Position { X = 4 });
        Load(remote, loader);

        Assert.True(loader.Contains(a));
        Assert.Equal(localA, loader.Map(a));
        Assert.True(local.IsAlive(localA));
        Assert.False(loader.Contains(b));
        Assert.False(loader.Contains(c));
        Assert.True(loader.Map(b).IsNull);
        Assert.False(local.IsAlive(localB));
        Assert.False(local.IsAlive(localC));
        Assert.True(loader.Contains(d));
        Assert.Equal(4, local.Get<Position>(loader.Map(d)).X);
        Assert.Equal(2, local.Storage<Position>().Count);
    }

    private static (Registry Remote, Entity A, Entity B, Entity C) TaggedRemote()
    {
        var (remote, a, b, c) = Remote();
        remote.Tag<Marked>(a);
        remote.Tag<Marked>(b);
        remote.Relate<Parent>(a, c);
        remote.Relate<Parent>(b, c);
        return (remote, a, b, c);
    }

    private static void LoadTagsAndRelations(Registry remote, ContinuousLoader<Entity, Archive> loader)
    {
        var archive = new Archive();
        remote.Snapshot<Archive>().Entities(ref archive).Tag<Marked>(ref archive).Relation<Parent>(ref archive);
        var reader = archive.Reopen();
        loader.Entities(ref reader).Tag<Marked>(ref reader).Relation<Parent>(ref reader).Orphans();
    }

    private sealed class TagCounts
    {
        public int Tagged, Untagged, Related, Unrelated;

        public static TagCounts On(Registry registry)
        {
            var counts = new TagCounts();
            registry.Tags<Marked>().OnTag += _ => counts.Tagged++;
            registry.Tags<Marked>().OnUntag += _ => counts.Untagged++;
            registry.Relations<Parent>().OnRelate += (_, _) => counts.Related++;
            registry.Relations<Parent>().OnUnrelate += (_, _) => counts.Unrelated++;
            return counts;
        }

        public (int, int, int, int) All => (Tagged, Untagged, Related, Unrelated);
    }

    [Fact]
    public void TagsAndRelations_UnchangedPass_FiresNothing()
    {
        var (remote, _, _, _) = TaggedRemote();
        var local = new Registry();
        var loader = local.ContinuousLoader<Archive>();
        LoadTagsAndRelations(remote, loader);
        var counts = TagCounts.On(local);

        LoadTagsAndRelations(remote, loader);

        Assert.Equal((0, 0, 0, 0), counts.All);
    }

    [Fact]
    public void TagsAndRelations_ChangedPass_FiresOnlyTheDiff()
    {
        var (remote, a, b, _) = TaggedRemote();
        var local = new Registry();
        var loader = local.ContinuousLoader<Archive>();
        LoadTagsAndRelations(remote, loader);
        var counts = TagCounts.On(local);
        remote.Untag<Marked>(b);
        remote.Unrelate<Parent>(b);
        remote.Relate<Parent>(a, b);

        LoadTagsAndRelations(remote, loader);

        Assert.Equal((0, 1, 1, 2), counts.All);
        Assert.True(local.HasTag<Marked>(loader.Map(a)));
        Assert.False(local.HasTag<Marked>(loader.Map(b)));
        Assert.Equal(loader.Map(b), local.GetTarget<Parent>(loader.Map(a)));
        Assert.True(local.GetTarget<Parent>(loader.Map(b)).IsNull);
    }

    private static (Registry Remote, Entity A, Entity B, Entity C) LinkedRemote()
    {
        var (remote, a, b, c) = Remote();
        var remoteLinks = remote.Links<Attacks>();
        remoteLinks.Link(a, new Attacks { Target = b, Damage = 1 });
        remoteLinks.Link(a, new Attacks { Target = c, Damage = 2 });
        var remoteSlots = remote.Attachments<Slot, int>();
        remoteSlots.Attach(a, new Slot { Key = 0, Value = 1 });
        remoteSlots.Attach(a, new Slot { Key = 1, Value = 2 });
        return (remote, a, b, c);
    }

    private static void LoadLinksAndAttachments(Registry remote, ContinuousLoader<Entity, Archive> loader)
    {
        var archive = new Archive();
        remote.Snapshot<Archive>().Entities(ref archive).Link<Attacks>(ref archive).Attachment<Slot, int>(ref archive);
        var reader = archive.Reopen();
        loader.Entities(ref reader)
            .Link<Attacks>(ref reader, (l, map) => l with { Target = map(l.Target) })
            .Attachment<Slot, int>(ref reader)
            .Orphans();
    }

    private sealed class LinkCounts
    {
        public int Linked, LinkUpdated, Unlinked, Attached, SlotUpdated, Detached;

        public static LinkCounts On(Registry registry)
        {
            var counts = new LinkCounts();
            var links = registry.Links<Attacks>();
            var slots = registry.Attachments<Slot, int>();
            links.OnLink += (_, _) => counts.Linked++;
            links.OnUpdate += (_, _) => counts.LinkUpdated++;
            links.OnUnlink += (_, _) => counts.Unlinked++;
            slots.OnAttach += (_, _) => counts.Attached++;
            slots.OnUpdate += (_, _) => counts.SlotUpdated++;
            slots.OnDetach += (_, _) => counts.Detached++;
            return counts;
        }

        public (int, int, int, int, int, int) All => (Linked, LinkUpdated, Unlinked, Attached, SlotUpdated, Detached);
    }

    [Fact]
    public void LinksAndAttachments_UnchangedPass_FiresOnlyUpdates()
    {
        var (remote, _, _, _) = LinkedRemote();
        var local = new Registry();
        var loader = local.ContinuousLoader<Archive>();
        LoadLinksAndAttachments(remote, loader);
        var counts = LinkCounts.On(local);

        LoadLinksAndAttachments(remote, loader);

        Assert.Equal((0, 2, 0, 0, 2, 0), counts.All);
    }

    [Fact]
    public void LinksAndAttachments_ChangedPass_FiresOnlyTheDiff()
    {
        var (remote, a, b, c) = LinkedRemote();
        var local = new Registry();
        var loader = local.ContinuousLoader<Archive>();
        LoadLinksAndAttachments(remote, loader);
        var counts = LinkCounts.On(local);
        remote.Links<Attacks>().Unlink(a, b);
        remote.Attachments<Slot, int>().Detach(a, 0);

        LoadLinksAndAttachments(remote, loader);

        var links = local.Links<Attacks>();
        var slots = local.Attachments<Slot, int>();
        Assert.Equal((0, 1, 1, 0, 1, 1), counts.All);
        Assert.False(links.Has(loader.Map(a), loader.Map(b)));
        Assert.Equal(2, links.Get(loader.Map(a), loader.Map(c)).Damage);
        Assert.False(slots.Has(loader.Map(a), 0));
        Assert.Equal(2, slots.Get(loader.Map(a), 1).Value);
    }
}
