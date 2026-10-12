using Cestyll.Ecs;
using Cestyll.Ecs.Groups;
using Cestyll.Ecs.Tags;
using Cestyll.Ecs.Views;

namespace Cestyll.Ecs.Tests;

// Groups from registry.Group()...Build(), read by type.
public class FluentGroupTests
{
    private struct Pos : IComponent { public int X; }
    private struct Vel : IComponent { public int X; }
    private struct Hp : IComponent { }
    private struct Frozen : ITag { }

    private struct Move : IEach<Move, Entity, Pos, Vel>
    {
        public int Count;

        public void Invoke(Entity e, ref Pos p, ref Vel v)
        {
            p.X += v.X;
            Count++;
        }
    }

    [Fact]
    public void Own_PacksMembers_AndSpansLineUp()
    {
        var registry = new Registry();
        registry.Create(new Pos { X = 1 });
        Entity a = registry.Create(new Pos { X = 2 }, new Vel { X = 20 });
        Entity b = registry.Create(new Pos { X = 3 }, new Vel { X = 30 });
        registry.Tag<Frozen>(b);

        var group = registry.Group().Own<Pos>().Own<Vel>().WithoutTag<Frozen>().Build();

        Assert.True(group.Owning);
        Assert.Equal([a], group.Entities.ToArray());
        Assert.Equal(2, group.Span<Pos>()[0].X);
        Assert.Equal(20, group.Span<Vel>()[0].X);
    }

    [Fact]
    public void Span_OnATypeTheGroupDoesNotOwn_Throws()
    {
        var registry = new Registry();
        registry.Create(new Pos(), new Vel());
        var group = registry.Group().Own<Pos>().Own<Vel>().Build();

        Assert.Throws<InvalidOperationException>(() => group.Span<Hp>());
    }

    [Fact]
    public void AskingAgain_InAnyOrder_ReturnsTheSameGroup()
    {
        var registry = new Registry();
        var group = registry.Group().Own<Pos>().Own<Vel>().WithoutTag<Frozen>().Build();

        Assert.Same(group, registry.Group().WithoutTag<Frozen>().Own<Vel>().Own<Pos>().Build());
        Assert.NotSame(group, registry.Group().Own<Pos>().Own<Vel>().WithoutTag<Frozen>().With<Hp>().Build());
    }

    [Fact]
    public void Each_DelegateAndStruct_WalkTheMembers()
    {
        var registry = new Registry();
        Entity a = registry.Create(new Pos(), new Vel { X = 2 });
        Entity b = registry.Create(new Pos(), new Vel { X = 3 });
        registry.Create(new Pos());
        var group = registry.Group().Own<Pos>().Own<Vel>().Build();

        var seen = new List<Entity>();
        group.Each((Entity e, ref Vel v, ref Pos p) => seen.Add(e));
        var move = new Move();
        group.Each(ref move);

        Assert.Equal([a, b], seen.OrderBy(e => e.Index));
        Assert.Equal(2, move.Count);
        Assert.Equal(2, registry.Get<Pos>(a).X);
        Assert.Equal(3, registry.Get<Pos>(b).X);
    }

    [Fact]
    public void Each_MayDestroyTheCurrentEntity()
    {
        var registry = new Registry();
        for (var i = 0; i < 5; i++)
        {
            registry.Create(new Pos { X = i }, new Vel());
        }

        var group = registry.Group().Own<Pos>().Own<Vel>().Build();
        var seen = 0;
        group.Each((Entity e, ref Pos p) =>
        {
            seen++;
            if (p.X % 2 == 0)
            {
                registry.Destroy(e);
            }
        });

        Assert.Equal(5, seen);
        Assert.Equal(2, group.Count);
    }

    [Fact]
    public void NoOwn_IsNonOwning_AndEachLooksComponentsUp()
    {
        var registry = new Registry();
        Entity a = registry.Create(new Pos(), new Vel { X = 4 });
        Entity b = registry.Create(new Pos(), new Vel { X = 5 }, new Hp());
        registry.Create(new Pos());
        var group = registry.Group().With<Pos>().With<Vel>().Without<Hp>().Build();

        group.Each((Entity e, ref Pos p, ref Vel v) => p.X += v.X);
        var move = new Move();
        group.Each(ref move);

        Assert.False(group.Owning);
        Assert.Equal([a], group.Entities.ToArray());
        Assert.True(group.Contains(a));
        Assert.False(group.Contains(b));
        Assert.Equal(1, move.Count);
        Assert.Equal(8, registry.Get<Pos>(a).X);
        Assert.Equal(0, registry.Get<Pos>(b).X);
    }

    [Fact]
    public void NonOwning_Span_Throws()
    {
        var registry = new Registry();
        registry.Create(new Pos(), new Vel());
        var group = registry.Group().With<Pos>().With<Vel>().Without<Hp>().Build();

        Assert.Throws<InvalidOperationException>(() => group.Span<Pos>());
    }

    [Fact]
    public void NonOwning_AskingAgain_ReturnsTheSameGroup()
    {
        var registry = new Registry();
        var group = registry.Group().With<Pos>().With<Vel>().Without<Hp>().Build();

        var again = registry.Group().Without<Hp>().With<Vel>().With<Pos>().Build();

        Assert.Same(group, again);
    }

    // Throws rather than skipping members that lack the type, as an owning group does.
    [Fact]
    public void NonOwningEach_OnATypeTheGroupDoesNotRequire_Throws()
    {
        var registry = new Registry();
        registry.Create(new Pos(), new Vel());
        registry.Create(new Pos());
        var group = registry.Group().With<Pos>().Build();

        Assert.Throws<InvalidOperationException>(() => group.Each((Entity e, ref Pos p, ref Vel v) => { }));
        var move = new Move();
        Assert.Throws<InvalidOperationException>(() => group.Each(ref move));
        Assert.Equal(0, move.Count);
    }

    [Fact]
    public void Owning_Contains_FollowsWithRule()
    {
        var registry = new Registry();
        Entity a = registry.Create(new Pos(), new Vel { X = 6 });
        Entity b = registry.Create(new Pos());
        var group = registry.Group().Own<Pos>().With<Vel>().Build();

        var containsA = group.Contains(a);
        var containsB = group.Contains(b);

        Assert.True(containsA);
        Assert.False(containsB);
    }

    [Fact]
    public void OwningEach_LooksWithTypesUp()
    {
        var registry = new Registry();
        Entity a = registry.Create(new Pos(), new Vel { X = 6 });
        registry.Create(new Pos());
        var group = registry.Group().Own<Pos>().With<Vel>().Build();

        group.Each((Entity e, ref Vel v, ref Pos p) => p.X += v.X);

        Assert.Equal(6, registry.Get<Pos>(a).X);
    }

    [Fact]
    public void OwningEach_OnATypeTheGroupDoesNotRequire_Throws()
    {
        var registry = new Registry();
        registry.Create(new Pos(), new Vel { X = 6 });
        registry.Create(new Pos());
        var group = registry.Group().Own<Pos>().With<Vel>().Build();

        Assert.Throws<InvalidOperationException>(() => group.Each((Entity e, ref Pos p, ref Hp h) => { }));
    }

    private struct Flash : IComponent { }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AutoDefer_OwningEach_AppliesAfterTheWalk(bool withLookup)
    {
        var registry = new Registry { AutoDefer = true };
        for (var i = 0; i < 20; i++)
        {
            registry.Create(new Pos { X = i }, new Vel());
        }

        var group = withLookup ? registry.Group().Own<Pos>().With<Vel>().Build() : registry.Group().Own<Pos>().Own<Vel>().Build();
        var visited = 0;
        var deferredInWalk = true;

        group.Each((Entity e, ref Pos p, ref Vel v) =>
        {
            visited++;
            registry.Add(e, new Flash());
            registry.Create(new Pos(), new Vel());
            deferredInWalk &= registry.IsDeferring;
        });

        Assert.Equal(20, visited);
        Assert.True(deferredInWalk);
        Assert.False(registry.IsDeferring);
        Assert.Equal(20, registry.ComponentCount<Flash>());
        Assert.Equal(40, group.Count);
    }

#if DEBUG
    [Fact]
    public void OwningEach_AddToAWalkedPool_Throws()
    {
        var registry = new Registry();
        registry.Create(new Pos(), new Vel());
        Entity other = registry.Create(new Vel());
        var group = registry.Group().Own<Pos>().Own<Vel>().Build();

        Assert.Throws<InvalidOperationException>(() =>
            group.Each((Entity e, ref Pos p) => registry.Add(other, new Pos())));
    }
#endif

    [Fact]
    public void ViewUse_OwningGroup_Throws()
    {
        var registry = new Registry();
        var packed = registry.Group().Own<Hp>().Build();

        Assert.Throws<InvalidOperationException>(() => registry.View().Use(packed));
    }
}
