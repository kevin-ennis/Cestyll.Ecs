using Cestyll.Ecs;
using Cestyll.Ecs.Groups;
using Cestyll.Ecs.Snapshots;
using Cestyll.Ecs.Storage;
using Cestyll.Ecs.Tags;

namespace Cestyll.Ecs.Tests;

// Nested groups (several groups over shared owned pools) and partial ownership (With rules).
public class NestedGroupTests
{
    private struct A : IComponent { public int V; }
    private struct B : IComponent { public int V; }
    private struct C : IComponent { public int V; }
    private struct D : IComponent { public int V; }
    private struct Marked : ITag { }
    private struct Hidden : ITag { }

    // Component values encode their entity, so a misaligned span shows as a wrong value.
    private static int Id(Entity e) => e.Index;

    private static void Give(Registry r, Entity e, int mask)
    {
        if ((mask & 1) != 0) { r.Add(e, new A { V = Id(e) * 10 + 1 }); }
        if ((mask & 2) != 0) { r.Add(e, new B { V = Id(e) * 10 + 2 }); }
        if ((mask & 4) != 0) { r.Add(e, new C { V = Id(e) * 10 + 3 }); }
        if ((mask & 8) != 0) { r.Add(e, new D { V = Id(e) * 10 + 4 }); }
    }

    // Brute-force membership, and each owned pool's first Count slots match Entities.
    private static void AssertGroup(Registry r, Group<Entity> g, Func<Entity, bool> qualifies,
        params (SparseSet<Entity> Pool, Func<int, int> Value, int Tag)[] owned)
    {
        AssertMembers(r, g.Entities, qualifies);
        foreach (var (pool, value, tag) in owned)
        {
            for (var i = 0; i < g.Count; i++)
            {
                Assert.Equal(g.Entities[i], pool.Entities[i]);
                Assert.Equal(Id(g.Entities[i]) * 10 + tag, value(i));
            }
        }
    }

    // Every group here requires at least one of A..D, so their pools cover every candidate.
    private static void AssertMembers(Registry r, ReadOnlySpan<Entity> members, Func<Entity, bool> qualifies)
    {
        var expected = new HashSet<Entity>();
        foreach (var pool in new SparseSet<Entity>[] { r.Storage<A>(), r.Storage<B>(), r.Storage<C>(), r.Storage<D>() })
        {
            foreach (var e in pool.Entities)
            {
                if (qualifies(e))
                {
                    expected.Add(e);
                }
            }
        }

        Assert.Equal(expected.Count, members.Length);
        Assert.True(expected.SetEquals(members.ToArray()));
    }

    private static void AssertPrefix(Group<Entity> inner, Group<Entity> outer)
    {
        Assert.True(inner.Count <= outer.Count);
        Assert.True(inner.Entities.SequenceEqual(outer.Entities[..inner.Count]));
    }

    private static bool Has<T>(Registry r, Entity e) where T : IComponent => r.Has<T>(e);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Nested_StayAlignedUnderChurn(bool innerFirst)
    {
        var r = new Registry();
        var ents = new List<Entity>();
        var rng = new Random(innerFirst ? 7 : 11);
        for (var i = 0; i < 300; i++)
        {
            var e = r.Create();
            ents.Add(e);
            Give(r, e, rng.Next(16));
            if (rng.Next(3) == 0) { r.Tag<Marked>(e); }
        }

        // Outer <A, B>, middle <A, B, C> With<Marked>, inner <A, B, C, D> With<Marked>.
        Group<Entity> inner;
        Group<Entity> middle;
        Group<Entity> outer;
        if (innerFirst)
        {
            inner = r.Group().Own<A>().Own<B>().Own<C>().Own<D>().WithTag<Marked>().Build();
            outer = r.Group().Own<A>().Own<B>().Build();
            middle = r.Group().Own<A>().Own<B>().Own<C>().WithTag<Marked>().Build(); // lands between the two
        }
        else
        {
            outer = r.Group().Own<A>().Own<B>().Build();
            middle = r.Group().Own<A>().Own<B>().Own<C>().WithTag<Marked>().Build();
            inner = r.Group().Own<A>().Own<B>().Own<C>().Own<D>().WithTag<Marked>().Build();
        }

        void Check() => CheckChain(r, outer, middle, inner);

        Check();
        for (var step = 0; step < 3000; step++)
        {
            RandomStep(r, ents, rng);
            Check();
        }
    }

    // Outer <A, B>, middle <A, B, C> With<Marked>, inner <A, B, C, D> With<Marked>.
    private static (Group<Entity>, Group<Entity>, Group<Entity>) MakeChain(Registry r) =>
        (r.Group().Own<A>().Own<B>().Build(), r.Group().Own<A>().Own<B>().Own<C>().WithTag<Marked>().Build(),
            r.Group().Own<A>().Own<B>().Own<C>().Own<D>().WithTag<Marked>().Build());

    private static List<Entity> Populate(Registry r, int n, Random rng)
    {
        var ents = new List<Entity>();
        for (var i = 0; i < n; i++)
        {
            var e = r.Create();
            ents.Add(e);
            Give(r, e, rng.Next(16));
            if (rng.Next(3) == 0) { r.Tag<Marked>(e); }
        }

        return ents;
    }

    private static void CheckChain(Registry r, Group<Entity> outer, Group<Entity> middle, Group<Entity> inner)
    {
        AssertGroup(r, outer, e => Has<A>(r, e) && Has<B>(r, e),
            (r.Storage<A>(), i => outer.Span<A>()[i].V, 1),
            (r.Storage<B>(), i => outer.Span<B>()[i].V, 2));
        AssertGroup(r, middle, e => Has<A>(r, e) && Has<B>(r, e) && Has<C>(r, e) && r.HasTag<Marked>(e),
            (r.Storage<A>(), i => middle.Span<A>()[i].V, 1),
            (r.Storage<B>(), i => middle.Span<B>()[i].V, 2),
            (r.Storage<C>(), i => middle.Span<C>()[i].V, 3));
        AssertGroup(r, inner,
            e => Has<A>(r, e) && Has<B>(r, e) && Has<C>(r, e) && Has<D>(r, e) && r.HasTag<Marked>(e),
            (r.Storage<A>(), i => inner.Span<A>()[i].V, 1),
            (r.Storage<B>(), i => inner.Span<B>()[i].V, 2),
            (r.Storage<C>(), i => inner.Span<C>()[i].V, 3),
            (r.Storage<D>(), i => inner.Span<D>()[i].V, 4));
        AssertPrefix(middle, outer);
        AssertPrefix(inner, middle);
    }

    private static void RandomStep(Registry r, List<Entity> ents, Random rng)
    {
        var e = ents[rng.Next(ents.Count)];
        var bit = rng.Next(6);
        switch (bit)
        {
            case < 4:
                var mask = 1 << bit;
                var has = bit switch
                {
                    0 => r.Has<A>(e), 1 => r.Has<B>(e), 2 => r.Has<C>(e), _ => r.Has<D>(e),
                };
                if (has)
                {
                    _ = bit switch
                    {
                        0 => r.Remove<A>(e), 1 => r.Remove<B>(e), 2 => r.Remove<C>(e), _ => r.Remove<D>(e),
                    };
                }
                else
                {
                    Give(r, e, mask);
                }
                break;
            case 4:
                if (!r.Untag<Marked>(e))
                {
                    r.Tag<Marked>(e);
                }
                break;
            default:
                r.Destroy(e);
                ents.Remove(e);
                var fresh = r.Create();
                ents.Add(fresh);
                Give(r, fresh, rng.Next(16));
                break;
        }
    }

    // A + B on 100 entities, every third one Hidden.
    private static Entity[] HiddenEveryThird(Registry r)
    {
        var ents = new Entity[100];
        for (var i = 0; i < ents.Length; i++)
        {
            ents[i] = r.Create();
            Give(r, ents[i], 3);
            if (i % 3 == 0) { r.Tag<Hidden>(ents[i]); }
        }

        return ents;
    }

    [Fact]
    public void Nested_OnTagRulesAlone_IsAPrefix()
    {
        var r = new Registry();
        HiddenEveryThird(r);

        var visible = r.Group().Own<A>().Own<B>().WithoutTag<Hidden>().Build();
        var all = r.Group().Own<A>().Own<B>().Build();

        Assert.Equal(100, all.Count);
        Assert.Equal(66, visible.Count);
        AssertPrefix(visible, all);
    }

    [Fact]
    public void Nested_OnTagRulesAlone_FollowsTagChanges()
    {
        var r = new Registry();
        var ents = HiddenEveryThird(r);
        var visible = r.Group().Own<A>().Own<B>().WithoutTag<Hidden>().Build();
        var all = r.Group().Own<A>().Own<B>().Build();

        r.Untag<Hidden>(ents[0]);
        r.Tag<Hidden>(ents[1]);

        Assert.Equal(66, visible.Count);
        Assert.Contains(ents[0], visible.Entities.ToArray());
        Assert.DoesNotContain(ents[1], visible.Entities.ToArray());
        AssertPrefix(visible, all);
    }

    // 60 entities cycling through none, A, B, A + B.
    private static Entity[] PartialMix(Registry r)
    {
        var ents = new Entity[60];
        for (var i = 0; i < ents.Length; i++)
        {
            ents[i] = r.Create();
            Give(r, ents[i], i % 4); // 0: none, 1: A, 2: B, 3: A + B
        }

        return ents;
    }

    [Fact]
    public void Partial_OwnsOnlyItsOwnPools()
    {
        var r = new Registry();
        PartialMix(r);

        var g = r.Group().Own<A>().With<B>().Build();

        Assert.Same(g, r.Group().Own<A>().With<B>().Build());
        Assert.Equal(15, g.Count);
        var b = r.Storage<B>();
        foreach (var (e, i) in g.Entities.ToArray().Select((e, i) => (e, i)))
        {
            Assert.Equal(Id(e) * 10 + 1, g.Span<A>()[i].V);
            Assert.Equal(Id(e) * 10 + 2, b.Get(e).V);
        }
    }

    // B stays unowned: it sorts freely and another group can own it.
    [Fact]
    public void Partial_LeavesWithPoolFree()
    {
        var r = new Registry();
        var ents = PartialMix(r);
        r.Group().Own<A>().With<B>().Build();
        r.Sort<B>((x, y) => x.V.CompareTo(y.V));
        r.Add(ents[3], new C { V = Id(ents[3]) * 10 + 3 });

        var other = r.Group().Own<B>().Own<C>().Build();

        Assert.Equal(1, other.Count);
    }

    [Fact]
    public void Partial_TracksMembershipThroughWithPool()
    {
        var r = new Registry();
        var ents = PartialMix(r);
        var g = r.Group().Own<A>().With<B>().Build();
        var other = r.Group().Own<B>().Own<C>().Build();
        r.Add(ents[3], new C { V = Id(ents[3]) * 10 + 3 });

        r.Add(ents[1], new B { V = Id(ents[1]) * 10 + 2 });
        r.Remove<B>(ents[3]);

        Assert.Equal(15, g.Count);
        Assert.Contains(ents[1], g.Entities.ToArray());
        Assert.DoesNotContain(ents[3], g.Entities.ToArray());
        Assert.Equal(0, other.Count);
    }

    private static void EightWayMix(Registry r)
    {
        for (var i = 0; i < 80; i++)
        {
            Give(r, r.Create(), i % 8);
        }
    }

    [Fact]
    public void Partial_NestsInsideFullOwner()
    {
        var r = new Registry();
        EightWayMix(r);

        var outer = r.Group().Own<A>().With<B>().Build();
        var inner = r.Group().Own<A>().Own<B>().With<C>().Build();

        Assert.Equal(20, outer.Count);
        Assert.Equal(10, inner.Count);
        AssertPrefix(inner, outer);
        for (var i = 0; i < inner.Count; i++)
        {
            Assert.Equal(inner.Entities[i], r.Storage<B>().Entities[i]);
            Assert.Equal(Id(inner.Entities[i]) * 10 + 2, inner.Span<B>()[i].V);
        }
    }

    [Fact]
    public void Partial_InnerLeavesWhenWithPoolDrops()
    {
        var r = new Registry();
        EightWayMix(r);
        var outer = r.Group().Own<A>().With<B>().Build();
        var inner = r.Group().Own<A>().Own<B>().With<C>().Build();

        r.Remove<C>(inner.Entities[0]);

        Assert.Equal(20, outer.Count);
        Assert.Equal(9, inner.Count);
        AssertPrefix(inner, outer);
    }

    [Fact]
    public void Rejects_GroupsThatDontNest()
    {
        var r = new Registry();
        r.Group().Own<A>().Own<B>().Build();
        r.Group().Own<C>().Own<D>().Build();

        Assert.Throws<InvalidOperationException>(() => r.Group().Own<A>().Own<C>().Build());           // siblings
        Assert.Throws<InvalidOperationException>(() => r.Group().Own<A>().With<B>().With<C>().Build()); // stricter, owns less
        Assert.Throws<InvalidOperationException>(() => r.Group().Own<A>().Own<B>().With<A>().Build()); // own + get
        Assert.Throws<InvalidOperationException>(() => r.Group().Own<A>().Own<B>().Own<C>().Own<D>().Build()); // bridges two chains
    }

    [Fact]
    public void Rejects_LeavesTheExistingGroupIntact()
    {
        var r = new Registry();
        var ab = r.Group().Own<A>().Own<B>().Build();
        r.Group().Own<C>().Own<D>().Build();
        _ = Record.Exception(() => r.Group().Own<A>().Own<C>().Build());
        _ = Record.Exception(() => r.Group().Own<A>().With<B>().With<C>().Build());
        _ = Record.Exception(() => r.Group().Own<A>().Own<B>().With<A>().Build());
        _ = Record.Exception(() => r.Group().Own<A>().Own<B>().Own<C>().Own<D>().Build());

        Give(r, r.Create(), 3);

        Assert.Equal(1, ab.Count);
    }

    // 60 entities with shuffled A values; every third also has C.
    private static (Group<Entity> Outer, Group<Entity> Inner) SortChain(Registry r)
    {
        var rng = new Random(3);
        for (var i = 0; i < 60; i++)
        {
            var e = r.Create();
            r.Add(e, new A { V = rng.Next(1000) * 100 + Id(e) });
            r.Add(e, new B { V = Id(e) * 10 + 2 });
            if (i % 3 == 0) { r.Add(e, new C { V = Id(e) * 10 + 3 }); }
        }

        return (r.Group().Own<A>().Own<B>().Build(), r.Group().Own<A>().Own<B>().Own<C>().Build());
    }

    [Fact]
    public void Sort_OuterSortsOnlyWhatsOutsideTheInnerGroup()
    {
        var r = new Registry();
        var (outer, inner) = SortChain(r);
        var innerBefore = inner.Entities.ToArray();

        outer.Sort<A>((x, y) => x.V.CompareTo(y.V));

        Assert.Equal(innerBefore, inner.Entities.ToArray()); // inner members stay in front, as they were
        AssertPrefix(inner, outer);
        for (var i = inner.Count + 1; i < outer.Count; i++)
        {
            Assert.True(outer.Span<A>()[i - 1].V <= outer.Span<A>()[i].V);
        }
    }

    [Fact]
    public void SortEntities_OuterSortsOnlyWhatsOutsideTheInnerGroup()
    {
        var r = new Registry();
        var (outer, inner) = SortChain(r);
        var innerBefore = inner.Entities.ToArray();

        outer.SortEntities((x, y) => y.Index.CompareTo(x.Index));

        Assert.Equal(innerBefore, inner.Entities.ToArray());
        for (var i = inner.Count + 1; i < outer.Count; i++)
        {
            Assert.True(outer.Entities[i - 1].Index > outer.Entities[i].Index);
        }
    }

    [Fact]
    public void Sort_InnerKeepsOuterAligned()
    {
        var r = new Registry();
        var (outer, inner) = SortChain(r);

        inner.Sort<A>((x, y) => y.V.CompareTo(x.V));

        for (var i = 1; i < inner.Count; i++)
        {
            Assert.True(inner.Span<A>()[i - 1].V >= inner.Span<A>()[i].V);
        }

        AssertPrefix(inner, outer);
        for (var i = 0; i < outer.Count; i++)
        {
            Assert.Equal(Id(outer.Entities[i]) * 10 + 2, outer.Span<B>()[i].V);
        }
    }

    // 40 entities with A + B; odd ones also have C.
    private static Entity[] HalfWithC(Registry r)
    {
        var ents = new Entity[40];
        for (var i = 0; i < ents.Length; i++)
        {
            ents[i] = r.Create();
            Give(r, ents[i], i % 2 == 0 ? 3 : 7);
        }

        return ents;
    }

    [Fact]
    public void Without_ExcludesComponents()
    {
        var r = new Registry();
        HalfWithC(r);

        var free = r.Group().Own<A>().Own<B>().Without<C>().Build();

        Assert.Equal(20, free.Count);
    }

    [Fact]
    public void Without_TracksAddRemoveAndDestroy()
    {
        var r = new Registry();
        var ents = HalfWithC(r);
        var free = r.Group().Own<A>().Own<B>().Without<C>().Build();

        r.Remove<C>(ents[1]);
        r.Add(ents[0], new C());
        r.Destroy(ents[3]); // loses C before or after A and B; either way stays out

        Assert.Contains(ents[1], free.Entities.ToArray());
        Assert.DoesNotContain(ents[0], free.Entities.ToArray());
        Assert.DoesNotContain(ents[3], free.Entities.ToArray());
        Assert.Equal(20, free.Count);
    }

    [Fact]
    public void Without_ClearingTheExcludedPool_AdmitsAll()
    {
        var r = new Registry();
        HalfWithC(r);
        var free = r.Group().Own<A>().Own<B>().Without<C>().Build();

        r.Storage<C>().Clear();

        Assert.Equal(40, free.Count);
    }

    // Excluding a component nests like excluding a tag.
    [Fact]
    public void Without_NestsLikeWithoutTag()
    {
        var r = new Registry();
        HalfWithC(r);
        var free = r.Group().Own<A>().Own<B>().Without<C>().Build();

        var all = r.Group().Own<A>().Own<B>().Build();

        AssertPrefix(free, all);
    }

    [Fact]
    public void Without_SameTypeAsWith_Throws()
    {
        var r = new Registry();

        Assert.Throws<InvalidOperationException>(() => r.Group().Own<A>().Own<B>().With<C>().Without<C>().Build());
    }

    [Fact]
    public void Mixed_StayConsistentUnderChurn()
    {
        var r = new Registry();
        var ents = new List<Entity>();
        var rng = new Random(5);
        for (var i = 0; i < 300; i++)
        {
            var e = r.Create();
            ents.Add(e);
            Give(r, e, rng.Next(16));
            if (rng.Next(3) == 0) { r.Tag<Marked>(e); }
        }

        var outer = r.Group().Own<A>().Own<B>().Without<D>().Build();
        var inner = r.Group().Own<A>().Own<B>().Own<C>().Without<D>().WithTag<Marked>().Build();
        var partial = r.Group().Own<D>().With<C>().WithTag<Marked>().Build();
        var plain = r.Group().With<A>().With<C>().WithoutTag<Marked>().Build();

        void Check()
        {
            AssertGroup(r, outer, e => Has<A>(r, e) && Has<B>(r, e) && !Has<D>(r, e),
                (r.Storage<A>(), i => outer.Span<A>()[i].V, 1),
                (r.Storage<B>(), i => outer.Span<B>()[i].V, 2));
            AssertGroup(r, inner,
                e => Has<A>(r, e) && Has<B>(r, e) && Has<C>(r, e) && !Has<D>(r, e) && r.HasTag<Marked>(e),
                (r.Storage<A>(), i => inner.Span<A>()[i].V, 1),
                (r.Storage<B>(), i => inner.Span<B>()[i].V, 2),
                (r.Storage<C>(), i => inner.Span<C>()[i].V, 3));
            AssertGroup(r, partial, e => Has<C>(r, e) && Has<D>(r, e) && r.HasTag<Marked>(e),
                (r.Storage<D>(), i => partial.Span<D>()[i].V, 4));
            AssertMembers(r, plain.Entities, e => Has<A>(r, e) && Has<C>(r, e) && !r.HasTag<Marked>(e));
            AssertPrefix(inner, outer);
        }

        Check();
        for (var step = 0; step < 3000; step++)
        {
            RandomStep(r, ents, rng);
            Check();
        }
    }

    // 30 entities with A; even ones also have B.
    private static Group<Entity> PartialOverHalf(Registry r)
    {
        for (var i = 0; i < 30; i++)
        {
            Give(r, r.Create(), i % 2 == 0 ? 3 : 1);
        }

        return r.Group().Own<A>().With<B>().Build();
    }

    [Fact]
    public void Storage_ReturnsRequiredPools()
    {
        var r = new Registry();
        var g = PartialOverHalf(r);

        var b = g.Storage<B>();
        var a = g.Storage<A>();

        Assert.Same(r.Storage<B>(), b);
        Assert.Same(r.Storage<A>(), a);
        Assert.Throws<InvalidOperationException>(() => g.Storage<C>());
    }

    [Fact]
    public void GetSlots_MapsMembersToPoolSlots()
    {
        var r = new Registry();
        var g = PartialOverHalf(r);
        var bSlots = new int[g.Count];
        var aSlots = new int[g.Count];

        g.GetSlots<B>(bSlots);
        g.GetSlots<A>(aSlots);

        var b = g.Storage<B>();
        for (var i = 0; i < g.Count; i++)
        {
            Assert.Equal(Id(g.Entities[i]) * 10 + 2, b.At(bSlots[i]).V);
        }

        Assert.Equal(Enumerable.Range(0, g.Count), aSlots);
    }

    // 40 entities with A + B; every fourth also has C.
    private static Entity[] QuarterWithC(Registry r)
    {
        var ents = new Entity[40];
        for (var i = 0; i < ents.Length; i++)
        {
            ents[i] = r.Create();
            Give(r, ents[i], i % 4 == 0 ? 7 : 3);
        }

        return ents;
    }

    [Fact]
    public void NonOwning_CoexistsWithGroupsItCantNestWith()
    {
        var r = new Registry();
        QuarterWithC(r);

        var ab = r.Group().Own<A>().Own<B>().Build();
        var ac = r.Group().With<A>().With<C>().Build();

        Assert.Equal(40, ab.Count);
        Assert.Equal(10, ac.Count);
        Assert.Same(r.Storage<C>(), ac.Storage<C>());
    }

    [Fact]
    public void NonOwning_TracksMembership()
    {
        var r = new Registry();
        var ents = QuarterWithC(r);
        r.Group().Own<A>().Own<B>().Build();
        var ac = r.Group().With<A>().With<C>().Build();

        r.Add(ents[1], new C());
        r.Remove<A>(ents[0]);

        Assert.True(ac.Contains(ents[1]));
        Assert.False(ac.Contains(ents[0]));
        Assert.Equal(10, ac.Count);
    }

    [Fact]
    public void NonOwning_SortEntities_OrdersMembers()
    {
        var r = new Registry();
        QuarterWithC(r);
        r.Group().Own<A>().Own<B>().Build();
        var ac = r.Group().With<A>().With<C>().Build();

        ac.SortEntities((x, y) => y.Index.CompareTo(x.Index));

        for (var i = 1; i < ac.Count; i++)
        {
            Assert.True(ac.Entities[i - 1].Index > ac.Entities[i].Index);
        }
    }

    [Fact]
    public void NonOwning_TagRulesAlone_Throw()
    {
        var r = new Registry();

        Assert.Throws<ArgumentException>(() => r.Group().WithTag<Marked>().Build());
    }

    [Fact]
    public void NonOwning_Dispose_StopsTracking()
    {
        var r = new Registry();
        var ents = QuarterWithC(r);
        r.Group().Own<A>().Own<B>().Build();
        var ac = r.Group().With<A>().With<C>().Build();

        ac.Dispose();
        r.Remove<A>(ents[4]); // no longer tracked

        Assert.Equal(0, ac.Count);
        Assert.NotSame(ac, r.Group().With<A>().With<C>().Build());
    }

    private static List<Entity> RandomMix(Registry r, Random rng)
    {
        var ents = new List<Entity>();
        for (var i = 0; i < 200; i++)
        {
            var e = r.Create();
            ents.Add(e);
            Give(r, e, rng.Next(16));
        }

        return ents;
    }

    [Fact]
    public void Dispose_EmptiesTheGroupAndRejectsSort()
    {
        var r = new Registry();
        RandomMix(r, new Random(9));
        r.Group().Own<A>().Own<B>().Build();
        var middle = r.Group().Own<A>().Own<B>().Own<C>().Build();
        r.Group().Own<A>().Own<B>().Own<C>().Own<D>().Build();

        middle.Dispose();

        Assert.Equal(0, middle.Count);
        Assert.True(middle.Entities.IsEmpty);
        Assert.Throws<ObjectDisposedException>(() => middle.Sort<A>((x, y) => x.V.CompareTo(y.V)));
        Assert.NotSame(middle, r.Group().Own<A>().Own<B>().Own<C>().Build()); // released, so asking again builds a new one
    }

    [Fact]
    public void Dispose_KeepsTheRestNested()
    {
        var r = new Registry();
        var rng = new Random(9);
        var ents = RandomMix(r, rng);
        var outer = r.Group().Own<A>().Own<B>().Build();
        var middle = r.Group().Own<A>().Own<B>().Own<C>().Build();
        var inner = r.Group().Own<A>().Own<B>().Own<C>().Own<D>().Build();
        middle.Dispose();
        var middle2 = r.Group().Own<A>().Own<B>().Own<C>().Build();

        for (var step = 0; step < 1000; step++)
        {
            RandomStep(r, ents, rng);
            AssertPrefix(inner, middle2);
            AssertPrefix(middle2, outer);
            AssertGroup(r, inner, e => Has<A>(r, e) && Has<B>(r, e) && Has<C>(r, e) && Has<D>(r, e),
                (r.Storage<A>(), i => inner.Span<A>()[i].V, 1),
                (r.Storage<D>(), i => inner.Span<D>()[i].V, 4));
        }
    }

    // Once every group over C and D is gone, they sort and can be owned freely.
    [Fact]
    public void Dispose_ReleasesPoolsForSortAndOwnership()
    {
        var r = new Registry();
        RandomMix(r, new Random(9));
        r.Group().Own<A>().Own<B>().Build();
        var middle = r.Group().Own<A>().Own<B>().Own<C>().Build();
        var inner = r.Group().Own<A>().Own<B>().Own<C>().Own<D>().Build();

        inner.Dispose();
        middle.Dispose();

        r.Sort<D>((x, y) => x.V.CompareTo(y.V));
        Assert.Throws<InvalidOperationException>(() => r.Sort<A>((x, y) => x.V.CompareTo(y.V)));
        var cd = r.Group().Own<C>().Own<D>().Build();
        AssertGroup(r, cd, e => Has<C>(r, e) && Has<D>(r, e), (r.Storage<C>(), i => cd.Span<C>()[i].V, 3));
    }

    [Fact]
    public void Dispose_LastOwner_ReleasesThePool()
    {
        var r = new Registry();
        RandomMix(r, new Random(9));
        var outer = r.Group().Own<A>().Own<B>().Build();
        var middle = r.Group().Own<A>().Own<B>().Own<C>().Build();
        middle.Dispose();
        outer.Dispose();

        var thrown = Record.Exception(() => r.Sort<A>((x, y) => x.V.CompareTo(y.V)));

        Assert.Null(thrown);
    }

    // 80 entities with random A; even ones also have C.
    private static void RandomAHalfC(Registry r)
    {
        var rng = new Random(13);
        for (var i = 0; i < 80; i++)
        {
            var e = r.Create();
            r.Add(e, new A { V = rng.Next(1000) });
            if (i % 2 == 0) { r.Add(e, new C { V = Id(e) * 10 + 3 }); }
        }
    }

    [Fact]
    public void NonOwning_SortsByComponent()
    {
        var r = new Registry();
        RandomAHalfC(r);
        var g = r.Group().With<A>().With<C>().Build();
        var a = r.Storage<A>();
        var before = a.Entities.ToArray();
        Comparison<A> up = (x, y) => x.V.CompareTo(y.V);

        g.Sort(up);

        for (var i = 1; i < g.Count; i++)
        {
            Assert.True(a.Get(g.Entities[i - 1]).V <= a.Get(g.Entities[i]).V);
        }

        Assert.Equal(before, a.Entities.ToArray()); // the pool itself doesn't move
    }

    [Fact]
    public void NonOwning_Sort_AllocatesNothingOnceWarm()
    {
        var r = new Registry();
        RandomAHalfC(r);
        var g = r.Group().With<A>().With<C>().Build();
        Comparison<A> up = (x, y) => x.V.CompareTo(y.V);
        g.Sort(up);

        var allocated = GC.GetAllocatedBytesForCurrentThread();
        g.Sort(up);
        var after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(allocated, after);
    }

    [Fact]
    public void Lookup_DoesNotAllocate()
    {
        var r = new Registry();
        Give(r, r.Create(), 15);
        var g = r.Group().Own<A>().Own<B>().WithTag<Marked>().With<C>().Build();
        var plain = r.Group().With<C>().Without<D>().Build();
        _ = r.Group().Own<A>().Own<B>().WithTag<Marked>().With<C>().Build();
        _ = r.Group().With<C>().Without<D>().Build();

        // Builder steps allocate; Build itself doesn't.
        var ownedAgain = r.Group().Own<A>().Own<B>().With<C>().WithTag<Marked>();
        var plainAgain = r.Group().Without<D>().With<C>();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 100; i++)
        {
            Assert.Same(g, ownedAgain.Build());
            Assert.Same(plain, plainAgain.Build());
        }

        Assert.Equal(before, GC.GetAllocatedBytesForCurrentThread());
    }

    public enum WalkFilter { None, WithTag, WithAndWithoutTag, WithoutTag }

    public static IEnumerable<object[]> WalkCases() =>
        from arity in new[] { 2, 4 }
        from pinLast in new[] { false, true }
        from filter in Enum.GetValues<WalkFilter>()
        from useForeach in new[] { false, true }
        select new object[] { arity, pinLast, filter, useForeach };

    // Removals leave inner groups first, swapping at or above the current slot, so each entity is visited once.
    // Covers every walk the planner picks, through Each and foreach.
    [Theory]
    [MemberData(nameof(WalkCases))]
    public void ViewWalk_RemovesOrDestroysCurrent_OverChainedPools(int arity, bool pinLast, WalkFilter filter,
        bool useForeach)
    {
        var r = new Registry();
        var rng = new Random(17 + arity);
        var ents = Populate(r, 300, rng);
        foreach (var e in ents)
        {
            if (rng.Next(4) == 0) { r.Tag<Hidden>(e); }
        }

        var (outer, middle, inner) = MakeChain(r);
        var expected = ents.Where(e => Has<A>(r, e) && Has<B>(r, e)
            && (arity == 2 || (Has<C>(r, e) && Has<D>(r, e)))
            && (filter is not (WalkFilter.WithTag or WalkFilter.WithAndWithoutTag) || r.HasTag<Marked>(e))
            && (filter is not (WalkFilter.WithoutTag or WalkFilter.WithAndWithoutTag) || !r.HasTag<Hidden>(e)))
            .ToHashSet();

        var visited = new List<Entity>();
        void Act(Entity e)
        {
            visited.Add(e);
            switch (rng.Next(7))
            {
                case 0: r.Remove<A>(e); break;
                case 1: r.Remove<B>(e); break;
                case 2: r.Remove<C>(e); break;
                case 3: r.Remove<D>(e); break;
                case 4: r.Untag<Marked>(e); break;
                case 5: r.Destroy(e); break;
            }
        }

        if (arity == 2)
        {
            var view = r.View();
            view = filter switch
            {
                WalkFilter.WithTag => view.WithTag<Marked>(),
                WalkFilter.WithAndWithoutTag => view.WithTag<Marked>().WithoutTag<Hidden>(),
                WalkFilter.WithoutTag => view.WithoutTag<Hidden>(),
                _ => view,
            };
            view = pinLast ? view.Use<B>() : view;
            if (useForeach)
            {
                foreach (var row in view.Rows<A, B>())
                {
                    Act(row.Entity);
                }
            }
            else
            {
                view.Each((Entity e, ref A a, ref B b) => Act(e));
            }
        }
        else
        {
            var view = r.View();
            view = filter switch
            {
                WalkFilter.WithTag => view.WithTag<Marked>(),
                WalkFilter.WithAndWithoutTag => view.WithTag<Marked>().WithoutTag<Hidden>(),
                WalkFilter.WithoutTag => view.WithoutTag<Hidden>(),
                _ => view,
            };
            view = pinLast ? view.Use<D>() : view;
            if (useForeach)
            {
                foreach (var row in view.Rows<A, B, C, D>())
                {
                    Act(row.Entity);
                }
            }
            else
            {
                view.Each((Entity e, ref A a, ref B b, ref C c, ref D d) => Act(e));
            }
        }

        Assert.NotEmpty(expected);
        Assert.Equal(visited.Count, visited.Distinct().Count());
        Assert.True(expected.SetEquals(visited));
        CheckChain(r, outer, middle, inner);
    }

    // Handlers change components while every group in the chain is mid-update.
    [Fact]
    public void DestroyHandler_ChangesComponents_UnderNestedGroups()
    {
        var r = new Registry();
        var rng = new Random(19);
        var ents = Populate(r, 300, rng);
        var (outer, middle, inner) = MakeChain(r);
        var partner = new Random(23);
        r.Storage<D>().OnRemove += e =>
        {
            r.Remove<C>(e);
            var other = ents[partner.Next(ents.Count)];
            if (other == e || !r.IsAlive(other))
            {
                return;
            }

            if (!r.Remove<A>(other))
            {
                r.Add(other, new A { V = Id(other) * 10 + 1 });
            }
        };

        CheckChain(r, outer, middle, inner);
        for (var step = 0; step < 3000; step++)
        {
            RandomStep(r, ents, rng);
            CheckChain(r, outer, middle, inner);
        }
    }

    // Middle and inner both own C, so each removal leaves inner first, then middle.
    [Fact]
    public void ClearT_InnerOwnedPool_KeepsOuter()
    {
        var r = new Registry();
        Populate(r, 300, new Random(29));
        var (outer, middle, inner) = MakeChain(r);
        var outerCount = outer.Count;
        var innerBefore = inner.Count;

        r.Clear<C>();

        Assert.True(innerBefore > 0);
        Assert.Equal(0, middle.Count);
        Assert.Equal(0, inner.Count);
        Assert.Equal(outerCount, outer.Count);
        CheckChain(r, outer, middle, inner);
    }

    [Fact]
    public void ClearT_ThenRefill_RebuildsChain()
    {
        var r = new Registry();
        var ents = Populate(r, 300, new Random(29));
        var (outer, middle, inner) = MakeChain(r);
        r.Clear<C>();

        foreach (var e in ents)
        {
            Give(r, e, r.Has<D>(e) ? 4 : 4 | 8);
        }

        Assert.True(inner.Count > 0);
        Assert.Equal(middle.Count, inner.Count);
        CheckChain(r, outer, middle, inner);
    }

    [Fact]
    public void ClearT_OuterOwnedPool_EmptiesChain()
    {
        var r = new Registry();
        Populate(r, 300, new Random(29));
        var (outer, middle, inner) = MakeChain(r);
        var innerBefore = inner.Count;

        r.Clear<A>();

        Assert.True(innerBefore > 0);
        Assert.Equal(0, outer.Count);
        CheckChain(r, outer, middle, inner);
    }

    // Saves the shared mix, less 40 destroyed entities.
    private static Archive SaveMix(Registry source, Random rng, out List<Entity> ents)
    {
        ents = Populate(source, 300, rng);
        for (var i = 0; i < 40; i++)
        {
            var e = ents[rng.Next(ents.Count)];
            source.Destroy(e);
            ents.Remove(e);
        }

        var archive = new Archive();
        source.Snapshot<Archive>().Entities(ref archive)
            .Component<A>(ref archive).Component<B>(ref archive).Component<C>(ref archive)
            .Component<D>(ref archive).Tag<Marked>(ref archive);
        return archive;
    }

    private static void Load(Registry loaded, Archive archive)
    {
        var reader = archive.Reopen();
        loaded.SnapshotLoader<Archive>().Entities(ref reader)
            .Component<A>(ref reader).Component<B>(ref reader).Component<C>(ref reader)
            .Component<D>(ref reader).Tag<Marked>(ref reader);
    }

    [Fact]
    public void SnapshotLoad_IntoRegistryWithNestedGroups()
    {
        var source = new Registry();
        var archive = SaveMix(source, new Random(31), out _);
        var loaded = new Registry();
        var (outer, middle, inner) = MakeChain(loaded);
        var plain = loaded.Group().With<C>().WithoutTag<Marked>().Build();

        Load(loaded, archive);

        Assert.True(inner.Count > 0);
        CheckChain(loaded, outer, middle, inner);
        AssertMembers(loaded, plain.Entities, e => Has<C>(loaded, e) && !loaded.HasTag<Marked>(e));
        Assert.Equal(source.Group().Own<A>().Own<B>().Build().Count, outer.Count);
    }

    [Fact]
    public void SnapshotLoad_GroupHooksStayLive()
    {
        var rng = new Random(31);
        var archive = SaveMix(new Registry(), rng, out var ents);
        var loaded = new Registry();
        var (outer, middle, inner) = MakeChain(loaded);
        Load(loaded, archive);

        for (var step = 0; step < 500; step++)
        {
            RandomStep(loaded, ents, rng);
            CheckChain(loaded, outer, middle, inner);
        }
    }
}
