using Cestyll.Ecs;
using Cestyll.Ecs.Attachments;
using Cestyll.Ecs.Tags;

namespace Cestyll.Ecs.Tests;

// Reads in a defer scope see pending state for entities born in it, so factories can defer.
public class PendingReadTests
{
    private struct Position : IComponent { public int X; }
    private struct Size : IComponent { public float Scale; }
    private struct Speed : IComponent { public float Value; }
    private struct Elite : ITag;

    private enum MutationKind { Swift, Armored }

    private struct Mutation : IAttachment<MutationKind>
    {
        public MutationKind Kind;
        public int Stacks;
        public readonly MutationKind Key => Kind;
    }

    // Shaped like Spawner.Mutate: guard on Has so a mutation's effect applies once.
    private static void Mutate(Registry registry, Entity e, MutationKind kind)
    {
        var mutations = registry.Attachments<Mutation, MutationKind>();
        if (mutations.Has(e, kind))
        {
            return;
        }

        registry.Attach<Mutation, MutationKind>(e, new Mutation { Kind = kind, Stacks = 1 });
        registry.Get<Speed>(e).Value *= 2f;
    }

    // Shaped like a game factory: create, then read back to adjust.
    private static Entity Enemy(Registry registry, bool elite)
    {
        Entity e = registry.Create(new Position { X = 1 }, new Size { Scale = 1f }, new Speed { Value = 2f });
        if (elite)
        {
            registry.Get<Size>(e).Scale *= 1.5f;
            registry.Handle(e).Get<Speed>().Value += 1f;
            registry.Tag<Elite>(e);
        }

        return e;
    }

    [Fact]
    public void Factory_CreatesThenAdjusts_InsideAScope()
    {
        var registry = new Registry();
        Entity e;
        bool hasSize;
        bool found;
        Speed speed;
        bool inPool;

        using (registry.Defer())
        {
            e = Enemy(registry, elite: true);
            hasSize = registry.Has<Size>(e);
            found = registry.TryGet<Speed>(e, out speed);
            inPool = registry.Storage<Size>().Has(e);
        }

        Assert.True(hasSize);
        Assert.True(found);
        Assert.Equal(3f, speed.Value);
        Assert.False(inPool); // still only recorded
        Assert.Equal(1.5f, registry.Get<Size>(e).Scale);
        Assert.Equal(3f, registry.Get<Speed>(e).Value);
        Assert.True(registry.HasTag<Elite>(e));
    }

    [Fact]
    public void Factory_RunsFromAnAutoDeferWalk()
    {
        var registry = new Registry { AutoDefer = true };
        var spawners = new List<Entity>();
        for (var i = 0; i < 3; i++)
        {
            spawners.Add(registry.Create(new Position { X = 100 + i }));
        }

        registry.View().Each((Entity _, ref Position _) => Enemy(registry, elite: true));

        Assert.Equal(6, registry.ComponentCount<Position>());
        Assert.Equal(3, registry.ComponentCount<Size>());
        registry.View().Each((Entity _, ref Size s) => Assert.Equal(1.5f, s.Scale));
    }

    [Fact]
    public void LatestRecordWins()
    {
        var registry = new Registry();
        float value;

        using (registry.Defer())
        {
            Entity e = registry.Create();
            registry.Add(e, new Speed { Value = 1f });
            registry.AddOrReplace(e, new Speed { Value = 4f });
            value = registry.Get<Speed>(e).Value;
        }

        Assert.Equal(4f, value);
    }

    [Fact]
    public void RecordedRemove_ReadsAsAbsent()
    {
        var registry = new Registry();
        bool has;
        bool found;
        Exception? getError;

        using (registry.Defer())
        {
            Entity e = registry.Create();
            registry.Add(e, new Speed { Value = 1f });
            registry.AddOrReplace(e, new Speed { Value = 4f });
            registry.Remove<Speed>(e);
            has = registry.Has<Speed>(e);
            found = registry.TryGet<Speed>(e, out _);
            getError = Record.Exception(() => registry.Get<Speed>(e));
        }

        Assert.False(has);
        Assert.False(found);
        Assert.IsType<KeyNotFoundException>(getError);
    }

    [Fact]
    public void GetOrAdd_FindsThePendingValue_RecordingOnce()
    {
        var registry = new Registry();
        Entity e;
        int pending;

        using (registry.Defer())
        {
            e = registry.Create();
            registry.GetOrAdd<Speed>(e).Value = 2f;
            registry.GetOrAdd<Speed>(e).Value += 1f;
            pending = registry.PendingCount;
        }

        Assert.Equal(1, pending);
        Assert.Equal(3f, registry.Get<Speed>(e).Value);
    }

    [Fact]
    public void EntitiesFromBeforeTheScope_StillReadTheRegistryAsItIs()
    {
        var registry = new Registry();
        Entity old = registry.Create(new Position());
        bool has;
        Exception? getError;

        using (registry.Defer())
        {
            registry.Create(new Speed()); // an entity born in the scope switches the slow path on
            registry.Add(old, new Speed { Value = 9f });
            has = registry.Has<Speed>(old);
            getError = Record.Exception(() => registry.Get<Speed>(old));
        }

        Assert.False(has);
        Assert.IsType<KeyNotFoundException>(getError);
        Assert.Equal(9f, registry.Get<Speed>(old).Value);
    }

    [Fact]
    public void NestedScopes_KeepEntitiesBornInAnInnerOne_UntilTheOutermostEnds()
    {
        var registry = new Registry();

        using (registry.Defer())
        {
            Entity e;
            using (registry.Defer())
            {
                e = registry.Create(new Speed { Value = 5f });
            }

            Assert.Equal(5f, registry.Get<Speed>(e).Value);
        }
    }

    [Fact]
    public void AfterTheScope_ReadsGoToThePools()
    {
        var registry = new Registry();
        Entity e;
        using (registry.Defer())
        {
            e = registry.Create(new Speed { Value = 5f });
        }

        registry.Remove<Speed>(e);

        Assert.False(registry.Has<Speed>(e));
    }

    [Fact]
    public void AttachmentHas_SeesARecordedAttach_SoAGuardAppliesOnce()
    {
        var registry = new Registry();
        Entity e;

        using (registry.Defer())
        {
            e = registry.Create(new Speed { Value = 2f });
            Mutate(registry, e, MutationKind.Swift);
            Mutate(registry, e, MutationKind.Swift);
        }

        Assert.Equal(4f, registry.Get<Speed>(e).Value);
        Assert.Equal(1, registry.Attachments<Mutation, MutationKind>().ItemCount(e));
    }

    [Fact]
    public void AttachmentReads_ForABornEntity_SeeRecordedItems()
    {
        var registry = new Registry();
        var mutations = registry.Attachments<Mutation, MutationKind>();
        bool hasSwift;
        bool hasArmored;
        int stacks;
        bool found;

        using (registry.Defer())
        {
            Entity e = registry.Create(new Speed { Value = 2f });
            Mutate(registry, e, MutationKind.Swift);
            hasSwift = mutations.Has(e, MutationKind.Swift);
            hasArmored = mutations.Has(e, MutationKind.Armored);
            stacks = mutations.Get(e, MutationKind.Swift).Stacks;
            found = mutations.TryGet(e, MutationKind.Swift, out _);
        }

        Assert.True(hasSwift);
        Assert.False(hasArmored);
        Assert.Equal(1, stacks);
        Assert.True(found);
    }

    // Reads over all items see applied ones only.
    [Fact]
    public void AttachmentItemCount_SeesAppliedItemsOnly()
    {
        var registry = new Registry();
        var mutations = registry.Attachments<Mutation, MutationKind>();
        int count;

        using (registry.Defer())
        {
            Entity e = registry.Create(new Speed { Value = 2f });
            Mutate(registry, e, MutationKind.Swift);
            count = mutations.ItemCount(e);
        }

        Assert.Equal(0, count);
    }

    [Fact]
    public void AttachmentDetach_ReadsAsAbsent()
    {
        var registry = new Registry();
        var mutations = registry.Attachments<Mutation, MutationKind>();
        Entity e;
        bool has;
        Exception? getError;

        using (registry.Defer())
        {
            e = registry.Create();
            registry.Attach<Mutation, MutationKind>(e, new Mutation { Kind = MutationKind.Swift, Stacks = 1 });
            registry.Detach<Mutation, MutationKind>(e, MutationKind.Swift);
            has = mutations.Has(e, MutationKind.Swift);
            getError = Record.Exception(() => mutations.Get(e, MutationKind.Swift));
        }

        Assert.False(has);
        Assert.IsType<KeyNotFoundException>(getError);
        Assert.False(mutations.Has(e, MutationKind.Swift));
    }

    [Fact]
    public void AttachmentGet_EditsTheRecordedItem()
    {
        var registry = new Registry();
        var mutations = registry.Attachments<Mutation, MutationKind>();
        Entity e;

        using (registry.Defer())
        {
            e = registry.Create();
            registry.Attach<Mutation, MutationKind>(e, new Mutation { Kind = MutationKind.Armored, Stacks = 1 });
            mutations.Get(e, MutationKind.Armored).Stacks = 3;
        }

        Assert.Equal(3, mutations.Get(e, MutationKind.Armored).Stacks);
    }

    [Fact]
    public void AttachOrPatch_MergesIntoTheRecordedItem()
    {
        var registry = new Registry();
        Entity e;
        MergeAction<Mutation> stack = static (ref Mutation m, in Mutation more) => m.Stacks += more.Stacks;
        int pending;

        using (registry.Defer())
        {
            e = registry.Create();
            registry.AttachOrPatch<Mutation, MutationKind>(e, new Mutation { Kind = MutationKind.Swift, Stacks = 1 }, stack);
            registry.AttachOrPatch<Mutation, MutationKind>(e, new Mutation { Kind = MutationKind.Swift, Stacks = 2 }, stack);
            pending = registry.PendingCount;
        }

        Assert.Equal(1, pending);
        Assert.Equal(3, registry.Attachments<Mutation, MutationKind>().Get(e, MutationKind.Swift).Stacks);
    }

    [Fact]
    public void AttachmentReads_ForOlderEntities_SeeAppliedItemsOnly()
    {
        var registry = new Registry();
        Entity old = registry.Create();
        var mutations = registry.Attachments<Mutation, MutationKind>();
        bool has;

        using (registry.Defer())
        {
            registry.Create(); // an entity born in the scope switches the slow path on
            registry.Attach<Mutation, MutationKind>(old, new Mutation { Kind = MutationKind.Swift });
            has = mutations.Has(old, MutationKind.Swift);
        }

        Assert.False(has);
        Assert.True(mutations.Has(old, MutationKind.Swift));
    }

    // Direct store calls apply at once, but the scope's records are newer, so reads follow them.
    [Fact]
    public void Record_NewerThanADirectPoolAdd_Wins()
    {
        var registry = new Registry();
        Entity e;
        float value;
        bool found;
        Speed speed;
        float getOrAdd;

        using (registry.Defer())
        {
            e = registry.Create();
            registry.Storage<Speed>().Add(e, new Speed { Value = 1f }); // direct: not recorded
            registry.AddOrReplace(e, new Speed { Value = 7f });
            value = registry.Get<Speed>(e).Value;
            found = registry.TryGet<Speed>(e, out speed);
            getOrAdd = registry.GetOrAdd<Speed>(e).Value;
        }

        Assert.Equal(7f, value);
        Assert.True(found);
        Assert.Equal(7f, speed.Value);
        Assert.Equal(7f, getOrAdd);
        Assert.Equal(7f, registry.Get<Speed>(e).Value);
    }

    [Fact]
    public void RecordedRemove_HidesADirectPoolAdd()
    {
        var registry = new Registry();
        Entity e;
        bool has;
        bool found;
        Exception? getError;

        using (registry.Defer())
        {
            e = registry.Create();
            registry.Storage<Speed>().Add(e, new Speed { Value = 1f });
            registry.Remove<Speed>(e);
            has = registry.Has<Speed>(e);
            found = registry.TryGet<Speed>(e, out _);
            getError = Record.Exception(() => registry.Get<Speed>(e));
        }

        Assert.False(has);
        Assert.False(found);
        Assert.IsType<KeyNotFoundException>(getError);
        Assert.False(registry.Has<Speed>(e));
    }

    [Fact]
    public void GetOrAdd_AfterARecordedRemove_RecordsANewAdd()
    {
        var registry = new Registry();
        Entity e;
        float value;

        using (registry.Defer())
        {
            e = registry.Create(new Speed { Value = 1f });
            registry.Remove<Speed>(e);
            registry.GetOrAdd(e, new Speed { Value = 3f });
            value = registry.Get<Speed>(e).Value;
        }

        Assert.Equal(3f, value);
        Assert.Equal(3f, registry.Get<Speed>(e).Value);
    }

    [Fact]
    public void AttachmentRecords_NewerThanADirectAttach_Win()
    {
        var registry = new Registry();
        var mutations = registry.Attachments<Mutation, MutationKind>();
        Entity e;
        int stacks;
        bool found;
        Mutation m;

        using (registry.Defer())
        {
            e = registry.Create();
            mutations.Attach(e, new Mutation { Kind = MutationKind.Swift, Stacks = 1 }); // direct
            registry.Attach<Mutation, MutationKind>(e, new Mutation { Kind = MutationKind.Swift, Stacks = 5 });
            stacks = mutations.Get(e, MutationKind.Swift).Stacks;
            found = mutations.TryGet(e, MutationKind.Swift, out m);
        }

        Assert.Equal(5, stacks);
        Assert.True(found);
        Assert.Equal(5, m.Stacks);
        Assert.Equal(5, mutations.Get(e, MutationKind.Swift).Stacks);
    }

    [Fact]
    public void RecordedDetach_HidesADirectAttach()
    {
        var registry = new Registry();
        var mutations = registry.Attachments<Mutation, MutationKind>();
        Entity e;
        bool has;
        Exception? getError;

        using (registry.Defer())
        {
            e = registry.Create();
            mutations.Attach(e, new Mutation { Kind = MutationKind.Armored, Stacks = 1 }); // direct
            registry.Detach<Mutation, MutationKind>(e, MutationKind.Armored);
            has = mutations.Has(e, MutationKind.Armored);
            getError = Record.Exception(() => mutations.Get(e, MutationKind.Armored));
        }

        Assert.False(has);
        Assert.IsType<KeyNotFoundException>(getError);
        Assert.False(mutations.Has(e, MutationKind.Armored));
    }

    [Fact]
    public void AttachOrPatch_AfterARecordedDetach_RecordsTheAttach()
    {
        var registry = new Registry();
        var mutations = registry.Attachments<Mutation, MutationKind>();
        MergeAction<Mutation> stack = static (ref Mutation m, in Mutation more) => m.Stacks += more.Stacks;
        Entity e;
        int stacks;

        using (registry.Defer())
        {
            e = registry.Create();
            mutations.Attach(e, new Mutation { Kind = MutationKind.Swift, Stacks = 4 }); // direct
            registry.Detach<Mutation, MutationKind>(e, MutationKind.Swift);
            registry.AttachOrPatch<Mutation, MutationKind>(e, new Mutation { Kind = MutationKind.Swift, Stacks = 1 }, stack);
            stacks = mutations.Get(e, MutationKind.Swift).Stacks;
        }

        Assert.Equal(1, stacks);
        Assert.Equal(1, mutations.Get(e, MutationKind.Swift).Stacks);
    }
}
