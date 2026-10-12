#if DEBUG
using Cestyll.Ecs;
using Cestyll.Ecs.Attachments;

namespace Cestyll.Ecs.Tests;

// Reordering a pool under an open walk throws. The checks are DEBUG-only in the library.
public class WalkGuardTests
{
    private struct Position : IComponent { public int X; }
    private struct Velocity : IComponent;
    private struct Flash : IComponent;

    private enum StatusKind { Burn, Chill }

    private struct Status : IAttachment<StatusKind>
    {
        public StatusKind Kind;
        public readonly StatusKind Key => Kind;
    }

    private static Registry Populated(int count = 4)
    {
        var registry = new Registry();
        for (var i = 0; i < count; i++)
        {
            registry.Create(new Position { X = i });
        }

        return registry;
    }

    [Fact]
    public void Each_AddToTheWalkedPool_Throws()
    {
        var registry = Populated();
        Entity other = registry.Create(new Velocity());

        var ex = Assert.Throws<InvalidOperationException>(() =>
            registry.View().Each((Entity e, ref Position _) => registry.Add(other, new Position())));

        Assert.Contains("Position pool", ex.Message);
    }

    [Fact]
    public void Foreach_CreateIntoTheWalkedPool_Throws()
    {
        var registry = Populated();

        Assert.Throws<InvalidOperationException>(() =>
        {
            foreach (var row in registry.View().Rows<Position>())
            {
                registry.Create(new Position { X = row.C1.X + 100 });
            }
        });
    }

    [Fact]
    public void AddToAnotherPool_RemoveAndDestroyTheCurrent_AreFine()
    {
        var registry = Populated();

        foreach (var row in registry.View().Rows<Position>())
        {
            registry.Add(row.Entity, new Flash());
            if (row.C1.X % 2 == 0)
            {
                registry.Remove<Position>(row.Entity);
            }
            else
            {
                registry.Destroy(row.Entity);
            }
        }

        Assert.Equal(0, registry.ComponentCount<Position>());
    }

    [Fact]
    public void Sort_DuringAWalk_Throws()
    {
        var registry = Populated();

        Assert.Throws<InvalidOperationException>(() =>
            registry.View().Each((Entity e, ref Position _) => registry.Sort<Position>((a, b) => a.X - b.X)));
    }

    [Fact]
    public void StorageShrinkToFit_DuringAWalk_Throws()
    {
        var registry = Populated();

        Assert.Throws<InvalidOperationException>(() =>
            registry.View().Each((Entity e, ref Position _) => registry.Storage<Position>().ShrinkToFit()));
    }

    [Fact]
    public void RegistryShrinkToFit_DuringAWalk_Throws()
    {
        var registry = Populated();

        Assert.Throws<InvalidOperationException>(() =>
            registry.View().Each((Entity e, ref Position _) => registry.ShrinkToFit()));
    }

    [Fact]
    public void JoiningAGroupThatOwnsTheWalkedPool_Throws()
    {
        var registry = Populated();
        registry.Group().Own<Position>().Own<Velocity>().Build();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            registry.View().Each((Entity e, ref Position _) => registry.Add(e, new Velocity())));

        Assert.Contains("group", ex.Message);
    }

    [Fact]
    public void WalkEndedByReturn_ReleasesThePool()
    {
        var registry = Populated();
        registry.View().Each((Entity _, ref Position _) => { });

        registry.Create(new Position());

        Assert.Equal(5, registry.ComponentCount<Position>());
    }

    [Fact]
    public void WalkEndedByThrow_ReleasesThePool()
    {
        var registry = Populated();
        try
        {
            registry.View().Each((Entity _, ref Position _) => throw new InvalidOperationException());
        }
        catch (InvalidOperationException)
        {
        }

        registry.Create(new Position());

        Assert.Equal(5, registry.ComponentCount<Position>());
    }

    [Fact]
    public void WalkEndedByBreak_ReleasesThePool()
    {
        var registry = Populated();
        foreach (var _ in registry.View().Rows<Position>())
        {
            break;
        }

        registry.Create(new Position());

        Assert.Equal(5, registry.ComponentCount<Position>());
    }

    [Fact]
    public void NestedWalks_HoldThePoolUntilTheOuterEnds()
    {
        var registry = Populated(1);

        Assert.Throws<InvalidOperationException>(() =>
            registry.View().Each((Entity e, ref Position _) =>
            {
                registry.View().Each((Entity _, ref Position _) => { });
                registry.Create(new Position());
            }));
    }

    [Fact]
    public void AttachDuringAll_Throws()
    {
        var registry = Populated(1);
        var statuses = registry.Attachments<Status, StatusKind>();
        foreach (var e in registry.Entities)
        {
            statuses.Attach(e, new Status { Kind = StatusKind.Burn });
        }

        Assert.Throws<InvalidOperationException>(() =>
        {
            foreach (var a in statuses.All)
            {
                statuses.Attach(a.Entity, new Status { Kind = StatusKind.Chill });
            }
        });
    }

    [Fact]
    public void BreakingOutOfAll_ReleasesTheStore()
    {
        var registry = Populated(1);
        var statuses = registry.Attachments<Status, StatusKind>();
        Entity first = default;
        foreach (var e in registry.Entities)
        {
            statuses.Attach(e, new Status { Kind = StatusKind.Burn });
            first = e;
        }

        foreach (var _ in statuses.All)
        {
            break;
        }

        statuses.Detach(first, StatusKind.Burn);

        Assert.Equal(0, statuses.ItemCount(first));
    }
}
#endif
