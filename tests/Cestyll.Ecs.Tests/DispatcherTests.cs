using Cestyll.Ecs;
using Cestyll.Ecs.Events;

namespace Cestyll.Ecs.Tests;

public class DispatcherTests
{
    private record struct Hit(int Damage);
    private record struct Died(int Id);

    private sealed class Listener
    {
        public readonly List<int> Hits = [];
        public int Deaths;
        public void OnHit(ref Hit h) => Hits.Add(h.Damage);
        public void OnDied(ref Died d) => Deaths++;
    }

    [Fact]
    public void Trigger_DeliversAtOnce_InConnectOrder()
    {
        var dispatcher = new Dispatcher();
        var log = new List<string>();
        dispatcher.Connect((ref Hit h) => log.Add($"a{h.Damage}"));
        dispatcher.Connect((ref Hit h) => log.Add($"b{h.Damage}"));

        dispatcher.Trigger(new Hit(3));
        dispatcher.Trigger(new Died(1)); // no handlers

        Assert.Equal(["a3", "b3"], log);
        Assert.Equal(0, dispatcher.Count());
    }

    [Fact]
    public void Handlers_TakeTheEventByRef()
    {
        var dispatcher = new Dispatcher();
        var seen = new List<int>();
        dispatcher.Connect((ref Hit h) => h.Damage *= 2);
        dispatcher.Connect((ref Hit h) => seen.Add(h.Damage));

        var hit = new Hit(3);
        dispatcher.Trigger(ref hit);
        dispatcher.Enqueue(new Hit(5));
        dispatcher.Update<Hit>();

        Assert.Equal(6, hit.Damage);
        Assert.Equal([6, 10], seen);
    }

    [Fact]
    public void Connect_SameHandlerTwice_MovesItToTheEnd()
    {
        var dispatcher = new Dispatcher();
        var listener = new Listener();
        var log = new List<int>();
        dispatcher.Connect<Hit>(listener.OnHit);
        dispatcher.Connect((ref Hit h) => log.Add(listener.Hits.Count));
        dispatcher.Connect<Hit>(listener.OnHit);

        dispatcher.Trigger(new Hit(1));

        Assert.Equal([1], listener.Hits); // once, not twice
        Assert.Equal([0], log); // and after the other handler
    }

    [Fact]
    public void Enqueue_WaitsForUpdate()
    {
        var dispatcher = new Dispatcher();
        var hits = new List<int>();
        dispatcher.Connect((ref Hit h) => hits.Add(h.Damage));

        dispatcher.Enqueue(new Hit(1));
        dispatcher.Enqueue(new Hit(2));

        Assert.Empty(hits);
        Assert.Equal(2, dispatcher.Count<Hit>());
    }

    [Fact]
    public void UpdateOtherType_DeliversNothing()
    {
        var dispatcher = new Dispatcher();
        var hits = new List<int>();
        dispatcher.Connect((ref Hit h) => hits.Add(h.Damage));
        dispatcher.Enqueue(new Hit(1));

        dispatcher.Update<Died>();

        Assert.Empty(hits);
    }

    [Fact]
    public void Update_DeliversEventsQueuedBeforeConnect()
    {
        var dispatcher = new Dispatcher();
        dispatcher.Enqueue(new Hit(1)); // queued before any handler
        var hits = new List<int>();
        dispatcher.Connect((ref Hit h) => hits.Add(h.Damage));
        dispatcher.Enqueue(new Hit(2));

        dispatcher.Update<Hit>();

        Assert.Equal([1, 2], hits);
        Assert.Equal(0, dispatcher.Count<Hit>());
    }

    [Fact]
    public void EventsEnqueuedByHandler_WaitForNextUpdate()
    {
        var dispatcher = new Dispatcher();
        var hits = new List<int>();
        dispatcher.Connect((ref Hit h) =>
        {
            hits.Add(h.Damage);
            if (h.Damage < 3)
            {
                dispatcher.Enqueue(new Hit(h.Damage + 1));
            }
        });
        dispatcher.Enqueue(new Hit(1));
        var counts = new List<int>();

        for (var i = 0; i < 4; i++)
        {
            dispatcher.Update();
            counts.Add(hits.Count);
        }

        Assert.Equal([1, 2, 3, 3], counts);
        Assert.Equal([1, 2, 3], hits);
    }

    [Fact]
    public void NestedUpdate_DeliversOnlyTheNextBatch()
    {
        var dispatcher = new Dispatcher();
        var hits = new List<int>();
        dispatcher.Connect((ref Hit h) =>
        {
            hits.Add(h.Damage);
            if (h.Damage == 1)
            {
                dispatcher.Enqueue(new Hit(10));
                dispatcher.Update<Hit>();
            }
        });
        dispatcher.Enqueue(new Hit(1));
        dispatcher.Enqueue(new Hit(2));

        dispatcher.Update<Hit>();

        Assert.Equal([1, 10, 2], hits);
        Assert.Equal(0, dispatcher.Count<Hit>());
    }

    [Fact]
    public void DisconnectDuringUpdate_TakesEffectAtNextEvent()
    {
        var dispatcher = new Dispatcher();
        var hits = new List<int>();
        Connection connection = default;
        connection = dispatcher.Connect((ref Hit h) =>
        {
            hits.Add(h.Damage);
            connection.Dispose();
        });
        dispatcher.Enqueue(new Hit(1));
        dispatcher.Enqueue(new Hit(2));

        dispatcher.Update();

        Assert.Equal([1], hits);
    }

    [Fact]
    public void NamedQueues_TriggerAndCountSeparately()
    {
        var dispatcher = new Dispatcher();
        var log = new List<string>();
        dispatcher.Connect((ref Hit h) => log.Add($"default{h.Damage}"));
        dispatcher.Connect("ui", (ref Hit h) => log.Add($"ui{h.Damage}"));

        dispatcher.Trigger("ui", new Hit(1));
        dispatcher.Trigger(new Hit(2));
        dispatcher.Enqueue("ui", new Hit(3));
        dispatcher.Enqueue("audio", new Hit(4)); // no handlers there

        Assert.Equal(["ui1", "default2"], log);
        Assert.Equal(0, dispatcher.Count<Hit>());
        Assert.Equal(1, dispatcher.Count<Hit>("ui"));
        Assert.Equal(2, dispatcher.Count());
    }

    [Fact]
    public void NamedQueues_UpdateSeparately()
    {
        var dispatcher = new Dispatcher();
        var log = new List<string>();
        dispatcher.Connect((ref Hit h) => log.Add($"default{h.Damage}"));
        dispatcher.Connect("ui", (ref Hit h) => log.Add($"ui{h.Damage}"));
        dispatcher.Enqueue("ui", new Hit(3));

        dispatcher.Update<Hit>();
        var afterDefault = log.ToArray();
        dispatcher.Update<Hit>("ui");

        Assert.Empty(afterDefault);
        Assert.Equal(["ui3"], log);
    }

    [Fact]
    public void Clear_NamedQueue_DropsItsEvents()
    {
        var dispatcher = new Dispatcher();
        dispatcher.Enqueue("audio", new Hit(4));

        dispatcher.Clear<Hit>("audio");

        Assert.Equal(0, dispatcher.Count());
    }

    [Fact]
    public void Disconnect_Instance_RemovesItsHandlersEverywhere()
    {
        var dispatcher = new Dispatcher();
        var listener = new Listener();
        var others = 0;
        dispatcher.Connect<Hit>(listener.OnHit);
        dispatcher.Connect<Hit>("ui", listener.OnHit);
        dispatcher.Connect<Died>(listener.OnDied);
        dispatcher.Connect((ref Hit _) => others++);

        dispatcher.Disconnect(listener);
        dispatcher.Trigger(new Hit(1));
        dispatcher.Trigger("ui", new Hit(1));
        dispatcher.Trigger(new Died());

        Assert.Empty(listener.Hits);
        Assert.Equal(0, listener.Deaths);
        Assert.Equal(1, others);
    }

    [Fact]
    public void Clear_DropsQueuedEvents()
    {
        var dispatcher = new Dispatcher();
        dispatcher.Enqueue(new Hit());
        dispatcher.Enqueue(new Died());

        dispatcher.Clear<Hit>();
        var afterHitClear = dispatcher.Count();
        dispatcher.Clear();

        Assert.Equal(1, afterHitClear);
        Assert.Equal(0, dispatcher.Count());
    }

    [Fact]
    public void Disconnect_StopsHandler()
    {
        var dispatcher = new Dispatcher();
        var hits = 0;
        RefAction<Hit> handler = (ref Hit _) => hits++;
        dispatcher.Connect(handler);
        dispatcher.Trigger(new Hit());

        dispatcher.Disconnect(handler);
        dispatcher.Trigger(new Hit());

        Assert.Equal(1, hits);
    }

    [Fact]
    public void ConnectionDispose_Disconnects()
    {
        var dispatcher = new Dispatcher();
        var hits = 0;
        RefAction<Hit> handler = (ref Hit _) => hits++;

        using (dispatcher.Connect(handler))
        {
            dispatcher.Trigger(new Hit());
        }
        dispatcher.Trigger(new Hit());

        Assert.Equal(1, hits);
    }

    [Fact]
    public void DisconnectAll_DropsEveryHandler()
    {
        var dispatcher = new Dispatcher();
        var hits = 0;
        dispatcher.Connect((ref Hit _) => hits++);

        dispatcher.DisconnectAll<Hit>();
        dispatcher.Trigger(new Hit());

        Assert.Equal(0, hits);
    }

    [Fact]
    public void DefaultConnection_DisposeIsNoOp()
    {
        var thrown = Record.Exception(() => default(Connection).Dispose());

        Assert.Null(thrown);
    }

    [Fact]
    public void ThrowingHandler_DropsItsEvent_KeepsTheRest()
    {
        var dispatcher = new Dispatcher();
        var hits = new List<int>();
        dispatcher.Connect((ref Hit h) =>
        {
            if (h.Damage == 2)
            {
                throw new InvalidOperationException();
            }
            hits.Add(h.Damage);
        });
        dispatcher.Enqueue(new Hit(1));
        dispatcher.Enqueue(new Hit(2));
        dispatcher.Enqueue(new Hit(3));

        var thrown = Record.Exception(() => dispatcher.Update<Hit>());
        var hitsAfterThrow = hits.ToArray();
        var queuedAfterThrow = dispatcher.Count<Hit>();
        dispatcher.Update<Hit>();

        Assert.IsType<InvalidOperationException>(thrown);
        Assert.Equal([1], hitsAfterThrow);
        Assert.Equal(1, queuedAfterThrow);
        Assert.Equal([1, 3], hits);
    }

    [Fact]
    public void DisconnectDuringTrigger_LaterHandlerStillGetsThisEvent()
    {
        var dispatcher = new Dispatcher();
        var log = new List<string>();
        RefAction<Hit> second = (ref Hit h) => log.Add($"second{h.Damage}");
        dispatcher.Connect((ref Hit h) =>
        {
            log.Add($"first{h.Damage}");
            dispatcher.Disconnect(second);
        });
        dispatcher.Connect(second);

        dispatcher.Trigger(new Hit(1));
        dispatcher.Trigger(new Hit(2));

        Assert.Equal(["first1", "second1", "first2"], log);
    }

    [Fact]
    public void NamedQueue_TriggerByRef_HandlersSeeEdits()
    {
        var dispatcher = new Dispatcher();
        var hits = 0;
        dispatcher.Connect("ui", (ref Hit h) => h.Damage++);
        dispatcher.Connect("ui", (ref Hit _) => hits++);
        var hit = new Hit(1);

        dispatcher.Trigger("ui", ref hit);

        Assert.Equal(2, hit.Damage);
        Assert.Equal(1, hits);
    }

    [Fact]
    public void NamedQueue_Disconnect_StopsHandler()
    {
        var dispatcher = new Dispatcher();
        var hits = 0;
        RefAction<Hit> count = (ref Hit _) => hits++;
        dispatcher.Connect("ui", (ref Hit h) => h.Damage++);
        dispatcher.Connect("ui", count);
        var hit = new Hit(1);

        dispatcher.Disconnect("ui", count);
        dispatcher.Trigger("ui", ref hit);

        Assert.Equal(2, hit.Damage);
        Assert.Equal(0, hits);
    }

    [Fact]
    public void NamedQueue_DisconnectAll_DropsEveryHandler()
    {
        var dispatcher = new Dispatcher();
        dispatcher.Connect("ui", (ref Hit h) => h.Damage++);
        var hit = new Hit(1);

        dispatcher.DisconnectAll<Hit>("ui");
        dispatcher.Trigger("ui", ref hit);

        Assert.Equal(1, hit.Damage);
    }

    [Fact]
    public void Update_RunsDefaultAndNamedQueues_InFirstUseOrder()
    {
        var dispatcher = new Dispatcher();
        var log = new List<string>();
        dispatcher.Connect("ui", (ref Hit h) => log.Add($"ui{h.Damage}"));
        dispatcher.Connect((ref Died d) => log.Add($"died{d.Id}"));
        dispatcher.Connect((ref Hit h) => log.Add($"hit{h.Damage}"));
        dispatcher.Enqueue(new Hit(3));
        dispatcher.Enqueue(new Died(2));
        dispatcher.Enqueue("ui", new Hit(1));

        dispatcher.Update();

        Assert.Equal(["ui1", "died2", "hit3"], log);
    }

    [Fact]
    public void NullName_Throws()
    {
        var dispatcher = new Dispatcher();

        Assert.Throws<ArgumentNullException>(() => dispatcher.Enqueue<Hit>(null!, new Hit()));
        Assert.Throws<ArgumentNullException>(() => dispatcher.Connect<Hit>(null!, (ref Hit _) => { }));
        Assert.Throws<ArgumentNullException>(() => dispatcher.Trigger<Hit>(null!, new Hit()));
        Assert.Throws<ArgumentNullException>(() => dispatcher.Update<Hit>(null!));
        Assert.Throws<ArgumentNullException>(() => dispatcher.Count<Hit>(null!));
        Assert.Throws<ArgumentNullException>(() => dispatcher.Clear<Hit>(null!));
    }

    [Fact]
    public void ShrinkToFit_TrimsQueues_KeepsQueuedEvents()
    {
        var dispatcher = new Dispatcher();
        for (var i = 0; i < 100; i++)
        {
            dispatcher.Enqueue(new Hit(i));
        }
        dispatcher.Update();
        dispatcher.Enqueue(new Hit(7));
        var hits = new List<int>();
        dispatcher.Connect((ref Hit h) => hits.Add(h.Damage));
        var before = dispatcher.Capacity;

        dispatcher.ShrinkToFit();
        var after = dispatcher.Capacity;
        dispatcher.Update();

        Assert.True(before >= 100);
        Assert.Equal(1, after);
        Assert.Equal([7], hits);
    }
}
