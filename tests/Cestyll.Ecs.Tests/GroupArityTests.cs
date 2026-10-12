using Cestyll.Ecs;
using Cestyll.Ecs.Groups;
using Cestyll.Ecs.Tags;

namespace Cestyll.Ecs.Tests;

public class GroupArityTests
{
    private struct C1 : IComponent { public int V; }
    private struct C2 : IComponent { public int V; }
    private struct C3 : IComponent { public int V; }
    private struct C4 : IComponent { public int V; }
    private struct C5 : IComponent { public int V; }
    private struct C6 : IComponent { public int V; }
    private struct C7 : IComponent { public int V; }
    private struct C8 : IComponent { public int V; }
    private struct Hidden : ITag { }

    private const int Count = 200;

    // Entity i lacks C(i % 9 + 1), so an arity-n group holds i % 9 >= n.
    private static (Registry, Entity[]) Build()
    {
        var registry = new Registry();
        var ents = new Entity[Count];
        for (var i = 0; i < Count; i++)
        {
            Entity e = registry.Create();
            ents[i] = e;
            var skip = i % 9 + 1;
            if (skip != 1) { registry.Add(e, new C1 { V = i * 10 + 1 }); }
            if (skip != 2) { registry.Add(e, new C2 { V = i * 10 + 2 }); }
            if (skip != 3) { registry.Add(e, new C3 { V = i * 10 + 3 }); }
            if (skip != 4) { registry.Add(e, new C4 { V = i * 10 + 4 }); }
            if (skip != 5) { registry.Add(e, new C5 { V = i * 10 + 5 }); }
            if (skip != 6) { registry.Add(e, new C6 { V = i * 10 + 6 }); }
            if (skip != 7) { registry.Add(e, new C7 { V = i * 10 + 7 }); }
            if (skip != 8) { registry.Add(e, new C8 { V = i * 10 + 8 }); }
            if (i % 4 == 0) { registry.Tag<Hidden>(e); }
        }

        return (registry, ents);
    }

    private static int[] Expected(int n, bool excludeHidden) =>
        [.. Enumerable.Range(0, Count).Where(i => i % 9 >= n && !(excludeHidden && i % 4 == 0))];

    // Every owned span must line up with Entities slot for slot.
    private static void AssertAligned(Entity[] ents, ReadOnlySpan<Entity> entities, int[] expected,
        params int[][] columns)
    {
        var seen = new List<int>();
        for (var s = 0; s < entities.Length; s++)
        {
            var i = Array.IndexOf(ents, entities[s]);
            seen.Add(i);
            for (var c = 0; c < columns.Length; c++)
            {
                Assert.Equal(i * 10 + c + 1, columns[c][s]);
            }
        }

        seen.Sort();
        Assert.Equal(expected, seen);
    }

    private static int[] Values<T>(Span<T> span, Func<T, int> v)
    {
        var a = new int[span.Length];
        for (var i = 0; i < span.Length; i++)
        {
            a[i] = v(span[i]);
        }

        return a;
    }

    [Fact]
    public void Group5_Aligns()
    {
        var (registry, ents) = Build();

        var g = registry.Group().Own<C1>().Own<C2>().Own<C3>().Own<C4>().Own<C5>().Build();

        AssertAligned(ents, g.Entities, Expected(5, false),
            Values(g.Span<C1>(), x => x.V), Values(g.Span<C2>(), x => x.V), Values(g.Span<C3>(), x => x.V),
            Values(g.Span<C4>(), x => x.V), Values(g.Span<C5>(), x => x.V));
    }

    [Fact]
    public void Group5_TracksChanges()
    {
        var (registry, ents) = Build();
        var g = registry.Group().Own<C1>().Own<C2>().Own<C3>().Own<C4>().Own<C5>().Build();

        registry.Remove<C3>(ents[8]);
        registry.Add(ents[5], new C6 { V = 56 }); // not owned: no change

        Assert.Equal(Expected(5, false).Length - 1, g.Count);
    }

    [Fact]
    public void Group6_AlignsWithTagRule()
    {
        var (registry, ents) = Build();

        var g = registry.Group().Own<C1>().Own<C2>().Own<C3>().Own<C4>().Own<C5>().Own<C6>().WithoutTag<Hidden>().Build();

        AssertAligned(ents, g.Entities, Expected(6, true),
            Values(g.Span<C1>(), x => x.V), Values(g.Span<C2>(), x => x.V), Values(g.Span<C3>(), x => x.V),
            Values(g.Span<C4>(), x => x.V), Values(g.Span<C5>(), x => x.V), Values(g.Span<C6>(), x => x.V));
    }

    [Fact]
    public void Group6_Untag_AddsTheMember()
    {
        var (registry, ents) = Build();
        var g = registry.Group().Own<C1>().Own<C2>().Own<C3>().Own<C4>().Own<C5>().Own<C6>().WithoutTag<Hidden>().Build();

        registry.Untag<Hidden>(ents[8]);

        Assert.Equal(Expected(6, true).Length + 1, g.Count);
    }

    // Requiring what another excludes: neither nests in the other.
    [Fact]
    public void Group_RequiringWhatAnotherExcludes_Throws()
    {
        var (registry, _) = Build();
        registry.Group().Own<C1>().Own<C2>().Own<C3>().Own<C4>().Own<C5>().Own<C6>().WithoutTag<Hidden>().Build();

        Assert.Throws<InvalidOperationException>(() => registry.Group().Own<C1>().Own<C2>().Own<C3>().Own<C4>().Own<C5>().Own<C6>().WithTag<Hidden>().Build());
    }

    [Fact]
    public void Group8_Aligns()
    {
        var (registry, ents) = Build();

        var g = registry.Group().Own<C1>().Own<C2>().Own<C3>().Own<C4>().Own<C5>().Own<C6>().Own<C7>().Own<C8>().Build();

        AssertAligned(ents, g.Entities, Expected(8, false),
            Values(g.Span<C1>(), x => x.V), Values(g.Span<C2>(), x => x.V), Values(g.Span<C3>(), x => x.V),
            Values(g.Span<C4>(), x => x.V), Values(g.Span<C5>(), x => x.V), Values(g.Span<C6>(), x => x.V),
            Values(g.Span<C7>(), x => x.V), Values(g.Span<C8>(), x => x.V));
    }

    [Fact]
    public void Group8_TracksAddAndDestroy()
    {
        var (registry, ents) = Build();
        var g = registry.Group().Own<C1>().Own<C2>().Own<C3>().Own<C4>().Own<C5>().Own<C6>().Own<C7>().Own<C8>().Build();
        var before = g.Count;

        registry.Add(ents[0], new C1 { V = 1 }); // entity 0 lacked only C1
        registry.Destroy(ents[8]);

        Assert.Equal(before, g.Count);
        Assert.Contains(ents[0], g.Entities.ToArray());
        Assert.DoesNotContain(ents[8], g.Entities.ToArray());
    }
}
