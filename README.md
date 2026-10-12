# Cestyll.Ecs

A fast sparse-set entity component system for .NET, written in safe managed code. No `unsafe`, no reflection, trimming and NativeAOT compatible.

> [!WARNING]
> Cestyll.Ecs is pre-production. The API is still changing and breaking changes can land in any release.

```
dotnet add package Cestyll.Ecs
```

Requires .NET 10.

## Quick start

Name the entity layout once per project with a global alias. The rest of your code then says `Registry`.

```csharp
// GlobalUsings.cs
global using Registry = Cestyll.Ecs.Registry<Cestyll.Ecs.Entity32>;
```

Components are plain structs marked with `IComponent`. Tags are data-less flags marked with `ITag`.

```csharp
using Cestyll.Ecs;
using Cestyll.Ecs.Tags;

struct Position : IComponent { public float X, Y; }
struct Velocity : IComponent { public float X, Y; }
struct Frozen : ITag { }

var registry = new Registry();

Entity32 ship = registry.Create(new Position(), new Velocity { X = 1 });
Entity32 rock = registry.Create(new Position { X = 10 });

registry.Tag<Frozen>(rock);
registry.Get<Velocity>(ship).Y = 2;
```

### Views

A view walks every entity that has the components its callback takes, driven from the smallest pool. Filter with `With`, `Without`, `WithTag` and `WithoutTag`.

```csharp
registry.View()
    .WithoutTag<Frozen>()
    .Each((Entity32 e, ref Position p, ref Velocity v) =>
    {
        p.X += v.X;
        p.Y += v.Y;
    });

foreach (var row in registry.View().Rows<Position>())
{
    Console.WriteLine($"{row.Entity}: {row.C1.X}");
}
```

### Groups

An owning group keeps its pools packed and aligned, so slot `i` of every span is the same entity. Walk them as plain spans, or as 256-bit vectors.

```csharp
var movers = registry.Group().Own<Position>().Own<Velocity>().Build();

var positions = movers.Span<Position>();
var velocities = movers.Span<Velocity>();
for (var i = 0; i < movers.Count; i++)
{
    positions[i].X += velocities[i].X;
}
```

### Command buffers

Record structural changes during a walk and apply them later, in order.

```csharp
var commands = registry.CommandBuffer();

foreach (var row in registry.View().Rows<Position>())
{
    if (row.C1.X > 100)
    {
        commands.Destroy(row.Entity);
    }
}

commands.Flush();
```

`registry.Defer()` and `registry.AutoDefer` defer changes made directly on the registry the same way.

## Features

- **Sparse-set storage**: one packed pool per component type, plus named pools for keeping more than one of a type per entity
- **Views**: 1 to 16 components, any number of filters, pinning the walk to a pool or tag set, and joining views
- **Groups**: owning, nested and non-owning, with sorting
- **Tags**: one bit per entity, never a pool
- **Relations and link sets**: exclusive and many-to-many links between entities, with reverse lookup
- **Attachments**: keyed sub-items per entity, such as inventory slots or status effects
- **Inboxes**: messages posted to an entity, such as hits taken this frame, read back in posting order
- **Observers and signals**: construct, update and destroy hooks, and reactive entity sets
- **Requirements**: rules that add a component to entities that gain another
- **Command buffers and deferral**: record changes now, apply them later
- **Snapshots**: save and load a registry through an archive you write, in any format
- **Events**: a typed dispatcher with immediate and queued delivery
- **SIMD**: walk pools and groups as `Vector256` lanes

See [doc/](https://github.com/kevin-ennis/Cestyll.Ecs/blob/main/doc/README.md) for the full API.

## Entity size

`Entity32` has 20 bits of index and 12 of version, for about a million live entities. If you need more, alias `Registry<Entity64>` instead, which has 32 bits of each.

## Threading

A registry is single-threaded. Call it from one thread at a time, reads included, and the same goes for its stores, views, groups, observers, command buffers and handles. Separate registries can run on separate threads: type ids are thread-safe and walk buffers are per-thread. The event `Dispatcher` is single-threaded too.

## License

[MIT](https://github.com/kevin-ennis/Cestyll.Ecs/blob/main/LICENSE)
