using Cestyll.Ecs;
using Cestyll.Ecs.Tags;
using Cestyll.Ecs.Views;

namespace Cestyll.Ecs.Tests;

public class ViewArityTests
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
    private struct Selected : ITag { }
    private struct Hidden : ITag { }

    private const int Count = 200;

    // Which pool is smallest, and so drives: C1, or the view's last type.
    public enum Layout { FirstSmallest, LastSmallest }

    private static bool Has(Layout layout, int i, int n) =>
        layout == Layout.FirstSmallest ? i % (n + 1) != 0 : i % (10 - n) != 0;

    private static int Value(int i, int n) => i * 10 + n;

    private static (Registry, Entity[]) Build(Layout layout)
    {
        var registry = new Registry();
        var ents = new Entity[Count];
        for (var i = 0; i < Count; i++)
        {
            Entity e = registry.Create();
            ents[i] = e;
            if (Has(layout, i, 1)) { registry.Add(e, new C1 { V = Value(i, 1) }); }
            if (Has(layout, i, 2)) { registry.Add(e, new C2 { V = Value(i, 2) }); }
            if (Has(layout, i, 3)) { registry.Add(e, new C3 { V = Value(i, 3) }); }
            if (Has(layout, i, 4)) { registry.Add(e, new C4 { V = Value(i, 4) }); }
            if (Has(layout, i, 5)) { registry.Add(e, new C5 { V = Value(i, 5) }); }
            if (Has(layout, i, 6)) { registry.Add(e, new C6 { V = Value(i, 6) }); }
            if (Has(layout, i, 7)) { registry.Add(e, new C7 { V = Value(i, 7) }); }
            if (Has(layout, i, 8)) { registry.Add(e, new C8 { V = Value(i, 8) }); }
            if (i % 3 == 0) { registry.Tag<Selected>(e); }
            if (i % 5 == 0) { registry.Tag<Hidden>(e); }
            if (i % 7 == 0) { registry.Add(e, new Frozen()); }
        }

        return (registry, ents);
    }

    // Entity i's index into ents, from its components' values.
    private static int SlotFromValue(int v, int n)
    {
        Assert.Equal(n, v % 10);
        return v / 10;
    }

    private static void Check(Layout layout, Entity[] ents, int arity, List<(Entity E, int[] Vs)> seen,
        Func<int, bool>? filter = null)
    {
        var expected = Enumerable.Range(0, Count)
            .Where(i => Enumerable.Range(1, arity).All(n => Has(layout, i, n)) && (filter?.Invoke(i) ?? true))
            .Select(i => ents[i])
            .OrderBy(e => e.Index)
            .ToList();
        Assert.Equal(expected, seen.Select(s => s.E).OrderBy(e => e.Index).ToList());

        foreach (var (e, vs) in seen)
        {
            Assert.Equal(arity, vs.Length);
            for (var n = 1; n <= arity; n++)
            {
                Assert.Equal(e, ents[SlotFromValue(vs[n - 1], n)]);
            }
        }
    }

    [Theory]
    [InlineData(Layout.FirstSmallest)]
    [InlineData(Layout.LastSmallest)]
    public void EveryArity_VisitsExactlyTheEntitiesWithAllTypes(Layout layout)
    {
        var (r, ents) = Build(layout);
        var s1 = new List<(Entity, int[])>();
        var s2 = new List<(Entity, int[])>();
        var s3 = new List<(Entity, int[])>();
        var s4 = new List<(Entity, int[])>();
        var s5 = new List<(Entity, int[])>();
        var s6 = new List<(Entity, int[])>();
        var s7 = new List<(Entity, int[])>();
        var s8 = new List<(Entity, int[])>();

        r.View().Each((Entity e, ref C1 a) => s1.Add((e, [a.V])));
        r.View().Each((Entity e, ref C1 a, ref C2 b) => s2.Add((e, [a.V, b.V])));
        r.View().Each((Entity e, ref C1 a, ref C2 b, ref C3 c) => s3.Add((e, [a.V, b.V, c.V])));
        r.View().Each((Entity e, ref C1 a, ref C2 b, ref C3 c, ref C4 d) =>
            s4.Add((e, [a.V, b.V, c.V, d.V])));
        r.View().Each((Entity e, ref C1 a, ref C2 b, ref C3 c, ref C4 d, ref C5 f) =>
            s5.Add((e, [a.V, b.V, c.V, d.V, f.V])));
        r.View().Each(
            (Entity e, ref C1 a, ref C2 b, ref C3 c, ref C4 d, ref C5 f, ref C6 g) =>
                s6.Add((e, [a.V, b.V, c.V, d.V, f.V, g.V])));
        r.View().Each(
            (Entity e, ref C1 a, ref C2 b, ref C3 c, ref C4 d, ref C5 f, ref C6 g, ref C7 h) =>
                s7.Add((e, [a.V, b.V, c.V, d.V, f.V, g.V, h.V])));
        r.View().Each(
            (Entity e, ref C1 a, ref C2 b, ref C3 c, ref C4 d, ref C5 f, ref C6 g, ref C7 h, ref C8 k) =>
                s8.Add((e, [a.V, b.V, c.V, d.V, f.V, g.V, h.V, k.V])));

        Check(layout, ents, 1, s1);
        Check(layout, ents, 2, s2);
        Check(layout, ents, 3, s3);
        Check(layout, ents, 4, s4);
        Check(layout, ents, 5, s5);
        Check(layout, ents, 6, s6);
        Check(layout, ents, 7, s7);
        Check(layout, ents, 8, s8);
    }

    [Theory]
    [InlineData(Layout.FirstSmallest)]
    [InlineData(Layout.LastSmallest)]
    public void Filters_ApplyAtEveryArity(Layout layout)
    {
        var (r, ents) = Build(layout);
        var tagged = new List<(Entity, int[])>();
        var excluded = new List<(Entity, int[])>();
        var masked = new List<(Entity, int[])>();
        var wide = new List<(Entity, int[])>();

        // One required tag: its words are the mask.
        r.View().WithTag<Selected>().Each((Entity e, ref C1 a) => tagged.Add((e, [a.V])));
        // Exclusions only: pool-driven with an exclude mask plus a component filter.
        r.View().WithoutTag<Hidden>().Without<Frozen>()
            .Each((Entity e, ref C1 a, ref C2 b, ref C3 c) => excluded.Add((e, [a.V, b.V, c.V])));
        // Several tag rules: a built mask.
        r.View().WithTag<Selected>().WithoutTag<Hidden>()
            .Each((Entity e, ref C1 a, ref C2 b, ref C3 c, ref C4 d, ref C5 f) =>
                masked.Add((e, [a.V, b.V, c.V, d.V, f.V])));
        r.View().WithTag<Selected>().With<Frozen>()
            .Each((Entity e, ref C1 a, ref C2 b, ref C3 c, ref C4 d, ref C5 f, ref C6 g, ref C7 h, ref C8 k) =>
                wide.Add((e, [a.V, b.V, c.V, d.V, f.V, g.V, h.V, k.V])));

        Check(layout, ents, 1, tagged, i => i % 3 == 0);
        Check(layout, ents, 3, excluded, i => i % 5 != 0 && i % 7 != 0);
        Check(layout, ents, 5, masked, i => i % 3 == 0 && i % 5 != 0);
        Check(layout, ents, 8, wide, i => i % 3 == 0 && i % 7 == 0);
    }

    [Theory]
    [InlineData(Layout.FirstSmallest)]
    [InlineData(Layout.LastSmallest)]
    public void PoolDriven_WhenTagIsLargerThanSmallestPool(Layout layout)
    {
        var (r, ents) = Build(layout);
        for (var i = 0; i < Count; i++)
        {
            if (i % 10 != 0)
            {
                r.Tag<Common>(ents[i]);
            }
        }

        var s = new List<(Entity, int[])>();
        r.View().WithTag<Common>()
            .Each((Entity e, ref C1 a, ref C2 b, ref C3 c) => s.Add((e, [a.V, b.V, c.V])));

        Check(layout, ents, 3, s, i => i % 10 != 0);
    }

    [Fact]
    public void Masked_WhenTagIsSmallerThanEveryPool()
    {
        var (r, ents) = Build(Layout.LastSmallest);
        var only = ents[1];
        r.Tag<Hidden>(only);
        var s = new List<(Entity, int[])>();

        r.View().WithTag<Hidden>().WithoutTag<Selected>()
            .Each((Entity e, ref C1 a, ref C2 b, ref C3 c, ref C4 d) => s.Add((e, [a.V, b.V, c.V, d.V])));

        Check(Layout.LastSmallest, ents, 4, s, i => (i % 5 == 0 || i == 1) && i % 3 != 0);
    }

    private struct Bump4 : IEach<Bump4, Entity, C1, C2, C3, C4>
    {
        public int Visits;

        public void Invoke(Entity e, ref C1 a, ref C2 b, ref C3 c, ref C4 d)
        {
            Visits++;
            a.V += 1000;
            d.V += 1000;
        }
    }

    [Fact]
    public void StructCallback_WritesThroughToEveryPool()
    {
        var (r, ents) = Build(Layout.LastSmallest);
        var fn = new Bump4();

        r.View().Each(ref fn);

        var matching = Enumerable.Range(0, Count)
            .Where(i => Enumerable.Range(1, 4).All(n => Has(Layout.LastSmallest, i, n)))
            .ToList();
        Assert.Equal(matching.Count, fn.Visits);
        foreach (var i in matching)
        {
            Assert.Equal(Value(i, 1) + 1000, r.Get<C1>(ents[i]).V);
            Assert.Equal(Value(i, 2), r.Get<C2>(ents[i]).V);
            Assert.Equal(Value(i, 4) + 1000, r.Get<C4>(ents[i]).V);
        }
    }

    [Theory]
    [InlineData(Layout.FirstSmallest)]
    [InlineData(Layout.LastSmallest)]
    public void RemovingFromCurrentEntity_VisitsEachOnce(Layout layout)
    {
        var (r, ents) = Build(layout);
        var s = new List<(Entity, int[])>();

        r.View().Each((Entity e, ref C1 a, ref C2 b, ref C3 c) =>
        {
            s.Add((e, [a.V, b.V, c.V]));
            r.Remove<C1>(e);
            r.Remove<C3>(e);
        });

        Check(layout, ents, 3, s);
    }

    [Fact]
    public void DestroyingCurrentEntity_VisitsEachOnce()
    {
        var (r, ents) = Build(Layout.FirstSmallest);
        var s = new List<(Entity, int[])>();

        r.View().Without<Frozen>().Each(
            (Entity e, ref C1 a, ref C2 b, ref C3 c, ref C4 d, ref C5 f, ref C6 g) =>
            {
                s.Add((e, [a.V, b.V, c.V, d.V, f.V, g.V]));
                r.Destroy(e);
            });

        Check(Layout.FirstSmallest, ents, 6, s, i => i % 7 != 0);
    }

    [Fact]
    public void RequiredTypeNobodyHas_VisitsNothing()
    {
        var (r, _) = Build(Layout.FirstSmallest);
        var visits = 0;

        r.View().WithTag<Ghost>().Each((Entity _, ref C1 _, ref C2 _, ref C3 _) => visits++);

        Assert.Equal(0, visits);
    }

    private struct Ghost : ITag { }
    private struct Common : ITag { }

    [Theory]
    [InlineData(Layout.FirstSmallest)]
    [InlineData(Layout.LastSmallest)]
    public void Foreach_EveryArity_VisitsExactlyTheEntitiesWithAllTypes(Layout layout)
    {
        var (r, ents) = Build(layout);
        var s1 = new List<(Entity, int[])>();
        var s2 = new List<(Entity, int[])>();
        var s3 = new List<(Entity, int[])>();
        var s4 = new List<(Entity, int[])>();
        var s5 = new List<(Entity, int[])>();
        var s6 = new List<(Entity, int[])>();
        var s7 = new List<(Entity, int[])>();
        var s8 = new List<(Entity, int[])>();

        foreach (var x in r.View().Rows<C1>())
        {
            s1.Add((x.Entity, [x.C1.V]));
        }

        foreach (var x in r.View().Rows<C1, C2>())
        {
            s2.Add((x.Entity, [x.C1.V, x.C2.V]));
        }

        foreach (var x in r.View().Rows<C1, C2, C3>())
        {
            s3.Add((x.Entity, [x.C1.V, x.C2.V, x.C3.V]));
        }

        foreach (var x in r.View().Rows<C1, C2, C3, C4>())
        {
            s4.Add((x.Entity, [x.C1.V, x.C2.V, x.C3.V, x.C4.V]));
        }

        foreach (var x in r.View().Rows<C1, C2, C3, C4, C5>())
        {
            s5.Add((x.Entity, [x.C1.V, x.C2.V, x.C3.V, x.C4.V, x.C5.V]));
        }

        foreach (var x in r.View().Rows<C1, C2, C3, C4, C5, C6>())
        {
            s6.Add((x.Entity, [x.C1.V, x.C2.V, x.C3.V, x.C4.V, x.C5.V, x.C6.V]));
        }

        foreach (var x in r.View().Rows<C1, C2, C3, C4, C5, C6, C7>())
        {
            s7.Add((x.Entity, [x.C1.V, x.C2.V, x.C3.V, x.C4.V, x.C5.V, x.C6.V, x.C7.V]));
        }

        foreach (var x in r.View().Rows<C1, C2, C3, C4, C5, C6, C7, C8>())
        {
            s8.Add((x.Entity, [x.C1.V, x.C2.V, x.C3.V, x.C4.V, x.C5.V, x.C6.V, x.C7.V, x.C8.V]));
        }

        Check(layout, ents, 1, s1);
        Check(layout, ents, 2, s2);
        Check(layout, ents, 3, s3);
        Check(layout, ents, 4, s4);
        Check(layout, ents, 5, s5);
        Check(layout, ents, 6, s6);
        Check(layout, ents, 7, s7);
        Check(layout, ents, 8, s8);
    }

    [Theory]
    [InlineData(Layout.FirstSmallest)]
    [InlineData(Layout.LastSmallest)]
    public void Foreach_FiltersApply(Layout layout)
    {
        var (r, ents) = Build(layout);
        var tagged = new List<(Entity, int[])>();
        var excluded = new List<(Entity, int[])>();
        var masked = new List<(Entity, int[])>();
        var wide = new List<(Entity, int[])>();

        // One required tag: its words are the mask.
        foreach (var x in r.View().WithTag<Selected>().Rows<C1>())
        {
            tagged.Add((x.Entity, [x.C1.V]));
        }

        // Exclusions only: pool-driven with an exclude mask plus a component filter.
        foreach (var x in r.View().WithoutTag<Hidden>().Without<Frozen>().Rows<C1, C2, C3>())
        {
            excluded.Add((x.Entity, [x.C1.V, x.C2.V, x.C3.V]));
        }

        // Several tag rules: a built mask.
        foreach (var x in r.View().WithTag<Selected>().WithoutTag<Hidden>().Rows<C1, C2, C3, C4, C5>())
        {
            masked.Add((x.Entity, [x.C1.V, x.C2.V, x.C3.V, x.C4.V, x.C5.V]));
        }

        foreach (var x in r.View().WithTag<Selected>().With<Frozen>().Rows<C1, C2, C3, C4, C5, C6, C7, C8>())
        {
            wide.Add((x.Entity, [x.C1.V, x.C2.V, x.C3.V, x.C4.V, x.C5.V, x.C6.V, x.C7.V, x.C8.V]));
        }

        Check(layout, ents, 1, tagged, i => i % 3 == 0);
        Check(layout, ents, 3, excluded, i => i % 5 != 0 && i % 7 != 0);
        Check(layout, ents, 5, masked, i => i % 3 == 0 && i % 5 != 0);
        Check(layout, ents, 8, wide, i => i % 3 == 0 && i % 7 == 0);
    }

    [Fact]
    public void Foreach_RequiredTypeNobodyHas_VisitsNothing()
    {
        var (r, _) = Build(Layout.FirstSmallest);
        var visits = 0;

        foreach (var x in r.View().WithTag<Ghost>().Rows<C1, C2, C3>())
        {
            visits++;
        }

        Assert.Equal(0, visits);
    }

    [Fact]
    public void Foreach_Masked_WhenTagIsSmallerThanEveryPool()
    {
        var (r, ents) = Build(Layout.LastSmallest);
        r.Tag<Hidden>(ents[1]);
        var s = new List<(Entity, int[])>();

        foreach (var x in r.View().WithTag<Hidden>().WithoutTag<Selected>().Without<Frozen>().Rows<C1, C2, C3, C4>())
        {
            s.Add((x.Entity, [x.C1.V, x.C2.V, x.C3.V, x.C4.V]));
        }

        Check(Layout.LastSmallest, ents, 4, s, i => (i % 5 == 0 || i == 1) && i % 3 != 0 && i % 7 != 0);
    }

    [Fact]
    public void Foreach_WritesThroughToEveryPool()
    {
        var (r, ents) = Build(Layout.LastSmallest);
        var visits = 0;

        foreach (var x in r.View().Rows<C1, C2, C3, C4>())
        {
            visits++;
            x.C1.V += 1000;
            x.C4.V += 1000;
        }

        var matching = Enumerable.Range(0, Count)
            .Where(i => Enumerable.Range(1, 4).All(n => Has(Layout.LastSmallest, i, n)))
            .ToList();
        Assert.Equal(matching.Count, visits);
        foreach (var i in matching)
        {
            Assert.Equal(Value(i, 1) + 1000, r.Get<C1>(ents[i]).V);
            Assert.Equal(Value(i, 2), r.Get<C2>(ents[i]).V);
            Assert.Equal(Value(i, 4) + 1000, r.Get<C4>(ents[i]).V);
        }
    }

    [Theory]
    [InlineData(Layout.FirstSmallest)]
    [InlineData(Layout.LastSmallest)]
    public void Foreach_RemovingFromCurrentEntity_VisitsEachOnce(Layout layout)
    {
        var (r, ents) = Build(layout);
        var s = new List<(Entity, int[])>();

        foreach (var x in r.View().Rows<C1, C2, C3>())
        {
            s.Add((x.Entity, [x.C1.V, x.C2.V, x.C3.V]));
            r.Remove<C1>(x.Entity);
            r.Remove<C3>(x.Entity);
        }

        Check(layout, ents, 3, s);
    }

    [Fact]
    public void Foreach_DestroyingCurrentEntity_VisitsEachOnce()
    {
        var (r, ents) = Build(Layout.LastSmallest);
        var s = new List<(Entity, int[])>();

        foreach (var x in r.View().WithTag<Selected>().Without<Frozen>().Rows<C1, C2, C3, C4, C5, C6>())
        {
            s.Add((x.Entity, [x.C1.V, x.C2.V, x.C3.V, x.C4.V, x.C5.V, x.C6.V]));
            r.Destroy(x.Entity);
        }

        Assert.NotEmpty(s);
        Check(Layout.LastSmallest, ents, 6, s, i => i % 3 == 0 && i % 7 != 0);
    }

    [Fact]
    public void Foreach_Nested_WalksIndependently()
    {
        var (r, _) = Build(Layout.FirstSmallest);
        var view = r.View().WithTag<Selected>().Without<Frozen>().Rows<C1, C4>();
        var outer = 0;
        var inner = 0;

        foreach (var a in view)
        {
            outer++;
            foreach (var b in view)
            {
                inner++;
            }
        }

        Assert.True(outer > 0);
        Assert.Equal(outer * outer, inner);
    }
}
