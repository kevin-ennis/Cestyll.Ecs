# Views

A view walks every entity that passes its filters and has the 1 to 16 component types its callback takes. It drives the walk from its smallest pool, or from a `WithTag` tag set when that is more selective. Types live in `Cestyll.Ecs.Views`.

The examples below run in order against one registry.

```csharp
using Cestyll.Ecs;
using Cestyll.Ecs.Attachments;
using Cestyll.Ecs.Groups;
using Cestyll.Ecs.Relations;
using Cestyll.Ecs.Tags;
using Cestyll.Ecs.Views;

struct Position : IComponent { public float X, Y; }
struct Velocity : IComponent { public float X, Y; }
struct Health : IComponent { public int Value; }
struct Frozen : ITag { }
struct Dead : ITag { }
struct Parent : IRelation
{
    public static OnTargetDestroyed Policy => OnTargetDestroyed.DestroySources;
}

enum StatusKind { Burn, Frozen }

struct Status : IAttachment<StatusKind>
{
    public StatusKind Kind;
    public readonly StatusKind Key => Kind;
}

var registry = new Registry();
```

## Creating a view

`registry.View()` returns a `View<TEntity>`. Filters chain onto it, and the component types come last, from the callback of `Each` or from `Rows<T1, ...>()`. The walk looks the pools up when it runs, so it sees entities added after the view was built.

```csharp
var all = registry.View();

Entity32 ship = registry.Create(new Position(), new Velocity { X = 1 });
// all.Each((Entity32 e, ref Position p, ref Velocity v) => ...) now walks ship.
```

Each filter step allocates, so keep a view you walk every frame in a field.

## Each

`Each` calls a `RefAction<TEntity, T1, ...>` once per matching entity, with the entity and a `ref` to each component. Write out the lambda's parameter types, since they name the components.

```csharp
registry.View().Each((Entity32 e, ref Position p, ref Velocity v) =>
{
    p.X += v.X;
    p.Y += v.Y;
});
```

## Each with a struct callback

Implement `IEach<TSelf, TEntity, T1, ...>` on a struct, with the struct itself as `TSelf`, and pass it by `ref`. The struct names its own types, so `Each(ref fn)` takes no type arguments. `Invoke` inlines, nothing is allocated, and fields carry state in and out.

```csharp
struct Move : IEach<Move, Entity32, Position, Velocity>
{
    public float Dt;
    public int Moved;

    public void Invoke(Entity32 e, ref Position p, ref Velocity v)
    {
        p.X += v.X * Dt;
        p.Y += v.Y * Dt;
        Moved++;
    }
}

var move = new Move { Dt = 0.016f };
registry.View().Each(ref move);
Console.WriteLine(move.Moved);
```

## Rows and foreach

`Rows<T1, ...>()` names the components up front and returns a `ViewRows<TEntity, T1, ...>`, which has `Each(action)`, `Each(ref fn)` and `foreach`. Each row has `Entity` and a `ref` field per component, `C1` to `C16`. `row.Get<T>()` returns a `ref` to the component by type rather than position.

```csharp
foreach (var row in registry.View().Rows<Position, Velocity, Health>())
{
    row.C1.X += row.C2.X;
    row.Get<Health>().Value -= 1;
    Console.WriteLine($"{row.Entity}: {row.C1.X}");
}
```

## Rows without components

`Rows()` with no type arguments returns a `ViewRows<TEntity>` that a `foreach` walks entity by entity. Use it when the filters pick the entities and the walk reads no components.

```csharp
foreach (var e in registry.View().WithTag<Frozen>().WithoutTag<Dead>().Rows())
{
    Console.WriteLine(e);
}
```

The view needs something to drive the walk: a pin, `Use(group)`, or a `With` filter other than a live tag or an attachment key. Otherwise `Rows()` throws `InvalidOperationException`. A view with only tag filters walks in ascending index order.

## Filters

`With<T>()` and `Without<T>()` require or exclude a component without fetching it. `WithTag<T>()` and `WithoutTag<T>()` do the same for tags, and `WithLiveTag<T>()` and `WithoutLiveTag<T>()` test the tag as the walk goes. Each returns a new view, so filters chain without limit. See [Live tag filters](#live-tag-filters).

```csharp
registry.View()
    .With<Velocity>()
    .Without<Health>()
    .WithTag<Frozen>()
    .WithoutTag<Dead>()
    .Each((Entity32 e, ref Position p) =>
    {
        p.X = 0;
    });
```

`WithAttachment<T>()` and `WithoutAttachment<T>()` keep entities holding at least one, or no, attachment of type `T`. See [Attachments](attachments.md#views-and-destroying).

`WithAttachment<T, TKey>(key)` and `WithoutAttachment<T, TKey>(key)` test one key instead. C# can't infer `TKey` from `T`, so name both.

```csharp
var thawed = registry.View().WithoutAttachment<Status, StatusKind>(StatusKind.Frozen);
```

`WithInbox<T>()` and `WithoutInbox<T>()` keep entities with at least one, or no, message of type `T` in their inbox. See [Inboxes](inboxes.md#views-and-destroying).

### Relations and links

These filter on which end of a relation or link set an entity is on. See [Relations](relations.md).

- `WithRelationTarget<TRelation>()` and `WithoutRelationTarget<TRelation>()`: the entity has, or has no, target in the exclusive relation. For `Parent`, children and roots.
- `WithRelationSources<TRelation>()` and `WithoutRelationSources<TRelation>()`: some entity targets it, or none does. For `Parent`, parents and leaves.
- `WithLinkTargets<T>()` and `WithoutLinkTargets<T>()`: the entity links to at least one entity in the link set, or to none.
- `WithLinkSources<T>()` and `WithoutLinkSources<T>()`: at least one entity links to it, or none does.

```csharp
// Roots of every Parent hierarchy.
registry.View().WithoutRelationTarget<Parent>().Each((Entity32 e, ref Position p) =>
{
    p.Y = 0;
});

// Leaves: parented, with no children of their own.
var leaves = registry.View().WithRelationTarget<Parent>().WithoutRelationSources<Parent>();
```

`WithRelationTarget<TRelation>(target)` and `WithoutRelationTarget<TRelation>(target)` test one target instead: the entity's target in the relation is, or isn't, `target`. `WithLinkTarget<T>(target)` and `WithoutLinkTarget<T>(target)` do the same for a link set: the entity links, or doesn't link, to `target`.

The first `WithRelationTarget(target)` or `WithLinkTarget(target)` drives the walk from the target's sources, last first, so it costs the number of sources, not the size of the pools. A pin or `Use(group)` drives instead when the view has one, and the filter only tests. The walk follows the [rules for walking sources](relations.md#what-a-walk-may-change).

```csharp
// Every turret on the ship that has Health.
registry.View().WithRelationTarget<Parent>(ship).Each((Entity32 e, ref Health h) =>
{
    h.Value -= 5;
});
```

## Live tag filters

`WithLiveTag<T>()` and `WithoutLiveTag<T>()` test the tag at each entity as the walk reaches it, rather than once when the walk starts. Use them when the walk changes that tag.

```csharp
registry.View()
    .WithoutLiveTag<Dead>()
    .Each((Entity32 e, ref Health h) =>
    {
        if (h.Value <= 0)
        {
            registry.Tag<Dead>(e);
        }
    });
```

## Named pools

`Rows(pool, ...)` walks the pools you pass, in order, such as named pools from `registry.Storage<T>(name)`. It takes pools, not names, so a default and a named pool of one type can share a walk. `With<T>(name)` and `Without<T>(name)` filter on a named pool, making it if missing. See [Storage](storage.md).

```csharp
var previous = registry.Storage<Position>("previous");

registry.View()
    .Rows(registry.Storage<Position>(), previous)
    .Each((Entity32 e, ref Position now, ref Position before) =>
    {
        before = now;
    });

var notTracked = registry.View().Without<Position>("previous");
```

## Pinning the walk

A pin fixes which store drives the walk. Filters still apply, and a later `Use`, `UseTag` or `Join` replaces or drops the pin.

`Use<T>()` drives from `T`'s default pool, last slot first, so a view over a sorted pool visits entities in sort order. The callback must fetch `T`. `Use<T>(name)` drives from a named pool, which must be one of the pools walked.

```csharp
registry.Sort<Position>((a, b) => a.X.CompareTo(b.X));

registry.View()
    .Use<Position>()
    .Each((Entity32 e, ref Position p, ref Velocity v) =>
    {
        p.X += v.X;
    });

var byPrevious = registry.View().Use<Position>("previous").Rows(registry.Storage<Position>(), previous);
```

`UseTag<T>()` drives from tag `T`'s set, in ascending index order. It implies `WithTag<T>()`.

```csharp
var frozen = registry.View().UseTag<Frozen>();
```

`Use(group)` drives from a [non-owning group](groups.md#non-owning-groups)'s members, last first. It pays off when the pools hold many entities the view doesn't match. The group should require the view's types with `With`. Walk an owning group with its own [`Each`](groups.md#each).

```csharp
var both = registry.Group().With<Position>().With<Velocity>().Build();

var fromGroup = registry.View().Use(both);
```

## Joining views

`Join(other)` makes one view with both views' filters and no pin. To walk a named pool along, pass it to `Rows`.

```csharp
var alive = registry.View().WithoutTag<Dead>();
var unhurt = registry.View().Without<Health>();

alive.Join(unhurt).Each((Entity32 e, ref Position p, ref Velocity v) =>
{
    p.X += v.X;
});

var withPrevious = alive.Join(unhurt).Rows(registry.Storage<Position>(), previous);
```

## Counting

Views have no count. For a whole pool use [`registry.ComponentCount<T>()`](registry.md#counts). To count a filtered view, count in the walk.

```csharp
var count = 0;
registry.View().WithoutTag<Frozen>().Each((Entity32 e, ref Position p, ref Velocity v) =>
{
    count++;
});
```

## What a walk may change

Inside `Each` or a `foreach`, the callback may change the current entity's components in place, remove its components, or destroy it. It may not:

- add components to any entity, or pull an entity into an owning group over a walked pool.
- remove components from, or destroy, any other entity. This includes a `Destroy` of the current entity that cascades through a relation.
- sort any pool or group.
- call `ShrinkToFit` on the registry, or on any pool, tag set or attachment set the walk reads.

DEBUG builds of the library throw `InvalidOperationException` for adding to a walked pool, pulling an entity into a group that owns one, sorting one, or calling `ShrinkToFit` on one. Removing from or destroying another entity isn't caught. Release builds check nothing.

For anything else, record it in a [command buffer](commands.md) and `Flush` after the walk, or let the registry record it with `Defer()` or `AutoDefer`.

## See also

- [Registry](registry.md)
- [Storage](storage.md)
- [Tags](tags.md)
- [Groups](groups.md)
- [Attachments](attachments.md)
- [Inboxes](inboxes.md)
- [Command buffers](commands.md)
- [SIMD](simd.md)
