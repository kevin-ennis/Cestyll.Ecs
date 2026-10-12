# Entity handles

An `EntityHandle<TEntity>` pairs an entity with its registry for fluent edits. Each call forwards to the matching registry method. `EntityHandle` lives in `Cestyll.Ecs`.

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
struct Frozen : ITag { }

var registry = new Registry();
```

## Getting a handle

`Create` and `CreateAt` return a handle. `Handle(e)` wraps an entity you already hold.

```csharp
var ship = registry.Create(new Position(), new Velocity { X = 1 });

Entity32 rock = registry.Create();
registry.Handle(rock).Add(new Position { X = 10 });
```

## Entity and Registry

The handle converts to its entity implicitly. `Entity` and `Registry` return the two halves.

```csharp
var handle = registry.Create(new Health { Value = 100 });

Entity32 e = handle;
var same = handle.Entity == e; // True
var owner = handle.Registry;
var alive = registry.IsAlive(handle);
```

## Chaining components

`Add`, `Replace`, `AddOrReplace` and `Patch` return the handle, so calls chain. `Patch<T>()` fires `OnUpdate` for a value already changed through a ref.

```csharp
Entity32 e = registry.Create()
    .Add(new Position())
    .Add(new Health { Value = 100 })
    .Replace(new Position { X = 5 })
    .AddOrReplace(new Velocity { X = 1 })
    .Patch(static (ref Health h) => h.Value -= 10)
    .Patch<Position>();
```

## Reading components

`Get<T>()`, `Has<T>()`, `TryGet(out value)`, `GetOrAdd(value)`, `GetOrAdd<T>()` and `Remove<T>()` work as on the registry.

```csharp
var e = registry.Handle(rock);

e.Get<Position>().X += 1;

if (e.TryGet(out Velocity velocity))
{
    Console.WriteLine(velocity.X);
}

e.GetOrAdd(new Health { Value = 50 }).Value -= 1;
e.GetOrAdd<Velocity>().Y = 2;

if (e.Has<Velocity>())
{
    e.Remove<Velocity>();
}
```

`HasAll<T1, ...>()` and `HasAny<T1, ...>()` test 2 to 16 component types at once.

```csharp
var e = registry.Create(new Position(), new Velocity());
var moving = e.HasAll<Position, Velocity>();
var anything = e.HasAny<Position, Velocity, Health>();
```

## Lifetime and inspection

`IsAlive`, `Destroy()` and `IsOrphan` forward to the registry. `ComponentTypes` and `TagTypes` are `GetComponentTypes(e)` and `GetTagTypes(e)`.

```csharp
var e = registry.Create(new Position());
e.Tag<Selected>();

foreach (var type in e.ComponentTypes)
{
    Console.WriteLine(type.Name);
}

foreach (var type in e.TagTypes)
{
    Console.WriteLine(type.Name);
}

e.Destroy();
var gone = !e.IsAlive; // True
```

## Tags

`Tag<T>()` returns the handle. `HasTag<T>()`, `Untag<T>()`, `HasAllTags<T1, ...>()` and `HasAnyTags<T1, ...>()` return a bool. See [Tags](tags.md).

```csharp
var e = registry.Create().Tag<Selected>().Tag<Frozen>();

var both = e.HasAllTags<Selected, Frozen>();
var either = e.HasAnyTags<Selected, Frozen>();

if (e.HasTag<Frozen>())
{
    e.Untag<Frozen>();
}
```

## Relations and link sets

`Relate<TRelation>(target)` sets an exclusive relation, `GetTarget<TRelation>()` and `TryGetTarget<TRelation>(out target)` read it and `Unrelate<TRelation>()` clears it. `SourceCount<TRelation>()` and `GetSources<TRelation>()` read the entities pointing at this one. `Link(link)` adds a link-set link and `Unlink<T>(target)` removes one. `GetLink<T>(target)`, `TryGetLink<T>(target, out link)`, `TargetCount<T>()` and `GetTargets<T>()` read links, and `Patch<T>(target, edit)` or `Patch<T>(target)` updates one. `Relate`, `Link` and `Patch` return the handle. See [Relations](relations.md).

```csharp
struct Parent : IRelation
{
    public static OnTargetDestroyed Policy => OnTargetDestroyed.DestroySources;
}

struct Attacking : ILink<Entity32>
{
    public Entity32 Target { get; init; }
    public float Damage;
}
```

```csharp
var parent = registry.Create();
var enemy = registry.Create();

var child = registry.Create()
    .Relate<Parent>(parent)
    .Link(new Attacking { Target = enemy, Damage = 5 });

var up = child.GetTarget<Parent>();
var children = parent.SourceCount<Parent>();
child.Patch<Attacking>(enemy, static (ref Attacking a) => a.Damage *= 2);
var damage = child.GetLink<Attacking>(enemy).Damage;
child.Unlink<Attacking>(enemy);
child.Unrelate<Parent>();
```

## Attachments

`Attach<T, TKey>(item)`, `AttachOrPatch<T, TKey>(item, merge)` and `Patch<T, TKey>(key, edit)` or `Patch<T, TKey>(key)` return the handle. `Detach<T, TKey>(key)` returns whether it removed an item. `GetAttachments<T>()` or `GetAttachments<T, TKey>()` returns a span, and `HasAttachments<T>()` tests for any. See [Attachments](attachments.md).

```csharp
enum ItemType { Coin, Gem }

struct Item : IAttachment<ItemType>
{
    public ItemType Type;
    public int Count;
    public readonly ItemType Key => Type;
}
```

```csharp
var player = registry.Create()
    .Attach<Item, ItemType>(new Item { Type = ItemType.Coin, Count = 10 })
    .AttachOrPatch<Item, ItemType>(
        new Item { Type = ItemType.Coin, Count = 5 },
        static (ref Item current, in Item incoming) => current.Count += incoming.Count);

foreach (var item in player.GetAttachments<Item>())
{
    Console.WriteLine($"{item.Type}: {item.Count}");
}

if (player.HasAttachments<Item>())
{
    player.Detach<Item, ItemType>(ItemType.Coin);
}
```

## Inboxes

`Post<T>(message)` posts to the entity's inbox and returns the handle, so posts chain. Inside a defer scope it is recorded and posted when the scope ends. `GetMessages<T>()` and `HasMessages<T>()` read it. See [Inboxes](inboxes.md).

```csharp
struct Hit : IMessage { public int Damage; }
```

```csharp
var orc = registry.Create()
    .Add(new Health { Value = 100 })
    .Post(new Hit { Damage = 10 })
    .Post(new Hit { Damage = 4 });
```

## ToString

`ToString()` returns the entity's own string.

```csharp
Console.WriteLine(registry.Create().ToString());
```

## See also

- [Registry](registry.md)
- [Tags](tags.md)
- [Relations](relations.md)
- [Attachments](attachments.md)
- [Inboxes](inboxes.md)
- [Command buffers](commands.md)
- [Storage](storage.md)
