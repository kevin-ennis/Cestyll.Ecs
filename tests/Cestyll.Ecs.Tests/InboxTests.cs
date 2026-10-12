using Cestyll.Ecs;
using Cestyll.Ecs.Inboxes;
using Cestyll.Ecs.Snapshots;

namespace Cestyll.Ecs.Tests;

public class InboxTests
{
    private struct Health : IComponent { public int Value; }
    private struct Armour : IComponent;

    private struct Hit : IMessage
    {
        public int Damage;
        public Entity Source;
    }

    private struct Note : IMessage { public string Text; }

    private struct Confused : IMessage, IComponent;

    private static Hit Damage(int amount) => new() { Damage = amount };

    [Fact]
    public void GetAll_KeepsPostingOrder_PerEntity()
    {
        var registry = new Registry();
        Entity a = registry.Create();
        Entity b = registry.Create();
        var hits = registry.Inboxes<Hit>();

        // Interleaved past one block's capacity, so both lists move while growing.
        for (var i = 0; i < 40; i++)
        {
            hits.Post(a, Damage(i));
            hits.Post(b, Damage(100 + i));
        }

        Assert.Equal(40, hits.MessageCount(a));
        Assert.Equal(Enumerable.Range(0, 40), hits.GetAll(a).ToArray().Select(h => h.Damage));
        Assert.Equal(Enumerable.Range(100, 40), hits.GetAll(b).ToArray().Select(h => h.Damage));
        Assert.Equal([a, b], hits.Entities.ToArray());
    }

    [Fact]
    public void GetAll_AnEntityWithoutMail_IsEmpty()
    {
        var registry = new Registry();
        Entity e = registry.Create();
        var hits = registry.Inboxes<Hit>();

        Assert.True(hits.GetAll(e).IsEmpty);
        Assert.False(hits.Has(e));
        Assert.Equal(0, hits.MessageCount(e));
    }

    [Fact]
    public void GetMessages_WithoutMail_DoesNotMakeTheInbox()
    {
        var registry = new Registry();
        Entity quiet = registry.Create();

        var empty = registry.GetMessages<Hit>(quiet).IsEmpty;
        var has = registry.HasMessages<Hit>(quiet);

        Assert.True(empty);
        Assert.False(has);
        Assert.Null(registry.FindInboxes<Hit>());
    }

    [Fact]
    public void GetMessages_ReadsFromRegistryAndHandle()
    {
        var registry = new Registry();
        Entity quiet = registry.Create();

        var e = registry.Create().Post(Damage(3));

        Assert.True(e.HasMessages<Hit>());
        Assert.Equal(3, e.GetMessages<Hit>()[0].Damage);
        Assert.Equal(3, registry.GetMessages<Hit>(e.Entity)[0].Damage);
        Assert.False(registry.HasMessages<Hit>(quiet));
    }

    [Fact]
    public void GetAll_WritesInPlace()
    {
        var registry = new Registry();
        Entity e = registry.Create();
        var hits = registry.Inboxes<Hit>();
        hits.Post(e, Damage(5));

        hits.GetAll(e)[0].Damage *= 2;

        Assert.Equal(10, hits.GetAll(e)[0].Damage);
    }

    [Fact]
    public void ClearMessages_DropsOneEntitysMail()
    {
        var registry = new Registry();
        Entity a = registry.Create();
        Entity b = registry.Create();
        var hits = registry.Inboxes<Hit>();
        hits.Post(a, Damage(1));
        hits.Post(a, Damage(2));
        hits.Post(b, Damage(3));

        var first = hits.ClearMessages(a);
        var second = hits.ClearMessages(a);

        Assert.Equal(2, first);
        Assert.Equal(0, second);
        Assert.False(hits.Has(a));
        Assert.Equal([b], hits.Entities.ToArray());
    }

    [Fact]
    public void ClearAll_EmptiesEveryInbox_AndTheNextFramePostsAgain()
    {
        var registry = new Registry();
        Entity a = registry.Create();
        Entity b = registry.Create();
        var hits = registry.Inboxes<Hit>();
        var postedThisFrame = new List<bool>();
        var entitiesEmpty = new List<bool>();
        var aHasMail = new List<bool>();
        var bEmpty = new List<bool>();

        for (var frame = 0; frame < 3; frame++)
        {
            for (var i = 0; i < 20; i++)
            {
                hits.Post(a, Damage(frame));
                hits.Post(b, Damage(frame));
            }

            postedThisFrame.Add(hits.GetAll(a).ToArray().All(h => h.Damage == frame));
            hits.Clear();
            entitiesEmpty.Add(hits.Entities.IsEmpty);
            aHasMail.Add(hits.Has(a));
            bEmpty.Add(hits.GetAll(b).IsEmpty);
        }

        Assert.Equal([true, true, true], postedThisFrame);
        Assert.Equal([true, true, true], entitiesEmpty);
        Assert.Equal([false, false, false], aHasMail);
        Assert.Equal([true, true, true], bEmpty);
    }

    [Fact]
    public void ClearAll_WithReferences_LetsGoOfThem()
    {
        var registry = new Registry();
        Entity e = registry.Create();
        var notes = registry.Inboxes<Note>();
        notes.Post(e, new Note { Text = "first" });
        notes.Post(e, new Note { Text = "second" });

        notes.Clear();
        notes.Post(e, new Note { Text = "third" });

        Assert.Equal(["third"], notes.GetAll(e).ToArray().Select(n => n.Text));
    }

    [Fact]
    public void Post_ToADeadEntity_Throws()
    {
        var registry = new Registry();
        Entity e = registry.Create();
        registry.Destroy(e);

        Assert.Throws<InvalidOperationException>(() => registry.Inboxes<Hit>().Post(e, Damage(1)));
        Assert.Throws<InvalidOperationException>(() => registry.Post(e, Damage(1)));
        Assert.Throws<InvalidOperationException>(() => registry.CommandBuffer().Post(e, Damage(1)));
    }

    [Fact]
    public void Destroy_DropsTheEntitysMail()
    {
        var registry = new Registry();
        Entity a = registry.Create();
        Entity b = registry.Create();
        var hits = registry.Inboxes<Hit>();
        hits.Post(a, Damage(1));
        hits.Post(b, Damage(2));

        registry.Destroy(a);

        Assert.Equal([b], hits.Entities.ToArray());
        Assert.False(hits.Has(a));
    }

    [Fact]
    public void Post_FromOnRemove_OfTheDyingEntity_Throws()
    {
        var registry = new Registry();
        Entity e = registry.Create(new Health());
        registry.Storage<Health>().OnRemove += dying => registry.Inboxes<Hit>().Post(dying, Damage(1));

        Assert.Throws<InvalidOperationException>(() => registry.Destroy(e));
    }

    [Fact]
    public void View_WithInbox_VisitsOnlyEntitiesWithMail()
    {
        var registry = new Registry();
        Entity a = registry.Create(new Health { Value = 10 });
        Entity b = registry.Create(new Health { Value = 10 });
        Entity c = registry.Create(new Health { Value = 10 }, new Armour());
        var hits = registry.Inboxes<Hit>();
        hits.Post(a, Damage(3));
        hits.Post(a, Damage(4));
        hits.Post(c, Damage(5));

        registry.View().WithInbox<Hit>().Without<Armour>().Each((Entity e, ref Health health) =>
        {
            foreach (var hit in hits.GetAll(e))
            {
                health.Value -= hit.Damage;
            }
        });

        Assert.Equal(3, registry.Get<Health>(a).Value);
        Assert.Equal(10, registry.Get<Health>(b).Value);
        Assert.Equal(10, registry.Get<Health>(c).Value);
    }

    [Fact]
    public void View_WithInbox_BeforeTheInboxExists_MatchesNothing()
    {
        var registry = new Registry();
        registry.Create(new Health());
        var visited = 0;

        registry.View().WithInbox<Hit>().Each((Entity _, ref Health _) => visited++);

        Assert.Equal(0, visited);
    }

    [Fact]
    public void OnPost_FiresWithTheMessage_UntilDisconnected()
    {
        var registry = new Registry();
        Entity e = registry.Create();
        var seen = new List<(Entity, int)>();
        var connection = registry.OnPost((Entity to, in Hit hit) => seen.Add((to, hit.Damage)));

        registry.Post(e, Damage(7));
        connection.Dispose();
        registry.Post(e, Damage(8));

        Assert.Equal([(e, 7)], seen);
        Assert.Equal(2, registry.Inboxes<Hit>().MessageCount(e));
    }

    [Fact]
    public void OnPost_HandlerMayPostAgain()
    {
        var registry = new Registry();
        Entity e = registry.Create();
        var hits = registry.Inboxes<Hit>();
        hits.OnPost += (Entity to, in Hit hit) =>
        {
            if (hit.Damage > 1)
            {
                hits.Post(to, Damage(hit.Damage / 2)); // splash halves
            }
        };

        hits.Post(e, Damage(8));

        Assert.Equal([8, 4, 2, 1], hits.GetAll(e).ToArray().Select(h => h.Damage));
    }

    [Fact]
    public void Observer_CollectsEntitiesThatGotMail()
    {
        var registry = new Registry();
        Entity a = registry.Create();
        Entity b = registry.Create();
        using var observer = registry.Observer().OnPost<Hit>();

        registry.Post(a, Damage(1));
        registry.Post(a, Damage(2));
        registry.Post(b, Damage(3));
        registry.Destroy(b);

        Assert.Equal([a], observer.Entities.ToArray());
    }

    [Fact]
    public void Post_InADeferScope_WaitsForTheScopeToEnd()
    {
        var registry = new Registry();
        Entity e = registry.Create();
        var hits = registry.Inboxes<Hit>();
        bool hadMailInScope;

        using (registry.Defer())
        {
            registry.Post(e, Damage(1));
            registry.Handle(e).Post(Damage(2));
            hadMailInScope = hits.Has(e);
        }

        Assert.False(hadMailInScope);
        Assert.Equal([1, 2], hits.GetAll(e).ToArray().Select(h => h.Damage));
    }

    [Fact]
    public void Post_DuringAnAutoDeferWalk_LandsAfterTheWalk()
    {
        var registry = new Registry { AutoDefer = true };
        Entity a = registry.Create(new Health());
        Entity b = registry.Create(new Health());
        var hits = registry.Inboxes<Hit>();
        var seenDuringWalk = 0;

        registry.View().Each((Entity e, ref Health _) =>
        {
            registry.Post(e == a ? b : a, Damage(1));
            seenDuringWalk += hits.Entities.Length;
        });

        Assert.Equal(0, seenDuringWalk);
        Assert.Equal(1, hits.MessageCount(a));
        Assert.Equal(1, hits.MessageCount(b));
    }

    [Fact]
    public void CommandBuffer_PostsAtFlush_InRecordOrder_AndSkipsTheDead()
    {
        var registry = new Registry();
        Entity a = registry.Create();
        Entity b = registry.Create();
        var hits = registry.Inboxes<Hit>();
        var commands = registry.CommandBuffer();

        commands.Post(a, Damage(1));
        commands.Post(b, Damage(2));
        commands.Post(a, Damage(3));
        var spawned = commands.Create().Post(Damage(4)).Entity;
        registry.Destroy(b);
        var emptyBeforeFlush = hits.Entities.IsEmpty;

        commands.Flush();

        Assert.Equal([1, 3], hits.GetAll(a).ToArray().Select(h => h.Damage));
        Assert.Equal([4], hits.GetAll(spawned).ToArray().Select(h => h.Damage));
        Assert.False(hits.Has(b));
        Assert.True(emptyBeforeFlush);
    }

    [Fact]
    public void Snapshot_RoundTripsMail_InOrder()
    {
        var source = new Registry();
        Entity a = source.Create();
        Entity b = source.Create();
        source.Post(a, new Hit { Damage = 1, Source = b });
        source.Post(a, new Hit { Damage = 2, Source = b });
        source.Post(b, Damage(3));
        var schema = new SnapshotSchema<Entity>().Inbox<Hit>();

        var archive = new Archive();
        schema.Save(source, ref archive);
        var loaded = new Registry();
        var reader = archive.Reopen();
        schema.Load(loaded, ref reader);

        Assert.True(reader.AllRead);
        var hits = loaded.Inboxes<Hit>();
        Assert.Equal([1, 2], hits.GetAll(a).ToArray().Select(h => h.Damage));
        Assert.Equal(b, hits.GetAll(a)[0].Source);
        Assert.Equal([3], hits.GetAll(b).ToArray().Select(h => h.Damage));
    }

    [Fact]
    public void Snapshot_WithNoInbox_LoadsEmpty()
    {
        var source = new Registry();
        source.Create();
        var archive = new Archive();
        source.Snapshot<Archive>().Entities(ref archive).Inbox<Hit>(ref archive);

        var loaded = new Registry();
        var reader = archive.Reopen();
        loaded.SnapshotLoader<Archive>().Entities(ref reader).Inbox<Hit>(ref reader);

        Assert.True(loaded.Inboxes<Hit>().Entities.IsEmpty);
    }

    private static void LoadHits(Registry remote, ContinuousLoader<Entity, Archive> loader)
    {
        var archive = new Archive();
        remote.Snapshot<Archive>().Entities(ref archive).Inbox<Hit>(ref archive);
        var reader = archive.Reopen();
        loader.Entities(ref reader)
            .Inbox<Hit>(ref reader, (hit, map) => hit with { Source = map(hit.Source) })
            .Orphans();
    }

    [Fact]
    public void ContinuousLoader_RemapsMessageEntities()
    {
        var remote = new Registry();
        Entity target = remote.Create();
        Entity attacker = remote.Create();
        remote.Post(target, new Hit { Damage = 5, Source = attacker });
        var local = new Registry();
        var loader = local.ContinuousLoader<Archive>();

        LoadHits(remote, loader);

        Assert.Equal(loader.Map(attacker), local.Inboxes<Hit>().GetAll(loader.Map(target))[0].Source);
    }

    // The remote frame moved on; the local inbox follows rather than adding up.
    [Fact]
    public void ContinuousLoader_SecondPass_ReplacesMail()
    {
        var remote = new Registry();
        Entity target = remote.Create();
        Entity attacker = remote.Create();
        remote.Post(target, new Hit { Damage = 5, Source = attacker });
        var local = new Registry();
        var loader = local.ContinuousLoader<Archive>();
        LoadHits(remote, loader);
        remote.Inboxes<Hit>().Clear();
        remote.Post(target, Damage(9));

        LoadHits(remote, loader);

        Assert.Equal([9], local.Inboxes<Hit>().GetAll(loader.Map(target)).ToArray().Select(h => h.Damage));
    }

    [Fact]
    public void TwoMarkers_Inboxes_Throws()
    {
        var registry = new Registry();

        Assert.Throws<ArgumentException>(() => registry.Inboxes<Confused>());
    }

    [Fact]
    public void CompactAndShrinkToFit_KeepTheMail()
    {
        var registry = new Registry();
        Entity a = registry.Create();
        Entity b = registry.Create();
        var hits = registry.Inboxes<Hit>();
        for (var i = 0; i < 50; i++)
        {
            hits.Post(a, Damage(i));
            hits.Post(b, Damage(i));
        }

        hits.ClearMessages(a);
        registry.Compact();
        registry.ShrinkToFit();

        Assert.Equal(Enumerable.Range(0, 50), hits.GetAll(b).ToArray().Select(h => h.Damage));
    }
}
