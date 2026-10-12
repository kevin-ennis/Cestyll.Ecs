using Cestyll.Ecs;
using Cestyll.Ecs.Tags;
using Cestyll.Ecs.Views;

namespace Cestyll.Ecs.Tests;

public class JoinedViewTests
{
    private struct A : IComponent { public int V; }
    private struct B : IComponent { public int V; }
    private struct C : IComponent { public int V; }
    private struct D : IComponent { public int V; }
    private struct E : IComponent { public int V; }
    private struct F : IComponent { public int V; }
    private struct G : IComponent { public int V; }
    private struct H : IComponent { public int V; }
    private struct Marked : ITag;
    private struct Unused : ITag;

    // A sorted ascending, B descending, so the driving pool shows in the walk order.
    private static (Registry Registry, Entity[] Entities) Build()
    {
        var registry = new Registry();
        var entities = new Entity[12];
        for (var i = 0; i < entities.Length; i++)
        {
            Entity e = registry.Create();
            entities[i] = e;
            registry.Add(e, new A { V = i });
            if (i % 2 == 0)
            {
                registry.Add(e, new B { V = i });
            }

            if (i % 3 == 0)
            {
                registry.Add(e, new C { V = i });
            }

            if (i % 4 == 0)
            {
                registry.Tag<Marked>(e);
            }
        }

        registry.Sort<A>((x, y) => x.V.CompareTo(y.V));
        registry.Sort<B>((x, y) => y.V.CompareTo(x.V));
        return (registry, entities);
    }

    private static List<int> Walk(View<Entity> view)
    {
        var list = new List<int>();
        view.Each((Entity _, ref A a, ref B b) =>
        {
            Assert.Equal(a.V, b.V);
            list.Add(a.V);
        });
        return list;
    }

    [Fact]
    public void Join_MatchesTheViewOverBothTypes()
    {
        var (registry, _) = Build();

        var joined = registry.View().Join(registry.View());

        Assert.Equal(Walk(registry.View()), Walk(joined));
        Assert.Equal([0, 2, 4, 6, 8, 10], Walk(joined).Order());
    }

    [Fact]
    public void Join_KeepsBothViewsFilters()
    {
        var (registry, _) = Build();

        var joined = registry.View().WithTag<Marked>().Join(registry.View().Without<C>());

        Assert.Equal([4, 8], Walk(joined).Order());
    }

    [Fact]
    public void Join_KeepsPoolFilters()
    {
        var (registry, entities) = Build();
        var spawned = registry.Storage<C>("spawn");
        spawned.Add(entities[2], default);
        spawned.Add(entities[3], default);

        var joined = registry.View().Join(registry.View().With<C>("spawn"));

        Assert.Equal([2], Walk(joined));
    }

    [Fact]
    public void Join_DropsPins_UseAfterPins()
    {
        var (registry, _) = Build();

        var joined = registry.View().Use<A>().Join(registry.View().Use<B>());

        Assert.Equal(Walk(registry.View()), Walk(joined));
        Assert.Equal(Walk(registry.View().Use<A>()), Walk(joined.Use<A>()));
        Assert.NotEqual(Walk(joined), Walk(joined.Use<A>()));
    }

    [Fact]
    public void Join_SameTypeTwice_WalksEachEntityOnce()
    {
        var (registry, _) = Build();
        var pairs = new List<(int, int)>();

        registry.View().Join(registry.View()).Each((Entity _, ref B x, ref B y) => pairs.Add((x.V, y.V)));

        Assert.Equal(6, pairs.Count);
        Assert.All(pairs, p => Assert.Equal(p.Item1, p.Item2));
    }

    [Fact]
    public void Join_DifferentRegistries_Throws()
    {
        var registry = new Registry();
        var other = new Registry();

        Assert.Throws<ArgumentException>(() => registry.View().Join(other.View()));
    }

    [Fact]
    public void Join_DefaultView_Throws()
    {
        var registry = new Registry();

        Assert.Throws<ArgumentException>(() => registry.View().Join(default(View<Entity>)));
    }

    [Fact]
    public void Rows_DroppedPool_Throws()
    {
        var registry = new Registry();
        var previous = registry.Storage<A>("previous");
        registry.DropStorage<A>("previous");

        Assert.Throws<ArgumentException>(() => registry.View().Rows(registry.Storage<B>(), previous));
    }

    // A join only knows the pools once the callback names them.
    [Fact]
    public void Join_ExcludesAFetchedPool_ThrowsAtEach()
    {
        var (registry, _) = Build();
        var previous = registry.Storage<A>("previous");
        var withoutB = registry.View().Without<B>().Join(registry.View());
        var otherWithoutB = registry.View().Join(registry.View().Without<B>());
        var withoutPrevious = registry.View().Without<A>("previous").Join(registry.View());
        var withoutC = registry.View().Without<C>().Join(registry.View());

        Assert.Throws<InvalidOperationException>(() => withoutB.Each((Entity _, ref A _, ref B _) => { }));
        Assert.Throws<InvalidOperationException>(() => otherWithoutB.Each((Entity _, ref B _, ref C _) => { }));
        Assert.Throws<InvalidOperationException>(() => withoutPrevious.Rows(registry.Storage<B>(), previous).Each((Entity _, ref B _, ref A _) => { }));
        Assert.Throws<InvalidOperationException>(() => withoutC.Each((Entity _, ref B _, ref C _) => { }));
    }

    // Without<A> names A's default pool, not a named one of A.
    [Fact]
    public void Join_WithoutDefaultPool_DoesNotExcludeANamedOne()
    {
        var (registry, _) = Build();
        var previous = registry.Storage<A>("previous");
        var joined = registry.View().Without<A>().Join(registry.View());
        var count = 0;

        joined.Rows(registry.Storage<B>(), previous).Each((Entity _, ref B _, ref A _) => count++);

        Assert.Equal(0, count);
    }

    [Fact]
    public void Join_RequiresAndExcludesTheSameStorage_Throws()
    {
        var (registry, _) = Build();

        Assert.Throws<InvalidOperationException>(() => registry.View().With<C>().Join(registry.View().Without<C>()));
        Assert.Throws<InvalidOperationException>(() => registry.View().WithTag<Marked>().Join(registry.View().WithoutTag<Marked>()));
        Assert.Throws<InvalidOperationException>(() => registry.View().With<C>("spawn").Join(registry.View().Without<C>("spawn")));

        // A tag nobody has made yet contradicts too, matched by its filter.
        var ex = Assert.Throws<InvalidOperationException>(() =>
            registry.View().WithTag<Unused>().Join(registry.View().WithoutTag<Unused>()));
        Assert.Contains(nameof(Unused), ex.Message);
    }

    // The default pool and a named one of C are different storage.
    [Fact]
    public void Join_DefaultAndNamedPool_DoNotContradict()
    {
        var (registry, _) = Build();

        var exception = Record.Exception(() => registry.View().With<C>().Join(registry.View().Without<C>("spawn")));

        Assert.Null(exception);
    }

    [Fact]
    public void Join_UnfilteredOrSameFilters_DoesNotAllocate()
    {
        var (registry, _) = Build();
        var a = registry.View();
        var b = registry.View();
        var marked = registry.View().WithTag<Marked>();
        var markedB = registry.View().WithTag<Marked>();
        a.Join(b);
        marked.Join(b);
        a.Join(markedB);
        marked.Join(markedB);

        var before = GC.GetAllocatedBytesForCurrentThread();
        var joined = a.Join(b);
        marked.Join(b);
        a.Join(markedB);
        marked.Join(markedB);
        var after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(before, after);
        Assert.Equal(Walk(registry.View()), Walk(joined));
    }

    // One filter, on both sides: kept once.
    [Fact]
    public void Join_SameFilterOnBothSides_KeptOnce()
    {
        var (registry, _) = Build();
        var marked = registry.View().WithTag<Marked>();
        var markedB = registry.View().WithTag<Marked>();

        var both = marked.Join(markedB);

        Assert.Equal([0, 4, 8], Walk(both).Order());
    }

    // Filters on one side or both, and Rows over given pools.
    [Fact]
    public void Join_ReachesEightPools()
    {
        var registry = new Registry();
        Entity e = registry.Create();
        registry.Add(e, new A { V = 1 });
        registry.Add(e, new B { V = 2 });
        registry.Add(e, new C { V = 3 });
        registry.Add(e, new D { V = 4 });
        registry.Add(e, new E { V = 5 });
        registry.Add(e, new F { V = 6 });
        registry.Add(e, new G { V = 7 });
        registry.Add(e, new H { V = 8 });
        registry.Add(registry.Create(), new A());

        var sums = new List<int>();
        registry.View().Join(registry.View())
            .Each((Entity _, ref A a, ref B b, ref C c, ref D d, ref E e2, ref F f, ref G g, ref H h) =>
                sums.Add(a.V + b.V + c.V + d.V + e2.V + f.V + g.V + h.V));
        registry.View().With<D>().Join(registry.View().With<E>())
            .Each((Entity _, ref A a, ref B b, ref C c, ref D d, ref E e2, ref F f, ref G g, ref H h) =>
                sums.Add(a.V + b.V + c.V + d.V + e2.V + f.V + g.V + h.V));
        registry.View()
            .Rows(
                registry.Storage<A>(), registry.Storage<B>(), registry.Storage<C>(), registry.Storage<D>(),
                registry.Storage<E>(), registry.Storage<F>(), registry.Storage<G>(), registry.Storage<H>())
            .Each((Entity _, ref A a, ref B b, ref C c, ref D d, ref E e2, ref F f, ref G g, ref H h) =>
                sums.Add(a.V + b.V + c.V + d.V + e2.V + f.V + g.V + h.V));

        Assert.Equal([36, 36, 36], sums);
    }

    [Fact]
    public void Join_OnAJoinedView_Chains()
    {
        var (registry, _) = Build();
        var values = new List<int>();

        registry.View().Join(registry.View()).Join(registry.View().WithTag<Marked>())
            .Each((Entity _, ref A a, ref B _, ref C _) => values.Add(a.V));

        Assert.Single(values);
        Assert.Equal(0, values[0] % 12);
    }
}
