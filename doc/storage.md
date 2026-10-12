# Storage

Each component type lives in a `Pool<TEntity, T>`: a sparse set mapping entities to packed slots, with components in a parallel array. Each type gets one default pool, plus any named pools. Types live in `Cestyll.Ecs.Storage`.

```csharp
using Cestyll.Ecs;
using Cestyll.Ecs.Storage;

struct Position : IComponent { public float X, Y; }
struct Velocity : IComponent { public float X, Y; }
struct Health : IComponent { public int Value; }

var registry = new Registry();
```

## Getting a pool

`Storage<T>()` returns `T`'s default pool, making it on first use.

```csharp
var positions = registry.Storage<Position>();
```

`FindStorage<T>()` returns the pool or null. `FindStorage(type)` does the same by runtime `Type` and returns a `SparseSet<TEntity>`.

```csharp
Pool<Entity32, Health>? health = registry.FindStorage<Health>();
SparseSet<Entity32>? byType = registry.FindStorage(typeof(Health));
```

## Count and Entities

`Count` is the number of entities in the pool. `Entities` and `Components` are the packed arrays: slot `i` of one belongs to slot `i` of the other.

```csharp
var pool = registry.Storage<Position>();
var entities = pool.Entities;
var components = pool.Components;
for (var i = 0; i < pool.Count; i++)
{
    Console.WriteLine($"{entities[i]}: {components[i].X}");
}
```

## Add, Get, Replace and AddOrReplace

These work as their [registry counterparts](registry.md#add) do, on this pool only.

```csharp
var positions = registry.Storage<Position>();
Entity32 e = registry.Create();

positions.Add(e, new Position { X = 1 });
positions.Get(e).Y = 2;
positions.Replace(e, new Position { X = 3 });
positions.AddOrReplace(e, new Position { X = 4 });
```

## Patch

`Patch(e, edit)` edits the component in place and fires `OnUpdate`. `Patch(e)` fires `OnUpdate` after you changed it through a ref.

```csharp
var health = registry.Storage<Health>();
health.Add(e, new Health { Value = 100 });
health.Patch(e, static (ref Health h) => h.Value -= 10);

health.Get(e).Value = 1;
health.Patch(e);
```

## IndexOf, Has and At

`IndexOf(e)` returns `e`'s slot, or -1. `Has(e)` tests membership. `At(slot)` returns a ref to a slot's component.

```csharp
var positions = registry.Storage<Position>();

var slot = positions.IndexOf(e);
if (slot >= 0)
{
    positions.At(slot).X += 1;
}

var has = positions.Has(e);
```

## IndexOfMany

`IndexOfMany(entities, slots)` fills `slots[i]` with `IndexOf(entities[i])` in one pass, so the CPU can overlap the lookups.

```csharp
var targets = registry.CreateMany(4);
var slots = new int[targets.Length];
registry.Storage<Position>().IndexOfMany(targets, slots);
```

## Remove

`Remove(e)` takes `e` out and returns whether it was there. The last slot fills the hole. `Remove(entities)` returns how many it removed.

```csharp
var velocities = registry.Storage<Velocity>();
velocities.Remove(e);

var removed = velocities.Remove(velocities.Entities);
```

## Add and AddValues on many entities

These match the [registry's](registry.md#add-and-addvalues-on-many-entities) `Add(entities, value)` and `AddValues`.

```csharp
var entities = registry.CreateMany(3);
var velocities = registry.Storage<Velocity>();
velocities.Add(entities, new Velocity { X = 1 });

var healths = registry.Storage<Health>();
Health[] values = [new Health { Value = 10 }, new Health { Value = 20 }, new Health { Value = 30 }];
healths.AddValues(entities, values);
```

## Clear

`Clear()` removes every entity from the pool.

```csharp
registry.Storage<Velocity>().Clear();
```

## Capacity

`Reserve(capacity)` grows the pool up front, and `ShrinkToFit()` gives unused memory back. `Capacity` and `SparseCapacity` are the packed and sparse array lengths.

```csharp
var positions = registry.Storage<Position>();
positions.Reserve(10_000);
Console.WriteLine($"{positions.Count} of {positions.Capacity}, sparse {positions.SparseCapacity}");
positions.ShrinkToFit();
```

## Sorting

`Sort(compare)` orders the pool by component, `SortEntities(compare)` by entity. A view driven by the pool then visits in comparison order. The packed arrays hold that order reversed.

```csharp
var health = registry.Storage<Health>();
health.Sort((a, b) => a.Value.CompareTo(b.Value));
health.SortEntities((a, b) => a.Index.CompareTo(b.Index));
```

`SortAs(other)` puts the entities shared with `other` first, in `other`'s view order, then the rest.

```csharp
registry.Storage<Velocity>().SortAs(registry.Storage<Position>());
```

The registry has the same three by type.

```csharp
registry.Sort<Health>((a, b) => a.Value.CompareTo(b.Value));
registry.SortEntities<Health>((a, b) => a.Index.CompareTo(b.Index));
registry.SortAs<Velocity, Position>();
```

A multi-component view drives from its smallest pool, so pin the sorted one with `Use`. See [Views](views.md#pinning-the-walk).

## Signals

Pools raise `OnAdd`, `OnUpdate` and `OnRemove`. Connect with `registry.OnAdd<T>(handler)` and the like, or `+=` on the pool. See [Observers and signals](observers.md#pool-signals).

## Named storage

`Storage<T>(name)` returns a named pool of `T`, making it on first use. It keeps a second `T` per entity, such as last frame's position. `Name` is the pool's name, or null for a default pool.

```csharp
var current = registry.Storage<Position>();
var previous = registry.Storage<Position>("previous");

Entity32 e = registry.Create(new Position { X = 2 });
previous.Add(e, new Position { X = 1 });

Console.WriteLine($"{previous.Name}: {previous.Get(e).X} -> {current.Get(e).X}");
```

`FindStorage<T>(name)` and `FindStorage(type, name)` find a named pool without making it.

```csharp
var maybe = registry.FindStorage<Position>("previous");
var untyped = registry.FindStorage(typeof(Position), "previous");
```

`DropStorage<T>(name)` removes `T` from every entity in the pool, firing `OnRemove`, and forgets the pool. It returns false if there is none.

```csharp
registry.DropStorage<Position>("previous");
```

To walk a named pool, pass it to a view's `Rows(pool, ...)`, or name it in `Use<T>(name)`, `With<T>(name)` or `Without<T>(name)`. See [Views](views.md#named-pools). Signal calls take the name first: `registry.OnRemove<Position>("previous", handler)`.

## Listing pools

`Storages` walks every pool, default and named, in creation order. `GetStorages(e)` walks the pools `e` is in. Tell them apart by `ComponentType` and `Name`.

```csharp
foreach (var pool in registry.Storages)
{
    Console.WriteLine($"{pool.ComponentType?.Name} {pool.Name}: {pool.Count}");
}

foreach (var pool in registry.GetStorages(e))
{
    Console.WriteLine(pool.ComponentType?.Name);
}
```

## SparseSet

`SparseSet<TEntity>` is the base of every pool, holding the members that don't need `T`: `Count`, `Entities`, `Capacity`, `SparseCapacity`, `ComponentType`, `Name`, `IndexOf`, `IndexOfMany`, `Has`, `Remove`, `Reserve`, `ShrinkToFit`, `SortEntities` and `SortAs`. It is public because `Pool` derives from it, but only the library can subclass it. `RelationStore<TEntity>` and `AttachmentStore<TEntity>` are public for the same reason, as the bases of relation and link sets and of attachment sets.

```csharp
SparseSet<Entity32>? set = registry.FindStorage(typeof(Velocity));
if (set is not null && set.Has(e))
{
    set.Remove(e);
}
```

## See also

- [Registry](registry.md)
- [Views](views.md)
- [Groups](groups.md)
- [Observers and signals](observers.md)
- [Command buffers](commands.md)
- [Snapshots](snapshots.md)
- [SIMD](simd.md)
