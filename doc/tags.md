# Tags

A tag is a data-less flag on an entity, kept as one bit per entity index in a tag set. Tags never get a pool. Types live in `Cestyll.Ecs.Tags`.

```csharp
using Cestyll.Ecs;
using Cestyll.Ecs.Tags;

struct Position : IComponent { public float X, Y; }
struct Frozen : ITag { }
struct Selected : ITag { }
struct Dead : ITag { }

var registry = new Registry();
Entity32 rock = registry.Create(new Position());
```

## ITag

Mark a struct with `ITag` to make it a tag.

```csharp
struct Hidden : ITag { }
```

## Tag, Untag and HasTag

`Tag<T>(e)` sets the tag and returns true if it was clear. `Untag<T>(e)` clears it and returns true if it was set. `HasTag<T>(e)` tests it.

```csharp
registry.Tag<Frozen>(rock);
var frozen = registry.HasTag<Frozen>(rock); // True
registry.Untag<Frozen>(rock);
```

## HasAllTags and HasAnyTags

`HasAllTags<T1, ...>(e)` tests for every listed tag, `HasAnyTags<T1, ...>(e)` for at least one. Both take 2 to 16 tag types.

```csharp
registry.Tag<Selected>(rock);
var both = registry.HasAllTags<Frozen, Selected>(rock);  // False
var either = registry.HasAnyTags<Frozen, Selected>(rock); // True
```

## TagCount

`TagCount<T>()` is the number of entities with tag `T`.

```csharp
var selectedCount = registry.TagCount<Selected>();
```

## Walking tagged entities

A view with tag filters and no components walks the tagged entities in ascending index order. See [Rows without components](views.md#rows-without-components).

```csharp
foreach (var e in registry.View().WithTag<Selected>().Rows())
{
    Console.WriteLine(e);
}
```

Wrap the walk in [`Defer()`](commands.md#defer) to tag or untag other entities during it.

```csharp
using (registry.Defer())
{
    foreach (var e in registry.View().WithTag<Selected>().Rows())
    {
        registry.Tag<Selected>(registry.Create());
    }
}
```

## GetTagTypes

`GetTagTypes(e)` walks the tag types an entity has, in tag set creation order.

```csharp
foreach (var type in registry.GetTagTypes(rock))
{
    Console.WriteLine(type.Name);
}
```

## ClearTags

`ClearTags<T>()` untags every entity with `T`, with the same signals and group updates as `Untag`.

```csharp
registry.ClearTags<Selected>();
```

## DestroyTagged

`DestroyTagged<T>()` destroys every entity with `T`, with the same signals and cascades as `Destroy`. Use it to sweep away entities marked with a tag such as `Dead`.

```csharp
registry.DestroyTagged<Dead>();
```

## TagSet

`Tags<T>()` returns the `TagSet<Entity32>` for `T`, creating it if needed. It has `TagType` and `Count`. `Contains(e)` is like `HasTag<T>(e)` but sees applied tags only. In a defer scope, `HasTag` also sees pending tags on entities created there. The set is read-only: tagging and untagging go through the registry.

```csharp
var selected = registry.Tags<Selected>();
Console.WriteLine($"{selected.TagType.Name}: {selected.Count}");
var picked = selected.Contains(rock);
```

## Signals

`OnTag<T>(handler)` and `OnUntag<T>(handler)` connect to the tag set and return a `Connection`. See [Observers and signals](observers.md#tag-signals) for when they fire.

```csharp
using var froze = registry.OnTag<Frozen>(e => Console.WriteLine($"{e} froze"));
```

## Words

`Words` exposes the raw bits: bit `i` of word `w` is index `w * 64 + i`. Combine tag sets bitwise for custom queries.

```csharp
var frozenWords = registry.Tags<Frozen>().Words;
var selectedWords = registry.Tags<Selected>().Words;
var frozenAndSelected = 0;
for (var w = 0; w < Math.Min(frozenWords.Length, selectedWords.Length); w++)
{
    frozenAndSelected += System.Numerics.BitOperations.PopCount(frozenWords[w] & selectedWords[w]);
}
```

## ShrinkToFit

`ShrinkToFit()` trims a tag set to the highest index tagged now, keeping at least 16 words.

```csharp
registry.Tags<Frozen>().ShrinkToFit();
```

## Filtering by tag

Views filter on tags with `WithTag` and `WithoutTag`, and groups with the builder's `WithTag<T>()` and `WithoutTag<T>()`. See [Views](views.md) and [Groups](groups.md).

## See also

- [Registry](registry.md)
- [Entity handles](entity-handles.md)
- [Views](views.md)
- [Groups](groups.md)
- [Observers and signals](observers.md)
- [Command buffers](commands.md)
- [Snapshots](snapshots.md)
