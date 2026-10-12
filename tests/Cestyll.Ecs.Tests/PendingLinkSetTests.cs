using Cestyll.Ecs;
using Cestyll.Ecs.Relations;

namespace Cestyll.Ecs.Tests;

// Link sets in a defer scope: recorded, applied at its end, readable for a source born in it.
public class PendingLinkSetTests
{
    private struct Position : IComponent;

    private struct Struck : ILink<Entity>
    {
        public Entity Target { get; init; }
        public float Cooldown;
    }

    [Fact]
    public void RegistryLink_OutsideAScope_AppliesAtOnce()
    {
        var registry = new Registry();
        Entity blade = registry.Create(new Position());
        Entity enemy = registry.Create(new Position());

        registry.Link(blade, new Struck { Target = enemy, Cooldown = 0.45f });

        Assert.True(registry.Links<Struck>().Has(blade, enemy));
    }

    [Fact]
    public void LinksInAWalk_WaitForTheScope()
    {
        var registry = new Registry { AutoDefer = true };
        Entity blade = registry.Create(new Position());
        Entity a = registry.Create(new Position());
        Entity b = registry.Create(new Position());
        var inWalk = new List<bool>();

        registry.View().Each((Entity e, ref Position _) =>
        {
            if (e != blade)
            {
                registry.Handle(blade).Link(new Struck { Target = e, Cooldown = 1f });
                inWalk.Add(registry.Links<Struck>().Has(blade, e));
            }
        });

        Assert.Equal([false, false], inWalk); // an older source: applied links only
        var struck = registry.Links<Struck>();
        Assert.True(struck.Has(blade, a));
        Assert.True(struck.Has(blade, b));
        Assert.Equal(2, struck.TargetCount(blade));
    }

    [Fact]
    public void BornSource_ReadsItsRecordedLinks()
    {
        var registry = new Registry();
        var struck = registry.Links<Struck>();
        Entity enemy = registry.Create(new Position());
        Entity shot;
        bool has;
        bool found;
        Struck link;

        using (registry.Defer())
        {
            shot = registry.Create(new Position());
            registry.Link(shot, new Struck { Target = enemy, Cooldown = 0.5f });
            struck.Get(shot, enemy).Cooldown = 0.25f; // edits the recorded link
            has = struck.Has(shot, enemy);
            found = struck.TryGet(shot, enemy, out link);
        }

        Assert.True(has);
        Assert.True(found);
        Assert.Equal(0.25f, link.Cooldown);
        Assert.Equal(0.25f, struck.Get(shot, enemy).Cooldown);
    }

    [Fact]
    public void BornSource_TargetCount_SeesAppliedLinksOnly()
    {
        var registry = new Registry();
        var struck = registry.Links<Struck>();
        Entity enemy = registry.Create(new Position());
        int count;

        using (registry.Defer())
        {
            Entity shot = registry.Create(new Position());
            registry.Link(shot, new Struck { Target = enemy, Cooldown = 0.5f });
            count = struck.TargetCount(shot);
        }

        Assert.Equal(0, count);
    }

    [Fact]
    public void BornSource_UnlinkingADirectLink_ReadsAsAbsent()
    {
        var registry = new Registry();
        var struck = registry.Links<Struck>();
        Entity other = registry.Create(new Position());
        Entity shot;
        bool unlinked;
        bool has;
        Exception? getError;

        using (registry.Defer())
        {
            shot = registry.Create(new Position());
            struck.Link(shot, new Struck { Target = other }); // direct: applies at once
            unlinked = registry.Unlink<Struck>(shot, other);
            has = struck.Has(shot, other);
            getError = Record.Exception(() => struck.Get(shot, other));
        }

        Assert.True(unlinked); // had it
        Assert.False(has);
        Assert.IsType<KeyNotFoundException>(getError);
        Assert.False(struck.Has(shot, other));
    }

    [Fact]
    public void CommandBuffer_RecordsLinkSetLinks_SkippingDeadTargets()
    {
        var registry = new Registry();
        Entity enemy = registry.Create(new Position());
        Entity gone = registry.Create(new Position());
        var commands = registry.CommandBuffer();

        Entity shot = commands.Create(new Position())
            .Link(new Struck { Target = enemy, Cooldown = 2f })
            .Link(new Struck { Target = gone });
        commands.Destroy(gone);
        commands.Flush();

        var struck = registry.Links<Struck>();
        Assert.Equal(2f, struck.Get(shot, enemy).Cooldown);
        Assert.Equal(1, struck.TargetCount(shot));
    }
}
