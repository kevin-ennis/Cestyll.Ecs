using Cestyll.Ecs;
using Cestyll.Ecs.Groups;
using Cestyll.Ecs.Tags;

namespace Cestyll.Ecs.Tests;

public class SortTests
{
    private struct A : IComponent { public int V; }
    private struct B : IComponent { public int V; }
    private struct C : IComponent;
    private struct Hidden : ITag;

    // Shuffled values so a sort has real work to do.
    private static int Scramble(int i) => (i * 37 + 11) % 101;

    // The order a view over T alone visits entities in.
    private static List<Entity> Walk<T>(Registry registry)
        where T : IComponent
    {
        var seen = new List<Entity>();
        registry.View().Each((Entity e, ref T _) => seen.Add(e));
        return seen;
    }

    private static void AssertConsistent<T>(Registry registry, Func<T, int> v, Func<Entity, int> expected)
        where T : IComponent
    {
        var pool = registry.Storage<T>();
        for (var s = 0; s < pool.Count; s++)
        {
            var e = pool.Entities[s];
            Assert.Equal(s, pool.IndexOf(e));
            Assert.Equal(expected(e), v(pool.At(s)));
        }
    }

    private static (Registry, Dictionary<Entity, int>) Scrambled(int count)
    {
        var registry = new Registry();
        var values = new Dictionary<Entity, int>();
        for (var i = 0; i < count; i++)
        {
            Entity e = registry.Create();
            registry.Add(e, new A { V = Scramble(i) });
            values[e] = Scramble(i);
        }

        return (registry, values);
    }

    [Fact]
    public void Sort_ViewVisitsInComparisonOrder()
    {
        var (registry, values) = Scrambled(100);

        registry.Sort<A>((x, y) => x.V.CompareTo(y.V));

        var walked = Walk<A>(registry).Select(e => values[e]).ToList();
        Assert.Equal(values.Values.Order(), walked);
        AssertConsistent<A>(registry, a => a.V, e => values[e]);
    }

    [Fact]
    public void SortEntities_ViewVisitsInComparisonOrder()
    {
        var (registry, values) = Scrambled(100);
        registry.Sort<A>((x, y) => x.V.CompareTo(y.V));

        registry.SortEntities<A>((x, y) => y.Index.CompareTo(x.Index));

        var walked = Walk<A>(registry).Select(e => e.Index).ToList();
        Assert.Equal(walked.OrderDescending(), walked);
        AssertConsistent<A>(registry, a => a.V, e => values[e]);
    }

    [Fact]
    public void Sort_AllocatesNothingOnceWarm()
    {
        var (registry, _) = Scrambled(1000);
        Comparison<A> up = static (x, y) => x.V.CompareTo(y.V);
        Comparison<A> down = static (x, y) => y.V.CompareTo(x.V);
        registry.Sort(up);
        registry.Sort(down);

        var before = GC.GetAllocatedBytesForCurrentThread();
        registry.Sort(up);
        Assert.Equal(before, GC.GetAllocatedBytesForCurrentThread());
    }

    [Fact]
    public void SortAs_ViewVisitsSharedFirstInOtherOrder()
    {
        var registry = new Registry();
        var ents = new Entity[60];
        for (var i = 0; i < ents.Length; i++)
        {
            ents[i] = registry.Create();
            registry.Add(ents[i], new A { V = i });
            if (i % 3 != 0)
            {
                registry.Add(ents[i], new B { V = Scramble(i) });
            }
        }

        // B only: not in A, so it must not affect A's order.
        registry.Add(registry.Create(), new B { V = -1 });
        registry.Sort<B>((x, y) => x.V.CompareTo(y.V));
        var restBefore = Walk<A>(registry).Where(e => !registry.Has<B>(e)).ToList();

        registry.SortAs<A, B>();

        var walked = Walk<A>(registry);
        var shared = Walk<B>(registry).Where(e => registry.Has<A>(e)).ToList();
        Assert.Equal(shared, walked[..shared.Count]);
        Assert.Equal(restBefore, walked[shared.Count..]);
        AssertConsistent<A>(registry, x => x.V, e => Array.IndexOf(ents, e));
    }

    [Fact]
    public void SortAs_MissingPoolIsNoOp()
    {
        var registry = new Registry();
        Entity e = registry.Create();
        registry.Add(e, new A { V = 1 });

        registry.SortAs<A, B>();

        Assert.Equal(e, registry.Storage<A>().Entities[0]);
    }

    [Fact]
    public void Sort_MissingPoolIsNoOp()
    {
        var registry = new Registry();
        Entity e = registry.Create();
        registry.Add(e, new A { V = 1 });

        registry.Sort<C>((x, y) => 0);

        Assert.Equal(e, registry.Storage<A>().Entities[0]);
    }

    [Fact]
    public void SortingOwnedPool_Throws()
    {
        var registry = new Registry();
        registry.Group().Own<A>().Own<B>().Build();

        Assert.Throws<InvalidOperationException>(() => registry.Sort<A>((x, y) => x.V.CompareTo(y.V)));
        Assert.Throws<InvalidOperationException>(() => registry.SortEntities<A>((x, y) => 0));
        Assert.Throws<InvalidOperationException>(() => registry.SortAs<B, C>());
    }

    // Reading an owned pool as the reference order is fine.
    [Fact]
    public void SortAs_OwnedPoolAsReference_IsAllowed()
    {
        var registry = new Registry();
        registry.Group().Own<A>().Own<B>().Build();
        registry.Storage<C>();

        registry.SortAs<C, A>();
    }

    private static (Registry Registry, Group<Entity> Group) ScrambledGroup()
    {
        var registry = new Registry();
        for (var i = 0; i < 100; i++)
        {
            Entity e = registry.Create();
            registry.Add(e, new A { V = Scramble(i) });
            if (i % 5 != 0)
            {
                registry.Add(e, new B { V = Scramble(i) + 1000 });
            }

            if (i % 7 == 0)
            {
                registry.Tag<Hidden>(e);
            }
        }

        return (registry, registry.Group().Own<A>().Own<B>().WithoutTag<Hidden>().Build());
    }

    [Fact]
    public void GroupSort_OrdersPrefixAndKeepsAlignment()
    {
        var (registry, g) = ScrambledGroup();
        var tail = registry.Storage<A>().Entities[g.Count..].ToArray();

        g.Sort<B>((x, y) => x.V.CompareTo(y.V));

        for (var s = 1; s < g.Count; s++)
        {
            Assert.True(g.Span<B>()[s - 1].V <= g.Span<B>()[s].V);
        }

        for (var s = 0; s < g.Count; s++)
        {
            Assert.Equal(g.Span<A>()[s].V + 1000, g.Span<B>()[s].V);
            Assert.Equal(g.Entities[s], registry.Storage<B>().Entities[s]);
        }

        Assert.Equal(tail, registry.Storage<A>().Entities[g.Count..].ToArray());
        AssertConsistent<A>(registry, a => a.V, e => registry.Get<A>(e).V);
        AssertConsistent<B>(registry, b => b.V, e => registry.Get<A>(e).V + 1000);
    }

    [Fact]
    public void GroupSortEntities_OrdersPrefixAndKeepsAlignment()
    {
        var (_, g) = ScrambledGroup();
        g.Sort<B>((x, y) => x.V.CompareTo(y.V));

        g.SortEntities((x, y) => x.Index.CompareTo(y.Index));

        for (var s = 1; s < g.Count; s++)
        {
            Assert.True(g.Entities[s - 1].Index < g.Entities[s].Index);
            Assert.Equal(g.Span<A>()[s].V + 1000, g.Span<B>()[s].V);
        }
    }

    [Fact]
    public void GroupSort_MembershipStillTracks()
    {
        var (registry, g) = ScrambledGroup();
        g.Sort<B>((x, y) => x.V.CompareTo(y.V));
        g.SortEntities((x, y) => x.Index.CompareTo(y.Index));
        var first = g.Entities[0];

        registry.Tag<Hidden>(first);
        var memberWhileHidden = g.Entities.Contains(first);
        registry.Untag<Hidden>(first);

        Assert.False(memberWhileHidden);
        Assert.True(g.Entities.Contains(first));
    }

    [Fact]
    public void GroupSort_UnownedTypeThrows()
    {
        var registry = new Registry();
        var g = registry.Group().Own<A>().Own<B>().Build();

        Assert.Throws<InvalidOperationException>(() => g.Sort<C>((x, y) => 0));
    }
}
