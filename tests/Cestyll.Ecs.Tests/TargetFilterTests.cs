using Cestyll.Ecs;
using Cestyll.Ecs.Relations;
using Cestyll.Ecs.Tags;
using Cestyll.Ecs.Views;

namespace Cestyll.Ecs.Tests;

// View.WithRelationTarget(target): the target's sources drive the walk and every pool is probed.
public class TargetFilterTests
{
    private struct A : IComponent { public int V; }
    private struct B : IComponent { public int V; }
    private struct C : IComponent { public int V; }
    private struct Marked : ITag { }
    private struct Orbiting : IRelation
    {
        public static OnTargetDestroyed Policy => OnTargetDestroyed.Unrelate;
    }

    private struct Escorting : IRelation
    {
        public static OnTargetDestroyed Policy => OnTargetDestroyed.Unrelate;
    }

    private struct Attacks : ILink<Entity>
    {
        public Entity Target { get; init; }
    }


    // Four owners; each other entity orbits one of them and holds a mix of A, B and C.
    private static (Registry, Entity[] owners) Build()
    {
        var registry = new Registry();
        var owners = new Entity[4];
        for (var k = 0; k < owners.Length; k++)
        {
            owners[k] = registry.Create(new A { V = -1 }, new B { V = -2 });
        }

        for (var i = 0; i < 400; i++)
        {
            Entity e = i % 3 == 0 ? registry.Create(new A { V = i }, new B { V = i * 2 })
                : i % 3 == 1 ? registry.Create(new A { V = i })
                : registry.Create(new B { V = i * 2 });
            if (i % 5 == 0)
            {
                registry.Add(e, new C { V = i * 3 });
            }

            if (i % 2 == 0)
            {
                registry.Tag<Marked>(e);
            }

            registry.Relate<Orbiting>(e, owners[i % owners.Length]);
            if (i % 7 == 0)
            {
                registry.Relate<Escorting>(e, owners[0]);
            }

            // Some attack two owners, so a source sits in several in-lists.
            if (i % 3 != 2)
            {
                registry.Link(e, new Attacks { Target = owners[i % 2] });
            }

            if (i % 4 == 0)
            {
                registry.Link(e, new Attacks { Target = owners[3] });
            }
        }

        return (registry, owners);
    }

    // The same filter applied by hand to every entity holding A and B.
    private static List<Entity> Expected(Registry registry, Func<Entity, bool> keep)
    {
        var seen = new List<Entity>();
        foreach (var row in registry.View().Rows<A, B>())
        {
            if (keep(row.Entity))
            {
                seen.Add(row.Entity);
            }
        }

        return seen.OrderBy(e => e.Index).ToList();
    }

    private static List<Entity> Walk(View<Entity> view)
    {
        var seen = new List<Entity>();
        view.Each((Entity e, ref A a, ref B b) =>
        {
            Assert.Equal(a.V * 2, b.V);
            seen.Add(e);
        });
        return seen.OrderBy(e => e.Index).ToList();
    }

    private static List<Entity> Foreach(View<Entity> view)
    {
        var seen = new List<Entity>();
        foreach (var row in view.Rows<A, B>())
        {
            Assert.Equal(row.C1.V * 2, row.C2.V);
            seen.Add(row.Entity);
        }

        return seen.OrderBy(e => e.Index).ToList();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void VisitsTheTargetsSourcesThatMatch(int ownerIndex)
    {
        var (registry, owners) = Build();
        var owner = owners[ownerIndex];
        var expected = Expected(registry, e => registry.GetTarget<Orbiting>(e) == owner);

        var view = registry.View().WithRelationTarget<Orbiting>(owner);

        Assert.NotEmpty(expected);
        Assert.Equal(expected, Walk(view));
        Assert.Equal(expected, Foreach(view));
    }

    [Fact]
    public void WalksSourcesLastFirst()
    {
        var (registry, owners) = Build();
        var sources = registry.Relations<Orbiting>().SourceSpan(owners[1]).ToArray();
        Array.Reverse(sources);

        var seen = new List<Entity>();
        foreach (var e in registry.View().WithRelationTarget<Orbiting>(owners[1]).Rows())
        {
            seen.Add(e);
        }

        Assert.Equal(sources, seen);
    }

    [Fact]
    public void EveryArity_SeesTheMatchingEntities()
    {
        var (registry, owners) = Build();
        var view = registry.View().WithRelationTarget<Orbiting>(owners[2]);
        var one = new List<Entity>();
        var three = new List<Entity>();
        var rowsOnly = new List<Entity>();

        view.Each((Entity e, ref A a) => one.Add(e));
        view.Each((Entity e, ref A a, ref B b, ref C c) => three.Add(e));
        foreach (var e in view.Rows())
        {
            rowsOnly.Add(e);
        }

        Assert.Equal(
            registry.Relations<Orbiting>().SourceSpan(owners[2]).ToArray().Where(registry.Has<A>).OrderBy(e => e.Index),
            one.OrderBy(e => e.Index));
        Assert.Equal(
            Expected(registry, e => registry.GetTarget<Orbiting>(e) == owners[2] && registry.Has<C>(e)),
            three.OrderBy(e => e.Index));
        Assert.Equal(registry.SourceCount<Orbiting>(owners[2]), rowsOnly.Count);
    }

    [Fact]
    public void WithoutRelationTarget_ExcludesTheSources()
    {
        var (registry, owners) = Build();
        var expected = Expected(registry, e => registry.GetTarget<Orbiting>(e) != owners[0]);

        Assert.Equal(expected, Walk(registry.View().WithoutRelationTarget<Orbiting>(owners[0])));
        Assert.Equal(expected, Foreach(registry.View().WithoutRelationTarget<Orbiting>(owners[0])));
    }

    [Fact]
    public void CombinesWithOtherFilters()
    {
        var (registry, owners) = Build();
        var expected = Expected(registry, e =>
            registry.GetTarget<Orbiting>(e) == owners[0] && registry.HasTag<Marked>(e) && !registry.Has<C>(e)
            && registry.GetTarget<Escorting>(e) == owners[0]);
        var view = registry.View()
            .WithRelationTarget<Orbiting>(owners[0])
            .WithTag<Marked>()
            .Without<C>()
            .WithRelationTarget<Escorting>(owners[0]);

        Assert.NotEmpty(expected);
        Assert.Equal(expected, Walk(view));
        Assert.Equal(expected, Foreach(view));
    }

    [Fact]
    public void PinOrJoin_StillFilterByTarget()
    {
        var (registry, owners) = Build();
        var expected = Expected(registry, e => registry.GetTarget<Orbiting>(e) == owners[3]);

        Assert.Equal(expected, Walk(registry.View().Use<A>().WithRelationTarget<Orbiting>(owners[3])));
        Assert.Equal(expected, Walk(registry.View().WithRelationTarget<Orbiting>(owners[3]).Use<B>()));
        Assert.Equal(expected, Walk(registry.View().WithRelationTarget<Orbiting>(owners[3]).Join(registry.View().With<A>())));
        Assert.Equal(expected, Foreach(registry.View().WithRelationTarget<Orbiting>(owners[3]).Join(registry.View().With<A>())));
    }

    [Fact]
    public void NoRelationStore_WalksNothing()
    {
        var registry = new Registry();
        Entity lonely = registry.Create(new A(), new B());

        var view = registry.View().WithRelationTarget<Orbiting>(lonely);

        Assert.Empty(Walk(view));
        Assert.Empty(Foreach(view));
    }

    [Fact]
    public void NoSources_WalksNothing()
    {
        var registry = new Registry();
        Entity lonely = registry.Create(new A(), new B());
        registry.Relate<Orbiting>(registry.Create(new A()), registry.Create());

        var view = registry.View().WithRelationTarget<Orbiting>(lonely);

        Assert.Empty(Walk(view));
        Assert.Single(Walk(registry.View().WithoutRelationTarget<Orbiting>(lonely)));
    }

    [Fact]
    public void SourcesRelatedAfterTheViewIsBuilt_AreSeen()
    {
        var registry = new Registry();
        Entity owner = registry.Create();
        var view = registry.View().WithRelationTarget<Orbiting>(owner);
        Walk(view);
        Entity blade = registry.Create(new A { V = 1 }, new B { V = 2 });

        registry.Relate<Orbiting>(blade, owner);

        Assert.Equal([blade], Walk(view));
        Assert.Equal([blade], Foreach(view));
    }

    [Fact]
    public void UnrelatingTheCurrentSource_VisitsEachOnce()
    {
        var (registry, owners) = Build();
        var expected = Expected(registry, e => registry.GetTarget<Orbiting>(e) == owners[1]);

        var seen = new List<Entity>();
        registry.View().WithRelationTarget<Orbiting>(owners[1]).Each((Entity e, ref A a, ref B b) =>
        {
            seen.Add(e);
            registry.Unrelate<Orbiting>(e);
        });

        Assert.Equal(expected, seen.OrderBy(e => e.Index));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void WithLinkTarget_VisitsEntitiesLinkingToTheTarget(int ownerIndex)
    {
        var (registry, owners) = Build();
        var owner = owners[ownerIndex];
        var attacks = registry.Links<Attacks>();
        var expected = Expected(registry, e => attacks.Has(e, owner));

        var view = registry.View().WithLinkTarget<Attacks>(owner);

        Assert.Equal(expected, Walk(view));
        Assert.Equal(expected, Foreach(view));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void WithoutLinkTarget_ExcludesTheSources(int ownerIndex)
    {
        var (registry, owners) = Build();
        var owner = owners[ownerIndex];
        var attacks = registry.Links<Attacks>();
        var expected = Expected(registry, e => !attacks.Has(e, owner));

        var view = registry.View().WithoutLinkTarget<Attacks>(owner);

        Assert.Equal(expected, Walk(view));
    }

    [Fact]
    public void WithLinkTarget_CombinesWithWithRelationTarget()
    {
        var (registry, owners) = Build();
        var attacks = registry.Links<Attacks>();
        var expected = Expected(registry, e =>
            attacks.Has(e, owners[3]) && registry.GetTarget<Orbiting>(e) == owners[0]);
        var view = registry.View().WithLinkTarget<Attacks>(owners[3]).WithRelationTarget<Orbiting>(owners[0]);

        Assert.NotEmpty(expected);
        Assert.Equal(expected, Walk(view));
        Assert.Equal(expected, Foreach(view));
    }

    [Fact]
    public void WithLinkTarget_UnlinkingTheCurrentSource_VisitsEachOnce()
    {
        var (registry, owners) = Build();
        var attacks = registry.Links<Attacks>();
        var expected = Expected(registry, e => attacks.Has(e, owners[1]));

        var seen = new List<Entity>();
        registry.View().WithLinkTarget<Attacks>(owners[1]).Each((Entity e, ref A a, ref B b) =>
        {
            seen.Add(e);
            attacks.Unlink(e, owners[1]);
        });

        Assert.Equal(expected, seen.OrderBy(e => e.Index));
        Assert.All(seen, e => Assert.False(attacks.Has(e, owners[1])));
    }

    [Fact]
    public void WithLinkTarget_NoStore_WalksNothing()
    {
        var registry = new Registry();
        Entity lonely = registry.Create(new A(), new B());

        Assert.Empty(Walk(registry.View().WithLinkTarget<Attacks>(lonely)));
        Assert.Empty(Foreach(registry.View().WithLinkTarget<Attacks>(lonely)));
        Assert.Single(Walk(registry.View().WithoutLinkTarget<Attacks>(lonely)));
    }
}
