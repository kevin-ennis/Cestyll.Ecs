using Cestyll.Ecs;
using Cestyll.Ecs.Attachments;

namespace Cestyll.Ecs.Tests;

public class KeyFreeAttachmentTests
{
    private struct Position : IComponent;

    private enum UpgradeKind { Blades, Dash }

    private struct Upgrade : IAttachment<UpgradeKind>
    {
        public UpgradeKind Kind;
        public int Level;
        public readonly UpgradeKind Key => Kind;
    }

    [Fact]
    public void GetAttachments_ReadsWithoutTheKey_AndWritesInPlace()
    {
        var registry = new Registry();
        Entity e = registry.Create(new Position());
        registry.Attach<Upgrade, UpgradeKind>(e, new Upgrade { Kind = UpgradeKind.Blades, Level = 1 });
        registry.Attach<Upgrade, UpgradeKind>(e, new Upgrade { Kind = UpgradeKind.Dash, Level = 2 });

        var upgrades = registry.GetAttachments<Upgrade>(e);
        upgrades[0].Level += 10;

        Assert.Equal(2, upgrades.Length);
        Assert.Equal(11, registry.Attachments<Upgrade, UpgradeKind>().Get(e, UpgradeKind.Blades).Level);
        Assert.Equal(2, registry.Handle(e).GetAttachments<Upgrade>().Length);
    }

    [Fact]
    public void HasAttachments_AfterAttach_IsTrue()
    {
        var registry = new Registry();
        Entity e = registry.Create(new Position());

        registry.Attach<Upgrade, UpgradeKind>(e, new Upgrade { Kind = UpgradeKind.Dash });

        Assert.True(registry.Handle(e).HasAttachments<Upgrade>());
    }

    [Fact]
    public void HasAttachments_AfterLastDetach_IsFalse()
    {
        var registry = new Registry();
        Entity e = registry.Create(new Position());
        registry.Attach<Upgrade, UpgradeKind>(e, new Upgrade { Kind = UpgradeKind.Dash });

        registry.Detach<Upgrade, UpgradeKind>(e, UpgradeKind.Dash);

        Assert.False(registry.HasAttachments<Upgrade>(e));
    }

    [Fact]
    public void Reads_NeverMakeTheStore()
    {
        var registry = new Registry();
        Entity e = registry.Create(new Position());

        var empty = registry.GetAttachments<Upgrade>(e).IsEmpty;
        var has = registry.HasAttachments<Upgrade>(e);

        Assert.True(empty);
        Assert.False(has);
        Assert.DoesNotContain(registry.Stores(), s => s.Type == typeof(Upgrade));
    }

    [Fact]
    public void Reads_MissDeadEntities()
    {
        var registry = new Registry();
        Entity e = registry.Create(new Position());
        registry.Attach<Upgrade, UpgradeKind>(e, new Upgrade());

        registry.Destroy(e);

        Assert.True(registry.GetAttachments<Upgrade>(e).IsEmpty);
        Assert.False(registry.HasAttachments<Upgrade>(e));
    }
}
