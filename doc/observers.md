# Observers and signals

Every store raises signals when entities gain, change or lose what it holds. Connect a handler to react inside the write that caused it, or let an observer collect the affected entities into a set you walk later. `Observer<TEntity>` lives in `Cestyll.Ecs.Observers`. Relation, link set, attachment and inbox signals are covered in [Relations](relations.md), [Attachments](attachments.md) and [Inboxes](inboxes.md).

```csharp
using Cestyll.Ecs;
using Cestyll.Ecs.Attachments;
using Cestyll.Ecs.Inboxes;
using Cestyll.Ecs.Relations;
using Cestyll.Ecs.Tags;

struct Position : IComponent { public float X, Y; }
struct Velocity : IComponent { public float X, Y; }
struct Health : IComponent { public int Value; }
struct Selected : ITag { }
struct Hidden : ITag { }

struct Parent : IRelation
{
    public static OnTargetDestroyed Policy => OnTargetDestroyed.DestroySources;
}

struct Attacks : ILink<Entity32>
{
    public Entity32 Target { get; init; }
    public int Damage;
}

enum ItemType { Coin, Axe, Potion }

struct Item : IAttachment<ItemType>
{
    public ItemType Type;
    public int Count;

    public readonly ItemType Key => Type;
}

struct Hit : IMessage { public int Damage; }

var registry = new Registry();
```

## Pool signals

`Pool<TEntity, T>` has three `Action<TEntity>` events: `OnAdd`, `OnUpdate` and `OnRemove`. `registry.OnAdd<T>(handler)` and the like connect a handler and return a `Connection`. Pass a name first for a named pool: `registry.OnRemove<Health>("previous", handler)`.

### OnAdd

`OnAdd` fires when an entity gains `T`, after the value is written. `Add`, `AddValues` and `AddOrReplace` on an entity without `T` fire it once per entity.

```csharp
registry.OnAdd<Position>(e => registry.Tag<Selected>(e));

Entity32 ship = registry.Create();
registry.Add(ship, new Position { X = 3 }); // handler runs here
```

### OnUpdate

`OnUpdate` fires after `Replace`, `Patch` or `AddOrReplace` on an entity that already has `T`. Writes through a ref or span fire nothing, so call `Patch(e)` afterwards to signal them.

```csharp
Entity32 e = registry.Create();
registry.Add(e, new Health { Value = 10 });

registry.Replace(e, new Health { Value = 8 });
registry.Patch(e, static (ref Health h) => h.Value -= 1);
registry.AddOrReplace(e, new Health { Value = 5 });

registry.Get<Health>(e).Value = 2; // no signal
registry.Patch<Health>(e);         // fires OnUpdate for the write above
```

### OnRemove

`OnRemove` fires before `T` is removed, so the handler can still read it. `Remove<T>`, `Clear<T>` and `Destroy` fire it.

```csharp
registry.OnRemove<Health>(e =>
{
    var last = registry.Get<Health>(e).Value;
    Console.WriteLine($"{e} had {last} health left");
});

Entity32 ship = registry.Create();
registry.Add(ship, new Health { Value = 3 });
registry.Destroy(ship);
```

### Unsubscribing

Dispose the `Connection` to disconnect the handler.

```csharp
var healed = registry.OnUpdate<Health>(e => Console.WriteLine($"{e} healed"));
healed.Dispose();
```

## Tag signals

`TagSet<TEntity>` has `OnTag` and `OnUntag`, connected with `registry.OnTag<T>(handler)` and `registry.OnUntag<T>(handler)`. See [Tags](tags.md#signals). `OnUntag` fires while the entity still has the tag.

```csharp
using var unselected = registry.OnUntag<Selected>(e => Console.WriteLine($"{e} still tagged: {registry.HasTag<Selected>(e)}"));

Entity32 ship = registry.Create();
registry.Tag<Selected>(ship);
registry.Untag<Selected>(ship); // handler prints True
```

## Observers

An observer is a reactive entity set. It collects each entity once when any signal it watches fires, and keeps it until you call `Clear` or the entity is destroyed.

### Observer, OnAdd, OnUpdate and OnRemove

`registry.Observer()` returns a new, empty `Observer<TEntity>`. `OnAdd<T>()`, `OnUpdate<T>()` and `OnRemove<T>()` collect entities when `T` is added, updated or removed. Each returns the observer, so calls chain.

```csharp
using var changed = registry.Observer()
    .OnAdd<Health>()
    .OnUpdate<Health>()
    .OnRemove<Position>();

// the named form watches registry.Storage<Position>("previous")
using var previous = registry.Observer().OnUpdate<Position>("previous");
```

### OnTag and OnUntag

`OnTag<T>()` and `OnUntag<T>()` collect entities when tag `T` is set or cleared.

```csharp
using var selected = registry.Observer().OnTag<Selected>();
using var deselected = registry.Observer().OnUntag<Selected>();
```

### OnRelate, OnUnrelate, OnLink, OnLinkUpdate and OnUnlink

`OnRelate`, `OnUnrelate`, `OnLink`, `OnLinkUpdate` and `OnUnlink` collect the source of a relation or link pair when it is linked, updated or unlinked. The link-set update watch is `OnLinkUpdate`, since `OnUpdate<T>()` is the component watch.

```csharp
using var reparented = registry.Observer().OnRelate<Parent>().OnUnrelate<Parent>();
using var targeting = registry.Observer().OnLink<Attacks>().OnLinkUpdate<Attacks>();
```

### OnAttach, OnAttachmentUpdate and OnDetach for attachments

`OnAttach<T, TKey>()`, `OnAttachmentUpdate<T, TKey>()` and `OnDetach<T, TKey>()` collect the entity holding an attachment when one is attached, updated or detached. Each takes both type arguments, as `Attach` does.

```csharp
using var inventoryChanged = registry.Observer()
    .OnAttach<Item, ItemType>()
    .OnAttachmentUpdate<Item, ItemType>()
    .OnDetach<Item, ItemType>();
```

### OnPost for inboxes

`OnPost<T>()` collects the entity a message of type `T` was posted to.

```csharp
using var struck = registry.Observer().OnPost<Hit>();
```

### With and Without

`With<T>()` and `Without<T>()` collect only entities that have, or lack, component `T` when the signal fires. Both have a named form for a named pool.

```csharp
using var hurt = registry.Observer()
    .OnUpdate<Health>()
    .With<Position>()
    .Without<Velocity>();
```

Excluding a type the observer watches throws, since signals fire while the entity still has it.

```csharp
using var observer = registry.Observer().OnAdd<Position>();

observer.With<Position>();       // fine
// observer.Without<Position>(); // throws InvalidOperationException
```

### WithTag and WithoutTag

`WithTag<T>()` and `WithoutTag<T>()` collect only entities that have, or lack, tag `T` when the signal fires.

```csharp
using var visible = registry.Observer()
    .OnAdd<Position>()
    .WithTag<Selected>()
    .WithoutTag<Hidden>();
```

### Other store filters

`WithAttachment<T>()`, `WithInbox<T>()`, `WithRelationTarget<TRelation>()`, `WithRelationSources<TRelation>()`, `WithLinkTargets<T>()` and `WithLinkSources<T>()`, each with a `Without` form, filter as on a [group](groups.md). Requiring and excluding the same one throws.

```csharp
using var orphans = registry.Observer()
    .OnAdd<Position>()
    .WithoutRelationTarget<Parent>()
    .WithAttachment<Item>();
```

### Count, Entities and Contains

`Count` is the number of entities collected. `Entities` is a `ReadOnlySpan` of them in collection order, and `Contains` tests one entity.

```csharp
using var spawned = registry.Observer().OnAdd<Position>();
Entity32 e = registry.Create();
registry.Add(e, new Position());

Console.WriteLine(spawned.Count);       // 1
Console.WriteLine(spawned.Contains(e)); // True
```

### Walking and Clear

Walk `Entities`, then call `Clear` to start the next batch. Membership means a signal fired since the last `Clear`, not that the entity still has the component, so check before `Get`.

```csharp
using var moved = registry.Observer().OnUpdate<Position>();

foreach (var e in moved.Entities)
{
    if (registry.Has<Position>(e))
    {
        var p = registry.Get<Position>(e);
        Console.WriteLine($"{e} moved to {p.X}, {p.Y}");
    }
}

moved.Clear();
```

### ShrinkToFit

`ShrinkToFit` trims the observer's lookup to what it holds now. `registry.ShrinkToFit()` does this for every live observer.

```csharp
moved.ShrinkToFit();
```

### Dispose

`Dispose` disconnects the observer from every signal and forgets its entities.

```csharp
var hidden = registry.Observer().OnTag<Hidden>();
hidden.Dispose();
```

## See also

- [Registry](registry.md)
- [Requirements](requirements.md)
- [Storage](storage.md)
- [Tags](tags.md)
- [Groups](groups.md)
- [Relations](relations.md)
- [Attachments](attachments.md)
- [Inboxes](inboxes.md)
- [Command buffers](commands.md)
- [Events](events.md)
