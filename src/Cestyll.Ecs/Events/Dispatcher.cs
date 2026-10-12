using Cestyll.Ecs.Collections;

namespace Cestyll.Ecs.Events;

// Typed event bus between systems: Trigger delivers now, Enqueue waits for Update. Not thread-safe.
// Handlers take the event by ref, so later handlers see earlier changes.
public sealed class Dispatcher
{
    private TypeSlots<EventChannel> _channels = new();
    private readonly Dictionary<(int Type, string Name), EventChannel> _named = [];
    private readonly List<EventChannel> _order = []; // Update() walks queues in first-use order

    // Reconnecting an equal delegate moves it to the end rather than adding it twice.
    public Connection Connect<T>(RefAction<T> handler) => Channel<T>().Connect(handler);
    public Connection Connect<T>(string name, RefAction<T> handler) => Channel<T>(name).Connect(handler);

    public void Disconnect<T>(RefAction<T> handler) => Find<T>()?.Disconnect(handler);
    public void Disconnect<T>(string name, RefAction<T> handler) => Find<T>(name)?.Disconnect(handler);

    // Misses lambdas capturing locals (bound to a closure); keep their Connection instead.
    public void Disconnect(object instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        foreach (var channel in _order)
        {
            channel.DisconnectTarget(instance);
        }
    }

    // Queued events stay.
    public void DisconnectAll<T>() => Find<T>()?.DisconnectAll();
    public void DisconnectAll<T>(string name) => Find<T>(name)?.DisconnectAll();

    // Connect or disconnect from a handler takes effect at the next event.
    public void Trigger<T>(T e) => Find<T>()?.Trigger(ref e);
    public void Trigger<T>(ref T e) => Find<T>()?.Trigger(ref e);
    public void Trigger<T>(string name, T e) => Find<T>(name)?.Trigger(ref e);
    public void Trigger<T>(string name, ref T e) => Find<T>(name)?.Trigger(ref e);

    // Queued even with no handler connected.
    public void Enqueue<T>(T e) => Channel<T>().Queue.Add(e);
    public void Enqueue<T>(string name, T e) => Channel<T>(name).Queue.Add(e);

    // Events enqueued by handlers wait for the next Update. A throwing event is dropped, the rest requeued.
    public void Update<T>() => Find<T>()?.Update();
    public void Update<T>(string name) => Find<T>(name)?.Update();

    // Every queue in first-use order. A throwing handler stops the pass.
    public void Update()
    {
        // By index: a handler may use a new queue, which appends to _order.
        for (var i = 0; i < _order.Count; i++)
        {
            _order[i].Update();
        }
    }

    public void Clear<T>() => Find<T>()?.Clear();
    public void Clear<T>(string name) => Find<T>(name)?.Clear();

    public void Clear()
    {
        foreach (var channel in _order)
        {
            channel.Clear();
        }
    }

    public int Count<T>() => Find<T>()?.Count ?? 0;
    public int Count<T>(string name) => Find<T>(name)?.Count ?? 0;

    public int Count()
    {
        var count = 0;
        foreach (var channel in _order)
        {
            count += channel.Count;
        }
        return count;
    }

    // Not called by Registry.ShrinkToFit.
    public void ShrinkToFit()
    {
        foreach (var channel in _order)
        {
            channel.ShrinkToFit();
        }
    }

    // For tests.
    internal int Capacity
    {
        get
        {
            var capacity = 0;
            foreach (var channel in _order)
            {
                capacity += channel.Capacity;
            }
            return capacity;
        }
    }

    private EventChannel<T>? Find<T>()
    {
        return (EventChannel<T>?)_channels.Find(TypeId<T>.Value);
    }

    private EventChannel<T>? Find<T>(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return _named.TryGetValue((TypeId<T>.Value, name), out var channel) ? (EventChannel<T>)channel : null;
    }

    private EventChannel<T> Channel<T>()
    {
        ref var slot = ref _channels.Slot(TypeId<T>.Value);
        if (slot is not EventChannel<T> channel)
        {
            channel = new EventChannel<T>();
            slot = channel;
            _order.Add(channel);
        }
        return channel;
    }

    private EventChannel<T> Channel<T>(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        ref var slot = ref CollectionsMarshal.GetValueRefOrAddDefault(_named, (TypeId<T>.Value, name), out var exists);
        if (!exists)
        {
            slot = new EventChannel<T>();
            _order.Add(slot);
        }
        return (EventChannel<T>)slot!;
    }
}

internal abstract class EventChannel
{
    public abstract int Count { get; }
    public abstract int Capacity { get; }
    public abstract void Update();
    public abstract void Clear();
    public abstract void ShrinkToFit();
    public abstract void Disconnect(Delegate handler);
    public abstract void DisconnectTarget(object instance);
}

internal sealed class EventChannel<T> : EventChannel
{
    private RefAction<T>? _handlers;
    public List<T> Queue = [];
    private List<T>? _spare; // the list the last Update drained, reused as the next Queue

    public override int Count => Queue.Count;
    public override int Capacity => Queue.Capacity + (_spare?.Capacity ?? 0);
    public override void Clear() => Queue.Clear();

    public override void ShrinkToFit()
    {
        Queue.Capacity = Queue.Count;
        _spare = null;
    }

    public Connection Connect(RefAction<T> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _handlers -= handler;
        _handlers += handler;
        return new Connection(() => Disconnect(handler));
    }

    public override void Disconnect(Delegate handler) => _handlers -= (RefAction<T>)handler;

    public override void DisconnectTarget(object instance)
    {
        if (_handlers is null)
        {
            return;
        }

        foreach (var handler in _handlers.GetInvocationList())
        {
            if (ReferenceEquals(handler.Target, instance))
            {
                _handlers -= (RefAction<T>)handler;
            }
        }
    }

    public void DisconnectAll() => _handlers = null;

    public void Trigger(ref T e) => _handlers?.Invoke(ref e);

    public override void Update()
    {
        if (Queue.Count == 0)
        {
            return;
        }

        // Swap queues first so handlers touch the next batch; refs into this one stay valid.
        var batch = Queue;
        Queue = _spare ?? [];
        _spare = null;
        var delivered = 0;
        try
        {
            var events = CollectionsMarshal.AsSpan(batch);
            for (; delivered < events.Length; delivered++)
            {
                _handlers?.Invoke(ref events[delivered]);
            }
        }
        finally
        {
            if (delivered < batch.Count)
            {
                // Skip the event that threw: delivering it again would throw again.
                batch.RemoveRange(0, delivered + 1);
                Queue.InsertRange(0, batch);
            }

            batch.Clear();
            _spare = batch;
        }
    }
}
