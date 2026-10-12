using Cestyll.Ecs;

namespace Cestyll.Ecs.Tests;

public class FingerprintTests
{
    // A byte then an int: three bytes of padding a block snapshot would copy.
    private struct Visual : IComponent
    {
        public byte Shape;
        public int Color;
    }

    private static ulong Hash(Visual v) => ((ulong)v.Shape << 32) | (uint)v.Color;

    private static Registry World(int color)
    {
        var registry = new Registry();
        registry.Create(new Visual { Shape = 1, Color = 10 });
        registry.Create(new Visual { Shape = 2, Color = color });
        return registry;
    }

    [Fact]
    public void EqualWorlds_EqualFingerprints()
    {
        Assert.Equal(World(20).Fingerprint<Visual>(Hash), World(20).Fingerprint<Visual>(Hash));
    }

    [Fact]
    public void AValueChange_ChangesTheFingerprint()
    {
        Assert.NotEqual(World(20).Fingerprint<Visual>(Hash), World(21).Fingerprint<Visual>(Hash));
    }

    [Fact]
    public void SameValuesOnOtherEntities_ChangeTheFingerprint()
    {
        var a = World(20);
        var b = new Registry();
        b.Destroy(b.Create()); // the next entities get other versions or indices
        b.Create();
        b.Create(new Visual { Shape = 1, Color = 10 });
        b.Create(new Visual { Shape = 2, Color = 20 });

        Assert.NotEqual(a.Fingerprint<Visual>(Hash), b.Fingerprint<Visual>(Hash));
    }

    [Fact]
    public void NoPool_IsZero_AndNotCreated()
    {
        var registry = new Registry();

        Assert.Equal(0UL, registry.Fingerprint<Visual>(Hash));
        Assert.Null(registry.FindStorage<Visual>());
    }
}
