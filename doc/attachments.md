# Attachments

An attachment is data an entity can hold several of, told apart by a key: inventory items keyed by item type, status effects keyed by kind, sockets keyed by slot. Attaching an item whose key the entity already holds replaces it. Types live in `Cestyll.Ecs.Attachments`.

```csharp
using Cestyll.Ecs;
using Cestyll.Ecs.Attachments;

enum ItemType { Coin, Axe, Potion }

struct Item : IAttachment<ItemType>
{
    public ItemType Type;
    public int Count;

    public readonly ItemType Key => Type;
}

var registry = new Registry();
Entity32 player = registry.Create();
```

## IAttachment

An attachment type is a struct implementing `IAttachment<TKey>`, whose `Key` tells its items apart. Enums, numbers and strings all work as keys.

## Attach and Detach on the registry

`Attach<T, TKey>(e, item)` adds `item` to `e`, or replaces the item with the same key, and returns a ref to the stored item. `Detach<T, TKey>(e, key)` removes one item and returns false if there was none.

```csharp
registry.Attach<Item, ItemType>(player, new Item { Type = ItemType.Coin, Count = 42 });
registry.Attach<Item, ItemType>(player, new Item { Type = ItemType.Axe, Count = 1 }).Count++;

registry.Detach<Item, ItemType>(player, ItemType.Axe);
```

`Patch<T, TKey>(e, key, edit)` and `Patch<T, TKey>(e, key)` forward to the store's [Patch](#patch). They throw `KeyNotFoundException` for a missing item, without making the store.

```csharp
registry.Patch<Item, ItemType>(player, ItemType.Coin, static (ref Item i) => i.Count += 5);
```

## AttachOrPatch

`AttachOrPatch<T, TKey>(e, item, merge)` refreshes or adds. If `e` already has an item under `item.Key`, `merge` folds `item` into it in place. Otherwise `item` is attached.

```csharp
registry.AttachOrPatch<Item, ItemType>(player, new Item { Type = ItemType.Coin, Count = 5 },
    static (ref Item current, in Item incoming) => current.Count += incoming.Count);
```

## GetAttachments and HasAttachments

`GetAttachments<T, TKey>(e)` returns `e`'s items as a span, writable in place, and empty if it has none. `GetAttachments<T>(e)` and `HasAttachments<T>(e)` read without naming the key.

```csharp
foreach (ref var item in registry.GetAttachments<Item>(player))
{
    item.Count *= 2;
}

if (registry.HasAttachments<Item>(player))
{
    Console.WriteLine(registry.GetAttachments<Item, ItemType>(player).Length);
}
```

## The AttachmentSet store

`Attachments<T, TKey>()` returns the `AttachmentSet<TEntity, T, TKey>` that holds every `T`, creating it on first use. Its calls drop the type arguments.

```csharp
var items = registry.Attachments<Item, ItemType>();

items.Attach(player, new Item { Type = ItemType.Coin, Count = 42 });
items.Get(player, ItemType.Coin).Count++;
```

### Attach, AttachOrPatch and Detach

These match the registry's, without the type arguments and never deferred.

```csharp
items.Attach(player, new Item { Type = ItemType.Potion, Count = 1 });
items.AttachOrPatch(player, new Item { Type = ItemType.Potion, Count = 2 },
    static (ref Item current, in Item incoming) => current.Count += incoming.Count);
items.Detach(player, ItemType.Potion);
```

### Get and TryGet

`Get(e, key)` returns a ref to the item under `key`, and throws `KeyNotFoundException` if there is none. `TryGet` copies it out instead.

```csharp
items.Get(player, ItemType.Coin).Count -= 10;

if (items.TryGet(player, ItemType.Axe, out var axe))
{
    Console.WriteLine(axe.Count);
}
```

### Has and ItemCount

`Has(e)` tells whether `e` holds any `T`, and `Has(e, key)` whether it holds one under `key`. `ItemCount(e)` is how many it holds.

```csharp
if (items.Has(player, ItemType.Coin))
{
    Console.WriteLine($"{items.ItemCount(player)} stacks");
}
```

### GetAll

`GetAll(e)` is the store's `GetAttachments`: `e`'s items as a writable span, empty if it has none.

```csharp
foreach (ref var item in items.GetAll(player))
{
    item.Count = Math.Min(item.Count, 99);
}
```

### Entities

`Entities` is every entity holding at least one `T`, in packed order.

```csharp
foreach (var e in items.Entities)
{
    Console.WriteLine($"{e}: {items.ItemCount(e)}");
}
```

### All

`All` walks every item of every entity, entity by entity. Each step is an `Attached`, with the owning `Entity` and a ref to the `Item` that you can edit in place.

```csharp
foreach (var a in items.All)
{
    if (a.Item.Type == ItemType.Coin)
    {
        a.Item.Count++;
    }
}
```

### Patch

`Patch(e, key, edit)` edits an item in place and fires `OnUpdate`. `Patch(e, key)` fires `OnUpdate` for an item you already changed through a ref.

```csharp
items.Patch(player, ItemType.Coin, (ref Item i) => i.Count += 5);

items.Get(player, ItemType.Coin).Count = 0;
items.Patch(player, ItemType.Coin);
```

### OnAttach, OnUpdate and OnDetach

`OnAttach` fires after a new key is attached. `OnUpdate` fires after `Attach` replaces an existing key's item, after `AttachOrPatch` merges, and after `Patch`. `OnDetach` fires before an item is removed, so `Get` still reads it in the handler. Each takes an `AttachmentHandler<TEntity, TKey>(entity, key)`.

```csharp
items.OnAttach += (e, key) => Console.WriteLine($"{e} picked up {key}");
items.OnUpdate += (e, key) => Console.WriteLine($"{e} has {items.Get(e, key).Count} {key}");
items.OnDetach += (e, key) => Console.WriteLine($"{e} dropped {key}");
```

`registry.OnAttach<Item, ItemType>(handler)`, `registry.OnAttachmentUpdate<Item, ItemType>(handler)` and `registry.OnDetach<Item, ItemType>(handler)` connect them without fetching the set, and return a `Connection`. See [Observers and signals](observers.md#unsubscribing).

### DetachItems

`DetachItems(e)` removes every `T` from `e` and returns how many there were.

```csharp
var dropped = items.DetachItems(player);
```

## Views and destroying

`WithAttachment<T>()` and `WithoutAttachment<T>()` filter a view or a group on holding at least one `T`. On a view, `WithAttachment<T, TKey>(key)` and `WithoutAttachment<T, TKey>(key)` test one key. See [Views](views.md#filters) and [Groups](groups.md#filters).

```csharp
var unarmed = registry.View().WithoutAttachment<Item, ItemType>(ItemType.Axe);
```

Destroying an entity detaches all its attachments.

## Memory

Detaching never gives memory back. Every `AttachmentSet` derives from `AttachmentStore<TEntity>`, whose `Compact(minWaste)` closes the gaps in place when they make up at least `minWaste` (0 to 1) of the slab. `ShrinkToFit()` repacks and trims every array. `SlabUsed`, `SlabGaps` and `SlabCapacity` report the slab's state, in slots. `registry.Compact()` and `registry.ShrinkToFit()` do this for every store. See [Registry](registry.md).

```csharp
if (items.SlabGaps > items.SlabCapacity / 2)
{
    items.Compact();
}

items.ShrinkToFit();
```

## See also

- [Registry](registry.md)
- [Entity handles](entity-handles.md#attachments)
- [Views](views.md)
- [Relations](relations.md)
- [Observers and signals](observers.md)
- [Command buffers](commands.md)
- [Snapshots](snapshots.md)
- [Inboxes](inboxes.md)
