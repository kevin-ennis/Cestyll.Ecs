using Cestyll.Ecs;
using Cestyll.Ecs.Groups;
using Cestyll.Ecs.Tags;

namespace Cestyll.Ecs.Tests;

public class ViewDriverTests
{
    private struct A : IComponent { public int V; }
    private struct B : IComponent { public int V; }
    private struct C : IComponent { public int V; }
    private struct Marked : ITag;
    private struct Hidden : ITag;

    private static int Scramble(int i) => (i * 37 + 11) % 101;

    // A sorted by value, B the smallest pool. Grouped: a group owns A and B and sorts them.
    private static Registry Build() => Build(grouped: false).Registry;

    private static (Registry Registry, Group<Entity>? Group) Build(bool grouped)
    {
        var registry = new Registry();
        for (var i = 0; i < 100; i++)
        {
            Entity e = registry.Create();
            registry.Add(e, new A { V = Scramble(i) });
            if (i % 3 == 0)
            {
                registry.Add(e, new B { V = i });
            }

            if (i % 2 == 0)
            {
                registry.Add(e, new C { V = i });
            }

            if (i % 7 == 0)
            {
                registry.Tag<Marked>(e);
            }

            if (i % 5 == 0)
            {
                registry.Tag<Hidden>(e);
            }
        }

        if (!grouped)
        {
            registry.Sort<A>((x, y) => x.V.CompareTo(y.V));
            return (registry, null);
        }

        var group = registry.Group().Own<A>().Own<B>().Build();
        group.Sort<A>((x, y) => x.V.CompareTo(y.V));
        return (registry, group);
    }

    // A's walk order, kept to the entities that pass.
    private static List<Entity> AOrder(Registry registry, Func<Entity, bool> pass)
    {
        var seen = new List<Entity>();
        registry.View().Each((Entity e, ref A _) => seen.Add(e));
        return [.. seen.Where(pass)];
    }

    private static List<Entity> Each<T1, T2>(Cestyll.Ecs.Views.ViewRows<Entity, T1, T2> view)
        where T1 : IComponent
        where T2 : IComponent
    {
        var seen = new List<Entity>();
        view.Each((Entity e, ref T1 _, ref T2 _) => seen.Add(e));
        return seen;
    }

    private static List<Entity> Foreach<T1, T2>(Cestyll.Ecs.Views.ViewRows<Entity, T1, T2> view)
        where T1 : IComponent
        where T2 : IComponent
    {
        var seen = new List<Entity>();
        foreach (var row in view)
        {
            seen.Add(row.Entity);
        }

        return seen;
    }

    [Fact]
    public void Use_FollowsPinnedPoolOrder()
    {
        var registry = Build();
        var expected = AOrder(registry, e => registry.Has<B>(e));

        var view = registry.View().Use<A>().Rows<A, B>();

        Assert.Equal(expected, Each(view));
        Assert.Equal(expected, Foreach(view));

        // Unpinned, the smaller B drives, which is not A's order.
        Assert.NotEqual(expected, Each(registry.View().Rows<A, B>()));
    }

    // Marked is smaller than either pool, so an unpinned view walks its bits instead.
    [Fact]
    public void Use_FollowsPinnedPoolOrder_WithRequiredTag()
    {
        var registry = Build();
        var marked = registry.View().WithTag<Marked>().Use<A>().Rows<A, B>();
        var expected = AOrder(registry, e => registry.Has<B>(e) && registry.HasTag<Marked>(e));

        var each = Each(marked);
        var rows = Foreach(marked);

        Assert.NotEmpty(expected);
        Assert.Equal(expected, each);
        Assert.Equal(expected, rows);
    }

    [Fact]
    public void Use_FollowsPinnedPoolOrder_WithMixedTags()
    {
        var registry = Build();
        var mixed = registry.View().Use<A>().WithTag<Marked>().WithoutTag<Hidden>().Rows<A, B>();
        var expected = AOrder(registry,
            e => registry.Has<B>(e) && registry.HasTag<Marked>(e) && !registry.HasTag<Hidden>(e));

        var each = Each(mixed);
        var rows = Foreach(mixed);

        Assert.Equal(expected, each);
        Assert.Equal(expected, rows);
    }

    [Fact]
    public void Use_FollowsPinnedPoolOrder_WithExcludedTag()
    {
        var registry = Build();
        var excluded = registry.View().WithoutTag<Hidden>().Use<A>().Rows<A, B>();
        var expected = AOrder(registry, e => registry.Has<B>(e) && !registry.HasTag<Hidden>(e));

        var each = Each(excluded);
        var rows = Foreach(excluded);

        Assert.Equal(expected, each);
        Assert.Equal(expected, rows);
    }

    [Fact]
    public void Use_FollowsPinnedPoolOrder_WithComponentFilter()
    {
        var registry = Build();
        var component = registry.View().With<C>().Use<A>().Rows<A, B>();
        var expected = AOrder(registry, e => registry.Has<B>(e) && registry.Has<C>(e));

        var each = Each(component);
        var rows = Foreach(component);

        Assert.Equal(expected, each);
        Assert.Equal(expected, rows);
    }

    [Fact]
    public void Use_PinsAnyPosition()
    {
        var registry = Build();
        var expected = AOrder(registry, e => registry.Has<B>(e) && registry.Has<C>(e));

        var each = new List<Entity>();
        registry.View().Use<A>().Each((Entity e, ref B _, ref C _, ref A _) => each.Add(e));
        var rows = new List<Entity>();
        foreach (var row in registry.View().Use<A>().Rows<C, A, B>())
        {
            rows.Add(row.Entity);
        }

        Assert.Equal(expected, each);
        Assert.Equal(expected, rows);
    }

    [Fact]
    public void Use_SinglePoolKeepsOrderUnderTagMask()
    {
        var registry = Build();
        var expected = AOrder(registry, e => registry.HasTag<Marked>(e));

        var seen = new List<Entity>();
        registry.View().WithTag<Marked>().Use<A>().Each((Entity e, ref A _) => seen.Add(e));

        Assert.Equal(expected, seen);
    }

    [Fact]
    public void Use_GroupOwnedPoolWalksPrefixInReverse()
    {
        var (registry, group) = Build(grouped: true);
        var expected = group!.Entities.ToArray().Reverse().ToList();

        // Every A + B entity is in the prefix, so either owned pool walks it from the end.
        var viaA = Each(registry.View().Use<A>().Rows<A, B>());
        var viaB = Foreach(registry.View().Use<B>().Rows<A, B>());
        var withC = new List<Entity>();
        registry.View().Use<A>().Each((Entity e, ref A _, ref B _, ref C _) => withC.Add(e));

        Assert.Equal(expected, viaA);
        Assert.Equal(expected, viaB);
        Assert.Equal([.. expected.Where(e => registry.Has<C>(e))], withC);
    }

    public enum Mutation { RemovePinned, RemoveOther, Destroy }

    public static TheoryData<bool, bool, bool, Mutation> MutationCases()
    {
        var data = new TheoryData<bool, bool, bool, Mutation>();
        foreach (var grouped in new[] { false, true })
        {
            foreach (var filtered in new[] { false, true })
            {
                foreach (var useForeach in new[] { false, true })
                {
                    foreach (var m in Enum.GetValues<Mutation>())
                    {
                        data.Add(grouped, filtered, useForeach, m);
                    }
                }
            }
        }

        return data;
    }

    // Pinned to a larger or group-owned pool, each entity is still visited once, in its order.
    [Theory]
    [MemberData(nameof(MutationCases))]
    public void Use_CurrentEntityMutationsDuringWalk(bool grouped, bool filtered, bool useForeach, Mutation m)
    {
        var (registry, group) = Build(grouped);
        var view = filtered ? registry.View().WithTag<Marked>().Use<A>().Rows<A, B>() : registry.View().Use<A>().Rows<A, B>();
        var expected = AOrder(registry, e => registry.Has<B>(e) && (!filtered || registry.HasTag<Marked>(e)));
        var removed = new List<bool>();

        void Mutate(Entity e)
        {
            switch (m)
            {
                case Mutation.RemovePinned:
                    removed.Add(registry.Remove<A>(e));
                    break;
                case Mutation.RemoveOther:
                    removed.Add(registry.Remove<B>(e));
                    break;
                default:
                    registry.Destroy(e);
                    break;
            }
        }

        var seen = new List<Entity>();
        if (useForeach)
        {
            foreach (var row in view)
            {
                seen.Add(row.Entity);
                Mutate(row.Entity);
            }
        }
        else
        {
            view.Each((Entity e, ref A _, ref B _) =>
            {
                seen.Add(e);
                Mutate(e);
            });
        }

        Assert.Equal(expected, seen);
        Assert.DoesNotContain(false, removed);
        Assert.Empty(Each(view));
        foreach (var e in expected)
        {
            Assert.Equal(m == Mutation.Destroy, !registry.IsAlive(e));
            Assert.Equal(m == Mutation.RemoveOther, registry.Has<A>(e));
            Assert.Equal(m == Mutation.RemovePinned, registry.Has<B>(e));
        }

        // Lookups still agree with the packed arrays, and the group kept its alignment.
        Cestyll.Ecs.Storage.SparseSet<Entity>[] pools = [registry.Storage<A>(), registry.Storage<B>()];
        foreach (var pool in pools)
        {
            for (var s = 0; s < pool.Count; s++)
            {
                Assert.Equal(s, pool.IndexOf(pool.Entities[s]));
            }
        }

        if (group is not null)
        {
            var both = registry.Storage<A>().Entities.ToArray()
                .Where(e => registry.Has<B>(e)).OrderBy(e => e.Index).ToList();
            Assert.Equal(both, group.Entities.ToArray().OrderBy(e => e.Index).ToList());
            Assert.Equal(group.Entities.ToArray(), registry.Storage<B>().Entities[..group.Count].ToArray());
        }
    }
}
