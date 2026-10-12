using Cestyll.Ecs;
using Cestyll.Ecs.Tags;

namespace Cestyll.Ecs.Tests;

// The widest form of each entry point; the narrower ones share its code.
public class ArityTests
{
    private struct C1 : IComponent { public int V; }
    private struct C2 : IComponent { public int V; }
    private struct C3 : IComponent { public int V; }
    private struct C4 : IComponent { public int V; }
    private struct C5 : IComponent { public int V; }
    private struct C6 : IComponent { public int V; }
    private struct C7 : IComponent { public int V; }
    private struct C8 : IComponent { public int V; }
    private struct C9 : IComponent { public int V; }
    private struct C10 : IComponent { public int V; }
    private struct C11 : IComponent { public int V; }
    private struct C12 : IComponent { public int V; }
    private struct C13 : IComponent { public int V; }
    private struct C14 : IComponent { public int V; }
    private struct C15 : IComponent { public int V; }
    private struct C16 : IComponent { public int V; }

    private struct G1 : ITag;
    private struct G2 : ITag;
    private struct G3 : ITag;
    private struct G4 : ITag;
    private struct G5 : ITag;
    private struct G6 : ITag;
    private struct G7 : ITag;
    private struct G8 : ITag;
    private struct G9 : ITag;
    private struct G10 : ITag;
    private struct G11 : ITag;
    private struct G12 : ITag;
    private struct G13 : ITag;
    private struct G14 : ITag;
    private struct G15 : ITag;
    private struct G16 : ITag;

    private static Entity CreateSixteen(Registry registry, int v) =>
        registry.Create(new C1 { V = v }, new C2 { V = v }, new C3 { V = v }, new C4 { V = v }, new C5 { V = v }, new C6 { V = v }, new C7 { V = v }, new C8 { V = v }, new C9 { V = v }, new C10 { V = v }, new C11 { V = v }, new C12 { V = v }, new C13 { V = v }, new C14 { V = v }, new C15 { V = v }, new C16 { V = v });

    [Fact]
    public void View_EachAndForeach_FetchSixteen()
    {
        var registry = new Registry();
        var e = CreateSixteen(registry, 1);
        registry.Create(new C1 { V = 9 });

        registry.View().Each(
            (Entity _, ref C1 c1, ref C2 c2, ref C3 c3, ref C4 c4, ref C5 c5, ref C6 c6, ref C7 c7, ref C8 c8, ref C9 c9, ref C10 c10, ref C11 c11, ref C12 c12, ref C13 c13, ref C14 c14, ref C15 c15, ref C16 c16) =>
            {
                c1.V += c16.V;
                c16.V++;
            });

        var rows = new List<(Entity Entity, int C1, int C16, int GetC16)>();
        foreach (var row in registry.View().Rows<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16>())
        {
            rows.Add((row.Entity, row.C1.V, row.C16.V, row.Get<C16>().V));
        }

        Assert.Equal((e, 2, 2, 2), Assert.Single(rows));
    }

    [Fact]
    public void OwningGroup_Each_FetchesSixteen()
    {
        var registry = new Registry();
        var group = registry.Group().Own<C1>().Own<C2>().Own<C3>().Own<C4>().Own<C5>().Own<C6>().Own<C7>().Own<C8>().Own<C9>().Own<C10>().Own<C11>().Own<C12>().Own<C13>().Own<C14>().Own<C15>().Own<C16>().Build();
        for (var i = 0; i < 10; i++)
        {
            CreateSixteen(registry, i);
        }

        group.Each((Entity _, ref C1 c1, ref C2 c2, ref C3 c3, ref C4 c4, ref C5 c5, ref C6 c6, ref C7 c7, ref C8 c8, ref C9 c9, ref C10 c10, ref C11 c11, ref C12 c12, ref C13 c13, ref C14 c14, ref C15 c15, ref C16 c16) => c16.V += c1.V);

        Assert.Equal(10, group.Count);
        foreach (var e in group.Entities)
        {
            Assert.Equal(2 * registry.Get<C8>(e).V, registry.Get<C16>(e).V);
        }
    }

    [Fact]
    public void OwningGroup_Vectors_CoverSixteen()
    {
        var registry = new Registry();
        var group = registry.Group().Own<C1>().Own<C2>().Own<C3>().Own<C4>().Own<C5>().Own<C6>().Own<C7>().Own<C8>().Own<C9>().Own<C10>().Own<C11>().Own<C12>().Own<C13>().Own<C14>().Own<C15>().Own<C16>().Build();
        for (var i = 0; i < 10; i++)
        {
            CreateSixteen(registry, i);
        }

        foreach (var v in group.Rows<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16>().Vectors256(Lanes.Int))
        {
            v.V1 += v.V16;
        }

        Assert.Equal(10, group.Count);
        foreach (var e in group.Entities)
        {
            Assert.Equal(2 * registry.Get<C8>(e).V, registry.Get<C1>(e).V);
        }
    }

    [Fact]
    public void NonOwningGroup_Each_FetchesSixteen()
    {
        var registry = new Registry();
        var group = registry.Group().With<C1>().With<C2>().With<C3>().With<C4>().With<C5>().With<C6>().With<C7>().With<C8>().With<C9>().With<C10>().With<C11>().With<C12>().With<C13>().With<C14>().With<C15>().With<C16>().Build();
        var e = CreateSixteen(registry, 4);
        registry.Create(new C16());

        var seen = 0;
        group.Each((Entity _, ref C1 c1, ref C2 c2, ref C3 c3, ref C4 c4, ref C5 c5, ref C6 c6, ref C7 c7, ref C8 c8, ref C9 c9, ref C10 c10, ref C11 c11, ref C12 c12, ref C13 c13, ref C14 c14, ref C15 c15, ref C16 c16) =>
        {
            c16.V += c1.V;
            seen++;
        });

        Assert.Equal(1, seen);
        Assert.Equal(8, registry.Get<C16>(e).V);
    }

    [Fact]
    public void CreateMany_TakesSixteen()
    {
        var registry = new Registry();

        var entities = registry.CreateMany(3, new C1 { V = 1 }, new C2 { V = 2 }, new C3 { V = 3 }, new C4 { V = 4 }, new C5 { V = 5 }, new C6 { V = 6 }, new C7 { V = 7 }, new C8 { V = 8 }, new C9 { V = 9 }, new C10 { V = 10 }, new C11 { V = 11 }, new C12 { V = 12 }, new C13 { V = 13 }, new C14 { V = 14 }, new C15 { V = 15 }, new C16 { V = 16 });

        Assert.All(entities, e => Assert.True(registry.HasAll<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16>(e)));
        Assert.All(entities, e => Assert.Equal(16, registry.Get<C16>(e).V));
    }

    [Fact]
    public void HasAllAndHasAny_TakeSixteen()
    {
        var registry = new Registry();
        var lone = registry.Create(new C16());

        var hasAll = registry.HasAll<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16>(lone);
        var hasAny = registry.HasAny<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16>(lone);
        var handleHasAny = registry.Handle(lone).HasAny<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16>();
        var handleHasAll = registry.Handle(lone).HasAll<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16>();

        Assert.False(hasAll);
        Assert.True(hasAny);
        Assert.True(handleHasAny);
        Assert.False(handleHasAll);
    }

    [Fact]
    public void TagChecks_TakeSixteen()
    {
        var registry = new Registry();
        var full = TagSixteen(registry);
        var partial = TagSixteen(registry);
        registry.Untag<G8>(partial);

        var fullHasAll = registry.HasAllTags<G1, G2, G3, G4, G5, G6, G7, G8, G9, G10, G11, G12, G13, G14, G15, G16>(full);
        var fullHandleHasAll = registry.Handle(full).HasAllTags<G1, G2, G3, G4, G5, G6, G7, G8, G9, G10, G11, G12, G13, G14, G15, G16>();
        var partialHasAll = registry.HasAllTags<G1, G2, G3, G4, G5, G6, G7, G8, G9, G10, G11, G12, G13, G14, G15, G16>(partial);
        var partialHasAny = registry.HasAnyTags<G1, G2, G3, G4, G5, G6, G7, G8, G9, G10, G11, G12, G13, G14, G15, G16>(partial);
        var partialHandleHasAny = registry.Handle(partial).HasAnyTags<G1, G2, G3, G4, G5, G6, G7, G8, G9, G10, G11, G12, G13, G14, G15, G16>();

        Assert.True(fullHasAll);
        Assert.True(fullHandleHasAll);
        Assert.False(partialHasAll);
        Assert.True(partialHasAny);
        Assert.True(partialHandleHasAny);
    }

    private static Entity TagSixteen(Registry registry)
    {
        Entity e = registry.Create();
        registry.Tag<G1>(e);
        registry.Tag<G2>(e);
        registry.Tag<G3>(e);
        registry.Tag<G4>(e);
        registry.Tag<G5>(e);
        registry.Tag<G6>(e);
        registry.Tag<G7>(e);
        registry.Tag<G8>(e);
        registry.Tag<G9>(e);
        registry.Tag<G10>(e);
        registry.Tag<G11>(e);
        registry.Tag<G12>(e);
        registry.Tag<G13>(e);
        registry.Tag<G14>(e);
        registry.Tag<G15>(e);
        registry.Tag<G16>(e);
        return e;
    }

    [Fact]
    public void Create_Deferred_TakesSixteen()
    {
        var registry = new Registry();

        Entity deferred;
        using (registry.Defer())
        {
            deferred = CreateSixteen(registry, 5);
        }

        Assert.True(registry.HasAll<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16>(deferred));
        Assert.Equal(5, registry.Get<C16>(deferred).V);
    }

    [Fact]
    public void CommandBufferCreate_TakesSixteen()
    {
        var registry = new Registry();
        var commands = registry.CommandBuffer();
        var recorded = commands.Create(new C1 { V = 6 }, new C2 { V = 6 }, new C3 { V = 6 }, new C4 { V = 6 }, new C5 { V = 6 }, new C6 { V = 6 }, new C7 { V = 6 }, new C8 { V = 6 }, new C9 { V = 6 }, new C10 { V = 6 }, new C11 { V = 6 }, new C12 { V = 6 }, new C13 { V = 6 }, new C14 { V = 6 }, new C15 { V = 6 }, new C16 { V = 6 }).Entity;

        commands.Flush();

        Assert.True(registry.HasAll<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16>(recorded));
        Assert.Equal(6, registry.Get<C16>(recorded).V);
    }
}
