using Cestyll.Ecs;

namespace Cestyll.Ecs.Tests;

public class RowGetTests
{
    private struct Position : IComponent { public int X; }
    private struct Velocity : IComponent { public int X; }
    private struct Projectile : IComponent { public int PierceLeft; }
    private struct Speed : IComponent { public int Value; }
    private struct Unused : IComponent;

    private sealed class Name : IComponent { public string Text = ""; }

    [Fact]
    public void Get_ReadsAndWritesByType()
    {
        var registry = new Registry();
        Entity e = registry.Create(new Position { X = 1 }, new Velocity { X = 2 }, new Projectile { PierceLeft = 3 }, new Speed { Value = 4 });
        var pierce = new List<int>();
        var speeds = new List<int>();

        foreach (var row in registry.View().Rows<Position, Velocity, Projectile, Speed>())
        {
            pierce.Add(row.Get<Projectile>().PierceLeft);
            speeds.Add(row.Get<Speed>().Value);
            row.Get<Projectile>().PierceLeft--;
            row.Get<Position>().X += row.Get<Velocity>().X;
        }

        Assert.Equal([3], pierce);
        Assert.Equal([4], speeds);
        Assert.Equal(2, registry.Get<Projectile>(e).PierceLeft);
        Assert.Equal(3, registry.Get<Position>(e).X);
    }

    [Fact]
    public void Get_WorksForClassComponents()
    {
        var registry = new Registry();
        registry.Create(new Name { Text = "a" }, new Position { X = 5 });
        var names = new List<string>();
        var xs = new List<int>();

        foreach (var row in registry.View().Rows<Name>())
        {
            names.Add(row.Get<Name>().Text);
        }

        foreach (var row in registry.View().Rows<Position, Name>())
        {
            xs.Add(row.Get<Position>().X);
            row.Get<Name>() = new Name { Text = "b" };
        }

        foreach (var row in registry.View().Rows<Name>())
        {
            names.Add(row.Get<Name>().Text);
        }

        Assert.Equal(["a", "b"], names);
        Assert.Equal([5], xs);
    }

    [Fact]
    public void Get_TypeTheViewLacks_Throws()
    {
        var registry = new Registry();
        registry.Create(new Position());

        var thrown = false;
        foreach (var row in registry.View().Rows<Position>())
        {
            try
            {
                row.Get<Unused>();
            }
            catch (InvalidOperationException)
            {
                thrown = true;
            }
        }

        Assert.True(thrown);
    }
}
