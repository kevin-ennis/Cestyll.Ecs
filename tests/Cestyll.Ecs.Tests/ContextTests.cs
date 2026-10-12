using Cestyll.Ecs;

namespace Cestyll.Ecs.Tests;

public class ContextTests
{
    private struct Clock
    {
        public float Time;
        public int Frame;
    }

    private struct Pos : IComponent
    {
        public int X;
    }

    private sealed class Grid
    {
        public int Width;
    }

    [Fact]
    public void Context_Missing_ThrowsWithTypeName()
    {
        var registry = new Registry();

        var ex = Assert.Throws<KeyNotFoundException>(() => registry.GetContext<Clock>());
        Assert.Contains(nameof(Clock), ex.Message);
        Assert.False(registry.HasContext<Clock>());
    }

    [Fact]
    public void AddOrReplaceContext_ThenContext_ReturnsValue()
    {
        var registry = new Registry();

        registry.AddOrReplaceContext(new Clock { Time = 1.5f, Frame = 3 });

        Assert.True(registry.HasContext<Clock>());
        Assert.Equal(3, registry.GetContext<Clock>().Frame);
        Assert.Equal(1.5f, registry.GetContext<Clock>().Time);
    }

    [Fact]
    public void Context_RefEditsInPlace()
    {
        var registry = new Registry();
        registry.AddOrReplaceContext(new Clock());

        ref var clock = ref registry.GetContext<Clock>();
        clock.Frame++;
        registry.GetContext<Clock>().Frame++;

        Assert.Equal(2, registry.GetContext<Clock>().Frame);
    }

    [Fact]
    public void AddOrReplaceContext_Existing_ReplacesAndKeepsRefsLive()
    {
        var registry = new Registry();
        ref var clock = ref registry.AddOrReplaceContext(new Clock { Frame = 1 });

        registry.AddOrReplaceContext(new Clock { Frame = 9 });

        Assert.Equal(9, clock.Frame);
    }

    [Fact]
    public void Context_RefSurvivesSlotArrayGrowth()
    {
        var registry = new Registry();
        ref var clock = ref registry.AddOrReplaceContext(new Clock());

        // Fresh types take ids above every existing one, so the slot array grows.
        registry.AddOrReplaceContext(new Wide<byte>());
        registry.AddOrReplaceContext(new Wide<short>());
        registry.AddOrReplaceContext(new Wide<int>());
        registry.AddOrReplaceContext(new Wide<long>());
        registry.AddOrReplaceContext(new Wide<float>());
        registry.AddOrReplaceContext(new Wide<double>());
        registry.AddOrReplaceContext(new Wide<decimal>());
        registry.AddOrReplaceContext(new Wide<char>());
        registry.AddOrReplaceContext(new Wide<bool>());
        registry.AddOrReplaceContext(new Wide<uint>());
        registry.AddOrReplaceContext(new Wide<ulong>());
        registry.AddOrReplaceContext(new Wide<ushort>());
        registry.AddOrReplaceContext(new Wide<sbyte>());
        registry.AddOrReplaceContext(new Wide<nint>());
        registry.AddOrReplaceContext(new Wide<nuint>());
        registry.AddOrReplaceContext(new Wide<Half>());
        registry.AddOrReplaceContext(new Wide<Guid>());

        clock.Frame = 42;

        Assert.Equal(42, registry.GetContext<Clock>().Frame);
    }

    private struct Wide<T>;

    [Fact]
    public void RemoveContext_RemovesOnce()
    {
        var registry = new Registry();
        registry.AddOrReplaceContext(new Clock());

        var first = registry.RemoveContext<Clock>();
        var second = registry.RemoveContext<Clock>();

        Assert.True(first);
        Assert.False(second);
        Assert.False(registry.HasContext<Clock>());
        Assert.Throws<KeyNotFoundException>(() => registry.GetContext<Clock>());
    }

    [Fact]
    public void RemoveContext_NeverSet_ReturnsFalse()
    {
        var registry = new Registry();

        Assert.False(registry.RemoveContext<Grid>());
    }

    [Fact]
    public void Context_ReferenceType()
    {
        var registry = new Registry();
        var grid = new Grid { Width = 8 };

        registry.AddOrReplaceContext(grid);

        Assert.Same(grid, registry.GetContext<Grid>());
    }

    [Fact]
    public void Context_IndependentOfComponentsOfSameType()
    {
        var registry = new Registry();
        Entity e = registry.Create();
        registry.Add(e, new Pos { X = 1 });

        registry.AddOrReplaceContext(new Pos { X = 2 });
        registry.Destroy(e);

        Assert.Equal(2, registry.GetContext<Pos>().X);
        Assert.Equal(0, registry.Storage<Pos>().Count);
    }

    [Fact]
    public void Context_PerRegistry()
    {
        var a = new Registry();
        var b = new Registry();

        a.AddOrReplaceContext(new Clock { Frame = 1 });

        Assert.True(a.HasContext<Clock>());
        Assert.False(b.HasContext<Clock>());
    }
}
