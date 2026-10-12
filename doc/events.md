# Events

`Dispatcher` is a typed event bus for carrying game events between systems. `Trigger` delivers an event at once, and `Enqueue` holds it until `Update`. Unlike the store signals in [Observers and signals](observers.md), it is not tied to any write. `Dispatcher` lives in `Cestyll.Ecs.Events`. `Connection` and the handler delegate `RefAction<T>` live in `Cestyll.Ecs`.

```csharp
using Cestyll.Ecs;
using Cestyll.Ecs.Events;

struct Hit { public int Damage; }
struct Died { public Entity32 Entity; }

var dispatcher = new Dispatcher();
```

## Connect

`Connect` adds a `RefAction<T>` handler for events of type `T` and returns a `Connection`. Any type can be an event. A method group needs the event type spelled out.

```csharp
dispatcher.Connect((ref Hit hit) => Console.WriteLine($"hit for {hit.Damage}"));

void OnDied(ref Died died) => Console.WriteLine($"{died.Entity} died");

dispatcher.Connect<Died>(OnDied);
```

Handlers take the event by ref, so a change one handler makes is seen by the handlers after it.

```csharp
dispatcher.Connect((ref Hit hit) => hit.Damage *= 2);
dispatcher.Connect((ref Hit hit) => Console.WriteLine(hit.Damage)); // sees the doubled value
```

## Trigger

`Trigger` delivers the event to its handlers now. `Trigger(ref e)` passes your own variable, so you see what the handlers changed.

```csharp
dispatcher.Trigger(new Hit { Damage = 3 });

var hit = new Hit { Damage = 3 };
dispatcher.Trigger(ref hit);
Console.WriteLine(hit.Damage);
```

## Enqueue

`Enqueue` adds the event to its queue without delivering it, even with no handler connected.

```csharp
dispatcher.Enqueue(new Hit { Damage = 5 });
dispatcher.Enqueue(new Died());
```

## Update

`Update<T>()` delivers the `T` events queued before the call, in order. `Update()` does the same for every queue.

```csharp
dispatcher.Update<Hit>();

// once per frame, after the systems that post events
dispatcher.Update();
```

## Named queues

The overloads of `Connect`, `Disconnect`, `DisconnectAll`, `Trigger`, `Enqueue`, `Update`, `Clear` and `Count` that take a name work on a separate queue for `T`, with its own handlers.

```csharp
dispatcher.Connect("ui", (ref Hit hit) => Console.WriteLine($"flash {hit.Damage}"));

dispatcher.Trigger("ui", new Hit { Damage = 1 });
dispatcher.Enqueue("ui", new Hit { Damage = 2 });
dispatcher.Update<Hit>("ui");

var flash = new Hit { Damage = 3 };
dispatcher.Trigger("ui", ref flash);
```

## Disconnecting

### Connection

Disposing the `Connection` from `Connect` disconnects the handler, so `using` scopes a handler to a block.

```csharp
using (dispatcher.Connect((ref Hit hit) => Console.WriteLine(hit.Damage)))
{
    dispatcher.Trigger(new Hit { Damage = 4 });
}
```

### Disconnect a handler

`Disconnect<T>(handler)` removes one handler from the default queue, and `Disconnect<T>(name, handler)` from a named one. Pass the same delegate you connected.

```csharp
RefAction<Hit> log = (ref Hit hit) => Console.WriteLine(hit.Damage);

dispatcher.Connect(log);
dispatcher.Disconnect(log);

dispatcher.Connect("ui", log);
dispatcher.Disconnect("ui", log);
```

### Disconnect an instance

`Disconnect(instance)` drops every handler bound to that object, on every queue. It matches method groups on the instance and lambdas that capture only `this`.

```csharp
sealed class DamageSystem
{
    public DamageSystem(Dispatcher events)
    {
        events.Connect<Hit>(OnHit);
        events.Connect<Hit>("ui", OnHit);
    }

    private void OnHit(ref Hit hit) => Console.WriteLine(hit.Damage);
}

var system = new DamageSystem(dispatcher);
dispatcher.Disconnect(system); // both handlers gone
```

### DisconnectAll

`DisconnectAll<T>()` drops every handler for `T` on the default queue, and `DisconnectAll<T>(name)` on a named one.

```csharp
dispatcher.DisconnectAll<Hit>();
dispatcher.DisconnectAll<Hit>("ui");
```

## Clear

`Clear` drops queued events without delivering them. `Clear<T>()` and `Clear<T>(name)` empty one queue, and `Clear()` empties every queue.

```csharp
dispatcher.Clear<Hit>();
dispatcher.Clear<Hit>("ui");
dispatcher.Clear();
```

## Count

`Count` is the number of queued events. `Count<T>()` and `Count<T>(name)` count one queue, and `Count()` counts every queue.

```csharp
if (dispatcher.Count<Hit>() > 0)
{
    dispatcher.Update<Hit>();
}

Console.WriteLine(dispatcher.Count());
```

## ShrinkToFit

`ShrinkToFit` trims each queue's capacity to what it holds now.

```csharp
dispatcher.ShrinkToFit();
```

## Keeping a dispatcher with a registry

A registry doesn't own a dispatcher. To share one, store it as a [context value](registry.md).

```csharp
var registry = new Registry();
registry.AddOrReplaceContext(new Dispatcher());

var events = registry.GetContext<Dispatcher>();
events.Enqueue(new Hit { Damage = 1 });
```

## See also

- [Observers and signals](observers.md)
- [Registry](registry.md)
- [Command buffers](commands.md)
