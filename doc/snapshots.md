# Snapshots

A snapshot saves a registry's entities and the stores you name, section by section, through an archive you write. `SnapshotLoader` restores into an empty registry with the same entity ids, and `ContinuousLoader` merges repeated snapshots into a live registry. Types live in `Cestyll.Ecs.Snapshots`.

```csharp
using Cestyll.Ecs;
using Cestyll.Ecs.Attachments;
using Cestyll.Ecs.Relations;
using Cestyll.Ecs.Snapshots;
using Cestyll.Ecs.Tags;

struct Position : IComponent { public float X, Y; }
struct Target : IComponent { public Entity32 Entity; }
struct Frozen : ITag { }

struct Orbit : IRelation
{
    public static OnTargetDestroyed Policy => OnTargetDestroyed.Unrelate;
}

struct Attacks : ILink<Entity32>
{
    public Entity32 Target { get; init; }
    public int Damage;
}

enum SlotKind { Head, Hand }

struct Gear : IAttachment<SlotKind>
{
    public SlotKind Slot;
    public int Power;
    public readonly SlotKind Key => Slot;
}

struct Clock { public double Time; }
```

## ISnapshotWriter and ISnapshotReader

`ISnapshotWriter<TEntity>` and `ISnapshotReader<TEntity>` are the archive interfaces. A snapshot hands the writer counts, entities and values, and a loader asks the reader for the same things in the same order.

```csharp
public interface ISnapshotWriter<TEntity> where TEntity : unmanaged, IEntity<TEntity>
{
    void BeginSection(SnapshotSection kind, Type type, string? name);
    void EndSection();
    void WriteCount(int count);
    void WriteEntity(TEntity entity);
    void Write<T>(in T value);
    void WriteComponents<T>(ReadOnlySpan<T> values);
}

public interface ISnapshotReader<TEntity> where TEntity : unmanaged, IEntity<TEntity>
{
    void BeginSection(SnapshotSection kind, Type type, string? name);
    void EndSection();
    int ReadCount();
    TEntity ReadEntity();
    T Read<T>();
    void ReadComponents<T>(Span<T> values);
}
```

`BeginSection` and `EndSection` bracket every section. `Write<T>` gets each link, attachment, message and context value whole, and `WriteComponents<T>` gets a component section's values as one span.

## A binary archive

This is a minimal archive over `BinaryWriter` and `BinaryReader`. Each section starts with its kind as a byte, which the reader checks, and values go through a codec you register per type.

```csharp
// How one type is written and read. Register each component, link, attachment, message and context type.
static class BinaryCodec<T>
{
    public static Action<BinaryWriter, T>? Write;
    public static Func<BinaryReader, T>? Read;
}

sealed class BinarySnapshotWriter(BinaryWriter writer) : ISnapshotWriter<Entity32>
{
    public void BeginSection(SnapshotSection kind, Type type, string? name)
    {
        writer.Write((byte)kind);
    }

    public void EndSection()
    {
    }

    public void WriteCount(int count)
    {
        writer.Write(count);
    }

    public void WriteEntity(Entity32 entity)
    {
        writer.Write(entity.Index);
        writer.Write(entity.Version);
    }

    public void Write<T>(in T value)
    {
        var write = BinaryCodec<T>.Write ?? throw new NotSupportedException($"No codec for {typeof(T).Name}");
        write(writer, value);
    }

    public void WriteComponents<T>(ReadOnlySpan<T> values)
    {
        foreach (var value in values)
        {
            Write(in value);
        }
    }
}

sealed class BinarySnapshotReader(BinaryReader reader) : ISnapshotReader<Entity32>
{
    public void BeginSection(SnapshotSection kind, Type type, string? name)
    {
        var found = (SnapshotSection)reader.ReadByte();
        if (found != kind)
        {
            throw new InvalidDataException($"Expected a {kind} section, found {found}");
        }
    }

    public void EndSection()
    {
    }

    public int ReadCount()
    {
        return reader.ReadInt32();
    }

    public Entity32 ReadEntity()
    {
        return Entity32.Make(reader.ReadInt32(), reader.ReadUInt32());
    }

    public T Read<T>()
    {
        var read = BinaryCodec<T>.Read ?? throw new NotSupportedException($"No codec for {typeof(T).Name}");
        return read(reader);
    }

    public void ReadComponents<T>(Span<T> values)
    {
        for (var i = 0; i < values.Length; i++)
        {
            values[i] = Read<T>();
        }
    }
}
```

Register a codec for each type that goes through `Write<T>` or `WriteComponents<T>`. Entities, tags and relations need none.

```csharp
BinaryCodec<Position>.Write = (w, p) =>
{
    w.Write(p.X);
    w.Write(p.Y);
};
BinaryCodec<Position>.Read = r => new Position { X = r.ReadSingle(), Y = r.ReadSingle() };

BinaryCodec<Target>.Write = (w, t) =>
{
    w.Write(t.Entity.Index);
    w.Write(t.Entity.Version);
};
BinaryCodec<Target>.Read = r => new Target { Entity = Entity32.Make(r.ReadInt32(), r.ReadUInt32()) };
```

For a JSON archive over `System.Text.Json`, see [JSON snapshots](snapshots-json.md).

### Copying components as bytes

A component that holds no references can go as one block of bytes. `MemoryMarshal.AsBytes` needs `T : struct`, which `WriteComponents<T>` doesn't promise, so register the copy from a method constrained to `unmanaged`.

```csharp
using System.Runtime.InteropServices;

static class BinaryCodec<T>
{
    public static Action<BinaryWriter, T>? Write;
    public static Func<BinaryReader, T>? Read;
    public static Action<BinaryWriter, ReadOnlySpan<T>>? WriteMany;
    public static Action<BinaryReader, Span<T>>? ReadMany;
}

static class BinaryCodec
{
    public static void CopyBytes<T>() where T : unmanaged
    {
        BinaryCodec<T>.WriteMany = (w, values) => w.Write(MemoryMarshal.AsBytes(values));
        BinaryCodec<T>.ReadMany = (r, values) => r.ReadExactly(MemoryMarshal.AsBytes(values));
    }
}
```

`WriteComponents` and `ReadComponents` use the copy when one is registered, and loop otherwise.

```csharp
public void WriteComponents<T>(ReadOnlySpan<T> values)
{
    if (BinaryCodec<T>.WriteMany is { } writeMany)
    {
        writeMany(writer, values);
        return;
    }

    foreach (var value in values)
    {
        Write(in value);
    }
}

public void ReadComponents<T>(Span<T> values)
{
    if (BinaryCodec<T>.ReadMany is { } readMany)
    {
        readMany(reader, values);
        return;
    }

    for (var i = 0; i < values.Length; i++)
    {
        values[i] = Read<T>();
    }
}
```

```csharp
BinaryCodec.CopyBytes<Position>();
```

## SnapshotSection

`SnapshotSection` is the kind of section passed to `BeginSection`: `Entities`, `Component`, `Relation`, `Link`, `Attachment`, `Tag`, `Context` or `Inbox`. The `type` argument is the entity, component, relation, link, attachment, tag, context or message type, and `name` is set for a named pool.

```csharp
public void BeginSection(SnapshotSection kind, Type type, string? name)
{
    var key = name is null ? $"{kind}:{type.Name}" : $"{kind}:{type.Name}/{name}";
    // ...
}
```

## Saving

`registry.Snapshot<TWriter>()` returns a `Snapshot<TEntity, TWriter>`. Each section call takes the archive by `ref`, writes one section, and returns the snapshot so calls chain. Start with `Entities`.

```csharp
var registry = new Registry();
Entity32 ship = registry.Create(new Position { X = 1 });
Entity32 rock = registry.Create(new Position { X = 10 }, new Target { Entity = ship });
registry.Tag<Frozen>(rock);

using var file = new BinaryWriter(File.Create("save.bin"));
var w = new BinarySnapshotWriter(file);

registry.Snapshot<BinarySnapshotWriter>()
    .Entities(ref w)
    .Component<Position>(ref w)
    .Component<Target>(ref w)
    .Tag<Frozen>(ref w);
```

### Snapshot sections

- `Entities(ref w)` writes every entity id handed out so far with its version, then the free ones.
- `Component<T>(ref w)` writes the default pool of `T`, and `Component<T>(ref w, name)` writes a named pool.
- `Tag<T>(ref w)` writes the tagged entities.
- `Relation<TRelation>(ref w)` writes each (source, target) pair.
- `Link<T>(ref w)` writes each link's source, then each link value through `Write<T>`.
- `Attachment<T, TKey>(ref w)` writes the owners, their counts, then every item through `Write<T>`.
- `Inbox<T>(ref w)` writes the owners, their counts, then every message in posting order through `Write<T>`.
- `Context<T>(ref w)` writes a count of 1 and the value through `Write<T>`, or a count of 0 if the registry has none.

The sections below also need codecs for `Attacks`, `Gear` and `Clock`.

```csharp
registry.Snapshot<BinarySnapshotWriter>()
    .Entities(ref w)
    .Component<Position>(ref w)
    .Component<Position>(ref w, "previous")
    .Tag<Frozen>(ref w)
    .Relation<Orbit>(ref w)
    .Link<Attacks>(ref w)
    .Attachment<Gear, SlotKind>(ref w)
    .Context<Clock>(ref w);
```

## Loading

`registry.SnapshotLoader<TReader>()` returns a `SnapshotLoader<TEntity, TReader>`, which restores into an empty registry with identical entity ids. It has the same section calls as `Snapshot`, and they must come in the same order.

```csharp
var loaded = new Registry();

using var file = new BinaryReader(File.OpenRead("save.bin"));
var r = new BinarySnapshotReader(file);

loaded.SnapshotLoader<BinarySnapshotReader>()
    .Entities(ref r)
    .Component<Position>(ref r)
    .Component<Target>(ref r)
    .Tag<Frozen>(ref r);
```

`Context<T>(ref r)` sets the context value as saved. One saved as absent removes any value the registry has, such as a default set before the load.

```csharp
// r reads a save written with .Entities(ref w).Context<Clock>(ref w)
var world = new Registry();
world.AddOrReplaceContext(new Clock());

world.SnapshotLoader<BinarySnapshotReader>()
    .Entities(ref r)
    .Context<Clock>(ref r);
```

## SnapshotSchema

A `SnapshotSchema<TEntity>` declares the sections once and uses them both ways, so a save and its load can't disagree. The entity section comes first, then the rest in declaration order. It holds no registry state, so build it once and share it.

```csharp
var schema = new SnapshotSchema<Entity32>()
    .Component<Position>()
    .Component<Position>("previous")
    .Component<Target>()
    .Tag<Frozen>()
    .Relation<Orbit>()
    .Link<Attacks>()
    .Attachment<Gear, SlotKind>()
    .Context<Clock>();
```

### Save and Load

`Save(registry, ref w)` writes the entity section and every declared section. `Load(registry, ref r)` reads them through a `SnapshotLoader`, so the registry must be empty.

```csharp
schema.Save(registry, ref w);

var loaded = new Registry();
schema.Load(loaded, ref r);
```

### Skip

`Skip<T>()` leaves every store of `T` out on purpose, whatever its kind. `Skip<T>(name)` leaves out one named pool of component `T`.

```csharp
var schema = new SnapshotSchema<Entity32>()
    .Component<Position>()
    .Skip<Target>()
    .Skip<Position>("previous");
```

### Uncovered

`Uncovered(registry)` lists every store the registry holds that the schema neither saves nor skips, empty ones included. Assert that it is empty in a test, so a new component doesn't go unsaved.

```csharp
var missing = schema.Uncovered(registry);
if (missing.Count > 0)
{
    throw new InvalidOperationException($"Not saved: {string.Join(", ", missing)}");
}
```

### Saved

`Saved` lists the declared stores in section order, after the entity section.

```csharp
foreach (var store in schema.Saved)
{
    Console.WriteLine(store);
}
```

## SnapshotStore

A `SnapshotStore` names one store as the section that saves it: its `Kind`, its `Type`, and its `Name` for a named pool. `ToString` prints the kind and type name, plus the quoted name for a named pool, such as `Component Position "previous"`.

```csharp
foreach (var store in schema.Uncovered(registry))
{
    if (store.Kind == SnapshotSection.Component && store.Name is null)
    {
        Console.WriteLine($"Pool of {store.Type.Name} is not saved");
    }
}
```

## Continuous loading

`registry.ContinuousLoader<TReader>()` returns a `ContinuousLoader<TEntity, TReader>`, which merges snapshots from another registry into a live one, such as server state or hot reload. It keeps a map from remote ids to local ids, so keep the loader and run a pass for each snapshot that arrives.

```csharp
var mirror = new Registry();
var loader = mirror.ContinuousLoader<BinarySnapshotReader>();

void Receive(byte[] packet)
{
    using var file = new BinaryReader(new MemoryStream(packet));
    var r = new BinarySnapshotReader(file);

    loader.Entities(ref r)
        .Component<Position>(ref r)
        .Component<Target>(ref r, (t, map) => new Target { Entity = map(t.Entity) })
        .Tag<Frozen>(ref r)
        .Orphans();
}
```

### Entities

`Entities(ref r)` starts a pass. It creates a local entity for each remote entity it hasn't seen, and recreates any whose local entity has died.

### Component, Tag, Relation, Link, Attachment and Inbox

Each section clears its store on every mapped local entity, then applies what the snapshot holds. Data on entities the loader didn't create is left alone.

- `Component<T>(ref r, remap)` and `Component<T>(ref r, name, remap)` load a default or named pool. `remap` is optional.
- `Tag<T>(ref r)` loads a tag.
- `Relation<TRelation>(ref r)` maps both ends of each pair.
- `Link<T>(ref r, remap)` loads a link set. `remap` is required, since the target lives inside the link value.
- `Attachment<T, TKey>(ref r, remap)` loads attachments. `remap` is optional.
- `Inbox<T>(ref r, remap)` replaces mapped entities' messages, in the saved order. `remap` is optional.
- `Context<T>(ref r, remap)` sets the context value, or removes it if saved as absent. `remap` is optional.

```csharp
loader.Entities(ref r)
    .Relation<Orbit>(ref r)
    .Link<Attacks>(ref r, (l, map) => l with { Target = map(l.Target) })
    .Attachment<Gear, SlotKind>(ref r);
```

### Remapping entity fields

A `remap` callback gets each value and a `map` function, and returns the value with its entity fields translated to local ids, like the `Target` remap in [Continuous loading](#continuous-loading). An entity that isn't in this pass, or whose local entity has died, maps to `Entity32.Null`.

### Orphans

`Orphans()` destroys every local entity whose remote entity wasn't in the last `Entities` pass, and forgets its mapping.

```csharp
loader.Entities(ref r)
    .Component<Position>(ref r)
    .Orphans();
```

### Map and Contains

`Map(remote)` returns the local entity mapped to a remote id, or `Null` if there is none. `Contains(remote)` says whether the remote id is mapped.

```csharp
if (loader.Contains(remoteShip))
{
    Entity32 local = loader.Map(remoteShip);
    mirror.Get<Position>(local).X += 1;
}
```

## See also

- [JSON snapshots](snapshots-json.md)
- [Registry](registry.md)
- [Storage](storage.md)
- [Tags](tags.md)
- [Relations](relations.md)
- [Attachments](attachments.md)
- [Inboxes](inboxes.md)
- [Observers and signals](observers.md)
- [Command buffers](commands.md)
