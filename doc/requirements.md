# Requirements

A requirement is a rule that gives an entity gaining `T` a `TRequired` as well, added when you call `Apply`. A layer such as presentation or networking uses them to add its own components to the entities the game creates, without touching the game's spawn code. `Requirements<TEntity>` lives in `Cestyll.Ecs.Observers`.

The examples below run in order against one registry.

```csharp
using Cestyll.Ecs;
using Cestyll.Ecs.Observers;
using Cestyll.Ecs.Tags;

struct Position : IComponent { public float X, Y; }
struct Sprite : IComponent { public int Art; }
struct Rigged : IComponent { }
struct Visible : IComponent { public bool Value; }
struct Drawable : IComponent { public int Art; public float X; }
struct Decor : ITag { }

var registry = new Registry();
```

## Requirements and Require

`registry.Requirements()` returns an empty set of rules. `Require<T, TRequired>()` adds a rule that gives `TRequired`, made by its parameterless constructor, to entities that gain `T`. Pass a `make` function to build it from the registry and the entity instead. Several rules may give the same type.

```csharp
var requirements = registry.Requirements()
    .Require<Sprite, Visible>()
    .Require<Rigged, Visible>()
    .Require<Visible, Drawable>(static (r, e) =>
        r.TryGet<Sprite>(e, out var sprite)
            ? new Drawable { Art = sprite.Art, X = r.Get<Position>(e).X }
            : new Drawable());
```

A `when` condition limits a rule to the entities it holds for.

```csharp
var scenery = registry.Requirements()
    .Require<Sprite, Visible>(when: static (r, e) => !r.HasTag<Decor>(e));
```

## Apply

`Apply()` adds every `TRequired` due. Call it once a frame, before the systems that read those components. A rule skips an entity that already has `TRequired`, so a value given in `Create` or added by hand is kept.

`Apply` repeats until nothing more is added, so a chain settles in one call. Here a new sprite gets `Visible`, which then gets `Drawable`.

```csharp
Entity32 sheep = registry.Create(new Sprite { Art = 3 }, new Position { X = 12 });
Entity32 wolf = registry.Create(new Sprite { Art = 4 }, new Position(), new Drawable { Art = 99 });

requirements.Apply();

Console.WriteLine(registry.Get<Drawable>(sheep).X); // 12
Console.WriteLine(registry.Get<Drawable>(wolf).Art); // 99, given by hand
```

A rule acts when the entity gains `T`. Removing `TRequired` later doesn't bring it back, and removing `T` leaves `TRequired` in place.

```csharp
registry.Remove<Visible>(sheep);
requirements.Apply();
Console.WriteLine(registry.Has<Visible>(sheep)); // False
```

`Apply` throws `InvalidOperationException` inside a defer scope.

## Existing entities

The first `Apply` after a rule is added also covers entities that already had `T`. So rules can be set up after the world is built or a snapshot is loaded.

```csharp
var world = new Registry();
// ... schema.Load(world, ref reader);
using var rules = world.Requirements().Require<Sprite, Visible>();
rules.Apply(); // every loaded sprite gets Visible
```

## Dispose

`Dispose()` disconnects the rules. Dispose a set of rules when its world is done with it.

```csharp
requirements.Dispose();
```

## See also

- [Registry](registry.md)
- [Observers and signals](observers.md)
- [Command buffers](commands.md)
- [Snapshots](snapshots.md)
