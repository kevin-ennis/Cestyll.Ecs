# Relations

Relations point one entity at another, and a target can list the entities pointing at it. An exclusive relation gives each source at most one target, like a parent. A link set gives a source many targets, each at most once, with data on each link. Types live in `Cestyll.Ecs.Relations`.

```csharp
using Cestyll.Ecs;
using Cestyll.Ecs.Relations;

struct Position : IComponent { public float X, Y; }
struct Score : IComponent { public int Value; }

struct Parent : IRelation
{
    public static OnTargetDestroyed Policy => OnTargetDestroyed.DestroySources;
}

struct OwnedBy : IRelation
{
    public static OnTargetDestroyed Policy => OnTargetDestroyed.Unrelate;
}

struct Attacks : ILink<Entity32>
{
    public Entity32 Target { get; init; }
    public int Damage;
}

var registry = new Registry();
```

## Exclusive relations

### IRelation and OnTargetDestroyed

A relation kind is a struct implementing `IRelation`. Its static `Policy` says what happens to the sources when a target is destroyed: `Unrelate` removes their target and leaves them alive, `DestroySources` destroys them too.

### Relate, Unrelate, GetTarget and TryGetTarget

`Relate<TRelation>(source, target)` points `source` at `target`, replacing any target it had. `Unrelate<TRelation>(source)` removes the target and returns false if there was none. `GetTarget<TRelation>(source)` returns the target, or a null entity if there is none.

```csharp
Entity32 ship = registry.Create();
Entity32 turret = registry.Create();

registry.Relate<Parent>(turret, ship);

if (registry.Unrelate<Parent>(turret))
{
    Console.WriteLine("detached");
}

var parent = registry.GetTarget<Parent>(turret);
if (!parent.IsNull)
{
    Console.WriteLine(parent);
}
```

`TryGetTarget<TRelation>(source, out target)` does the same read and returns false if there is no target. A target it returns is always alive.

```csharp
if (registry.TryGetTarget<Parent>(turret, out var mount))
{
    Console.WriteLine(mount);
}
```

### SourceCount and GetSources

`SourceCount<TRelation>(target)` counts the entities linked to `target`. `GetSources<TRelation>(target)` walks them.

```csharp
Console.WriteLine(registry.SourceCount<Parent>(ship));

foreach (var child in registry.GetSources<Parent>(ship))
{
    registry.Unrelate<Parent>(child);
}
```

### Walking a hierarchy

`GetTarget` climbs one level and `GetSources` descends one, so a recursive function covers a whole tree.

```csharp
registry.Relate<Parent>(turret, ship);

Entity32 Root(Entity32 e)
{
    var up = registry.GetTarget<Parent>(e);
    return up.IsNull ? e : Root(up);
}

void Print(Entity32 e, int depth)
{
    Console.WriteLine($"{new string(' ', depth * 2)}{e}");
    foreach (var child in registry.GetSources<Parent>(e))
    {
        Print(child, depth + 1);
    }
}

Print(Root(turret), 0);
```

### Target destroyed

Destroying an entity unrelates it as a source in every relation, then applies each relation's policy to its sources.

```csharp
Entity32 player = registry.Create();
Entity32 sword = registry.Create();
Entity32 cart = registry.Create();
Entity32 wheel = registry.Create();
registry.Relate<OwnedBy>(sword, player);
registry.Relate<Parent>(wheel, cart);

registry.Destroy(player); // sword lives on, unowned
registry.Destroy(cart);   // wheel is destroyed with it
```

## The RelationSet store

`Relations<TRelation>()` returns the `RelationSet<TEntity, TRelation>` that holds one relation kind, creating it on first use. Its `GetTarget`, `SourceCount` and `GetSources` match the registry's. The set is read-only: relating and unrelating go through the registry.

```csharp
var children = registry.Relations<Parent>();

var parent = children.GetTarget(turret);
var count = children.SourceCount(ship);
foreach (var child in children.GetSources(ship))
{
    Console.WriteLine(child);
}
```

### Sources and SourceCapacity

`Sources` is every entity that has a target in this relation, in packed order, as on a [link set](#sources-and-targets). `SourceCapacity` is the number of slots behind it.

```csharp
foreach (var e in registry.Relations<Parent>().Sources)
{
    Console.WriteLine(e);
}
```

### SourceSpan

`SourceSpan(target)` returns a target's sources as one span, for batched work.

```csharp
var sources = registry.Relations<Parent>().SourceSpan(ship);
for (var i = 0; i < sources.Length; i++)
{
    Console.WriteLine(sources[i]);
}
```

### OnRelate and OnUnrelate

`OnRelate` fires after a pair is related. `OnUnrelate` fires before a pair is removed, so `GetTarget` still returns the target in the handler. Both take a `PairHandler<TEntity>(source, target)`.

Connect from the registry, which returns a `Connection`, or with `+=` on the store. See [Observers and signals](observers.md#unsubscribing).

```csharp
using var joined = registry.OnRelate<Parent>((child, parent) => Console.WriteLine($"{child} joined {parent}"));
using var left = registry.OnUnrelate<Parent>((child, parent) => Console.WriteLine($"{child} left {parent}"));

registry.Relations<Parent>().OnRelate += (child, parent) => Console.WriteLine($"{child} joined {parent}");
```

## Link sets

### ILink

A link kind is a struct implementing `ILink<TEntity>`. The struct is the link's data: `Target` names the other end, and any other fields ride along.

### Link and Unlink on the registry

`Link(source, link)` links `source` to `link.Target`, or replaces the data if that pair is already linked, and returns a ref to the stored link. `Unlink<T>(source, target)` removes one link and returns false if there was none.

```csharp
Entity32 archer = registry.Create();
Entity32 orc = registry.Create();
Entity32 troll = registry.Create();

registry.Link(archer, new Attacks { Target = orc, Damage = 3 });
registry.Link(archer, new Attacks { Target = troll, Damage = 5 }).Damage++;

registry.Unlink<Attacks>(archer, troll);
```

`GetLink<T>(source, target)`, `TryGetLink<T>(source, target, out link)`, `TargetCount<T>(source)`, `GetTargets<T>(source)` and `Patch<T>(source, target, edit)` or `Patch<T>(source, target)` forward to the [link set](#the-linkset-store). `GetLink` and `Patch` throw `KeyNotFoundException` for a missing link, without making the store.

```csharp
registry.Patch<Attacks>(archer, orc, static (ref Attacks a) => a.Damage += 10);
var damage = registry.GetLink<Attacks>(archer, orc).Damage;
var targets = registry.TargetCount<Attacks>(archer);
```

### The LinkSet store

`Links<T>()` returns the `LinkSet<TEntity, T>` for one link kind, creating it on first use. It has the full set of link operations.

```csharp
var attacks = registry.Links<Attacks>();
attacks.Link(archer, new Attacks { Target = orc, Damage = 3 });
```

### Has, Get and TryGet

`Has(source, target)` tells whether the pair is linked. `Get` returns a ref to the link's data and throws `KeyNotFoundException` if there is none. `TryGet` copies it out instead.

```csharp
if (attacks.Has(archer, orc))
{
    attacks.Get(archer, orc).Damage *= 2;
}

if (attacks.TryGet(archer, troll, out var link))
{
    Console.WriteLine(link.Damage);
}
```

### TargetCount and SourceCount

`TargetCount(source)` counts the links out of a source. `SourceCount(target)` counts the links into a target.

```csharp
Console.WriteLine($"{attacks.TargetCount(archer)} targets, {attacks.SourceCount(orc)} attackers");
```

### GetTargets and GetSources

`GetTargets(source)` walks a source's links as refs to their data. `GetSources(target)` walks the entities linking to a target.

```csharp
foreach (ref var hit in attacks.GetTargets(archer))
{
    hit.Damage++;
}

foreach (var attacker in attacks.GetSources(orc))
{
    Console.WriteLine(attacker);
}
```

### Sources and Targets

`Sources` is every entity with at least one outgoing link, and `Targets` every entity with at least one incoming link, in packed order.

```csharp
foreach (var e in attacks.Sources)
{
    Console.WriteLine($"{e} attacks {attacks.TargetCount(e)}");
}
```

### SourceSpan

`SourceSpan(target)` returns a target's sources as one span, as on a [relation](#sourcespan). It goes stale after any link, unlink, `Compact` or `ShrinkToFit`.

```csharp
foreach (var attacker in attacks.SourceSpan(orc))
{
    Console.WriteLine(attacker);
}
```

### Patch

`Patch(source, target, edit)` edits a link in place and fires `OnUpdate`. `Patch(source, target)` fires `OnUpdate` for a link you already changed through a ref.

```csharp
attacks.Patch(archer, orc, (ref Attacks a) => a.Damage += 10);

attacks.Get(archer, orc).Damage = 1;
attacks.Patch(archer, orc);
```

### Sources with their components

To walk a target's sources along with their components, filter a view with [`WithLinkTarget<T>(target)`](views.md#relations-and-links).

```csharp
registry.View().WithLinkTarget<Attacks>(orc).Each((Entity32 e, ref Score s) =>
{
    s.Value++;
});
```

### Unlink, UnlinkTargets and UnlinkSources

`Unlink(source, target)` removes one link. `UnlinkTargets(source)` removes every link out of a source, and `UnlinkSources(target)` every link into a target. These two return how many there were.

```csharp
attacks.Unlink(archer, orc);
var dropped = attacks.UnlinkTargets(archer);
var freed = attacks.UnlinkSources(troll);
```

### OnLink, OnUpdate and OnUnlink

`OnLink` fires after a new link is added. `OnUpdate` fires after `Link` replaces an existing link's data, and after `Patch`. `OnUnlink` fires before a link is removed, so `Get` still reads it in the handler. Each takes a `PairHandler<TEntity>(source, target)`.

```csharp
attacks.OnLink += (source, target) => Console.WriteLine($"{source} attacks {target}");
attacks.OnUpdate += (source, target) => Console.WriteLine($"now {attacks.Get(source, target).Damage}");
attacks.OnUnlink += (source, target) => Console.WriteLine($"{source} stops attacking {target}");
```

`registry.OnLink<Attacks>(handler)`, `registry.OnLinkUpdate<Attacks>(handler)` and `registry.OnUnlink<Attacks>(handler)` connect them without fetching the set, and return a `Connection`.

## What a walk may change

`GetSources`, and a link set's `GetTargets`, walk one entity's list from last to first. The loop body may:

- read and write any component, tag, attachment or inbox, and add or remove components on any entity. The walk reads only its own list. A view walk around it still has its own rules. See [What a walk may change](views.md#what-a-walk-may-change).
- unlink the current entry. In `GetSources`, destroying the current source is fine too.

It may not:

- unlink or destroy another entry in the list being walked. A visited entry can be swapped into a slot still to come and walked twice. This includes a `Destroy` of the current source that cascades through a `DestroySources` relation into its siblings.
- link a new entry into the list being walked. It won't be visited.
- call `Compact` or `ShrinkToFit` on the store.

Wrap the walk in [`Defer()`](commands.md#defer) to make any of these changes. They apply when the scope ends.

```csharp
foreach (var part in registry.GetSources<Parent>(ship))
{
    registry.AddOrReplace(part, new Position { X = 1 }); // fine: another store
}

using (registry.Defer())
{
    foreach (var part in registry.GetSources<Parent>(ship))
    {
        registry.Relate<Parent>(registry.Create(), ship); // recorded, related after the walk
    }
}
```

## Filtering by relation

Views and groups filter on which end of a relation or link set an entity is on. `WithRelationTarget<TRelation>()` and `WithRelationSources<TRelation>()` cover exclusive relations, and `WithLinkTargets<T>()` and `WithLinkSources<T>()` cover link sets. Each has a `Without` form. On views only, `WithRelationTarget<TRelation>(target)` and `WithLinkTarget<T>(target)` walk one target's sources. See [Views](views.md#relations-and-links) and [Groups](groups.md#filters).

```csharp
var parents = registry.View().WithRelationSources<Parent>();
var attackers = registry.Group().WithLinkTargets<Attacks>().Build();
```

## Memory

Removing targets or links never gives memory back. `RelationSet` and `LinkSet` derive from `RelationStore<TEntity>`, whose `Compact(minWaste)` closes the gaps in place when they make up at least `minWaste` (0 to 1) of the slab. `ShrinkToFit()` repacks and trims every array. `SlabUsed`, `SlabGaps` and `SlabCapacity` report the slab's state, in slots. `registry.Compact()` and `registry.ShrinkToFit()` do this for every store. See [Registry](registry.md).

```csharp
var children = registry.Relations<Parent>();
if (children.SlabGaps > children.SlabCapacity / 2)
{
    children.Compact();
}

registry.Links<Attacks>().ShrinkToFit();
```

## See also

- [Registry](registry.md)
- [Entity handles](entity-handles.md#relations-and-link-sets)
- [Storage](storage.md)
- [Attachments](attachments.md)
- [Observers and signals](observers.md)
- [Command buffers](commands.md)
- [Snapshots](snapshots.md)
