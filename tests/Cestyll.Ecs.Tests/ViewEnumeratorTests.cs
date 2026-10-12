using Cestyll.Ecs;
using Cestyll.Ecs.Tags;
using Cestyll.Ecs.Views;

namespace Cestyll.Ecs.Tests;

// foreach must match Each: same entities, order and refs, for every walk kind.
// Unfiltered walks use the enumerator's own loop, filtered ones the cursor.
public class ViewEnumeratorTests
{
    private struct C1 : IComponent { public int V; }
    private struct C2 : IComponent { public int V; }
    private struct C3 : IComponent { public int V; }
    private struct C4 : IComponent { public int V; }
    private struct C5 : IComponent { public int V; }
    private struct C6 : IComponent { public int V; }
    private struct C7 : IComponent { public int V; }
    private struct C8 : IComponent { public int V; }
    private struct Frozen : IComponent { }
    private struct Absent : IComponent { }
    private struct Selected : ITag { }
    private struct Hidden : ITag { }

    public enum Walk
    {
        Plain,        // unfiltered: the enumerator's own loop
        Pinned,       // Use<C2>: pinned, still unfiltered
        AbsentFilter, // Without a type nobody has: filtered in name, nothing to test
        Component,    // Without<Frozen>: the cursor's per-entity filter
        Tag,          // WithTag<Selected>: a mask, driven by pool or by tag bits
        NotTag,       // WithoutTag<Hidden>: an exclusion mask
    }

    // The smallest pool, and so the driver, differs by arity.
    private static Registry Build()
    {
        var registry = new Registry();
        for (var i = 0; i < 300; i++)
        {
            Entity e = registry.Create();
            if (i % 2 != 0) { registry.Add(e, new C1 { V = i * 10 + 1 }); }
            if (i % 3 != 0) { registry.Add(e, new C2 { V = i * 10 + 2 }); }
            if (i % 4 != 1) { registry.Add(e, new C3 { V = i * 10 + 3 }); }
            if (i % 5 != 2) { registry.Add(e, new C4 { V = i * 10 + 4 }); }
            if (i % 6 != 3) { registry.Add(e, new C5 { V = i * 10 + 5 }); }
            if (i % 7 != 4) { registry.Add(e, new C6 { V = i * 10 + 6 }); }
            if (i % 8 != 5) { registry.Add(e, new C7 { V = i * 10 + 7 }); }
            if (i % 9 != 6) { registry.Add(e, new C8 { V = i * 10 + 8 }); }
            if (i % 3 == 1) { registry.Tag<Selected>(e); }
            if (i % 5 == 0) { registry.Tag<Hidden>(e); }
            if (i % 7 == 0) { registry.Add(e, new Frozen()); }
        }

        // Destroy a few, so the pools' packed order isn't creation order.
        var doomed = new List<Entity>();
        registry.View().Each((Entity e, ref C1 _) =>
        {
            if (doomed.Count * 11 <= e.Index)
            {
                doomed.Add(e);
            }
        });
        foreach (var e in doomed)
        {
            registry.Destroy(e);
        }

        return registry;
    }

    private static View<Entity> Filter(View<Entity> v, Walk walk) => walk switch
    {
        Walk.Plain or Walk.Pinned => v,
        Walk.AbsentFilter => v.Without<Absent>(),
        Walk.Component => v.Without<Frozen>(),
        Walk.Tag => v.WithTag<Selected>(),
        _ => v.WithoutTag<Hidden>(),
    };

    private static View<Entity> Filter<TPin>(View<Entity> v, Walk walk)
        where TPin : IComponent => walk == Walk.Pinned ? v.Use<TPin>() : Filter(v, walk);

    [Theory]
    [InlineData(Walk.Plain)]
    [InlineData(Walk.Pinned)]
    [InlineData(Walk.AbsentFilter)]
    [InlineData(Walk.Component)]
    [InlineData(Walk.Tag)]
    [InlineData(Walk.NotTag)]
    public void OneComponent(Walk walk)
    {
        var registry = Build();
        var view = Filter(registry.View(), walk).Rows<C1>();

        var each = new List<(Entity, int)>();
        view.Each((Entity e, ref C1 c1) => each.Add((e, c1.V)));

        var rows = new List<(Entity, int)>();
        foreach (var row in view)
        {
            rows.Add((row.Entity, row.C1.V));
            row.C1.V += 100000; // the ref is the stored component
        }

        Assert.NotEmpty(each);
        Assert.Equal(each, rows);
        foreach (var (e, v) in each)
        {
            Assert.Equal(v + 100000, registry.Get<C1>(e).V);
        }
    }

    [Theory]
    [InlineData(Walk.Plain)]
    [InlineData(Walk.Pinned)]
    [InlineData(Walk.AbsentFilter)]
    [InlineData(Walk.Component)]
    [InlineData(Walk.Tag)]
    [InlineData(Walk.NotTag)]
    public void TwoComponents(Walk walk)
    {
        var registry = Build();
        var view = Filter<C2>(registry.View(), walk).Rows<C1, C2>();

        var each = new List<(Entity, int, int)>();
        view.Each((Entity e, ref C1 c1, ref C2 c2) => each.Add((e, c1.V, c2.V)));

        var rows = new List<(Entity, int, int)>();
        foreach (var row in view)
        {
            rows.Add((row.Entity, row.C1.V, row.C2.V));
            row.C2.V += 100000;
        }

        Assert.NotEmpty(each);
        Assert.Equal(each, rows);
        foreach (var (e, _, v) in each)
        {
            Assert.Equal(v + 100000, registry.Get<C2>(e).V);
        }
    }

    [Theory]
    [InlineData(Walk.Plain)]
    [InlineData(Walk.Pinned)]
    [InlineData(Walk.AbsentFilter)]
    [InlineData(Walk.Component)]
    [InlineData(Walk.Tag)]
    [InlineData(Walk.NotTag)]
    public void ThreeComponents(Walk walk)
    {
        var registry = Build();
        var view = Filter<C2>(registry.View(), walk).Rows<C1, C2, C3>();

        var each = new List<(Entity, int, int, int)>();
        view.Each((Entity e, ref C1 c1, ref C2 c2, ref C3 c3) => each.Add((e, c1.V, c2.V, c3.V)));

        var rows = new List<(Entity, int, int, int)>();
        foreach (var row in view)
        {
            rows.Add((row.Entity, row.C1.V, row.C2.V, row.C3.V));
            row.C3.V += 100000;
        }

        Assert.NotEmpty(each);
        Assert.Equal(each, rows);
        foreach (var (e, _, _, v) in each)
        {
            Assert.Equal(v + 100000, registry.Get<C3>(e).V);
        }
    }

    // The widest view, driven from its smallest pool (C2) unless a filter says otherwise.
    [Theory]
    [InlineData(Walk.Plain)]
    [InlineData(Walk.Pinned)]
    [InlineData(Walk.AbsentFilter)]
    [InlineData(Walk.Component)]
    [InlineData(Walk.Tag)]
    [InlineData(Walk.NotTag)]
    public void EightComponents(Walk walk)
    {
        var registry = Build();
        var view = Filter<C8>(registry.View(), walk).Rows<C1, C2, C3, C4, C5, C6, C7, C8>();

        var each = new List<(Entity, int)>();
        view.Each((Entity e, ref C1 c1, ref C2 c2, ref C3 c3, ref C4 c4, ref C5 c5, ref C6 c6, ref C7 c7, ref C8 c8) =>
            each.Add((e, c1.V + c2.V + c3.V + c4.V + c5.V + c6.V + c7.V + c8.V)));

        var rows = new List<(Entity, int)>();
        foreach (var row in view)
        {
            rows.Add((row.Entity,
                row.C1.V + row.C2.V + row.C3.V + row.C4.V + row.C5.V + row.C6.V + row.C7.V + row.C8.V));
        }

        Assert.NotEmpty(each);
        Assert.Equal(each, rows);
    }

    // Breaking out and walking again starts over.
    [Fact]
    public void RestartsAfterBreak()
    {
        var registry = Build();
        var view = registry.View().Rows<C1, C2>();
        var first = Entity.Null;
        foreach (var row in view)
        {
            first = row.Entity;
            break;
        }

        var again = Entity.Null;
        foreach (var row in view)
        {
            again = row.Entity;
            break;
        }

        Assert.Equal(first, again);
    }

    [Fact]
    public void EmptyViewYieldsNothing()
    {
        var registry = Build();
        var none = 0;

        foreach (var row in registry.View().Rows<C1, Absent>())
        {
            none++;
        }

        foreach (var row in registry.View().With<Absent>().Rows<C1>())
        {
            none++;
        }

        Assert.Equal(0, none);
    }
}
