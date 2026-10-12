using Cestyll.Ecs;
using Cestyll.Ecs.Tags;
using Cestyll.Ecs.Views;

namespace Cestyll.Ecs.Tests;

// registry.View(): untyped filters; the callback or Rows names the component types.
public class FluentViewTests
{
    private struct Pos : IComponent { public int X; }
    private struct Vel : IComponent { public int X; }
    private struct Hp : IComponent { }
    private struct Selected : ITag { }

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
    public void Each_FetchesTheCallbacksTypes()
    {
        var registry = new Registry();
        Entity a = registry.Create(new Pos(), new Vel { X = 2 });
        registry.Create(new Pos());

        var seen = new List<Entity>();
        registry.View().Each((Entity e, ref Pos p, ref Vel v) =>
        {
            p.X += v.X;
            seen.Add(e);
        });

        Assert.Equal([a], seen);
        Assert.Equal(2, registry.Get<Pos>(a).X);
    }

    [Fact]
    public void Filters_Chain()
    {
        var registry = new Registry();
        Entity a = registry.Create(new Pos(), new Vel(), new Hp());
        Entity b = registry.Create(new Pos(), new Vel(), new Hp());
        registry.Create(new Pos(), new Vel());
        registry.Tag<Selected>(b);

        var seen = new List<Entity>();
        registry.View().With<Pos>().With<Hp>().WithoutTag<Selected>().Each((Entity e, ref Pos _) => seen.Add(e));

        Assert.Equal([a], seen);
    }

    [Fact]
    public void WithOnAFetchedType_IsDropped_AndStillMatches()
    {
        var registry = new Registry();
        Entity a = registry.Create(new Pos(), new Vel());
        registry.Create(new Pos());

        var seen = new List<Entity>();
        registry.View().With<Pos>().With<Vel>().Each((Entity e, ref Pos _, ref Vel _) => seen.Add(e));
        foreach (var row in registry.View().With<Vel>().Rows<Pos, Vel>())
        {
            seen.Add(row.Entity);
        }

        Assert.Equal([a, a], seen);
    }

    [Fact]
    public void StructCallback_NamesItsOwnTypes()
    {
        var registry = new Registry();
        Entity a = registry.Create(new Pos(), new Vel { X = 3 });
        registry.Create(new Vel());

        var move = new Move();
        registry.View().Each(ref move);

        Assert.Equal(1, move.Count);
        Assert.Equal(3, registry.Get<Pos>(a).X);
    }

    [Fact]
    public void UseT_OnATypeTheCallbackDoesNotTake_Throws()
    {
        var registry = new Registry();
        registry.Create(new Pos(), new Vel());

        Assert.Throws<InvalidOperationException>(() =>
            registry.View().Use<Hp>().Each((Entity e, ref Pos _) => { }));
    }

    private struct S1 : ITag { }
    private struct S2 : ITag { }
    private struct S3 : ITag { }
    private struct S4 : ITag { }
    private struct S5 : ITag { }
    private struct S6 : ITag { }
    private struct S7 : ITag { }
    private struct S8 : ITag { }
    private struct S9 : ITag { }

    [Fact]
    public void MoreThanEightFilters_AllApply()
    {
        var registry = new Registry();
        Entity a = registry.Create(new Pos());
        Entity b = registry.Create(new Pos());
        registry.Tag<S9>(b); // the ninth, which spills

        var seen = new List<Entity>();
        registry.View()
            .WithoutTag<S1>().WithoutTag<S2>().WithoutTag<S3>().WithoutTag<S4>().WithoutTag<S5>()
            .WithoutTag<S6>().WithoutTag<S7>().WithoutTag<S8>().WithoutTag<S9>()
            .Each((Entity e, ref Pos _) => seen.Add(e));

        Assert.Equal([a], seen);
    }
}
