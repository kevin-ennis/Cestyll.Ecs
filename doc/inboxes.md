# Inboxes

An inbox holds the messages posted to an entity, such as hits taken, in posting order. Unlike attachments, messages have no key, so an entity can hold any number of equal ones. Types live in `Cestyll.Ecs.Inboxes`.

The examples below run in order against one registry.

```csharp
using Cestyll.Ecs;
using Cestyll.Ecs.Inboxes;
using Cestyll.Ecs.Snapshots;

struct Health : IComponent { public float Value; }

struct Hit : IMessage
{
    public float Damage;
    public Entity32 Source;
}

var registry = new Registry();
Entity32 enemy = registry.Create(new Health { Value = 100 });
Entity32 player = registry.Create();
```

## IMessage

A message type is a struct implementing `IMessage`. A type can't also be an `IComponent`, `ITag` or `IAttachment`.

## Post, GetMessages and HasMessages on the registry

`Post<T>(e, message)` adds `message` after `e`'s other messages of type `T`. Inside a defer scope the post is recorded and lands when the scope ends. See [Defer and command buffers](#defer-and-command-buffers).

```csharp
registry.Post(enemy, new Hit { Damage = 10, Source = player });
registry.Post(enemy, new Hit { Damage = 4, Source = player });
```

`GetMessages<T>(e)` returns `e`'s messages as a span, writable in place, and empty if it has none. `HasMessages<T>(e)` tells whether it has any. Neither makes the inbox.

```csharp
foreach (ref readonly var hit in registry.GetMessages<Hit>(enemy))
{
    Console.WriteLine(hit.Damage);
}
```

## The Inbox store

`Inboxes<T>()` returns the `Inbox<TEntity, T>` that holds every `T`, creating it on first use. Its `Post` drops the type argument and is never deferred.

```csharp
var hits = registry.Inboxes<Hit>();
hits.Post(enemy, new Hit { Damage = 10, Source = player });
```

### GetAll, MessageCount and Has

`GetAll(e)` returns `e`'s messages as a writable `Span<T>` in posting order, empty if it has none. `MessageCount(e)` is how many it has, and `Has(e)` whether it has any.

```csharp
foreach (ref var hit in hits.GetAll(enemy))
{
    hit.Damage *= 0.5f; // armour, applied in place
}
```

### Entities

`Entities` lists every entity with at least one message.

```csharp
foreach (var e in hits.Entities)
{
    ref var health = ref registry.Get<Health>(e);
    foreach (var hit in hits.GetAll(e))
    {
        health.Value -= hit.Damage;
    }
}
```

### ClearMessages and Clear

`ClearMessages(e)` drops `e`'s messages and returns how many there were. `Clear()` drops every entity's messages and keeps the memory for the next frame.

```csharp
hits.Clear(); // end of frame
```

## Views and destroying

`WithInbox<T>()` and `WithoutInbox<T>()` filter a view or a group on having at least one message of type `T`. See [Views](views.md#filters) and [Groups](groups.md#filters).

```csharp
registry.View().WithInbox<Hit>().Each((Entity32 e, ref Health health) =>
{
    foreach (var hit in hits.GetAll(e))
    {
        health.Value -= hit.Damage;
    }
});
```

Destroying an entity drops its messages.

## OnPost

`OnPost` fires after a message is stored, with the entity and the message. `registry.OnPost(handler)` connects it and returns a `Connection`. `Clear` and destroying an entity fire nothing.

```csharp
using var connection = registry.OnPost((Entity32 e, in Hit hit) => Console.WriteLine($"{e} took {hit.Damage}"));
```

An observer's `OnPost<T>()` collects the entities that were posted to. See [Observers and signals](observers.md#onpost-for-inboxes).

```csharp
using var struck = registry.Observer().OnPost<Hit>();
```

## Defer and command buffers

`registry.Post` and the entity handle's `Post` are recorded inside `Defer()` and in `AutoDefer` walks, and post when the scope ends. Until then, `GetAll`, `MessageCount`, `Has` and `Entities` don't see them. A command buffer's `Post` records for the next `Flush`. See [Command buffers](commands.md#inboxes).

```csharp
var commands = registry.CommandBuffer();
commands.Post(enemy, new Hit { Damage = 10, Source = player });
commands.Create().Add(new Health { Value = 50 }).Post(new Hit { Damage = 1 });
commands.Flush();
```

## Snapshots

`Snapshot.Inbox<T>(ref w)`, `SnapshotLoader.Inbox<T>(ref r)` and `SnapshotSchema.Inbox<T>()` save and load an inbox like any other store, in posting order. `ContinuousLoader.Inbox<T>(ref r, remap)` replaces a mapped entity's messages. See [Snapshots](snapshots.md).

```csharp
var schema = new SnapshotSchema<Entity32>()
    .Component<Health>()
    .Inbox<Hit>();
```

## Memory

`Compact(minWaste)` closes the gaps that `ClearMessages(e)` leaves, and `ShrinkToFit()` repacks and trims the store. `Clear()` leaves no gaps. `registry.Compact()` and `registry.ShrinkToFit()` cover every inbox.

```csharp
hits.Compact();
hits.ShrinkToFit();
```

## See also

- [Attachments](attachments.md)
- [Events](events.md)
- [Views](views.md)
- [Groups](groups.md)
- [Entity handles](entity-handles.md#inboxes)
- [Observers and signals](observers.md)
- [Command buffers](commands.md)
- [Snapshots](snapshots.md)
