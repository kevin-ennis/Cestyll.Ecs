# Command buffers

A command buffer records structural changes now and applies them later, in record order. Use one for changes a walk must not make mid-walk. See [What a walk may change](views.md#what-a-walk-may-change). Types live in `Cestyll.Ecs.Commands`, and `Defer()` and `AutoDefer` let the registry record its own calls.

```csharp
using Cestyll.Ecs;
using Cestyll.Ecs.Attachments;
using Cestyll.Ecs.Commands;
using Cestyll.Ecs.Inboxes;
using Cestyll.Ecs.Relations;
using Cestyll.Ecs.Tags;

struct Position : IComponent { public float X, Y; }
struct Velocity : IComponent { public float X, Y; }
struct Flash : IComponent { public int Frames; }
struct Shot : ITag { }

struct Orbit : IRelation
{
    public static OnTargetDestroyed Policy => OnTargetDestroyed.Unrelate;
}

struct Attacks : ILink<Entity32>
{
    public Entity32 Target { get; init; }
    public int Damage;
}

enum StatusKind { Burn, Chill }

struct Status : IAttachment<StatusKind>
{
    public StatusKind Kind;
    public int Left;
    public readonly StatusKind Key => Kind;
}

struct Hit : IMessage
{
    public int Damage;
    public Entity32 Source;
}
```

## Making a buffer

`registry.CommandBuffer()` returns a new `CommandBuffer<TEntity>` over that registry. Keep it and reuse it: `Flush` empties it.

```csharp
var registry = new Registry();
var commands = registry.CommandBuffer();

Entity32 ship = registry.Create(new Position(), new Velocity { X = 1 });
Entity32 rock = registry.Create(new Position { X = 10 });
```

Each example below stands alone, starting from `ship` and `rock` as created here.

## Registry, Count and IsEmpty

`Registry` is the registry the buffer applies to. `Count` is the number of commands waiting for `Flush`, and `IsEmpty` is true when there are none.

```csharp
commands.Add(ship, new Flash { Frames = 3 });

Console.WriteLine(commands.Count);   // 1
Console.WriteLine(commands.IsEmpty); // False
Console.WriteLine(commands.Registry == registry); // True
```

## Recording

`Add`, `AddOrReplace`, `Replace`, `Patch`, `Remove`, `Destroy`, `Tag` and `Untag` record the matching registry call. Nothing changes in the registry until `Flush`.

```csharp
foreach (var row in registry.View().Rows<Position>())
{
    if (row.C1.X > 100)
    {
        commands.Destroy(row.Entity);
    }
    else
    {
        commands.AddOrReplace(row.Entity, new Flash { Frames = 2 });
        commands.Tag<Shot>(row.Entity);
    }
}

commands.Flush();
```

`Remove` and `Untag` take only the type.

```csharp
commands.Remove<Velocity>(ship);
commands.Untag<Shot>(ship);
```

`Patch(e, edit)` runs the edit at `Flush`, on the value current then. `Patch<T>(e)` only fires `OnUpdate`.

```csharp
commands.Replace(ship, new Flash { Frames = 1 });
commands.Patch(ship, static (ref Flash f) => f.Frames += 2);
commands.Patch<Flash>(ship);
```

## Relations and link sets

`Relate<TRelation>(source, target)` and `Unrelate<TRelation>(source)` record an exclusive relation. `Link(source, link)` and `Unlink<T>(source, target)` record a link-set link, whose value holds its target. `Patch<T>(source, target, edit)` edits a link at `Flush`, and `Patch<T>(source, target)` only fires `OnLinkUpdate`. Both are skipped if the target is dead by then.

```csharp
commands.Relate<Orbit>(rock, ship);
commands.Unrelate<Orbit>(rock);

commands.Link(ship, new Attacks { Target = rock, Damage = 5 });
commands.Patch(ship, rock, (ref Attacks a) => a.Damage++);
commands.Unlink<Attacks>(ship, rock);
```

## Attachments

`Attach<T, TKey>(e, item)` and `Detach<T, TKey>(e, key)` record an attach and a detach by key. `AttachOrPatch<T, TKey>(e, item, merge)` merges into the item under `item.Key` at `Flush`, or attaches `item` if there is none. `Patch<T, TKey>(e, key, edit)` edits an item at `Flush`, and `Patch<T, TKey>(e, key)` only fires `OnAttachmentUpdate`.

```csharp
commands.Attach<Status, StatusKind>(ship, new Status { Kind = StatusKind.Burn, Left = 3 });
commands.AttachOrPatch<Status, StatusKind>(
    ship, new Status { Kind = StatusKind.Burn, Left = 2 }, static (ref Status s, in Status more) => s.Left += more.Left);
commands.Detach<Status, StatusKind>(ship, StatusKind.Chill);
```

## Inboxes

`Post<T>(e, message)` records a post to `e`'s inbox. Posts land in record order.

```csharp
commands.Post(ship, new Hit { Damage = 5, Source = rock });
```

## Creating entities

`Create()` makes the entity at once, with no components, so its id can go into later commands. `Create(c1, ...)` takes 1 to 16 components, which arrive at `Flush`. Both return a [`CommandHandle`](#commandhandle).

```csharp
foreach (var row in registry.View().Rows<Position>())
{
    commands.Create(new Position { X = row.C1.X }, new Velocity { X = 1 });
}

commands.Flush();
```

## CommandHandle

A `CommandHandle<TEntity>` pairs an entity with a buffer for fluent recording. `Add`, `AddOrReplace`, `Replace`, `Patch`, `Remove`, `Tag`, `Untag`, `Relate`, `Unrelate`, `Link`, `Unlink`, link `Patch`, `Attach`, `AttachOrPatch`, attachment `Patch`, `Detach` and `Post` record and return the handle, and `Destroy` records a destroy. It converts to the entity.

```csharp
Entity32 shot = commands.Create(new Position())
    .Add(new Velocity { X = 4 })
    .Tag<Shot>()
    .Relate<Orbit>(ship)
    .Attach<Status, StatusKind>(new Status { Kind = StatusKind.Burn, Left = 3 });

commands.Flush();
```

`Entity` and `Buffer` give back the two halves, and `ToString` prints the entity.

```csharp
var handle = commands.Create();
Entity32 e = handle.Entity;
var buffer = handle.Buffer;
```

## Flush

`Flush()` applies every command in record order through the registry, then empties the buffer. A command whose entity is dead by then is skipped, so a destroy drops whatever was recorded for that entity after it. `Add`, `Replace`, `Patch`, `Tag`, `Attach`, `AttachOrPatch`, `Relate`, `Link` and `Post` throw `InvalidOperationException` when recorded for an entity that is already dead. `Remove`, `Untag`, `Detach`, `Unrelate`, `Unlink` and `Destroy` don't check, since removing from a dead entity does nothing.

```csharp
commands.Destroy(ship);
commands.Add(ship, new Flash()); // skipped at Flush
commands.Flush();
```

## Clear

`Clear()` drops every command unapplied and destroys the entities `Create` made since the last `Flush` that are still alive.

```csharp
Entity32 made = commands.Create(new Position());
commands.Clear();

Console.WriteLine(registry.IsAlive(made)); // False
```

## Defer

`registry.Defer()` opens a scope in which the registry records its own structural calls instead of applying them. They run, in order and by the same rules as `Flush`, when the scope is disposed.

```csharp
using (registry.Defer())
{
    registry.Add(ship, new Flash { Frames = 2 });
    registry.Tag<Shot>(ship);

    Console.WriteLine(registry.Has<Flash>(ship)); // False
}

Console.WriteLine(registry.Has<Flash>(ship)); // True
```

A call that returns a ref hands back a ref to the pending value, and writes through it are kept. A call that returns a bool answers as the matching read would just before the call.

```csharp
using (registry.Defer())
{
    registry.Add(ship, new Flash()).Frames = 7;
    var had = registry.Remove<Velocity>(ship);
}
```

Reads see the registry as it was, except that an entity created inside the open scope reads back what was recorded for it. A factory can create an entity and adjust it.

```csharp
using (registry.Defer())
{
    Entity32 e = registry.Create(new Position { X = 5 });
    registry.Get<Position>(e).X += 1;
}
```

Counts and walks never include recorded work, even for an entity created in the scope. `SourceCount`, `GetSources`, a link set's counts and walks, tag walks, view walks and pool counts all report the registry as it was before the scope. When a count needs this scope's changes, keep a tally of your own or make the changes after the walk.

```csharp
using (registry.Defer())
{
    registry.Relate<Orbit>(registry.Create(), ship);
    Console.WriteLine(registry.SourceCount<Orbit>(ship)); // 0
}
```

Scopes nest, and the outermost one's end applies everything recorded in any of them.

```csharp
using (registry.Defer())
{
    using (registry.Defer())
    {
        registry.Add(ship, new Flash());
    }

    Console.WriteLine(registry.Has<Flash>(ship)); // False
}
```

## DeferScope

`Defer()` returns a `DeferScope<TEntity>`, a ref struct whose `Dispose` ends the scope. Dispose it exactly once, with `using` or by hand.

```csharp
var scope = registry.Defer();
registry.Destroy(rock);
scope.Dispose();
```

## IsDeferring and PendingCount

`IsDeferring` is true while a defer scope, or an `AutoDefer` walk, is open. `PendingCount` is the number of commands recorded in the open scopes.

```csharp
using (registry.Defer())
{
    registry.Tag<Shot>(ship);

    Console.WriteLine(registry.IsDeferring); // True
    Console.WriteLine(registry.PendingCount); // 1
}
```

## AutoDefer

With `AutoDefer` on, every view walk is a defer scope: `Each`, and `foreach` until the enumerator is disposed. So is a group's `Each`. A walk's structural changes run when the outermost walk ends.

```csharp
var registry = new Registry { AutoDefer = true };

registry.View().Each((Entity32 e, ref Position p) =>
{
    registry.Add(e, new Flash());
    registry.Create(new Position { X = p.X + 100 });
});
```

Loops over a group's `Entities` or spans, attachment walks and relation walks do not defer on their own. Wrap them in `Defer()`.

```csharp
var movers = registry.Group().Own<Position>().Own<Velocity>().Build();

using (registry.Defer())
{
    foreach (var e in movers.Entities)
    {
        registry.Remove<Velocity>(e);
    }
}
```

## See also

- [Registry](registry.md)
- [Entity handles](entity-handles.md)
- [Views](views.md)
- [Groups](groups.md)
- [Tags](tags.md)
- [Relations](relations.md)
- [Attachments](attachments.md)
- [Inboxes](inboxes.md)
- [Observers and signals](observers.md)
- [Snapshots](snapshots.md)
