using Cestyll.Ecs;
using Cestyll.Ecs.Groups;
using Cestyll.Ecs.Tags;
using Cestyll.Ecs.Views;

namespace Cestyll.Ecs.Tests;

// View.Use(group): a non-owning group's members drive the walk and every pool is probed.
public class UseGroupTests
{
    private struct A : IComponent { public int V; }
    private struct B : IComponent { public int V; }
    private struct C : IComponent { public int V; }
    private struct Frozen : IComponent { }
    private struct Marked : ITag { }

    // Every sixth holds both A and B, so the pools are far larger than the group.
    private static Registry Build()
    {
        var registry = new Registry();
        for (var i = 0; i < 600; i++)
        {
            Entity e = i % 6 == 5 ? registry.Create(new A { V = i }, new B { V = i * 2 })
                : i % 2 == 0 ? registry.Create(new A { V = i })
                : registry.Create(new B { V = i * 2 });
            if (i % 4 == 1)
            {
                registry.Add(e, new C { V = i * 3 });
            }

            if (i % 7 == 0)
            {
                registry.Add(e, new Frozen());
            }

            if (i % 3 == 2)
            {
                registry.Tag<Marked>(e);
            }
        }

        return registry;
    }

    private static List<(Entity, int, int)> Walk(View<Entity> view)
    {
        var seen = new List<(Entity, int, int)>();
        view.Each((Entity e, ref A a, ref B b) => seen.Add((e, a.V, b.V)));
        return seen;
    }

    [Fact]
    public void VisitsTheSameEntitiesInMemberOrder()
    {
        var registry = Build();
        var group = registry.Group().With<A>().With<B>().Build();
        var viaPools = Walk(registry.View());
        // Last member first, as a pool-driven walk runs its pool.
        var members = group.Entities.ToArray();
        Array.Reverse(members);

        var viaGroup = Walk(registry.View().Use(group));

        Assert.Equal(100, viaGroup.Count);
        Assert.Equal(viaPools.OrderBy(t => t.Item1.Index), viaGroup.OrderBy(t => t.Item1.Index));
        Assert.Equal(members, viaGroup.Select(t => t.Item1));
    }

    [Fact]
    public void ForeachMatchesEach()
    {
        var registry = Build();
        var group = registry.Group().With<A>().With<B>().Build();
        var view = registry.View().Use(group);
        var each = Walk(view);

        var rows = new List<(Entity, int, int)>();
        foreach (var row in view.Rows<A, B>())
        {
            rows.Add((row.Entity, row.C1.V, row.C2.V));
        }

        Assert.Equal(each, rows);
    }

    [Fact]
    public void ForeachMatchesEachWithOneComponent()
    {
        var registry = Build();
        var group = registry.Group().With<A>().With<B>().Build();
        var one = registry.View().Use(group).Rows<A>();
        var eachOne = new List<(Entity, int)>();
        one.Each((Entity e, ref A a) => eachOne.Add((e, a.V)));

        var rowsOne = new List<(Entity, int)>();
        foreach (var row in one)
        {
            rowsOne.Add((row.Entity, row.C1.V));
        }

        Assert.Equal(100, eachOne.Count);
        Assert.Equal(eachOne, rowsOne);
    }

    [Fact]
    public void FiltersStillApply()
    {
        var registry = Build();
        var group = registry.Group().With<A>().With<B>().Build();
        var expected = Walk(registry.View().Without<Frozen>().WithTag<Marked>());

        var filtered = Walk(registry.View().Without<Frozen>().WithTag<Marked>().Use(group));

        Assert.NotEmpty(expected);
        Assert.Equal(expected.OrderBy(t => t.Item1.Index), filtered.OrderBy(t => t.Item1.Index));
    }

    // A pool the group doesn't require is probed.
    [Fact]
    public void ProbesPoolsTheGroupDoesNotRequire()
    {
        var registry = Build();
        var group = registry.Group().With<A>().With<B>().Build();
        var expected = new List<Entity>();
        registry.View().Each((Entity e, ref A _, ref B _, ref C _) => expected.Add(e));

        var three = new List<Entity>();
        registry.View().Use(group).Each((Entity e, ref A _, ref B _, ref C _) => three.Add(e));
        var threeRows = new List<Entity>();
        foreach (var row in registry.View().Use(group).Rows<A, B, C>())
        {
            threeRows.Add(row.Entity);
        }

        Assert.NotEmpty(expected);
        Assert.Equal(expected.OrderBy(e => e.Index), three.OrderBy(e => e.Index));
        Assert.Equal(three, threeRows);
    }

    [Fact]
    public void FollowsMembership()
    {
        var registry = Build();
        var group = registry.Group().With<A>().With<B>().Build();
        var view = registry.View().Use(group);
        var first = Walk(view)[0].Item1;
        registry.Remove<B>(first);

        var after = Walk(view);

        Assert.DoesNotContain(after, t => t.Item1 == first);
        Assert.Equal(99, after.Count);
    }

    // The member swapped into the current slot was already visited.
    [Fact]
    public void RemovingTheCurrentEntityMidWalk()
    {
        var registry = Build();
        var group = registry.Group().With<A>().With<B>().Build();
        var visited = new List<Entity>();
        foreach (var row in registry.View().Use(group).Rows<A, B>())
        {
            visited.Add(row.Entity);
            if (row.Entity.Index % 2 == 0)
            {
                registry.Remove<B>(row.Entity);
            }
        }

        Assert.Equal(100, visited.Count);
        Assert.Equal(100, visited.Distinct().Count());
    }

    // A later Use<T> drops the group pin.
    [Fact]
    public void LaterPinsReplaceIt()
    {
        var registry = Build();
        var group = registry.Group().With<A>().With<B>().Build();
        var expected = Walk(registry.View().Use<B>());

        var viaB = Walk(registry.View().Use(group).Use<B>());

        Assert.Equal(expected, viaB);
    }

    [Fact]
    public void RejectsAForeignGroup()
    {
        var registry = Build();
        var other = Build();
        var foreign = other.Group().With<A>().With<B>().Build();

        Assert.Throws<ArgumentException>(() => registry.View().Use(foreign));
    }

    [Fact]
    public void RejectsADisposedGroup()
    {
        var registry = Build();
        var group = registry.Group().With<A>().With<B>().Build();
        group.Dispose();

        Assert.Throws<ObjectDisposedException>(() => registry.View().Use(group));
    }

#if DEBUG
    // Joining mid-walk moves an unvisited member into the walk, so it throws as a pool does.
    [Fact]
    public void PullingAnEntityInMidWalkThrows()
    {
        var registry = Build();
        var group = registry.Group().With<A>().WithTag<Marked>().Build();
        Entity outside = default;
        registry.View().WithoutTag<Marked>().Each((Entity e, ref A _) => outside = e);

        Assert.Throws<InvalidOperationException>(() =>
        {
            foreach (var row in registry.View().Use(group).Rows<A>())
            {
                registry.Tag<Marked>(outside);
            }
        });
    }
#endif
}
