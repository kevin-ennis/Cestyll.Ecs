# Registry

The registry owns every entity and every store. Its type argument is the entity layout, such as `Entity32`, `Entity64` or a [custom layout](#custom-layouts), and the examples use a [`Registry` alias](#registry-alias) for `Registry<Entity32>`. Types live in `Cestyll.Ecs`.

```csharp
using Cestyll.Ecs;

struct Position : IComponent { public float X, Y; }
struct Velocity : IComponent { public float X, Y; }
struct Health : IComponent { public int Value; }

var registry = new Registry();
```

## Threading

A registry is single-threaded. Call it from one thread at a time, reads included, and the same goes for its stores, views, groups, observers, command buffers and handles. Separate registries can run on separate threads: type ids are thread-safe and walk buffers are per-thread. The event `Dispatcher` is single-threaded too.

## Registry alias

Name the layout once per project with a global alias. The alias is the same type as `Registry<Entity32>`, so a method that takes `Registry` accepts the registry the library passes back, such as in a [requirement](requirements.md)'s `make`.

```csharp
// GlobalUsings.cs
global using Registry = Cestyll.Ecs.Registry<Cestyll.Ecs.Entity32>;
```

## Components

Mark component types with `IComponent`, as above. Component calls don't compile for a tag or an unmarked type.

## Create

`Create()` makes an entity and returns an `EntityHandle`, which converts to the entity. `Create(c1, ...)` also adds 1 to 16 components.

```csharp
Entity32 e = registry.Create();
Entity32 ship = registry.Create(new Position(), new Velocity { X = 1 }, new Health { Value = 100 });
Entity32 rock = registry.Create().Add(new Position { X = 10 });
```

The handle takes fluent edits. See [Entity handles](entity-handles.md).

## CreateAt

`CreateAt(hint)` makes the entity at `hint`'s index and version if that index is free, and a plain new entity otherwise.

```csharp
Entity32 old = registry.Create();
registry.Destroy(old);

Entity32 back = registry.CreateAt(old);
var restored = back == old; // True
```

## Destroy and IsAlive

`Destroy(e)` removes everything the entity has, drops it from every relation and link set and frees its index. `IsAlive(e)` tells whether a handle still names a live entity.

```csharp
Entity32 e = registry.Create(new Position());
registry.Destroy(e);

var alive = registry.IsAlive(e); // False
registry.Destroy(e);             // no-op
```

## Add

`Add(e, value)` gives `e` a component and returns a ref to the stored value.

```csharp
Entity32 e = registry.Create();
registry.Add(e, new Velocity { X = 1 });
registry.Add(e, new Health()).Value = 50;
```

## Get, Has and TryGet

`Get<T>(e)` returns a ref and throws `KeyNotFoundException` if `e` has no `T`. `Has<T>(e)` tests for one. `TryGet(e, out value)` copies it out.

```csharp
registry.Get<Position>(e).X += 1;

if (registry.Has<Velocity>(e))
{
    registry.Get<Velocity>(e).Y = 2;
}

if (registry.TryGet(e, out Health health))
{
    Console.WriteLine(health.Value);
}
```

## GetOrAdd

`GetOrAdd(e, value)` returns `e`'s component, adding `value` first if missing. `GetOrAdd<T>(e)` adds `default(T)`.

```csharp
registry.GetOrAdd(e, new Health { Value = 100 }).Value -= 10;
registry.GetOrAdd<Velocity>(e).X = 3;
```

## Replace and AddOrReplace

`Replace(e, value)` overwrites a component and throws `KeyNotFoundException` if missing. `AddOrReplace(e, value)` adds or overwrites. Both return a ref and fire `OnUpdate` on overwrite.

```csharp
registry.Replace(e, new Position { X = 5, Y = 5 });
registry.AddOrReplace(e, new Velocity { X = 2 });
```

## Patch

`Patch(e, edit)` edits a component in place, then fires `OnUpdate`. `Patch<T>(e)` fires `OnUpdate` after you changed it through a ref.

```csharp
registry.Patch(e, static (ref Health h) => h.Value -= 10);

registry.Get<Health>(e).Value = 1;
registry.Patch<Health>(e);
```

## Remove

`Remove<T>(e)` takes a component off and returns whether it was there.

```csharp
var removed = registry.Remove<Velocity>(e);
```

## HasAll and HasAny

`HasAll<T1, ...>(e)` tests for every listed component, `HasAny<T1, ...>(e)` for at least one. Both take 2 to 16 types.

```csharp
if (registry.HasAll<Position, Velocity>(e))
{
    registry.Get<Position>(e).X += registry.Get<Velocity>(e).X;
}

var hasAny = registry.HasAny<Health, Velocity, Position>(e);
```

## IsOrphan

`IsOrphan(e)` is true when `e` is alive and holds no component, tag, attachment, relation link or inbox message.

```csharp
if (registry.IsOrphan(e))
{
    registry.Destroy(e);
}
```

## Counts

`AliveCount` is the number of live entities. `ComponentCount<T>()` is the number with `T`.

```csharp
Console.WriteLine($"{registry.AliveCount} entities, {registry.ComponentCount<Velocity>()} moving");
```

## Walking every entity

`Entities` walks every live entity in ascending index order. `Each(action)` calls a delegate for each. To walk by component, use a [view](views.md).

```csharp
foreach (var entity in registry.Entities)
{
    if (!registry.Has<Position>(entity))
    {
        registry.Destroy(entity);
    }
}

registry.Each(entity => Console.WriteLine(entity));
```

## GetComponentTypes

`GetComponentTypes(e)` yields the `Type` of each default-pool component `e` has, in pool creation order. Named pools come from `GetStorages`.

```csharp
foreach (var type in registry.GetComponentTypes(e))
{
    Console.WriteLine(type.Name);
}
```

## Fingerprint

`Fingerprint<T>(hash)` mixes each entity in `T`'s pool and its value's hash into one `ulong`, to check that two runs reached the same state.

```csharp
var fingerprint = registry.Fingerprint<Health>(static h => (ulong)(uint)h.Value);
```

## Context

Context values are per-registry singletons keyed by type, for world state such as a clock. `AddOrReplaceContext(value)` stores or replaces one and returns a ref. `GetContext<T>()` returns a ref and throws `KeyNotFoundException` if there is none.

```csharp
struct Clock { public float Time; public int Frame; }
```

```csharp
registry.AddOrReplaceContext(new Clock());

ref var clock = ref registry.GetContext<Clock>();
clock.Frame++;
registry.GetContext<Clock>().Time += 0.016f;
```

`HasContext<T>()` tests for a value. `RemoveContext<T>()` removes it and returns whether there was one.

```csharp
if (registry.HasContext<Clock>())
{
    registry.RemoveContext<Clock>();
}
```

## Bulk create

`CreateMany(count)` returns an array of new entities. `CreateMany(span)` fills your span.

```csharp
var entities = registry.CreateMany(100);

Span<Entity32> more = stackalloc Entity32[16];
registry.CreateMany(more);
```

`CreateMany(count, c1, ...)` and `CreateMany(span, c1, ...)` give each new entity the same 1 to 16 components.

```csharp
var arrows = registry.CreateMany(1000, new Position(), new Velocity { X = 10 });
```

## Add and AddValues on many entities

`Add(entities, value)` adds one component value to every entity. `AddValues(entities, values)` adds `values[i]` to `entities[i]`.

```csharp
var entities = registry.CreateMany(3);
registry.Add(entities, new Velocity { X = 1 });

Health[] values = [new Health { Value = 10 }, new Health { Value = 20 }, new Health { Value = 30 }];
registry.AddValues<Health>(entities, values);
```

## Bulk destroy and remove

`Destroy(entities)` destroys each live entity in the span. `Remove<T>(entities)` removes `T` from each and returns how many had it.

```csharp
registry.Destroy(registry.Storage<Velocity>().Entities);

var stopped = registry.Remove<Velocity>(entities);
```

## Clear

`Clear<T>()` removes `T` from every entity. `Clear()` destroys every entity. For tags, see [`ClearTags`](tags.md#cleartags) and [`DestroyTagged`](tags.md#destroytagged).

```csharp
registry.Clear<Velocity>();
registry.Clear();
```

## Memory

`Reserve(entities)` pre-sizes entity bookkeeping and existing pools. `ShrinkToFit()` trims every store. `Compact(minWaste)` closes gaps in relation, link set and attachment storage, skipping slabs with less than `minWaste` (0 to 1) wasted.

```csharp
registry.Reserve(10_000);

registry.Compact(0.25);
registry.ShrinkToFit();
```

## Sorting and pools

`Sort<T>`, `SortEntities<T>` and `SortAs<T, TOther>` reorder a pool, and `Storage<T>()` returns it. See [Storage](storage.md).

```csharp
registry.Sort<Health>((a, b) => a.Value.CompareTo(b.Value));
```

## Deferring changes

Inside `registry.Defer()`, or a view walk with `AutoDefer` on, structural calls such as `Add`, `Remove`, `Destroy`, `Tag`, `Relate` and `Link` are recorded, not applied. See [Command buffers](commands.md#defer).

## Signals

The registry connects store signals, making the store if needed. Each returns a `Connection` that disconnects on dispose.

- Pools: `OnAdd<T>`, `OnUpdate<T>`, `OnRemove<T>`, each with a named form ([Observers and signals](observers.md#pool-signals)).
- Tag sets: `OnTag<T>`, `OnUntag<T>` ([Observers and signals](observers.md#tag-signals)).
- Relations: `OnRelate`, `OnUnrelate` ([Relations](relations.md#onrelate-and-onunrelate)).
- Link sets: `OnLink`, `OnLinkUpdate`, `OnUnlink` ([Relations](relations.md#onlink-onupdate-and-onunlink)).
- Attachment sets: `OnAttach`, `OnAttachmentUpdate`, `OnDetach` ([Attachments](attachments.md#onattach-onupdate-and-ondetach)).
- Inboxes: `OnPost<T>` ([Inboxes](inboxes.md#onpost)).

## Delegates

`RefAction<T>` edits a value in place, and `Patch` takes one. `MergeAction<T>` folds an incoming value into the current one, and [`AttachOrPatch`](attachments.md#attachorpatch) takes one.

```csharp
RefAction<Health> heal = static (ref Health h) => h.Value += 5;
registry.Patch(e, heal);

MergeAction<Health> keepHighest = static (ref Health current, in Health incoming) =>
    current.Value = Math.Max(current.Value, incoming.Value);
```

## Entity32 and Entity64

`Entity32` packs a 20-bit index and 12-bit version into a `uint`, for about a million entities. `Entity64` packs 32 bits of each into a `ulong`.

```csharp
var big = new Registry<Entity64>();
Entity64 e = big.Create();
```

Both have `Raw`, `Index`, `Version`, `IsNull`, `Null`, `Make(index, version)` and the constants `IndexBits`, `IndexMask` and `VersionMask`.

```csharp
Entity32 e = registry.Create();
Console.WriteLine($"{e.Index} v{e.Version} raw {e.Raw}");

var copy = Entity32.Make(e.Index, e.Version);
var same = copy == e; // True

Entity32 none = Entity32.Null;
var isNull = none.IsNull; // True
```

## Custom layouts

Implement `IEntity<TSelf>` on a readonly struct holding one unsigned integer to choose another index and version split.

```csharp
readonly record struct Entity24(uint Raw) : IEntity<Entity24>
{
    private const uint IndexMask = (1u << 24) - 1;

    public static int MaxIndex => (int)IndexMask;
    public static uint VersionMask => 0xFF;

    public int Index => (int)(Raw & IndexMask);
    public uint Version => Raw >> 24;
    public bool IsNull => Raw == 0;

    public static Entity24 Make(int index, uint version) =>
        new(((version & VersionMask) << 24) | ((uint)index & IndexMask));

    public static bool SameVersion(Entity24 a, Entity24 b) => (a.Raw ^ b.Raw) <= IndexMask;
}
```

```csharp
var registry = new Registry<Entity24>();
Entity24 e = registry.Create();
```

## See also

- [Entity handles](entity-handles.md)
- [Storage](storage.md)
- [Tags](tags.md)
- [Views](views.md)
- [Groups](groups.md)
- [Observers and signals](observers.md)
- [Command buffers](commands.md)
- [Snapshots](snapshots.md)
- [Relations](relations.md)
- [Attachments](attachments.md)
