using Cestyll.Ecs;
using Cestyll.Ecs.Attachments;
using Cestyll.Ecs.Inboxes;
using Cestyll.Ecs.Relations;

namespace Cestyll.Ecs.Tests;

public class SignalTests
{
    private struct Position : IComponent { public float X, Y; }
    private struct Velocity : IComponent;
    private struct Health : IComponent { public int Value; }
    private struct Buff : IAttachment<int> { public readonly int Key => 0; }
    private struct Ping : IMessage;
    private struct Parent : IRelation
    {
        public static OnTargetDestroyed Policy => OnTargetDestroyed.Unrelate;
    }

    private struct Aims : ILink<Entity>
    {
        public Entity Target { get; init; }
    }

    [Fact]
    public void Observer_StoreKindFilters_ApplyAtCollection()
    {
        var registry = new Registry();
        using var observer = registry.Observer().OnUpdate<Health>()
            .WithAttachment<Buff>().WithoutInbox<Ping>()
            .WithRelationTarget<Parent>().WithoutRelationSources<Parent>()
            .WithLinkTargets<Aims>().WithoutLinkSources<Aims>();
        Entity parent = registry.Create();
        Entity aimed = registry.Create();

        Entity Full()
        {
            Entity e = registry.Create(new Health());
            registry.Attach<Buff, int>(e, new Buff());
            registry.Relate<Parent>(e, parent);
            registry.Link(e, new Aims { Target = aimed });
            return e;
        }

        var pass = Full();
        var unbuffed = Full();
        registry.Detach<Buff, int>(unbuffed, 0);
        var posted = Full();
        registry.Post(posted, new Ping());
        var unparented = Full();
        registry.Unrelate<Parent>(unparented);
        var parenting = Full();
        registry.Relate<Parent>(registry.Create(), parenting);
        var unaiming = Full();
        registry.Unlink<Aims>(unaiming, aimed);
        var targeted = Full();
        registry.Link(registry.Create(), new Aims { Target = targeted });

        foreach (var e in (Entity[])[pass, unbuffed, posted, unparented, parenting, unaiming, targeted])
        {
            registry.Patch<Health>(e);
        }

        Assert.Equal([pass], observer.Entities.ToArray());
    }

    [Fact]
    public void Observer_RequiringAndExcludingSameStore_Throws()
    {
        var registry = new Registry();
        using var observer = registry.Observer().WithAttachment<Buff>().WithoutLinkTargets<Aims>();

        Assert.Throws<InvalidOperationException>(() => observer.WithoutAttachment<Buff>());
        Assert.Throws<InvalidOperationException>(() => observer.WithLinkTargets<Aims>());
        observer.WithInbox<Ping>().WithRelationSources<Parent>().WithoutRelationTarget<Parent>().WithLinkSources<Aims>();
    }

    [Fact]
    public void OnAdd_FiresAfterValueIsWritten()
    {
        var registry = new Registry();
        Entity e = registry.Create();
        var seen = -1f;
        registry.Storage<Position>().OnAdd += x => seen = registry.Get<Position>(x).X;

        registry.Add(e, new Position { X = 3 });

        Assert.Equal(3, seen);
    }

    [Fact]
    public void OnAdd_SeesEntityAlreadyGrouped()
    {
        var registry = new Registry();
        var group = registry.Group().Own<Position>().Own<Velocity>().Build();
        Entity e = registry.Create();
        registry.Add(e, new Velocity());
        var grouped = false;
        registry.Storage<Position>().OnAdd += _ => grouped = group.Count == 1;

        registry.Add(e, new Position());

        Assert.True(grouped);
    }

    [Fact]
    public void Add_ReturnedRef_FollowsGroupSwap()
    {
        var registry = new Registry();
        registry.Group().Own<Position>().Own<Velocity>().Build();
        Entity e = registry.Create();
        registry.Add(e, new Velocity());

        ref var p = ref registry.Add(e, new Position());
        p.X = 5;

        Assert.Equal(5, registry.Get<Position>(e).X);
    }

    [Fact]
    public void Replace_And_Patch_FireOnUpdate()
    {
        var registry = new Registry();
        Entity e = registry.Create();
        registry.Add(e, new Health { Value = 1 });
        var updates = new List<int>();
        registry.Storage<Health>().OnUpdate += x => updates.Add(registry.Get<Health>(x).Value);

        registry.Replace(e, new Health { Value = 2 });
        registry.Patch(e, static (ref Health h) => h.Value *= 10);
        registry.Get<Health>(e).Value = 7; // plain write: no signal
        registry.Patch<Health>(e);

        Assert.Equal([2, 20, 7], updates);
    }

    [Fact]
    public void Replace_And_Patch_ThrowWhenPoolMissing()
    {
        var registry = new Registry();
        Entity e = registry.Create();

        Assert.Throws<KeyNotFoundException>(() => registry.Replace(e, new Health()));
        Assert.Throws<KeyNotFoundException>(() => registry.Patch<Health>(e));
    }

    [Fact]
    public void Patch_ThrowsWhenEntityLacksComponent()
    {
        var registry = new Registry();
        Entity e = registry.Create();
        registry.Add(registry.Create(), new Health());

        Assert.Throws<KeyNotFoundException>(() => registry.Patch<Health>(e));
    }

    [Fact]
    public void AddOrReplace_ConstructsThenUpdates()
    {
        var registry = new Registry();
        Entity e = registry.Create();
        var pool = registry.Storage<Health>();
        var log = new List<string>();
        pool.OnAdd += _ => log.Add("construct");
        pool.OnUpdate += _ => log.Add("update");

        registry.AddOrReplace(e, new Health { Value = 1 });
        registry.AddOrReplace(e, new Health { Value = 2 });

        Assert.Equal(["construct", "update"], log);
        Assert.Equal(2, registry.Get<Health>(e).Value);
    }

    [Fact]
    public void OnRemove_FiresOnRemoveAndDestroy_WithValueReadable()
    {
        var registry = new Registry();
        Entity a = registry.Create();
        Entity b = registry.Create();
        registry.Add(a, new Health { Value = 1 });
        registry.Add(b, new Health { Value = 2 });
        var seen = new List<(Entity, int, bool)>();
        registry.Storage<Health>().OnRemove +=
            x => seen.Add((x, registry.Get<Health>(x).Value, registry.IsAlive(x)));

        registry.Remove<Health>(a);
        registry.Destroy(b);
        registry.Remove<Health>(a); // already gone: no signal

        Assert.Equal([(a, 1, true), (b, 2, true)], seen);
    }

    [Fact]
    public void Unsubscribe_StopsSignal()
    {
        var registry = new Registry();
        var count = 0;
        void Handler(Entity _) => count++;
        var pool = registry.Storage<Health>();
        pool.OnAdd += Handler;
        registry.Add(registry.Create(), new Health());

        pool.OnAdd -= Handler;
        registry.Add(registry.Create(), new Health());

        Assert.Equal(1, count);
    }

    [Fact]
    public void Destroy_FromOwnOnRemove_FreesOnce()
    {
        var registry = new Registry();
        Entity e = registry.Create();
        registry.Add(e, new Health());
        registry.Add(e, new Position());
        registry.Storage<Health>().OnRemove += registry.Destroy;

        registry.Destroy(e);

        Assert.False(registry.IsAlive(e));
        Assert.False(registry.Has<Position>(e));
        Assert.NotEqual(registry.Create().Entity.Index, registry.Create().Entity.Index);
    }

    [Fact]
    public void OnRemove_HandlerCanDestroyOtherEntities()
    {
        var registry = new Registry();
        Entity parent = registry.Create();
        Entity child = registry.Create();
        registry.Add(parent, new Health { Value = child.Index });
        registry.Add(child, new Health());
        registry.Storage<Health>().OnRemove += x =>
        {
            if (x == parent)
            {
                registry.Destroy(child);
            }
        };

        registry.Destroy(parent);

        Assert.False(registry.IsAlive(child));
        Assert.Equal(0, registry.Storage<Health>().Count);
    }

    [Fact]
    public void OnRemove_CannotAddToDyingEntity()
    {
        var registry = new Registry();
        Entity e = registry.Create();
        registry.Add(e, new Health());
        Exception? thrown = null;
        registry.Storage<Health>().OnRemove += x =>
            thrown = Record.Exception(() => registry.Add(x, new Velocity()));

        registry.Destroy(e);

        Assert.IsType<InvalidOperationException>(thrown);
        Assert.Equal(0, registry.Storage<Velocity>().Count);
    }

    [Fact]
    public void Destroy_RecycledIndex_AcceptsComponents()
    {
        var registry = new Registry();
        Entity e = registry.Create();
        registry.Add(e, new Health());
        registry.Storage<Health>().OnRemove += x => Record.Exception(() => registry.Add(x, new Velocity()));
        registry.Destroy(e);
        Entity next = registry.Create();

        var thrown = Record.Exception(() => registry.Add(next, new Velocity()));

        Assert.Null(thrown);
    }

    [Fact]
    public void OnRemove_CanCreatePoolsDuringDestroy()
    {
        var registry = new Registry();
        Entity e = registry.Create();
        Entity other = registry.Create();
        registry.Add(e, new Health());
        registry.Storage<Health>().OnRemove += _ => registry.Add(other, new Velocity());

        registry.Destroy(e);

        Assert.True(registry.Has<Velocity>(other));
    }

    [Fact]
    public void Observer_CollectsEachEntityOnce()
    {
        var registry = new Registry();
        using var observer = registry.Observer().OnAdd<Health>().OnUpdate<Health>();
        Entity a = registry.Create();
        Entity b = registry.Create();

        registry.Add(a, new Health());
        registry.Replace(a, new Health { Value = 1 });
        registry.Add(b, new Health());
        registry.Add(registry.Create(), new Position()); // not watched

        Assert.Equal([a, b], observer.Entities.ToArray());
    }

    [Fact]
    public void Observer_Clear_EmptiesAndKeepsCollecting()
    {
        var registry = new Registry();
        using var observer = registry.Observer().OnAdd<Health>().OnUpdate<Health>();
        Entity a = registry.Create();
        Entity b = registry.Create();
        registry.Add(a, new Health());
        registry.Add(b, new Health());

        observer.Clear();
        var countAfterClear = observer.Count;
        registry.Patch<Health>(b);

        Assert.Equal(0, countAfterClear);
        Assert.Equal([b], observer.Entities.ToArray());
    }

    [Fact]
    public void Observer_DropsDestroyedEntities()
    {
        var registry = new Registry();
        using var observer = registry.Observer().OnAdd<Health>().OnRemove<Position>();
        Entity a = registry.Create();
        Entity b = registry.Create();
        registry.Add(a, new Health());
        registry.Add(b, new Position());
        registry.Remove<Position>(b);

        registry.Destroy(a);
        Entity c = registry.Create(); // recycles a's index
        registry.Add(c, new Health());

        Assert.Equal([b, c], observer.Entities.ToArray());
        Assert.False(observer.Contains(a));
    }

    [Fact]
    public void Observer_With_Without_FilterAtCollection()
    {
        var registry = new Registry();
        using var observer = registry.Observer().OnUpdate<Health>().With<Position>().Without<Velocity>();
        Entity plain = registry.Create();
        Entity placed = registry.Create();
        Entity moving = registry.Create();
        registry.Add(plain, new Health());
        registry.Add(placed, new Health());
        registry.Add(placed, new Position());
        registry.Add(moving, new Health());
        registry.Add(moving, new Position());
        registry.Add(moving, new Velocity());

        registry.Patch<Health>(plain);
        registry.Patch<Health>(placed);
        registry.Patch<Health>(moving);

        Assert.Equal([placed], observer.Entities.ToArray());
    }

    [Fact]
    public void Observer_FilterChangeAfterCollection_KeepsCollected()
    {
        var registry = new Registry();
        using var observer = registry.Observer().OnUpdate<Health>().With<Position>().Without<Velocity>();
        Entity plain = registry.Create();
        Entity placed = registry.Create();
        registry.Add(plain, new Health());
        registry.Add(placed, new Health());
        registry.Add(placed, new Position());
        registry.Patch<Health>(plain);
        registry.Patch<Health>(placed);

        registry.Remove<Position>(placed); // already collected: stays
        registry.Add(plain, new Position());
        registry.Patch<Health>(plain);

        Assert.Equal([placed, plain], observer.Entities.ToArray());
    }

    [Fact]
    public void Observer_FilterOnRemove_SeesComponentStillPresent()
    {
        var registry = new Registry();
        using var observer = registry.Observer().OnRemove<Position>().With<Position>().With<Health>();
        Entity a = registry.Create();
        Entity b = registry.Create();
        registry.Add(a, new Position());
        registry.Add(a, new Health());
        registry.Add(b, new Position());

        registry.Remove<Position>(a);
        registry.Remove<Position>(b);

        Assert.Equal([a], observer.Entities.ToArray());
    }

    [Fact]
    public void Observer_WatchingAndExcludingSameType_Throws()
    {
        var registry = new Registry();
        using var observer = registry.Observer();
        observer.OnAdd<Position>();
        observer.Without<Velocity>();

        Assert.Throws<InvalidOperationException>(() => observer.Without<Position>());
        Assert.Throws<InvalidOperationException>(() => observer.OnUpdate<Velocity>());
        Assert.Throws<InvalidOperationException>(() => observer.OnRemove<Velocity>());
    }

    [Fact]
    public void Observer_RejectedWatch_ConnectsNothing()
    {
        var registry = new Registry();
        using var observer = registry.Observer();
        observer.OnAdd<Position>();
        observer.Without<Velocity>();
        Record.Exception(() => observer.OnUpdate<Velocity>());
        Record.Exception(() => observer.OnRemove<Velocity>());
        Entity e = registry.Create();

        registry.Add(e, new Velocity());
        registry.Remove<Velocity>(e);

        Assert.Equal(0, observer.Count);
    }

    [Fact]
    public void Observer_RequiringAndExcludingSameType_Throws()
    {
        var registry = new Registry();
        using var observer = registry.Observer().OnAdd<Health>();
        observer.With<Position>();
        observer.Without<Velocity>();

        Assert.Throws<InvalidOperationException>(() => observer.Without<Position>());
        Assert.Throws<InvalidOperationException>(() => observer.With<Velocity>());
    }

    [Fact]
    public void Observer_RejectedFilter_AddsNothing()
    {
        var registry = new Registry();
        using var observer = registry.Observer().OnAdd<Health>();
        observer.With<Position>();
        Record.Exception(() => observer.Without<Position>());
        observer.Without<Velocity>();
        Record.Exception(() => observer.With<Velocity>());
        Entity e = registry.Create();

        registry.Add(e, new Position());
        registry.Add(e, new Health());

        Assert.Equal([e], observer.Entities.ToArray()); // Position alone required, Velocity alone excluded
    }

    [Fact]
    public void Observer_Dispose_Disconnects()
    {
        var registry = new Registry();
        var observer = registry.Observer().OnAdd<Health>();
        registry.Add(registry.Create(), new Health());

        observer.Dispose();
        registry.Add(registry.Create(), new Health());

        Assert.Equal(0, observer.Count);
    }

    [Fact]
    public void Observer_Disposed_RejectsBuilderCalls()
    {
        var registry = new Registry();
        var observer = registry.Observer().OnAdd<Health>();
        observer.Dispose();

        Assert.Throws<ObjectDisposedException>(() => observer.OnUpdate<Health>());
        Assert.Throws<ObjectDisposedException>(() => observer.With<Health>());
    }
}
