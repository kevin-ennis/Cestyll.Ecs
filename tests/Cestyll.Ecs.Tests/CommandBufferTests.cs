using Cestyll.Ecs;
using Cestyll.Ecs.Attachments;
using Cestyll.Ecs.Relations;
using Cestyll.Ecs.Tags;

namespace Cestyll.Ecs.Tests;

public class CommandBufferTests
{
    private struct Position : IComponent { public int X; }
    private struct Velocity : IComponent { public int X; }
    private struct Flash : IComponent { public int Frames; }
    private struct Shot : ITag;
    private struct Dead : ITag;
    private struct Orbits : IRelation
    {
        public static OnTargetDestroyed Policy => OnTargetDestroyed.Unrelate;
    }

    private enum StatusKind { Burn, Chill }

    private struct Status : IAttachment<StatusKind>
    {
        public StatusKind Kind;
        public int Left;
        public readonly StatusKind Key => Kind;
    }

    private struct Aims : ILink<Entity>
    {
        public Entity Target { get; init; }
        public int Power;
    }

    [Fact]
    public void ReplaceAndPatch_ApplyAtFlush_FiringOnUpdate()
    {
        var registry = new Registry();
        Entity a = registry.Create(new Position { X = 1 });
        var updates = 0;
        registry.Storage<Position>().OnUpdate += _ => updates++;
        var commands = registry.CommandBuffer();
        commands.Replace(a, new Position { X = 5 });
        commands.Patch(a, (ref Position p) => p.X++);
        commands.Patch<Position>(a);
        var beforeFlush = registry.Get<Position>(a).X;

        commands.Flush();

        Assert.Equal(1, beforeFlush);
        Assert.Equal(6, registry.Get<Position>(a).X);
        Assert.Equal(3, updates);
    }

    [Fact]
    public void Handle_ReplaceAndPatch_Record()
    {
        var registry = new Registry();
        var commands = registry.CommandBuffer();

        Entity e = commands.Create(new Position { X = 1 })
            .Replace(new Position { X = 2 })
            .Patch((ref Position p) => p.X *= 10)
            .Patch<Position>();
        commands.Flush();

        Assert.Equal(20, registry.Get<Position>(e).X);
    }

    [Fact]
    public void Handle_UnlinkAndDetach_Record()
    {
        var registry = new Registry();
        Entity target = registry.Create();
        var commands = registry.CommandBuffer();

        Entity e = commands.Create()
            .Relate<Orbits>(target).Unrelate<Orbits>()
            .Link(new Aims { Target = target }).Unlink<Aims>(target)
            .Attach<Status, StatusKind>(new Status { Kind = StatusKind.Burn })
            .Detach<Status, StatusKind>(StatusKind.Burn);
        commands.Flush();

        Assert.True(registry.IsOrphan(e));
    }

    [Fact]
    public void LinkPatch_AppliesAtFlush_AfterTheLinkItFollows()
    {
        var registry = new Registry();
        Entity a = registry.Create();
        Entity b = registry.Create();
        var updates = 0;
        using var updated = registry.OnLinkUpdate<Aims>((_, _) => updates++);
        var commands = registry.CommandBuffer();
        commands.Link(a, new Aims { Target = b, Power = 1 });
        commands.Patch(a, b, (ref Aims x) => x.Power += 2);
        commands.Patch<Aims>(a, b);
        var linkedBeforeFlush = registry.Links<Aims>().Has(a, b);

        commands.Flush();

        Assert.False(linkedBeforeFlush);
        Assert.Equal(3, registry.GetLink<Aims>(a, b).Power);
        Assert.Equal(2, updates);
    }

    [Fact]
    public void LinkPatch_SkippedWhenTargetDiesFirst()
    {
        var registry = new Registry();
        Entity a = registry.Create();
        Entity b = registry.Create();
        registry.Link(a, new Aims { Target = b });
        var commands = registry.CommandBuffer();

        commands.Destroy(b);
        commands.Patch(a, b, (ref Aims x) => x.Power = 9);
        commands.Flush();

        Assert.Equal(0, registry.TargetCount<Aims>(a));
    }

    [Fact]
    public void AttachmentPatchAndAttachOrPatch_ApplyAtFlush()
    {
        var registry = new Registry();
        Entity e = registry.Create();
        var commands = registry.CommandBuffer();
        MergeAction<Status> add = static (ref Status current, in Status incoming) => current.Left += incoming.Left;
        commands.Attach<Status, StatusKind>(e, new Status { Kind = StatusKind.Burn, Left = 1 });
        commands.AttachOrPatch<Status, StatusKind>(e, new Status { Kind = StatusKind.Burn, Left = 5 }, add);
        commands.AttachOrPatch<Status, StatusKind>(e, new Status { Kind = StatusKind.Chill, Left = 2 }, add);
        commands.Patch<Status, StatusKind>(e, StatusKind.Chill, (ref Status s) => s.Left *= 10);
        var attachedBeforeFlush = registry.HasAttachments<Status>(e);

        commands.Flush();

        Assert.False(attachedBeforeFlush);
        var statuses = registry.Attachments<Status, StatusKind>();
        Assert.Equal(6, statuses.Get(e, StatusKind.Burn).Left);
        Assert.Equal(20, statuses.Get(e, StatusKind.Chill).Left);
    }

    [Fact]
    public void Handle_LinkAndAttachmentPatches_Record()
    {
        var registry = new Registry();
        Entity target = registry.Create();
        var commands = registry.CommandBuffer();

        Entity e = commands.Create()
            .Link(new Aims { Target = target })
            .Patch(target, (ref Aims x) => x.Power = 7)
            .AttachOrPatch<Status, StatusKind>(
                new Status { Kind = StatusKind.Burn, Left = 1 }, static (ref Status c, in Status i) => c.Left += i.Left)
            .Patch<Status, StatusKind>(StatusKind.Burn, (ref Status s) => s.Left = 9);
        commands.Flush();

        Assert.Equal(7, registry.GetLink<Aims>(e, target).Power);
        Assert.Equal(9, registry.Attachments<Status, StatusKind>().Get(e, StatusKind.Burn).Left);
    }

    [Fact]
    public void Commands_WaitForFlush()
    {
        var registry = new Registry();
        Entity a = registry.Create(new Position());
        var commands = registry.CommandBuffer();
        commands.Add(a, new Velocity { X = 3 });
        commands.Tag<Shot>(a);
        var hadVelocity = registry.Has<Velocity>(a);
        var hadShot = registry.HasTag<Shot>(a);
        var recorded = commands.Count;

        commands.Flush();

        Assert.False(hadVelocity);
        Assert.False(hadShot);
        Assert.Equal(2, recorded);
        Assert.Equal(3, registry.Get<Velocity>(a).X);
        Assert.True(registry.HasTag<Shot>(a));
        Assert.True(commands.IsEmpty);
    }

    [Fact]
    public void Create_MakesEntityNow_ComponentsAtFlush()
    {
        var registry = new Registry();
        var commands = registry.CommandBuffer();
        Entity e = commands.Create(new Position { X = 1 }, new Velocity { X = 2 }).Tag<Shot>();
        var aliveBeforeFlush = registry.IsAlive(e);
        var orphanBeforeFlush = registry.IsOrphan(e);

        commands.Flush();

        Assert.True(aliveBeforeFlush);
        Assert.True(orphanBeforeFlush);
        Assert.Equal(1, registry.Get<Position>(e).X);
        Assert.Equal(2, registry.Get<Velocity>(e).X);
        Assert.True(registry.HasTag<Shot>(e));
    }

    [Fact]
    public void CreatingIntoTheWalkedPool_VisitsEachOldEntityOnce()
    {
        var registry = new Registry();
        for (var i = 0; i < 100; i++)
        {
            registry.Create(new Position { X = i });
        }

        var commands = registry.CommandBuffer();
        var visited = 0;
        foreach (var row in registry.View().Rows<Position>())
        {
            visited++;
            commands.Create(new Position { X = row.C1.X + 1000 });
        }

        commands.Flush();

        Assert.Equal(100, visited);
        Assert.Equal(200, registry.ComponentCount<Position>());
    }

    [Fact]
    public void RecordOrder_IsKept_RemoveThenAdd()
    {
        var registry = new Registry();
        Entity e = registry.Create(new Flash { Frames = 1 });
        var commands = registry.CommandBuffer();

        commands.Remove<Flash>(e);
        commands.Add(e, new Flash { Frames = 9 });
        commands.Flush();

        Assert.Equal(9, registry.Get<Flash>(e).Frames);
    }

    [Fact]
    public void RecordOrder_IsKept_AddThenRemove()
    {
        var registry = new Registry();
        Entity e = registry.Create(new Position());
        var commands = registry.CommandBuffer();

        commands.Add(e, new Flash());
        commands.Remove<Flash>(e);
        commands.Flush();

        Assert.False(registry.Has<Flash>(e));
    }

    [Fact]
    public void CommandsAfterDestroy_AreSkipped()
    {
        var registry = new Registry();
        Entity e = registry.Create(new Position());
        var commands = registry.CommandBuffer();

        commands.Destroy(e);
        commands.Add(e, new Flash());
        commands.Tag<Shot>(e);
        commands.Flush();

        Assert.False(registry.IsAlive(e));
        Assert.Equal(0, registry.ComponentCount<Flash>());
        Assert.Equal(0, registry.TagCount<Shot>());
    }

    [Fact]
    public void RecordingForDeadEntity_Throws()
    {
        var registry = new Registry();
        Entity e = registry.Create(new Position());
        registry.Destroy(e);
        var commands = registry.CommandBuffer();

        Assert.Throws<InvalidOperationException>(() => commands.Add(e, new Flash()));
        Assert.Throws<InvalidOperationException>(() => commands.Tag<Shot>(e));
    }

    [Fact]
    public void ThrowingCommand_StopsFlush_RestStayRecorded()
    {
        var registry = new Registry();
        Entity e = registry.Create(new Flash());
        var commands = registry.CommandBuffer();
        commands.Tag<Shot>(e);
        commands.Add(e, new Flash()); // e already has one
        commands.Tag<Dead>(e);

        var thrown = Record.Exception(commands.Flush);

        Assert.IsType<InvalidOperationException>(thrown);
        Assert.True(registry.HasTag<Shot>(e));
        Assert.False(registry.HasTag<Dead>(e));
        Assert.Equal(1, commands.Count);
    }

    [Fact]
    public void ThrowingCommand_NextFlush_AppliesTheRest()
    {
        var registry = new Registry();
        Entity e = registry.Create(new Flash());
        var commands = registry.CommandBuffer();
        commands.Tag<Shot>(e);
        commands.Add(e, new Flash()); // e already has one
        commands.Tag<Dead>(e);
        Record.Exception(commands.Flush);

        commands.Flush();

        Assert.True(registry.HasTag<Dead>(e));
        Assert.True(commands.IsEmpty);
    }

    [Fact]
    public void AddOrReplace_ToleratesTwoRecords()
    {
        var registry = new Registry();
        Entity e = registry.Create(new Position());
        var commands = registry.CommandBuffer();

        commands.AddOrReplace(e, new Flash { Frames = 1 });
        commands.AddOrReplace(e, new Flash { Frames = 2 });
        commands.Flush();

        Assert.Equal(2, registry.Get<Flash>(e).Frames);
    }

    [Fact]
    public void Clear_DropsCommands_AndDestroysCreatedEntities()
    {
        var registry = new Registry();
        Entity kept = registry.Create(new Position());
        var commands = registry.CommandBuffer();

        Entity made = commands.Create(new Position());
        commands.Add(kept, new Flash());
        commands.Clear();
        commands.Flush();

        Assert.False(registry.IsAlive(made));
        Assert.False(registry.Has<Flash>(kept));
    }

    [Fact]
    public void HandlerRecordingDuringFlush_RunsInSameFlush()
    {
        var registry = new Registry();
        var commands = registry.CommandBuffer();
        registry.Storage<Position>().OnAdd += e => commands.Add(e, new Flash());

        Entity e = commands.Create(new Position());
        commands.Flush();

        Assert.True(registry.Has<Flash>(e));
        Assert.True(commands.IsEmpty);
    }

    [Fact]
    public void FlushFromHandlerMidFlush_Throws()
    {
        var registry = new Registry();
        var commands = registry.CommandBuffer();
        Exception? caught = null;
        registry.Storage<Position>().OnAdd += _ => caught = Record.Exception(commands.Flush);

        commands.Create(new Position());
        commands.Flush();

        Assert.IsType<InvalidOperationException>(caught);
    }

    [Fact]
    public void Relate_SkippedWhenTargetDiesFirst()
    {
        var registry = new Registry();
        Entity pilot = registry.Create(new Position());
        Entity blade = registry.Create(new Position());
        var commands = registry.CommandBuffer();

        commands.Relate<Orbits>(blade, pilot);
        commands.Destroy(pilot);
        commands.Flush();

        Assert.True(registry.GetTarget<Orbits>(blade).IsNull);
    }

    [Fact]
    public void Handle_Relate_AppliesAtFlush()
    {
        var registry = new Registry();
        Entity pilot = registry.Create(new Position());
        var commands = registry.CommandBuffer();
        commands.Create(new Position()).Relate<Orbits>(pilot);

        commands.Flush();

        Assert.Equal(1, registry.SourceCount<Orbits>(pilot));
    }

    [Fact]
    public void AttachAndDetach_Apply()
    {
        var registry = new Registry();
        Entity e = registry.Create(new Position());
        var commands = registry.CommandBuffer();

        commands.Attach<Status, StatusKind>(e, new Status { Kind = StatusKind.Burn, Left = 3 });
        commands.Attach<Status, StatusKind>(e, new Status { Kind = StatusKind.Chill, Left = 1 });
        commands.Detach<Status, StatusKind>(e, StatusKind.Chill);
        commands.Flush();

        var items = registry.GetAttachments<Status, StatusKind>(e);
        Assert.Equal(1, items.Length);
        Assert.Equal(3, items[0].Left);
    }

    [Fact]
    public void Defer_RecordsRegistryCalls_UntilScopeEnds()
    {
        var registry = new Registry();
        Entity a = registry.Create(new Position());

        using (registry.Defer())
        {
            Assert.True(registry.IsDeferring);
            registry.Add(a, new Flash { Frames = 2 });
            registry.Tag<Shot>(a);

            Assert.False(registry.Has<Flash>(a));
            Assert.False(registry.HasTag<Shot>(a));
            Assert.Equal(2, registry.PendingCount);
        }

        Assert.False(registry.IsDeferring);
        Assert.Equal(2, registry.Get<Flash>(a).Frames);
        Assert.True(registry.HasTag<Shot>(a));
    }

    [Fact]
    public void Defer_RefFromAdd_WritesThePendingValue()
    {
        var registry = new Registry();
        Entity a = registry.Create(new Position());

        using (registry.Defer())
        {
            registry.Add(a, new Flash()).Frames = 7;
        }

        Assert.Equal(7, registry.Get<Flash>(a).Frames);
    }

    [Fact]
    public void Defer_BoolResults_AnswerForTheRegistryNow()
    {
        var registry = new Registry();
        Entity a = registry.Create(new Position());
        registry.Tag<Shot>(a);

        bool removedPosition, removedVelocity, taggedShot, taggedDead, untaggedShot;
        using (registry.Defer())
        {
            removedPosition = registry.Remove<Position>(a);
            removedVelocity = registry.Remove<Velocity>(a);
            taggedShot = registry.Tag<Shot>(a);
            taggedDead = registry.Tag<Dead>(a);
            untaggedShot = registry.Untag<Shot>(a);
        }

        Assert.True(removedPosition);
        Assert.False(removedVelocity);
        Assert.False(taggedShot);
        Assert.True(taggedDead);
        Assert.True(untaggedShot);
        Assert.False(registry.Has<Position>(a));
        Assert.False(registry.HasTag<Shot>(a));
        Assert.True(registry.HasTag<Dead>(a));
    }

    [Fact]
    public void Defer_DestroyWaits()
    {
        var registry = new Registry();
        Entity a = registry.Create(new Position());

        bool aliveInScope;
        using (registry.Defer())
        {
            registry.Destroy(a);
            aliveInScope = registry.IsAlive(a);
        }

        Assert.True(aliveInScope);
        Assert.False(registry.IsAlive(a));
    }

    [Fact]
    public void Defer_CreateWithComponents_AppliesAtEnd()
    {
        var registry = new Registry();

        Entity b;
        bool aliveInScope, inPoolInScope, hasInScope;
        using (registry.Defer())
        {
            b = registry.Create(new Position { X = 5 }).Tag<Shot>();
            aliveInScope = registry.IsAlive(b);
            inPoolInScope = registry.Storage<Position>().Has(b);
            hasInScope = registry.Has<Position>(b); // born in the scope: reads find it pending
        }

        Assert.True(aliveInScope);
        Assert.False(inPoolInScope);
        Assert.True(hasInScope);
        Assert.Equal(5, registry.Get<Position>(b).X);
        Assert.True(registry.HasTag<Shot>(b));
    }

    [Fact]
    public void Defer_Nested_AppliesAtOutermostEnd()
    {
        var registry = new Registry();
        Entity a = registry.Create(new Position());

        using (registry.Defer())
        {
            using (registry.Defer())
            {
                registry.Add(a, new Flash());
            }

            Assert.False(registry.Has<Flash>(a));
        }

        Assert.True(registry.Has<Flash>(a));
    }

    [Fact]
    public void Defer_EndedTwice_Throws()
    {
        var registry = new Registry();
        var scope = registry.Defer();
        scope.Dispose();

        Exception? caught = null;
        try
        {
            scope.Dispose(); // a ref struct can't go into Assert.Throws' lambda
        }
        catch (Exception e)
        {
            caught = e;
        }

        Assert.IsType<InvalidOperationException>(caught);
    }

    [Fact]
    public void Defer_BufferFlushedInside_RecordsIntoTheScope()
    {
        var registry = new Registry();
        Entity a = registry.Create(new Position());
        var commands = registry.CommandBuffer();
        commands.Add(a, new Flash());

        using (registry.Defer())
        {
            commands.Flush();
            Assert.False(registry.Has<Flash>(a));
        }

        Assert.True(registry.Has<Flash>(a));
    }

    [Fact]
    public void AutoDefer_Each_AddsAndCreatesAfterTheWalk()
    {
        var registry = new Registry { AutoDefer = true };
        for (var i = 0; i < 50; i++)
        {
            registry.Create(new Position { X = i });
        }

        var visited = 0;
        registry.View().Each((Entity e, ref Position p) =>
        {
            visited++;
            registry.Add(e, new Flash());
            registry.Create(new Position { X = p.X + 100 });
            Assert.True(registry.IsDeferring);
        });

        Assert.Equal(50, visited);
        Assert.False(registry.IsDeferring);
        Assert.Equal(100, registry.ComponentCount<Position>());
        Assert.Equal(50, registry.ComponentCount<Flash>());
    }

    [Fact]
    public void AutoDefer_Foreach_AppliesWhenTheLoopEnds()
    {
        var registry = new Registry { AutoDefer = true };
        Entity a = registry.Create(new Position());
        Entity b = registry.Create(new Position());

        foreach (var row in registry.View().Rows<Position>())
        {
            registry.Destroy(row.Entity == a ? b : a); // another entity: unsupported without deferral
            Assert.True(registry.IsDeferring);
        }

        Assert.False(registry.IsDeferring);
        Assert.False(registry.IsAlive(a));
        Assert.False(registry.IsAlive(b));
    }

    [Fact]
    public void AutoDefer_NestedWalks_ApplyAtOutermostEnd()
    {
        var registry = new Registry { AutoDefer = true };
        Entity a = registry.Create(new Position(), new Velocity());

        registry.View().Each((Entity e, ref Position _) =>
        {
            registry.View().Each((Entity v, ref Velocity _) => registry.Add(v, new Flash()));
            Assert.False(registry.Has<Flash>(a));
        });

        Assert.True(registry.Has<Flash>(a));
    }

    [Fact]
    public void AutoDefer_Off_WalksApplyAtOnce()
    {
        var registry = new Registry();
        Entity a = registry.Create(new Position());

        registry.View().Each((Entity e, ref Position _) =>
        {
            Assert.False(registry.IsDeferring);
            registry.Tag<Shot>(e);
            Assert.True(registry.HasTag<Shot>(e));
        });

        Assert.True(registry.HasTag<Shot>(a));
    }

    [Fact]
    public void AutoDefer_HandlerWalkDuringApply_JoinsTheSameRun()
    {
        var registry = new Registry { AutoDefer = true };
        Entity a = registry.Create(new Position());
        registry.Storage<Flash>().OnAdd += _ =>
            registry.View().Each((Entity e, ref Position _) => registry.Tag<Shot>(e));

        registry.View().Each((Entity e, ref Position _) => registry.Add(e, new Flash()));

        Assert.True(registry.Has<Flash>(a));
        Assert.True(registry.HasTag<Shot>(a));
        Assert.Equal(0, registry.PendingCount);
    }

    [Fact]
    public void AutoDefer_HandleCalls_AreRecorded()
    {
        var registry = new Registry { AutoDefer = true };
        Entity a = registry.Create(new Position());

        foreach (var row in registry.View().Rows<Position>())
        {
            registry.Handle(row.Entity).Add(new Velocity()).Tag<Shot>();
            Assert.False(registry.Has<Velocity>(row.Entity));
        }

        Assert.True(registry.Has<Velocity>(a));
        Assert.True(registry.HasTag<Shot>(a));
    }

    [Fact]
    public void AutoDefer_ThrowingWalk_StillEndsTheScope()
    {
        var registry = new Registry { AutoDefer = true };
        Entity a = registry.Create(new Position());

        Assert.Throws<InvalidOperationException>(() =>
            registry.View().Each((Entity e, ref Position _) =>
            {
                registry.Add(e, new Flash());
                throw new InvalidOperationException("walk failed");
            }));

        Assert.False(registry.IsDeferring);
        Assert.True(registry.Has<Flash>(a));
    }
}
