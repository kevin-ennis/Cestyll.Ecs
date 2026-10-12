using Cestyll.Ecs;
using Cestyll.Ecs.Relations;
using Cestyll.Ecs.Snapshots;

namespace Cestyll.Ecs.Tests;

public class LinkSetTests
{
    private struct Attacks : ILink<Entity>
    {
        public Entity Target { get; init; }
        public int Damage;
    }

    private static Attacks Hit(Entity target, int damage = 1) => new() { Target = target, Damage = damage };

    private static List<Entity> Targets(LinkSet<Entity, Attacks> set, Entity source)
    {
        var list = new List<Entity>();
        foreach (ref var link in set.GetTargets(source))
        {
            list.Add(link.Target);
        }

        return list;
    }

    private static List<Entity> Sources(LinkSet<Entity, Attacks> set, Entity target)
    {
        var list = new List<Entity>();
        foreach (var s in set.GetSources(target))
        {
            list.Add(s);
        }

        return list;
    }

    [Fact]
    public void Link_ManyTargets_BothDirections()
    {
        var registry = new Registry();
        Entity archer = registry.Create();
        Entity knight = registry.Create();
        Entity orcA = registry.Create();
        Entity orcB = registry.Create();
        var set = registry.Links<Attacks>();

        set.Link(archer, Hit(orcA));
        set.Link(archer, Hit(orcB));
        set.Link(knight, Hit(orcA));

        Assert.Equal(2, set.TargetCount(archer));
        Assert.Equal(2, set.SourceCount(orcA));
        Assert.Equal(new HashSet<Entity> { orcA, orcB }, Targets(set, archer).ToHashSet());
        Assert.Equal(new HashSet<Entity> { archer, knight }, Sources(set, orcA).ToHashSet());
        Assert.True(set.Has(knight, orcA));
        Assert.False(set.Has(knight, orcB));
        Assert.Equal(new HashSet<Entity> { archer, knight }, set.Sources.ToArray().ToHashSet());
        Assert.Equal(new HashSet<Entity> { orcA, orcB }, set.Targets.ToArray().ToHashSet());
    }

    [Fact]
    public void Link_SamePair_ReplacesData()
    {
        var registry = new Registry();
        Entity a = registry.Create();
        Entity b = registry.Create();
        var set = registry.Links<Attacks>();

        set.Link(a, Hit(b, 1));
        set.Link(a, Hit(b, 9));

        Assert.Equal(1, set.TargetCount(a));
        Assert.Equal(1, set.SourceCount(b));
        Assert.Equal(9, set.Get(a, b).Damage);
    }

    [Fact]
    public void DataEditsInPlace()
    {
        var registry = new Registry();
        Entity a = registry.Create();
        Entity b = registry.Create();
        Entity c = registry.Create();
        var set = registry.Links<Attacks>();

        set.Link(a, Hit(b, 1)).Damage += 1;
        set.Link(a, Hit(c, 5));
        set.Get(a, b).Damage *= 10;
        foreach (ref var link in set.GetTargets(a))
        {
            link.Damage++;
        }

        Assert.Equal(21, set.Get(a, b).Damage);
        Assert.True(set.TryGet(a, c, out var toC));
        Assert.Equal(6, toC.Damage);
    }

    [Fact]
    public void Get_MissingPair_Throws()
    {
        var registry = new Registry();
        Entity a = registry.Create();
        Entity b = registry.Create();
        var set = registry.Links<Attacks>();
        set.Link(a, Hit(b));

        Assert.Throws<KeyNotFoundException>(() => set.Get(b, a));
        Assert.False(set.TryGet(b, a, out _));
    }

    [Fact]
    public void Unlink_RemovesOnlyThatPair()
    {
        var registry = new Registry();
        Entity a = registry.Create();
        Entity b = registry.Create();
        Entity t1 = registry.Create();
        Entity t2 = registry.Create();
        var set = registry.Links<Attacks>();
        set.Link(a, Hit(t1));
        set.Link(a, Hit(t2));
        set.Link(b, Hit(t1));

        var first = set.Unlink(a, t1);
        var second = set.Unlink(a, t1);

        Assert.True(first);
        Assert.False(second);
        Assert.Equal([t2], Targets(set, a));
        Assert.Equal([b], Sources(set, t1));
        Assert.Equal([a], Sources(set, t2));
    }

    [Fact]
    public void SelfLink_ShowsOnBothEnds()
    {
        var registry = new Registry();
        Entity e = registry.Create();
        var set = registry.Links<Attacks>();

        set.Link(e, Hit(e));

        Assert.True(set.Has(e, e));
        Assert.Equal([e], Sources(set, e));
    }

    [Fact]
    public void SelfLink_Destroy_DropsTheLink()
    {
        var registry = new Registry();
        Entity e = registry.Create();
        var set = registry.Links<Attacks>();
        set.Link(e, Hit(e));

        registry.Destroy(e);

        Assert.Empty(set.Sources.ToArray());
        Assert.Empty(set.Targets.ToArray());
    }

    [Fact]
    public void Link_DeadEnds_Throw()
    {
        var registry = new Registry();
        Entity a = registry.Create();
        Entity dead = registry.Create();
        registry.Destroy(dead);
        var set = registry.Links<Attacks>();

        Assert.Throws<InvalidOperationException>(() => set.Link(a, Hit(dead)));
        Assert.Throws<InvalidOperationException>(() => set.Link(dead, Hit(a)));
    }

    [Fact]
    public void UnlinkTargets_LeavesIncomingLinks()
    {
        var registry = new Registry();
        Entity hub = registry.Create();
        var others = Enumerable.Range(0, 20).Select(_ => (Entity)registry.Create()).ToList();
        var set = registry.Links<Attacks>();
        foreach (var o in others)
        {
            set.Link(hub, Hit(o));
            set.Link(o, Hit(hub));
            set.Link(o, Hit(others[0]));
        }

        var removed = set.UnlinkTargets(hub);

        Assert.Equal(20, removed);
        Assert.Equal(0, set.TargetCount(hub));
        Assert.All(others, o => Assert.False(set.Has(hub, o)));
        Assert.Equal(20, set.SourceCount(hub)); // incoming untouched
    }

    [Fact]
    public void UnlinkSources_LeavesOutgoingLinks()
    {
        var registry = new Registry();
        Entity hub = registry.Create();
        var others = Enumerable.Range(0, 20).Select(_ => (Entity)registry.Create()).ToList();
        var set = registry.Links<Attacks>();
        foreach (var o in others)
        {
            set.Link(hub, Hit(o));
            set.Link(o, Hit(hub));
            set.Link(o, Hit(others[0]));
        }

        var removed = set.UnlinkSources(hub);

        Assert.Equal(20, removed);
        Assert.All(others, o => Assert.Equal([others[0]], Targets(set, o)));
        Assert.Equal(others.Append(hub).ToHashSet(), Sources(set, others[0]).ToHashSet());
        Assert.Equal(20, set.TargetCount(hub)); // outgoing untouched
    }

    [Fact]
    public void Destroy_DropsLinksBothWays()
    {
        var registry = new Registry();
        Entity a = registry.Create();
        Entity b = registry.Create();
        Entity c = registry.Create();
        var set = registry.Links<Attacks>();
        set.Link(a, Hit(b));
        set.Link(b, Hit(c));
        set.Link(c, Hit(b));
        set.Link(a, Hit(c));

        registry.Destroy(b);

        Assert.True(registry.IsAlive(a));
        Assert.True(registry.IsAlive(c));
        Assert.Equal([c], Targets(set, a));
        Assert.Empty(Targets(set, c));
        Assert.Equal([a], Sources(set, c));
        Assert.False(set.Has(a, b));
    }

    [Fact]
    public void GetTargets_UnlinkingCurrent_VisitsEachOnce()
    {
        var registry = new Registry();
        Entity hub = registry.Create();
        var others = Enumerable.Range(0, 100).Select(_ => (Entity)registry.Create()).ToList();
        var set = registry.Links<Attacks>();
        foreach (var o in others)
        {
            set.Link(hub, Hit(o));
            set.Link(o, Hit(hub));
        }

        var visits = new List<Entity>();
        foreach (ref var link in set.GetTargets(hub))
        {
            visits.Add(link.Target);
            set.Unlink(hub, link.Target); // shrinks and moves the block along the way
        }

        Assert.Equal(others.Count, visits.Count);
        Assert.Equal(others.ToHashSet(), visits.ToHashSet());
    }

    [Fact]
    public void GetSources_UnlinkingCurrent_VisitsEachOnce()
    {
        var registry = new Registry();
        Entity hub = registry.Create();
        var others = Enumerable.Range(0, 100).Select(_ => (Entity)registry.Create()).ToList();
        var set = registry.Links<Attacks>();
        foreach (var o in others)
        {
            set.Link(hub, Hit(o));
            set.Link(o, Hit(hub));
        }

        var visits = new List<Entity>();
        foreach (var s in set.GetSources(hub))
        {
            visits.Add(s);
            set.Unlink(s, hub);
        }

        Assert.Equal(others.Count, visits.Count);
        Assert.Equal(others.ToHashSet(), visits.ToHashSet());
    }

    [Fact]
    public void Compact_KeepsLinksAndData()
    {
        var (registry, set, sources, targets, model, _) = HalfUnlinked();
        var gapsBefore = set.SlabGaps;

        set.Compact();

        Assert.True(gapsBefore > 0);
        Assert.Equal(0, set.SlabGaps);
        Assert.Empty(Mismatches(registry, set, sources, targets, model));
    }

    [Fact]
    public void Unlink_AfterCompact_KeepsTwinSlotsPaired()
    {
        var (registry, set, sources, targets, model, random) = HalfUnlinked();
        set.Compact();
        // Appends after the compaction carve from its new end.
        foreach (var s in sources.Take(5))
        {
            foreach (var t in targets.Where(t => !model.Contains((s, t))).Take(3))
            {
                set.Link(s, Hit(t, s.Index * 1000 + t.Index));
                model.Add((s, t));
            }
        }

        var unlinked = new List<bool>();
        var counts = new List<(int Expected, int Actual)>();
        var problems = new List<string>();
        // Every removal swap-removes one entry and repairs its twin's slot.
        foreach (var (s, t) in model.OrderBy(_ => random.Next()).Take(model.Count / 2).ToList())
        {
            unlinked.Add(set.Unlink(s, t));
            model.Remove((s, t));
            problems.AddRange(Mismatches(registry, set, sources, targets, model));
        }

        foreach (var s in sources.Take(10))
        {
            counts.Add((model.Count(p => p.Source == s), set.UnlinkTargets(s)));
            model.RemoveWhere(p => p.Source == s);
            problems.AddRange(Mismatches(registry, set, sources, targets, model));
        }

        foreach (var t in targets)
        {
            registry.Destroy(t);
            model.RemoveWhere(p => p.Target == t);
            problems.AddRange(Mismatches(registry, set, sources, targets, model));
        }

        Assert.DoesNotContain(false, unlinked);
        Assert.All(counts, c => Assert.Equal(c.Expected, c.Actual));
        Assert.Empty(problems);
        Assert.Empty(model);
        Assert.Equal(0, set.SlabUsed);
    }

    // 40 x 40 links with uneven list lengths both ways, then half of them unlinked.
    private static (Registry, LinkSet<Entity, Attacks>, Entity[], Entity[], HashSet<(Entity Source, Entity Target)>, Random)
        HalfUnlinked()
    {
        var registry = new Registry();
        var set = registry.Links<Attacks>();
        var sources = registry.CreateMany(40);
        var targets = registry.CreateMany(40);
        var model = new HashSet<(Entity Source, Entity Target)>();
        foreach (var s in sources)
        {
            foreach (var t in targets)
            {
                if ((s.Index * 7 + t.Index * 3) % 5 != 0)
                {
                    set.Link(s, Hit(t, s.Index * 1000 + t.Index));
                    model.Add((s, t));
                }
            }
        }

        var random = new Random(7);
        foreach (var (s, t) in model.OrderBy(_ => random.Next()).Take(model.Count / 2).ToList())
        {
            set.Unlink(s, t);
            model.Remove((s, t));
        }

        return (registry, set, sources, targets, model, random);
    }

    // Every way the set disagrees with the model, from both ends.
    private static List<string> Mismatches(
        Registry registry, LinkSet<Entity, Attacks> set, Entity[] sources, Entity[] targets,
        HashSet<(Entity Source, Entity Target)> model)
    {
        var problems = new List<string>();
        foreach (var s in sources)
        {
            var expected = model.Where(p => p.Source == s).Select(p => p.Target).ToHashSet();
            if (!expected.SetEquals(Targets(set, s)) || expected.Count != set.TargetCount(s))
            {
                problems.Add($"targets of source {s.Index}");
            }

            foreach (var t in expected)
            {
                if (!set.TryGet(s, t, out var link) || link.Damage != s.Index * 1000 + t.Index)
                {
                    problems.Add($"data {s.Index} -> {t.Index}");
                }
            }
        }

        foreach (var t in targets.Where(registry.IsAlive))
        {
            var expected = model.Where(p => p.Target == t).Select(p => p.Source).ToHashSet();
            if (!expected.SetEquals(Sources(set, t)) || expected.Count != set.SourceCount(t))
            {
                problems.Add($"sources of target {t.Index}");
            }
        }

        return problems;
    }

    [Fact]
    public void Unlink_KeepsSlabCapacity()
    {
        var registry = new Registry();
        Entity hub = registry.Create();
        var others = Enumerable.Range(0, 5000).Select(_ => (Entity)registry.Create()).ToList();
        var set = registry.Links<Attacks>();
        foreach (var o in others)
        {
            set.Link(hub, Hit(o));
        }

        foreach (var o in others.Skip(5))
        {
            set.Unlink(hub, o);
        }

        Assert.True(set.SlabCapacity >= 10_000, "unlink gave memory back");
    }

    [Fact]
    public void ShrinkToFit_ReturnsCapacity_KeepsLinks()
    {
        var registry = new Registry();
        Entity hub = registry.Create();
        var others = Enumerable.Range(0, 5000).Select(_ => (Entity)registry.Create()).ToList();
        var set = registry.Links<Attacks>();
        foreach (var o in others)
        {
            set.Link(hub, Hit(o));
        }

        foreach (var o in others.Skip(5))
        {
            set.Unlink(hub, o);
        }

        set.ShrinkToFit();

        Assert.Equal(2 * 64, set.SlabCapacity);
        Assert.Equal(others.Take(5).ToHashSet(), Targets(set, hub).ToHashSet());
        Assert.All(others.Take(5), o => Assert.Equal([hub], Sources(set, o)));
    }

    [Fact]
    public void Snapshot_RoundTrip()
    {
        var source = new Registry();
        Entity a = source.Create();
        Entity b = source.Create();
        Entity c = source.Create();
        var set = source.Links<Attacks>();
        set.Link(a, Hit(b, 3));
        set.Link(a, Hit(c, 4));
        set.Link(c, Hit(a, 5));

        var archive = new Archive();
        source.Snapshot<Archive>().Entities(ref archive).Link<Attacks>(ref archive);

        var loaded = new Registry();
        var reader = archive.Reopen();
        loaded.SnapshotLoader<Archive>().Entities(ref reader).Link<Attacks>(ref reader);

        var restored = loaded.Links<Attacks>();
        Assert.Equal(3, restored.Get(a, b).Damage);
        Assert.Equal(4, restored.Get(a, c).Damage);
        Assert.Equal(5, restored.Get(c, a).Damage);
        Assert.Equal(new HashSet<Entity> { a, c }, Sources(restored, a).Concat(Sources(restored, b)).ToHashSet());
        Assert.Equal(2, restored.TargetCount(a));
    }

    [Fact]
    public void SnapshotLoader_DeadTarget_Throws()
    {
        var source = new Registry();
        Entity a = source.Create();
        Entity b = source.Create();
        source.Links<Attacks>().Link(a, Hit(b));

        var links = new Archive();
        source.Snapshot<Archive>().Link<Attacks>(ref links);
        source.Destroy(b);
        var archive = new Archive();
        source.Snapshot<Archive>().Entities(ref archive); // b is dead in this section
        archive.Append(links);

        var loaded = new Registry();
        var reader = archive.Reopen();
        var loader = loaded.SnapshotLoader<Archive>().Entities(ref reader);
        Assert.Throws<InvalidDataException>(() => loader.Link<Attacks>(ref reader));
    }

    // Random link / unlink / unlink-all / destroy against a set-of-pairs model.
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void RandomOperations_MatchModel(int seed)
    {
        var rng = new Random(seed);
        var registry = new Registry();
        var set = registry.Links<Attacks>();
        var model = new Dictionary<(Entity, Entity), int>();
        var alive = new List<Entity>();

        for (var step = 0; step < 30_000; step++)
        {
            // Repacks move both in-list lanes, so later unlinks check the twins survived.
            if (step % 5_000 == 4_999)
            {
                if (step % 10_000 == 4_999)
                {
                    set.Compact();
                }
                else
                {
                    set.ShrinkToFit();
                }
            }

            var op = rng.Next(100);
            if (alive.Count < 20 || op < 3)
            {
                alive.Add(registry.Create());
                continue;
            }

            var s = alive[rng.Next(alive.Count)];
            var t = alive[rng.Next(alive.Count)];
            if (op < 60)
            {
                var damage = rng.Next(100);
                set.Link(s, Hit(t, damage));
                model[(s, t)] = damage;
            }
            else if (op < 92)
            {
                Assert.Equal(model.Remove((s, t)), set.Unlink(s, t));
            }
            else if (op < 95)
            {
                var gone = model.Keys.Where(k => k.Item1 == s).ToList();
                Assert.Equal(gone.Count, set.UnlinkTargets(s));
                gone.ForEach(k => model.Remove(k));
            }
            else
            {
                registry.Destroy(s);
                alive.Remove(s);
                foreach (var k in model.Keys.Where(k => k.Item1 == s || k.Item2 == s).ToList())
                {
                    model.Remove(k);
                }
            }
        }

        foreach (var e in alive)
        {
            var outs = model.Where(kv => kv.Key.Item1 == e).ToDictionary(kv => kv.Key.Item2, kv => kv.Value);
            var actual = new Dictionary<Entity, int>();
            foreach (ref var link in set.GetTargets(e))
            {
                actual.Add(link.Target, link.Damage);
            }

            Assert.Equal(outs, actual);
            Assert.Equal(
                model.Keys.Where(k => k.Item2 == e).Select(k => k.Item1).ToHashSet(),
                Sources(set, e).ToHashSet());
        }

        Assert.Equal(model.Keys.Select(k => k.Item1).ToHashSet(), set.Sources.ToArray().ToHashSet());
        Assert.Equal(model.Keys.Select(k => k.Item2).ToHashSet(), set.Targets.ToArray().ToHashSet());
    }
}
