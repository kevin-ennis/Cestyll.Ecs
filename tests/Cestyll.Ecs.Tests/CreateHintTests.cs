using Cestyll.Ecs;
using Cestyll.Ecs.Snapshots;

namespace Cestyll.Ecs.Tests;

// Runs on both layouts: Cestyll.Ecs.Tests64 compiles this file with Entity aimed at Entity64.
public class CreateHintTests
{
    private struct Position : IComponent { public float X; }

    private static int MaxIndex<T>() where T : unmanaged, IEntity<T> => T.MaxIndex;

    [Fact]
    public void FreeIndex_TakesHintIncludingVersion()
    {
        var registry = new Registry();
        Entity e = registry.Create();
        registry.Create();
        registry.Destroy(e);

        Entity back = registry.CreateAt(e);

        Assert.Equal(e, back);
        Assert.True(registry.IsAlive(e));
        Assert.Equal(2, registry.AliveCount);
    }

    [Fact]
    public void FreeIndex_AcceptsAnyVersion()
    {
        var registry = new Registry();
        Entity e = registry.Create();
        registry.Destroy(e);
        var later = Entity.Make(e.Index, 7);

        Entity made = registry.CreateAt(later);

        Assert.Equal(later, made);
        Assert.True(registry.IsAlive(later));
        Assert.False(registry.IsAlive(e));
    }

    [Fact]
    public void LiveIndex_FallsBackToPlainCreate()
    {
        var registry = new Registry();
        Entity e = registry.Create();

        Entity made = registry.CreateAt(e);

        Assert.NotEqual(e, made);
        Assert.Equal(2, made.Index);
        Assert.True(registry.IsAlive(e));
        Assert.True(registry.IsAlive(made));
    }

    [Fact]
    public void Null_FallsBackToPlainCreate()
    {
        var registry = new Registry();

        Entity made = registry.CreateAt(default);

        Assert.Equal(Entity.Make(1, 0), made);
        Assert.True(registry.IsAlive(made));
    }

    [Fact]
    public void IndexZeroWithVersion_FallsBackToPlainCreate()
    {
        var registry = new Registry();
        var hint = Entity.Make(0, 5);

        Entity made = registry.CreateAt(hint);

        Assert.Equal(Entity.Make(1, 0), made);
        Assert.Equal(1, registry.AliveCount);
    }

    [Fact]
    public void MidDestroy_FallsBackToPlainCreate()
    {
        var registry = new Registry();
        Entity e = registry.Create(new Position());
        Entity made = default;
        registry.Storage<Position>().OnRemove += x => made = registry.CreateAt(x);

        registry.Destroy(e);

        Assert.Equal(2, made.Index);
        Assert.True(registry.IsAlive(made));
        Assert.False(registry.IsAlive(e));
        Assert.Equal(1, registry.AliveCount);
    }

    [Fact]
    public void AtHighWaterMark_LeavesNoGap()
    {
        var registry = new Registry();
        registry.Create();
        registry.Create();
        var hint = Entity.Make(3, 5);

        Entity made = registry.CreateAt(hint);

        Assert.Equal(hint, made);
        Assert.Equal(3, registry.AliveCount);
        Assert.Equal(Entity.Make(4, 0), registry.Create().Entity);
    }

    [Fact]
    public void SmallGap_WithinOneWord()
    {
        var registry = new Registry();
        registry.Create();
        registry.Create();

        registry.CreateAt(Entity.Make(6, 0));

        Assert.Equal(3, registry.AliveCount);
        Assert.Equal([3, 4, 5, 7], registry.CreateMany(4).Select(x => x.Index));
    }

    [Fact]
    public void PastTheEnd_TakesHint_LeavesGapDead()
    {
        var registry = new Registry();
        registry.Create(); // index 1
        var far = Entity.Make(200, 3);

        Entity made = registry.CreateAt(far);

        Assert.Equal(far, made);
        Assert.True(registry.IsAlive(far));
        Assert.Equal(2, registry.AliveCount);
        Assert.All(Enumerable.Range(2, 198), i => Assert.False(registry.IsAlive(Entity.Make(i, 0))));
    }

    // The gap is recycled lowest first at version 0, then fresh indices follow.
    [Fact]
    public void PastTheEnd_GapRecycledLowestFirst()
    {
        var registry = new Registry();
        registry.Create(); // index 1
        registry.CreateAt(Entity.Make(200, 3));

        var next = registry.CreateMany(199);

        Assert.Equal(Enumerable.Range(2, 198).Append(201), next.Select(x => x.Index));
        Assert.All(next.Take(198), x => Assert.Equal(0u, x.Version));
    }

    [Fact]
    public void GapSlot_CanItselfBeHinted()
    {
        var registry = new Registry();
        registry.CreateAt(Entity.Make(10, 0));

        Entity made = registry.CreateAt(Entity.Make(5, 2));

        Assert.Equal(Entity.Make(5, 2), made);
        Assert.Equal(2, registry.AliveCount);
        Assert.Equal(1, registry.Create().Entity.Index);
    }

    // Entity64's top index is past what a version array holds; Entity32's fits.
    [Fact]
    public void MaxIndex_ThrowsWithoutChange_OrCreates()
    {
        var registry = new Registry();
        registry.Create();
        var max = MaxIndex<Entity>();
        var hint = Entity.Make(max, 1);

        if (max >= Array.MaxLength)
        {
            var atMax = Record.Exception(() => registry.CreateAt(hint));
            var atArrayLimit = Record.Exception(() => registry.CreateAt(Entity.Make(Array.MaxLength, 0)));

            Assert.IsType<ArgumentOutOfRangeException>(atMax);
            Assert.IsType<ArgumentOutOfRangeException>(atArrayLimit);
            Assert.Equal(1, registry.AliveCount);
            Assert.Equal(Entity.Make(2, 0), registry.Create().Entity);
        }
        else
        {
            Entity made = registry.CreateAt(hint);

            Assert.Equal(hint, made);
            Assert.True(registry.IsAlive(hint));
            Assert.Equal(2, registry.AliveCount);
            Assert.Equal(Entity.Make(2, 0), registry.Create().Entity);
        }
    }

    // Entity64 reads -1 as a negative index; Entity32 masks it to its (valid) top index.
    [Fact]
    public void NegativeIndex_ThrowsWithoutChange()
    {
        var registry = new Registry();
        registry.CreateMany(3);

        if (Entity.Make(-1, 0).Index < 0)
        {
            var minusOne = Record.Exception(() => registry.CreateAt(Entity.Make(-1, 0)));
            var minValue = Record.Exception(() => registry.CreateAt(Entity.Make(int.MinValue, 2)));

            Assert.IsType<ArgumentOutOfRangeException>(minusOne);
            Assert.IsType<ArgumentOutOfRangeException>(minValue);
            Assert.Equal(3, registry.AliveCount);
            Assert.Equal(Entity.Make(4, 0), registry.Create().Entity);
        }
        else
        {
            var hint = Entity.Make(-1, 0);

            Entity made = registry.CreateAt(hint);

            Assert.Equal(MaxIndex<Entity>(), hint.Index);
            Assert.Equal(hint, made);
        }
    }

    // The gap 64..127 is exactly bitset word 1: head and tail masks cover a whole word.
    [Fact]
    public void Gap_OnExactWordBoundaries()
    {
        var registry = new Registry();
        registry.CreateMany(63); // indices 1..63, all of word 0 but the reserved index 0
        registry.CreateAt(Entity.Make(128, 0));

        var next = registry.CreateMany(65);

        Assert.Equal(129, registry.AliveCount);
        Assert.Equal(Enumerable.Range(64, 64).Append(129), next.Select(x => x.Index));
    }

    [Fact]
    public void HintedEntity_TakesComponents()
    {
        var registry = new Registry();
        var hint = Entity.Make(3, 1);

        registry.CreateAt(hint).Add(new Position { X = 4 });

        Assert.Equal(4, registry.Get<Position>(hint).X);
    }

    [Fact]
    public void HintedEntity_Destroyed_FreesIndexForNextVersion()
    {
        var registry = new Registry();
        var hint = Entity.Make(3, 1);
        Entity made = registry.CreateAt(hint).Add(new Position { X = 4 });

        registry.Destroy(made);

        Assert.False(registry.IsAlive(hint));
        Assert.False(registry.Has<Position>(hint));
        Assert.Equal(Entity.Make(3, 2), registry.CreateAt(Entity.Make(3, 2)).Entity);
    }

    [Fact]
    public void SurvivesSnapshotRoundTrip()
    {
        var source = new Registry();
        var hint = Entity.Make(6, 4);
        source.CreateAt(hint).Add(new Position { X = 2 });

        var archive = new Archive();
        source.Snapshot<Archive>().Entities(ref archive).Component<Position>(ref archive);
        var loaded = new Registry();
        var reader = archive.Reopen();
        loaded.SnapshotLoader<Archive>().Entities(ref reader).Component<Position>(ref reader);

        Assert.True(loaded.IsAlive(hint));
        Assert.Equal(2, loaded.Get<Position>(hint).X);
        Assert.Equal(1, loaded.AliveCount);
        Assert.Equal(1, loaded.Create().Entity.Index); // the gap came back free
    }
}
