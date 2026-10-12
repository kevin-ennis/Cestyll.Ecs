using Cestyll.Ecs;
using Cestyll.Ecs.Observers;
using Cestyll.Ecs.Snapshots;
using Cestyll.Ecs.Tags;

namespace Cestyll.Ecs.Tests;

public class RequirementsTests
{
    private struct Sprite : IComponent { public int Art; }
    private struct Position : IComponent { public int X; }
    private struct Rigged : IComponent;
    private struct Visible : IComponent { public bool Value; }
    private struct Drawable : IComponent { public int Art; public int X; }
    private struct Decor : ITag;

    private static Drawable MakeDrawable(Registry r, Entity e) =>
        r.TryGet<Sprite>(e, out var sprite)
            ? new Drawable { Art = sprite.Art, X = r.Get<Position>(e).X }
            : new Drawable { Art = -1 };

    private static Requirements<Entity> Presentation(Registry registry) =>
        registry.Requirements()
            .Require<Sprite, Visible>()
            .Require<Rigged, Visible>()
            .Require<Visible, Drawable>(MakeDrawable);

    [Fact]
    public void Requirements_AddNothingUntilApply()
    {
        var registry = new Registry();
        using var requirements = Presentation(registry);

        Entity sheep = registry.Create(new Sprite { Art = 3 }, new Position { X = 12 });

        Assert.False(registry.Has<Visible>(sheep));
    }

    [Fact]
    public void Apply_SettlesChainInOneCall_WithWholeEntityVisibleToMake()
    {
        var registry = new Registry();
        using var requirements = Presentation(registry);
        Entity sheep = registry.Create(new Sprite { Art = 3 }, new Position { X = 12 });

        requirements.Apply();

        Assert.True(registry.Has<Visible>(sheep));
        Assert.Equal(new Drawable { Art = 3, X = 12 }, registry.Get<Drawable>(sheep));
    }

    [Fact]
    public void Apply_KeepsValueGivenByHand()
    {
        var registry = new Registry();
        using var requirements = Presentation(registry);
        Entity wolf = registry.Create(new Sprite(), new Position(), new Drawable { Art = 99 });

        requirements.Apply();

        Assert.True(registry.Has<Visible>(wolf));
        Assert.Equal(99, registry.Get<Drawable>(wolf).Art);
    }

    [Fact]
    public void FirstApply_CoversEntitiesFromBeforeTheRules()
    {
        var registry = new Registry();
        Entity early = registry.Create(new Sprite { Art = 1 }, new Position());
        using var requirements = Presentation(registry);
        Entity late = registry.Create(new Rigged());

        requirements.Apply();

        Assert.Equal(1, registry.Get<Drawable>(early).Art);
        Assert.Equal(-1, registry.Get<Drawable>(late).Art);
    }

    [Fact]
    public void Apply_SkipsEntitiesDestroyedOrStrippedSince()
    {
        var registry = new Registry();
        using var requirements = Presentation(registry);
        requirements.Apply(); // seeded, so later entities come from the observers
        Entity ghost = registry.Create(new Sprite(), new Position());
        Entity rock = registry.Create(new Sprite(), new Position());
        registry.Destroy(ghost);
        registry.Remove<Sprite>(rock);

        requirements.Apply();

        Assert.False(registry.Has<Visible>(rock));
        Assert.Equal(0, registry.ComponentCount<Visible>());
    }

    [Fact]
    public void When_IsCheckedAtApply_AfterLaterTags()
    {
        var registry = new Registry();
        using var requirements = registry.Requirements()
            .Require<Sprite, Visible>((r, e) => !r.HasTag<Decor>(e));
        var decor = registry.Create(new Sprite()).Tag<Decor>();
        Entity sheep = registry.Create(new Sprite());

        requirements.Apply();

        Assert.False(registry.Has<Visible>(decor));
        Assert.True(registry.Has<Visible>(sheep));
    }

    [Fact]
    public void RemovedRequirement_StaysRemoved()
    {
        var registry = new Registry();
        using var requirements = registry.Requirements().Require<Sprite, Visible>();
        Entity e = registry.Create(new Sprite());
        requirements.Apply();
        registry.Remove<Visible>(e);

        requirements.Apply();

        Assert.False(registry.Has<Visible>(e));
    }

    [Fact]
    public void RemovedRequirement_ReturnsWhenTIsGainedAgain()
    {
        var registry = new Registry();
        using var requirements = registry.Requirements().Require<Sprite, Visible>();
        Entity e = registry.Create(new Sprite());
        requirements.Apply();
        registry.Remove<Visible>(e);
        requirements.Apply();
        registry.Remove<Sprite>(e);
        registry.Add(e, new Sprite());

        requirements.Apply();

        Assert.True(registry.Has<Visible>(e));
    }

    [Fact]
    public void TwoRulesForOneType_AddItOnce()
    {
        var registry = new Registry();
        var made = 0;
        using var requirements = registry.Requirements()
            .Require<Sprite, Visible>((_, _) => { made++; return new Visible(); })
            .Require<Rigged, Visible>((_, _) => { made++; return new Visible(); });
        registry.Create(new Sprite(), new Rigged());

        requirements.Apply();

        Assert.Equal(1, made);
    }

    [Fact]
    public void Apply_InDeferScope_Throws()
    {
        var registry = new Registry();
        using var requirements = Presentation(registry);
        Exception? thrown;

        using (registry.Defer())
        {
            registry.Create(new Sprite { Art = 7 }, new Position { X = 4 });
            thrown = Record.Exception(requirements.Apply);
        }

        Assert.IsType<InvalidOperationException>(thrown);
    }

    [Fact]
    public void EntitiesCreatedInDeferScope_AreAppliedAfterIt()
    {
        var registry = new Registry();
        using var requirements = Presentation(registry);
        Entity dog;
        using (registry.Defer())
        {
            dog = registry.Create(new Sprite { Art = 7 }, new Position { X = 4 });
        }

        requirements.Apply();

        Assert.Equal(new Drawable { Art = 7, X = 4 }, registry.Get<Drawable>(dog));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void LoadedWorld_GetsSkippedTypes_FromRulesBuiltBeforeOrAfter(bool rulesBeforeLoad)
    {
        var schema = new SnapshotSchema<Entity>().Component<Sprite>().Component<Position>()
            .Skip<Visible>().Skip<Drawable>();
        var source = new Registry();
        using (var sourceRules = Presentation(source))
        {
            source.Create(new Sprite { Art = 5 }, new Position { X = 9 });
            sourceRules.Apply();
        }

        var archive = new Archive();
        schema.Save(source, ref archive);
        var world = new Registry();
        var requirements = rulesBeforeLoad ? Presentation(world) : null;
        var reader = archive.Reopen();
        schema.Load(world, ref reader);
        requirements ??= Presentation(world);

        requirements.Apply();

        Assert.Equal(new Drawable { Art = 5, X = 9 }, world.Get<Drawable>(world.Storage<Sprite>().Entities[0]));
        requirements.Dispose();
    }

    [Fact]
    public void Dispose_StopsTheRules()
    {
        var registry = new Registry();
        var requirements = Presentation(registry);
        requirements.Dispose();
        requirements.Dispose(); // twice is fine

        Assert.Throws<ObjectDisposedException>(requirements.Apply);
        Assert.Throws<ObjectDisposedException>(() => requirements.Require<Sprite, Visible>());
    }

    [Fact]
    public void Apply_FromMake_Throws()
    {
        var registry = new Registry();
        Requirements<Entity>? requirements = null;
        requirements = registry.Requirements()
            .Require<Sprite, Visible>((_, _) => { requirements!.Apply(); return new Visible(); });
        registry.Create(new Sprite());

        Assert.Throws<InvalidOperationException>(requirements.Apply);
        requirements.Dispose();
    }

    [Fact]
    public void Require_SameType_Throws()
    {
        var registry = new Registry();
        using var requirements = registry.Requirements();

        Assert.Throws<ArgumentException>(() => requirements.Require<Sprite, Sprite>());
    }

    [Fact]
    public void Apply_Warm_AllocatesNothing()
    {
        var registry = new Registry();
        using var requirements = Presentation(registry);
        for (var i = 0; i < 64; i++)
        {
            registry.Create(new Sprite(), new Position());
        }

        requirements.Apply(); // seeds and sizes the buffer
        var spawned = new Entity[32];
        for (var i = 0; i < spawned.Length; i++)
        {
            spawned[i] = registry.Create(new Sprite(), new Position());
        }

        requirements.Apply(); // grows the pools and the observers' sets
        registry.Destroy(spawned);

        for (var i = 0; i < spawned.Length; i++)
        {
            spawned[i] = registry.Create(new Sprite(), new Position());
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        requirements.Apply();
        Assert.Equal(before, GC.GetAllocatedBytesForCurrentThread());
        Assert.All(spawned, e => Assert.True(registry.Has<Drawable>(e)));
    }
}
