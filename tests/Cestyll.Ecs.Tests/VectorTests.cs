using System.Runtime.Intrinsics;
using Cestyll.Ecs;
using Cestyll.Ecs.Groups;

namespace Cestyll.Ecs.Tests;

public class VectorTests
{
    private struct Pos : IComponent { public int X, Y; }
    private struct Vel : IComponent { public int X, Y; }
    private struct Mass : IComponent { public float V; }
    private struct Odd : IComponent { public int A, B, C; } // 12 bytes: straddles a vector
    private struct Wide : IComponent { public long A, B, C, D, E, F, G, H; } // 64 bytes: two vectors each

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(8)]
    [InlineData(45)]
    public void GroupRowsCoverEveryEntityOnceTailIncluded(int count)
    {
        var registry = new Registry();
        var group = registry.Group().Own<Pos>().Own<Vel>().Build();
        for (var i = 0; i < count; i++)
        {
            registry.Create(new Pos { X = i, Y = -i }, new Vel { X = 1, Y = 2 });
        }

        var rows = 0;
        foreach (var v in group.Rows<Pos, Vel>().Vectors256(Lanes.Int))
        {
            v.V1 += v.V2;
            rows++;
        }

        Assert.Equal((count + 3) / 4, rows); // two ints each: four components a vector
        var k = 0;
        foreach (var e in group.Entities)
        {
            var p = registry.Get<Pos>(e);
            Assert.Equal((k + 1, -k + 2), (p.X, p.Y));
            k++;
        }

        Assert.Equal(count, k);
    }

    [Fact]
    public void TailWritesLandOnlyOnTheirComponents()
    {
        var registry = new Registry();
        var group = registry.Group().Own<Mass>().Without<Pos>().Build();
        for (var i = 0; i < 11; i++)
        {
            registry.Create(new Mass { V = i });
        }

        // Past the group's span, where a padded tail vector would reach.
        registry.Create(new Mass { V = 100 }, new Pos());
        var other = registry.Create(new Mass { V = 200 }, new Pos());

        foreach (var v in group.Rows<Mass>().Vectors256(Lanes.Float))
        {
            v.V1 *= Vector256.Create(2f);
        }

        var k = 0;
        foreach (var e in group.Entities)
        {
            Assert.Equal(2f * (e.Index - 1), registry.Get<Mass>(e).V);
            k++;
        }

        Assert.Equal(11, k);
        Assert.Equal(200f, registry.Get<Mass>(other).V);
    }

    [Fact]
    public void BreakInsideTheTailRowStillWritesItBack()
    {
        var registry = new Registry();
        var group = registry.Group().Own<Mass>().Build();
        for (var i = 0; i < 9; i++)
        {
            registry.Create(new Mass { V = 1 });
        }

        foreach (var v in group.Rows<Mass>().Vectors256(Lanes.Float))
        {
            v.V1 += Vector256.Create(1f);
            if (v.V1[1] == 1f) // the tail row: only lane 0 holds a component
            {
                break;
            }
        }

        foreach (var e in group.Entities)
        {
            Assert.Equal(2f, registry.Get<Mass>(e).V);
        }
    }

    [Fact]
    public void APoolNeedsNoGroup()
    {
        var registry = new Registry();
        for (var i = 0; i < 21; i++)
        {
            var e = registry.Create(new Mass { V = i });
            if (i % 3 == 0)
            {
                registry.Add(e, new Pos()); // other components change nothing
            }
        }

        var rows = 0;
        foreach (var v in registry.Storage<Mass>().Vectors256(Lanes.Float))
        {
            v.V1 *= Vector256.Create(2f);
            rows++;
        }

        Assert.Equal(3, rows);
        foreach (var e in registry.Storage<Mass>().Entities)
        {
            Assert.Equal(2f * (e.Index - 1), registry.Get<Mass>(e).V);
        }
    }

    [Fact]
    public void AComponentLargerThanAVectorSpansSeveral()
    {
        var registry = new Registry();
        var group = registry.Group().Own<Wide>().Build();
        for (var i = 0; i < 3; i++)
        {
            registry.Create(new Wide { A = i, H = i });
        }

        var rows = 0;
        foreach (var v in group.Rows<Wide>().Vectors256(Lanes.Long))
        {
            v.V1 += Vector256.Create(10L);
            rows++;
        }

        Assert.Equal(6, rows);
        foreach (var e in group.Entities)
        {
            var w = registry.Get<Wide>(e);
            Assert.Equal((e.Index - 1 + 10, 10L, e.Index - 1 + 10), (w.A, w.D, w.H));
        }
    }

    [Fact]
    public void ASpanGivesItsVectorsAndLeavesTheTailToTheCaller()
    {
        var registry = new Registry();
        var group = registry.Group().Own<Pos>().Own<Vel>().Build();
        for (var i = 0; i < 11; i++)
        {
            registry.Create(new Pos { X = i, Y = -i }, new Vel { X = 1, Y = 2 });
        }

        var pos = group.Span<Pos>().Vectors256(Lanes.Int, out var posTail);
        var vel = group.Span<Vel>().Vectors256(Lanes.Int, out var velTail);
        for (var i = 0; i < pos.Length; i++)
        {
            pos[i] += vel[i];
        }

        for (var i = 0; i < posTail.Length; i++)
        {
            posTail[i].X += velTail[i].X;
            posTail[i].Y += velTail[i].Y;
        }

        Assert.Equal(2, pos.Length); // four components a vector
        Assert.Equal(3, posTail.Length);
        var k = 0;
        foreach (var e in group.Entities)
        {
            var p = registry.Get<Pos>(e);
            Assert.Equal((k + 1, -k + 2), (p.X, p.Y));
            k++;
        }

        Assert.Equal(11, k);
    }

    [Fact]
    public void ComponentThatStraddlesVectorsIsRefused()
    {
        var registry = new Registry();
        var group = registry.Group().Own<Odd>().Build();
        var ex = Assert.Throws<ArgumentException>(() => group.Rows<Odd>().Vectors256(Lanes.Int));
        Assert.Contains("12 bytes", ex.Message);
    }

    [Fact]
    public void ComponentsOfDifferentSizesAreRefusedTogether()
    {
        var registry = new Registry();
        var group = registry.Group().Own<Pos>().Own<Mass>().Build();
        var ex = Assert.Throws<ArgumentException>(() => group.Rows<Pos, Mass>().Vectors256(Lanes.Float));
        Assert.Contains("same size", ex.Message);
    }
}
