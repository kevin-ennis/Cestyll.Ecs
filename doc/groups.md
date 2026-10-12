# Groups

A group tracks the entities that have a set of components. An owning group keeps its pools packed so slot `i` of each holds the same entity, which turns a query into a loop over aligned spans. A non-owning group reorders nothing and keeps its members in a set of its own. Both are a `Group<TEntity>`. Types live in `Cestyll.Ecs.Groups`.

The examples below run in order against one registry.

```csharp
using Cestyll.Ecs;
using Cestyll.Ecs.Groups;
using Cestyll.Ecs.Relations;
using Cestyll.Ecs.Tags;

struct Position : IComponent { public float X, Y; }
struct Velocity : IComponent { public float X, Y; }
struct Health : IComponent { public int Value; }
struct Frozen : ITag { }
struct Parent : IRelation
{
    public static OnTargetDestroyed Policy => OnTargetDestroyed.DestroySources;
}

var registry = new Registry();
```

## Owning groups

`registry.Group()` returns a `GroupBuilder<TEntity>`. `Own<T>()` makes the group own `T`'s pool, and `Build()` returns the `Group<TEntity>`. Existing entities are packed in at once, and entities join and leave as components are added and removed.

Building again with the same steps, in any order, returns the same group.

```csharp
var movers = registry.Group().Own<Position>().Own<Velocity>().Build();
var tough = registry.Group().Own<Position>().Own<Velocity>().Own<Health>().Build();
var again = registry.Group().Own<Velocity>().Own<Position>().Build(); // same instance as movers
```

Each builder step allocates, so keep a group you use every frame in a field.

## Count, Entities and component spans

`Count` is the number of grouped entities and `Entities` a span of them. `Contains(e)` tells whether `e` is grouped, and `Owning` is true for an owning group. `Span<T>()` is a writable span of an owned component. Slot `i` of every span belongs to `Entities[i]`.

```csharp
var positions = movers.Span<Position>();
var velocities = movers.Span<Velocity>();
for (var i = 0; i < movers.Count; i++)
{
    positions[i].X += velocities[i].X;
    Console.WriteLine(movers.Entities[i]);
}
```

`Rows<T1, ...>()` takes 1 to 16 owned types and returns a `GroupRows<TEntity, T1, ...>` with `Count`, `Entities` and a span per type by position, `C1` to `C16`.

```csharp
var rows = movers.Rows<Position, Velocity>();
for (var i = 0; i < rows.Count; i++)
{
    rows.C1[i].Y += rows.C2[i].Y;
}
```

To walk the spans as 256-bit vectors, see [SIMD](simd.md).

## Each

`Each` calls a `RefAction<TEntity, T1, ...>` once per grouped entity, last first. Write out the lambda's parameter types, since they name the components. Every type must be owned or required with `With`, or `Each` throws `InvalidOperationException`.

`Each(ref fn)` takes a struct callback, as on [views](views.md#each-with-a-struct-callback). `Rows<T1, ...>()` has the same two `Each` forms, over owned types only.

```csharp
movers.Each((Entity32 e, ref Position p, ref Velocity v) =>
{
    p.X += v.X;
});
```

A group's `Each` follows the same rules as a view walk. See [What a walk may change](views.md#what-a-walk-may-change). With `AutoDefer` on, it is a defer scope.

## Filters

Filter steps on the builder narrow membership.

- `With<T>()`: the entity must have component `T`, but the group doesn't own its pool.
- `Without<T>()`: the entity must not have component `T`.
- `WithTag<T>()`: the entity must have tag `T`.
- `WithoutTag<T>()`: the entity must not have tag `T`.
- `WithAttachment<T>()` and `WithoutAttachment<T>()`: the entity must hold at least one, or no, attachment of type `T`.
- `WithInbox<T>()` and `WithoutInbox<T>()`: the entity must have at least one, or no, message of type `T`.
- `WithRelationTarget<TRelation>()` and `WithoutRelationTarget<TRelation>()`: the entity must have, or have no, target in the relation.
- `WithRelationSources<TRelation>()` and `WithoutRelationSources<TRelation>()`: some entity must target it, or none may.
- `WithLinkTargets<T>()` and `WithoutLinkTargets<T>()`: the entity must link to at least one entity in the link set, or to none.
- `WithLinkSources<T>()` and `WithoutLinkSources<T>()`: at least one entity must link to it, or none may.

Group filters always use a type's default pool. There is no named form like a view's `With<T>(name)`, and no target form. To walk one target's sources, use a view's `WithRelationTarget<TRelation>(target)` or `WithLinkTarget<T>(target)`.

```csharp
var active = registry.Group().Own<Position>().Own<Velocity>().Own<Health>().WithoutTag<Frozen>().Build();
```

Entities join and leave as their attachments, messages and links change.

```csharp
// Everything with a Position that has a parent.
var children = registry.Group().With<Position>().WithRelationTarget<Parent>().Build();
```

## With rules

A `With<T>()` rule requires `T` without owning its pool, so this group never reorders it. `Storage<T>()` returns the pool of an owned or `With` component. A `With` pool isn't aligned with `Entities`, so read it by lookup, or let `Each` do it.

```csharp
var moving = registry.Group().Own<Position>().With<Velocity>().Build();

var velocityPool = moving.Storage<Velocity>();
var movingPositions = moving.Span<Position>();
for (var i = 0; i < moving.Count; i++)
{
    movingPositions[i].X += velocityPool.Get(moving.Entities[i]).X;
}
```

`GetSlots<T>(slots)` fills `slots[i]` with the slot of `Entities[i]` in `T`'s pool, in one batched pass.

```csharp
var slots = new int[moving.Count];
moving.GetSlots<Velocity>(slots);
for (var i = 0; i < slots.Length; i++)
{
    movingPositions[i].Y += velocityPool.At(slots[i]).Y;
}
```

## Nested groups

Groups may share owned pools only if they nest: the more restrictive one owns every pool the other owns, and requires and excludes everything the other does. Its members are then a prefix of the other's, in the same slots. `movers`, `tough`, `active` and `moving` above all nest.

```csharp
// movers.Entities[..tough.Count] holds the same entities as tough.Entities.
// This would throw:
// registry.Group().Own<Position>().Own<Health>().Build();
```

## Sorting an owning group

`Sort<T>(compare)` reorders the grouped entities by an owned component and moves every owned pool along, so the spans stay aligned. `SortEntities(compare)` does the same by entity.

```csharp
active.Sort<Position>((a, b) => a.X.CompareTo(b.X));
active.SortEntities((a, b) => a.Index.CompareTo(b.Index));
```

## Non-owning groups

A group built with no `Own` is non-owning. It is the same `Group<TEntity>`, with `Owning` false. It reorders no pool, so it can sit alongside any owning group. It needs at least one `With` filter other than `WithTag`.

```csharp
var walkers = registry.Group().With<Position>().With<Velocity>().Without<Health>().Build();
```

`Count`, `Entities` and `Contains(e)` read its members. It has no aligned spans, so `Span<T>()` and `Rows` throw `InvalidOperationException`. Read components by lookup, through `Storage<T>()` or `GetSlots<T>(slots)` as on owning groups, or with `Each`, which looks up every component.

```csharp
Entity32 ship = registry.Create(new Position(), new Velocity { X = 1 });
var walks = walkers.Contains(ship); // True

var walkerPositions = walkers.Storage<Position>();
var walkerVelocities = walkers.Storage<Velocity>();
foreach (var e in walkers.Entities)
{
    walkerPositions.Get(e).X += walkerVelocities.Get(e).X;
}

walkers.Each((Entity32 e, ref Position p, ref Velocity v) =>
{
    p.Y += v.Y;
});
```

`Sort<T>(compare)` and `SortEntities(compare)` reorder the members only. `ShrinkToFit()` trims the member set's spare capacity. On an owning group it does nothing, so trim its pools through the registry.

```csharp
walkers.Sort<Position>((a, b) => a.X.CompareTo(b.X));
walkers.ShrinkToFit();
```

A view can walk a non-owning group's members with [`Use(group)`](views.md#pinning-the-walk).

## Dispose

Disposing a group stops it tracking, leaves it reading as empty, and releases its pools so they can be sorted or owned again. Building it again afterwards makes a new group.

```csharp
active.Dispose();
walkers.Dispose();
var fresh = registry.Group().Own<Position>().Own<Velocity>().Own<Health>().WithoutTag<Frozen>().Build(); // a new group
```

## See also

- [Registry](registry.md)
- [Storage](storage.md)
- [Tags](tags.md)
- [Views](views.md)
- [SIMD](simd.md)
