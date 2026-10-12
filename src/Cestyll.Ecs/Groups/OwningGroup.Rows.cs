using Cestyll.Ecs.Storage;
using Cestyll.Ecs.Views;

namespace Cestyll.Ecs.Groups;

// Reading an owning group by type.
internal sealed partial class OwningGroup<TEntity>
{
    // So building again returns the same Group.
    internal Group<TEntity>? Handle;

    internal Registry<TEntity> Registry => _pools[0].OwnerRegistry!;

    internal bool Contains(TEntity e)
    {
        var slot = _pools[0].IndexOf(e);
        return slot >= 0 && slot < Count;
    }

    // T's owned pool, or null. Own<T> only takes default pools, so no name check.
    private Pool<TEntity, T>? FindOwned<T>() where T : IComponent
    {
        foreach (var p in _pools)
        {
            if (p is Pool<TEntity, T> pool)
            {
                return pool;
            }
        }

        return null;
    }

    internal Pool<TEntity, T> OwnedPool<T>() where T : IComponent =>
        FindOwned<T>() ?? throw new InvalidOperationException($"This group doesn't own {typeof(T).Name}");

    // Slot for slot with Entities.
    public Span<T> Span<T>() where T : IComponent => OwnedPool<T>().Components[..Count];

    public GroupRows<TEntity, T1> Rows<T1>()
        where T1 : IComponent
        => new(Count, OwnedPool<T1>());

    // Owned types read aligned slots; With types are looked up.
    internal void Each<TFn, T1>(ref TFn fn)
        where TFn : struct, IEach<TFn, TEntity, T1>
        where T1 : IComponent
    {
        var o1 = FindOwned<T1>();
        if (o1 is not null)
        {
            new GroupRows<TEntity, T1>(Count, o1).Each(ref fn);
            return;
        }

        var p1 = Storage<T1>(); // not owned past the early return
        var registry = Registry;
        using var scope = WalkScope<TEntity>.Enter(registry, [p1]);
        EachLookedUp(ref fn, p1, false);
    }

    private void EachLookedUp<TFn, T1>(ref TFn fn, Pool<TEntity, T1> p1, bool a1)
        where TFn : struct, IEach<TFn, TEntity, T1>
        where T1 : IComponent
    {
        var ents = _pools[0].Entities;
        var s1 = p1.SparseSpan;
        var c1 = p1.Components;
        for (var i = Count - 1; i >= 0; i--)
        {
            var e = ents[i];
            var j1 = a1 ? i : SparseSet<TEntity>.Slot(s1, e.Index);
            fn.Invoke(e, ref c1[j1]);
        }
    }

    internal void Each<T1>(RefAction<TEntity, T1> action)
        where T1 : IComponent
    {
        var adapter = new RefActionAdapter<TEntity, T1>(action);
        Each<RefActionAdapter<TEntity, T1>, T1>(ref adapter);
    }

    public GroupRows<TEntity, T1, T2> Rows<T1, T2>()
        where T1 : IComponent
        where T2 : IComponent
        => new(Count, OwnedPool<T1>(), OwnedPool<T2>());

    // Owned types read aligned slots; With types are looked up.
    internal void Each<TFn, T1, T2>(ref TFn fn)
        where TFn : struct, IEach<TFn, TEntity, T1, T2>
        where T1 : IComponent
        where T2 : IComponent
    {
        var o1 = FindOwned<T1>();
        var o2 = FindOwned<T2>();
        if (o1 is not null && o2 is not null)
        {
            new GroupRows<TEntity, T1, T2>(Count, o1, o2).Each(ref fn);
            return;
        }

        var p1 = o1 ?? Storage<T1>();
        var p2 = o2 ?? Storage<T2>();
        var registry = Registry;
        using var scope = WalkScope<TEntity>.Enter(registry, [p1, p2]);
        EachLookedUp(ref fn, p1, o1 is not null, p2, o2 is not null);
    }

    private void EachLookedUp<TFn, T1, T2>(ref TFn fn, Pool<TEntity, T1> p1, bool a1, Pool<TEntity, T2> p2, bool a2)
        where TFn : struct, IEach<TFn, TEntity, T1, T2>
        where T1 : IComponent
        where T2 : IComponent
    {
        var ents = _pools[0].Entities;
        var s1 = p1.SparseSpan;
        var c1 = p1.Components;
        var s2 = p2.SparseSpan;
        var c2 = p2.Components;
        for (var i = Count - 1; i >= 0; i--)
        {
            var e = ents[i];
            var j1 = a1 ? i : SparseSet<TEntity>.Slot(s1, e.Index);
            var j2 = a2 ? i : SparseSet<TEntity>.Slot(s2, e.Index);
            fn.Invoke(e, ref c1[j1], ref c2[j2]);
        }
    }

    internal void Each<T1, T2>(RefAction<TEntity, T1, T2> action)
        where T1 : IComponent
        where T2 : IComponent
    {
        var adapter = new RefActionAdapter<TEntity, T1, T2>(action);
        Each<RefActionAdapter<TEntity, T1, T2>, T1, T2>(ref adapter);
    }

    public GroupRows<TEntity, T1, T2, T3> Rows<T1, T2, T3>()
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        => new(Count, OwnedPool<T1>(), OwnedPool<T2>(), OwnedPool<T3>());

    // Owned types read aligned slots; With types are looked up.
    internal void Each<TFn, T1, T2, T3>(ref TFn fn)
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3>
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
    {
        var o1 = FindOwned<T1>();
        var o2 = FindOwned<T2>();
        var o3 = FindOwned<T3>();
        if (o1 is not null && o2 is not null && o3 is not null)
        {
            new GroupRows<TEntity, T1, T2, T3>(Count, o1, o2, o3).Each(ref fn);
            return;
        }

        var p1 = o1 ?? Storage<T1>();
        var p2 = o2 ?? Storage<T2>();
        var p3 = o3 ?? Storage<T3>();
        var registry = Registry;
        using var scope = WalkScope<TEntity>.Enter(registry, [p1, p2, p3]);
        EachLookedUp(ref fn, p1, o1 is not null, p2, o2 is not null, p3, o3 is not null);
    }

    private void EachLookedUp<TFn, T1, T2, T3>(ref TFn fn, Pool<TEntity, T1> p1, bool a1, Pool<TEntity, T2> p2, bool a2, Pool<TEntity, T3> p3, bool a3)
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3>
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
    {
        var ents = _pools[0].Entities;
        var s1 = p1.SparseSpan;
        var c1 = p1.Components;
        var s2 = p2.SparseSpan;
        var c2 = p2.Components;
        var s3 = p3.SparseSpan;
        var c3 = p3.Components;
        for (var i = Count - 1; i >= 0; i--)
        {
            var e = ents[i];
            var j1 = a1 ? i : SparseSet<TEntity>.Slot(s1, e.Index);
            var j2 = a2 ? i : SparseSet<TEntity>.Slot(s2, e.Index);
            var j3 = a3 ? i : SparseSet<TEntity>.Slot(s3, e.Index);
            fn.Invoke(e, ref c1[j1], ref c2[j2], ref c3[j3]);
        }
    }

    internal void Each<T1, T2, T3>(RefAction<TEntity, T1, T2, T3> action)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
    {
        var adapter = new RefActionAdapter<TEntity, T1, T2, T3>(action);
        Each<RefActionAdapter<TEntity, T1, T2, T3>, T1, T2, T3>(ref adapter);
    }

    public GroupRows<TEntity, T1, T2, T3, T4> Rows<T1, T2, T3, T4>()
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        => new(Count, OwnedPool<T1>(), OwnedPool<T2>(), OwnedPool<T3>(), OwnedPool<T4>());

    // Owned types read aligned slots; With types are looked up.
    internal void Each<TFn, T1, T2, T3, T4>(ref TFn fn)
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4>
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
    {
        var o1 = FindOwned<T1>();
        var o2 = FindOwned<T2>();
        var o3 = FindOwned<T3>();
        var o4 = FindOwned<T4>();
        if (o1 is not null && o2 is not null && o3 is not null && o4 is not null)
        {
            new GroupRows<TEntity, T1, T2, T3, T4>(Count, o1, o2, o3, o4).Each(ref fn);
            return;
        }

        var p1 = o1 ?? Storage<T1>();
        var p2 = o2 ?? Storage<T2>();
        var p3 = o3 ?? Storage<T3>();
        var p4 = o4 ?? Storage<T4>();
        var registry = Registry;
        using var scope = WalkScope<TEntity>.Enter(registry, [p1, p2, p3, p4]);
        EachLookedUp(ref fn, p1, o1 is not null, p2, o2 is not null, p3, o3 is not null, p4, o4 is not null);
    }

    private void EachLookedUp<TFn, T1, T2, T3, T4>(ref TFn fn, Pool<TEntity, T1> p1, bool a1, Pool<TEntity, T2> p2, bool a2, Pool<TEntity, T3> p3, bool a3, Pool<TEntity, T4> p4, bool a4)
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4>
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
    {
        var ents = _pools[0].Entities;
        var s1 = p1.SparseSpan;
        var c1 = p1.Components;
        var s2 = p2.SparseSpan;
        var c2 = p2.Components;
        var s3 = p3.SparseSpan;
        var c3 = p3.Components;
        var s4 = p4.SparseSpan;
        var c4 = p4.Components;
        for (var i = Count - 1; i >= 0; i--)
        {
            var e = ents[i];
            var j1 = a1 ? i : SparseSet<TEntity>.Slot(s1, e.Index);
            var j2 = a2 ? i : SparseSet<TEntity>.Slot(s2, e.Index);
            var j3 = a3 ? i : SparseSet<TEntity>.Slot(s3, e.Index);
            var j4 = a4 ? i : SparseSet<TEntity>.Slot(s4, e.Index);
            fn.Invoke(e, ref c1[j1], ref c2[j2], ref c3[j3], ref c4[j4]);
        }
    }

    internal void Each<T1, T2, T3, T4>(RefAction<TEntity, T1, T2, T3, T4> action)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
    {
        var adapter = new RefActionAdapter<TEntity, T1, T2, T3, T4>(action);
        Each<RefActionAdapter<TEntity, T1, T2, T3, T4>, T1, T2, T3, T4>(ref adapter);
    }

    public GroupRows<TEntity, T1, T2, T3, T4, T5> Rows<T1, T2, T3, T4, T5>()
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        => new(Count, OwnedPool<T1>(), OwnedPool<T2>(), OwnedPool<T3>(), OwnedPool<T4>(), OwnedPool<T5>());

    // Owned types read aligned slots; With types are looked up.
    internal void Each<TFn, T1, T2, T3, T4, T5>(ref TFn fn)
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5>
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
    {
        var o1 = FindOwned<T1>();
        var o2 = FindOwned<T2>();
        var o3 = FindOwned<T3>();
        var o4 = FindOwned<T4>();
        var o5 = FindOwned<T5>();
        if (o1 is not null && o2 is not null && o3 is not null && o4 is not null && o5 is not null)
        {
            new GroupRows<TEntity, T1, T2, T3, T4, T5>(Count, o1, o2, o3, o4, o5).Each(ref fn);
            return;
        }

        var p1 = o1 ?? Storage<T1>();
        var p2 = o2 ?? Storage<T2>();
        var p3 = o3 ?? Storage<T3>();
        var p4 = o4 ?? Storage<T4>();
        var p5 = o5 ?? Storage<T5>();
        var registry = Registry;
        using var scope = WalkScope<TEntity>.Enter(registry, [p1, p2, p3, p4, p5]);
        EachLookedUp(ref fn, p1, o1 is not null, p2, o2 is not null, p3, o3 is not null, p4, o4 is not null, p5, o5 is not null);
    }

    private void EachLookedUp<TFn, T1, T2, T3, T4, T5>(ref TFn fn, Pool<TEntity, T1> p1, bool a1, Pool<TEntity, T2> p2, bool a2, Pool<TEntity, T3> p3, bool a3, Pool<TEntity, T4> p4, bool a4, Pool<TEntity, T5> p5, bool a5)
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5>
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
    {
        var ents = _pools[0].Entities;
        var s1 = p1.SparseSpan;
        var c1 = p1.Components;
        var s2 = p2.SparseSpan;
        var c2 = p2.Components;
        var s3 = p3.SparseSpan;
        var c3 = p3.Components;
        var s4 = p4.SparseSpan;
        var c4 = p4.Components;
        var s5 = p5.SparseSpan;
        var c5 = p5.Components;
        for (var i = Count - 1; i >= 0; i--)
        {
            var e = ents[i];
            var j1 = a1 ? i : SparseSet<TEntity>.Slot(s1, e.Index);
            var j2 = a2 ? i : SparseSet<TEntity>.Slot(s2, e.Index);
            var j3 = a3 ? i : SparseSet<TEntity>.Slot(s3, e.Index);
            var j4 = a4 ? i : SparseSet<TEntity>.Slot(s4, e.Index);
            var j5 = a5 ? i : SparseSet<TEntity>.Slot(s5, e.Index);
            fn.Invoke(e, ref c1[j1], ref c2[j2], ref c3[j3], ref c4[j4], ref c5[j5]);
        }
    }

    internal void Each<T1, T2, T3, T4, T5>(RefAction<TEntity, T1, T2, T3, T4, T5> action)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
    {
        var adapter = new RefActionAdapter<TEntity, T1, T2, T3, T4, T5>(action);
        Each<RefActionAdapter<TEntity, T1, T2, T3, T4, T5>, T1, T2, T3, T4, T5>(ref adapter);
    }

    public GroupRows<TEntity, T1, T2, T3, T4, T5, T6> Rows<T1, T2, T3, T4, T5, T6>()
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        => new(Count, OwnedPool<T1>(), OwnedPool<T2>(), OwnedPool<T3>(), OwnedPool<T4>(), OwnedPool<T5>(), OwnedPool<T6>());

    // Owned types read aligned slots; With types are looked up.
    internal void Each<TFn, T1, T2, T3, T4, T5, T6>(ref TFn fn)
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6>
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
    {
        var o1 = FindOwned<T1>();
        var o2 = FindOwned<T2>();
        var o3 = FindOwned<T3>();
        var o4 = FindOwned<T4>();
        var o5 = FindOwned<T5>();
        var o6 = FindOwned<T6>();
        if (o1 is not null && o2 is not null && o3 is not null && o4 is not null && o5 is not null && o6 is not null)
        {
            new GroupRows<TEntity, T1, T2, T3, T4, T5, T6>(Count, o1, o2, o3, o4, o5, o6).Each(ref fn);
            return;
        }

        var p1 = o1 ?? Storage<T1>();
        var p2 = o2 ?? Storage<T2>();
        var p3 = o3 ?? Storage<T3>();
        var p4 = o4 ?? Storage<T4>();
        var p5 = o5 ?? Storage<T5>();
        var p6 = o6 ?? Storage<T6>();
        var registry = Registry;
        using var scope = WalkScope<TEntity>.Enter(registry, [p1, p2, p3, p4, p5, p6]);
        EachLookedUp(ref fn, p1, o1 is not null, p2, o2 is not null, p3, o3 is not null, p4, o4 is not null, p5, o5 is not null, p6, o6 is not null);
    }

    private void EachLookedUp<TFn, T1, T2, T3, T4, T5, T6>(ref TFn fn, Pool<TEntity, T1> p1, bool a1, Pool<TEntity, T2> p2, bool a2, Pool<TEntity, T3> p3, bool a3, Pool<TEntity, T4> p4, bool a4, Pool<TEntity, T5> p5, bool a5, Pool<TEntity, T6> p6, bool a6)
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6>
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
    {
        var ents = _pools[0].Entities;
        var s1 = p1.SparseSpan;
        var c1 = p1.Components;
        var s2 = p2.SparseSpan;
        var c2 = p2.Components;
        var s3 = p3.SparseSpan;
        var c3 = p3.Components;
        var s4 = p4.SparseSpan;
        var c4 = p4.Components;
        var s5 = p5.SparseSpan;
        var c5 = p5.Components;
        var s6 = p6.SparseSpan;
        var c6 = p6.Components;
        for (var i = Count - 1; i >= 0; i--)
        {
            var e = ents[i];
            var j1 = a1 ? i : SparseSet<TEntity>.Slot(s1, e.Index);
            var j2 = a2 ? i : SparseSet<TEntity>.Slot(s2, e.Index);
            var j3 = a3 ? i : SparseSet<TEntity>.Slot(s3, e.Index);
            var j4 = a4 ? i : SparseSet<TEntity>.Slot(s4, e.Index);
            var j5 = a5 ? i : SparseSet<TEntity>.Slot(s5, e.Index);
            var j6 = a6 ? i : SparseSet<TEntity>.Slot(s6, e.Index);
            fn.Invoke(e, ref c1[j1], ref c2[j2], ref c3[j3], ref c4[j4], ref c5[j5], ref c6[j6]);
        }
    }

    internal void Each<T1, T2, T3, T4, T5, T6>(RefAction<TEntity, T1, T2, T3, T4, T5, T6> action)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
    {
        var adapter = new RefActionAdapter<TEntity, T1, T2, T3, T4, T5, T6>(action);
        Each<RefActionAdapter<TEntity, T1, T2, T3, T4, T5, T6>, T1, T2, T3, T4, T5, T6>(ref adapter);
    }

    public GroupRows<TEntity, T1, T2, T3, T4, T5, T6, T7> Rows<T1, T2, T3, T4, T5, T6, T7>()
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        => new(Count, OwnedPool<T1>(), OwnedPool<T2>(), OwnedPool<T3>(), OwnedPool<T4>(), OwnedPool<T5>(), OwnedPool<T6>(), OwnedPool<T7>());

    // Owned types read aligned slots; With types are looked up.
    internal void Each<TFn, T1, T2, T3, T4, T5, T6, T7>(ref TFn fn)
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7>
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
    {
        var o1 = FindOwned<T1>();
        var o2 = FindOwned<T2>();
        var o3 = FindOwned<T3>();
        var o4 = FindOwned<T4>();
        var o5 = FindOwned<T5>();
        var o6 = FindOwned<T6>();
        var o7 = FindOwned<T7>();
        if (o1 is not null && o2 is not null && o3 is not null && o4 is not null && o5 is not null && o6 is not null && o7 is not null)
        {
            new GroupRows<TEntity, T1, T2, T3, T4, T5, T6, T7>(Count, o1, o2, o3, o4, o5, o6, o7).Each(ref fn);
            return;
        }

        var p1 = o1 ?? Storage<T1>();
        var p2 = o2 ?? Storage<T2>();
        var p3 = o3 ?? Storage<T3>();
        var p4 = o4 ?? Storage<T4>();
        var p5 = o5 ?? Storage<T5>();
        var p6 = o6 ?? Storage<T6>();
        var p7 = o7 ?? Storage<T7>();
        var registry = Registry;
        using var scope = WalkScope<TEntity>.Enter(registry, [p1, p2, p3, p4, p5, p6, p7]);
        EachLookedUp(ref fn, p1, o1 is not null, p2, o2 is not null, p3, o3 is not null, p4, o4 is not null, p5, o5 is not null, p6, o6 is not null, p7, o7 is not null);
    }

    private void EachLookedUp<TFn, T1, T2, T3, T4, T5, T6, T7>(ref TFn fn, Pool<TEntity, T1> p1, bool a1, Pool<TEntity, T2> p2, bool a2, Pool<TEntity, T3> p3, bool a3, Pool<TEntity, T4> p4, bool a4, Pool<TEntity, T5> p5, bool a5, Pool<TEntity, T6> p6, bool a6, Pool<TEntity, T7> p7, bool a7)
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7>
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
    {
        var ents = _pools[0].Entities;
        var s1 = p1.SparseSpan;
        var c1 = p1.Components;
        var s2 = p2.SparseSpan;
        var c2 = p2.Components;
        var s3 = p3.SparseSpan;
        var c3 = p3.Components;
        var s4 = p4.SparseSpan;
        var c4 = p4.Components;
        var s5 = p5.SparseSpan;
        var c5 = p5.Components;
        var s6 = p6.SparseSpan;
        var c6 = p6.Components;
        var s7 = p7.SparseSpan;
        var c7 = p7.Components;
        for (var i = Count - 1; i >= 0; i--)
        {
            var e = ents[i];
            var j1 = a1 ? i : SparseSet<TEntity>.Slot(s1, e.Index);
            var j2 = a2 ? i : SparseSet<TEntity>.Slot(s2, e.Index);
            var j3 = a3 ? i : SparseSet<TEntity>.Slot(s3, e.Index);
            var j4 = a4 ? i : SparseSet<TEntity>.Slot(s4, e.Index);
            var j5 = a5 ? i : SparseSet<TEntity>.Slot(s5, e.Index);
            var j6 = a6 ? i : SparseSet<TEntity>.Slot(s6, e.Index);
            var j7 = a7 ? i : SparseSet<TEntity>.Slot(s7, e.Index);
            fn.Invoke(e, ref c1[j1], ref c2[j2], ref c3[j3], ref c4[j4], ref c5[j5], ref c6[j6], ref c7[j7]);
        }
    }

    internal void Each<T1, T2, T3, T4, T5, T6, T7>(RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7> action)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
    {
        var adapter = new RefActionAdapter<TEntity, T1, T2, T3, T4, T5, T6, T7>(action);
        Each<RefActionAdapter<TEntity, T1, T2, T3, T4, T5, T6, T7>, T1, T2, T3, T4, T5, T6, T7>(ref adapter);
    }

    public GroupRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8> Rows<T1, T2, T3, T4, T5, T6, T7, T8>()
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        => new(Count, OwnedPool<T1>(), OwnedPool<T2>(), OwnedPool<T3>(), OwnedPool<T4>(), OwnedPool<T5>(), OwnedPool<T6>(), OwnedPool<T7>(), OwnedPool<T8>());

    // Owned types read aligned slots; With types are looked up.
    internal void Each<TFn, T1, T2, T3, T4, T5, T6, T7, T8>(ref TFn fn)
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7, T8>
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
    {
        var o1 = FindOwned<T1>();
        var o2 = FindOwned<T2>();
        var o3 = FindOwned<T3>();
        var o4 = FindOwned<T4>();
        var o5 = FindOwned<T5>();
        var o6 = FindOwned<T6>();
        var o7 = FindOwned<T7>();
        var o8 = FindOwned<T8>();
        if (o1 is not null && o2 is not null && o3 is not null && o4 is not null && o5 is not null && o6 is not null && o7 is not null && o8 is not null)
        {
            new GroupRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8>(Count, o1, o2, o3, o4, o5, o6, o7, o8).Each(ref fn);
            return;
        }

        var p1 = o1 ?? Storage<T1>();
        var p2 = o2 ?? Storage<T2>();
        var p3 = o3 ?? Storage<T3>();
        var p4 = o4 ?? Storage<T4>();
        var p5 = o5 ?? Storage<T5>();
        var p6 = o6 ?? Storage<T6>();
        var p7 = o7 ?? Storage<T7>();
        var p8 = o8 ?? Storage<T8>();
        var registry = Registry;
        using var scope = WalkScope<TEntity>.Enter(registry, [p1, p2, p3, p4, p5, p6, p7, p8]);
        EachLookedUp(ref fn, p1, o1 is not null, p2, o2 is not null, p3, o3 is not null, p4, o4 is not null, p5, o5 is not null, p6, o6 is not null, p7, o7 is not null, p8, o8 is not null);
    }

    private void EachLookedUp<TFn, T1, T2, T3, T4, T5, T6, T7, T8>(ref TFn fn, Pool<TEntity, T1> p1, bool a1, Pool<TEntity, T2> p2, bool a2, Pool<TEntity, T3> p3, bool a3, Pool<TEntity, T4> p4, bool a4, Pool<TEntity, T5> p5, bool a5, Pool<TEntity, T6> p6, bool a6, Pool<TEntity, T7> p7, bool a7, Pool<TEntity, T8> p8, bool a8)
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7, T8>
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
    {
        var ents = _pools[0].Entities;
        var s1 = p1.SparseSpan;
        var c1 = p1.Components;
        var s2 = p2.SparseSpan;
        var c2 = p2.Components;
        var s3 = p3.SparseSpan;
        var c3 = p3.Components;
        var s4 = p4.SparseSpan;
        var c4 = p4.Components;
        var s5 = p5.SparseSpan;
        var c5 = p5.Components;
        var s6 = p6.SparseSpan;
        var c6 = p6.Components;
        var s7 = p7.SparseSpan;
        var c7 = p7.Components;
        var s8 = p8.SparseSpan;
        var c8 = p8.Components;
        for (var i = Count - 1; i >= 0; i--)
        {
            var e = ents[i];
            var j1 = a1 ? i : SparseSet<TEntity>.Slot(s1, e.Index);
            var j2 = a2 ? i : SparseSet<TEntity>.Slot(s2, e.Index);
            var j3 = a3 ? i : SparseSet<TEntity>.Slot(s3, e.Index);
            var j4 = a4 ? i : SparseSet<TEntity>.Slot(s4, e.Index);
            var j5 = a5 ? i : SparseSet<TEntity>.Slot(s5, e.Index);
            var j6 = a6 ? i : SparseSet<TEntity>.Slot(s6, e.Index);
            var j7 = a7 ? i : SparseSet<TEntity>.Slot(s7, e.Index);
            var j8 = a8 ? i : SparseSet<TEntity>.Slot(s8, e.Index);
            fn.Invoke(e, ref c1[j1], ref c2[j2], ref c3[j3], ref c4[j4], ref c5[j5], ref c6[j6], ref c7[j7], ref c8[j8]);
        }
    }

    internal void Each<T1, T2, T3, T4, T5, T6, T7, T8>(RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7, T8> action)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
    {
        var adapter = new RefActionAdapter<TEntity, T1, T2, T3, T4, T5, T6, T7, T8>(action);
        Each<RefActionAdapter<TEntity, T1, T2, T3, T4, T5, T6, T7, T8>, T1, T2, T3, T4, T5, T6, T7, T8>(ref adapter);
    }

    public GroupRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9> Rows<T1, T2, T3, T4, T5, T6, T7, T8, T9>()
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        => new(Count, OwnedPool<T1>(), OwnedPool<T2>(), OwnedPool<T3>(), OwnedPool<T4>(), OwnedPool<T5>(), OwnedPool<T6>(), OwnedPool<T7>(), OwnedPool<T8>(), OwnedPool<T9>());

    // Owned types read aligned slots; With types are looked up.
    internal void Each<TFn, T1, T2, T3, T4, T5, T6, T7, T8, T9>(ref TFn fn)
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9>
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
    {
        var o1 = FindOwned<T1>();
        var o2 = FindOwned<T2>();
        var o3 = FindOwned<T3>();
        var o4 = FindOwned<T4>();
        var o5 = FindOwned<T5>();
        var o6 = FindOwned<T6>();
        var o7 = FindOwned<T7>();
        var o8 = FindOwned<T8>();
        var o9 = FindOwned<T9>();
        if (o1 is not null && o2 is not null && o3 is not null && o4 is not null && o5 is not null && o6 is not null && o7 is not null && o8 is not null && o9 is not null)
        {
            new GroupRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9>(Count, o1, o2, o3, o4, o5, o6, o7, o8, o9).Each(ref fn);
            return;
        }

        var p1 = o1 ?? Storage<T1>();
        var p2 = o2 ?? Storage<T2>();
        var p3 = o3 ?? Storage<T3>();
        var p4 = o4 ?? Storage<T4>();
        var p5 = o5 ?? Storage<T5>();
        var p6 = o6 ?? Storage<T6>();
        var p7 = o7 ?? Storage<T7>();
        var p8 = o8 ?? Storage<T8>();
        var p9 = o9 ?? Storage<T9>();
        var registry = Registry;
        using var scope = WalkScope<TEntity>.Enter(registry, [p1, p2, p3, p4, p5, p6, p7, p8, p9]);
        EachLookedUp(ref fn, p1, o1 is not null, p2, o2 is not null, p3, o3 is not null, p4, o4 is not null, p5, o5 is not null, p6, o6 is not null, p7, o7 is not null, p8, o8 is not null, p9, o9 is not null);
    }

    private void EachLookedUp<TFn, T1, T2, T3, T4, T5, T6, T7, T8, T9>(ref TFn fn, Pool<TEntity, T1> p1, bool a1, Pool<TEntity, T2> p2, bool a2, Pool<TEntity, T3> p3, bool a3, Pool<TEntity, T4> p4, bool a4, Pool<TEntity, T5> p5, bool a5, Pool<TEntity, T6> p6, bool a6, Pool<TEntity, T7> p7, bool a7, Pool<TEntity, T8> p8, bool a8, Pool<TEntity, T9> p9, bool a9)
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9>
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
    {
        var ents = _pools[0].Entities;
        var s1 = p1.SparseSpan;
        var c1 = p1.Components;
        var s2 = p2.SparseSpan;
        var c2 = p2.Components;
        var s3 = p3.SparseSpan;
        var c3 = p3.Components;
        var s4 = p4.SparseSpan;
        var c4 = p4.Components;
        var s5 = p5.SparseSpan;
        var c5 = p5.Components;
        var s6 = p6.SparseSpan;
        var c6 = p6.Components;
        var s7 = p7.SparseSpan;
        var c7 = p7.Components;
        var s8 = p8.SparseSpan;
        var c8 = p8.Components;
        var s9 = p9.SparseSpan;
        var c9 = p9.Components;
        for (var i = Count - 1; i >= 0; i--)
        {
            var e = ents[i];
            var j1 = a1 ? i : SparseSet<TEntity>.Slot(s1, e.Index);
            var j2 = a2 ? i : SparseSet<TEntity>.Slot(s2, e.Index);
            var j3 = a3 ? i : SparseSet<TEntity>.Slot(s3, e.Index);
            var j4 = a4 ? i : SparseSet<TEntity>.Slot(s4, e.Index);
            var j5 = a5 ? i : SparseSet<TEntity>.Slot(s5, e.Index);
            var j6 = a6 ? i : SparseSet<TEntity>.Slot(s6, e.Index);
            var j7 = a7 ? i : SparseSet<TEntity>.Slot(s7, e.Index);
            var j8 = a8 ? i : SparseSet<TEntity>.Slot(s8, e.Index);
            var j9 = a9 ? i : SparseSet<TEntity>.Slot(s9, e.Index);
            fn.Invoke(e, ref c1[j1], ref c2[j2], ref c3[j3], ref c4[j4], ref c5[j5], ref c6[j6], ref c7[j7], ref c8[j8], ref c9[j9]);
        }
    }

    internal void Each<T1, T2, T3, T4, T5, T6, T7, T8, T9>(RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9> action)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
    {
        var adapter = new RefActionAdapter<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9>(action);
        Each<RefActionAdapter<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9>, T1, T2, T3, T4, T5, T6, T7, T8, T9>(ref adapter);
    }

    public GroupRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Rows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>()
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        => new(Count, OwnedPool<T1>(), OwnedPool<T2>(), OwnedPool<T3>(), OwnedPool<T4>(), OwnedPool<T5>(), OwnedPool<T6>(), OwnedPool<T7>(), OwnedPool<T8>(), OwnedPool<T9>(), OwnedPool<T10>());

    // Owned types read aligned slots; With types are looked up.
    internal void Each<TFn, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(ref TFn fn)
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
    {
        var o1 = FindOwned<T1>();
        var o2 = FindOwned<T2>();
        var o3 = FindOwned<T3>();
        var o4 = FindOwned<T4>();
        var o5 = FindOwned<T5>();
        var o6 = FindOwned<T6>();
        var o7 = FindOwned<T7>();
        var o8 = FindOwned<T8>();
        var o9 = FindOwned<T9>();
        var o10 = FindOwned<T10>();
        if (o1 is not null && o2 is not null && o3 is not null && o4 is not null && o5 is not null && o6 is not null && o7 is not null && o8 is not null && o9 is not null && o10 is not null)
        {
            new GroupRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Count, o1, o2, o3, o4, o5, o6, o7, o8, o9, o10).Each(ref fn);
            return;
        }

        var p1 = o1 ?? Storage<T1>();
        var p2 = o2 ?? Storage<T2>();
        var p3 = o3 ?? Storage<T3>();
        var p4 = o4 ?? Storage<T4>();
        var p5 = o5 ?? Storage<T5>();
        var p6 = o6 ?? Storage<T6>();
        var p7 = o7 ?? Storage<T7>();
        var p8 = o8 ?? Storage<T8>();
        var p9 = o9 ?? Storage<T9>();
        var p10 = o10 ?? Storage<T10>();
        var registry = Registry;
        using var scope = WalkScope<TEntity>.Enter(registry, [p1, p2, p3, p4, p5, p6, p7, p8, p9, p10]);
        EachLookedUp(ref fn, p1, o1 is not null, p2, o2 is not null, p3, o3 is not null, p4, o4 is not null, p5, o5 is not null, p6, o6 is not null, p7, o7 is not null, p8, o8 is not null, p9, o9 is not null, p10, o10 is not null);
    }

    private void EachLookedUp<TFn, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(ref TFn fn, Pool<TEntity, T1> p1, bool a1, Pool<TEntity, T2> p2, bool a2, Pool<TEntity, T3> p3, bool a3, Pool<TEntity, T4> p4, bool a4, Pool<TEntity, T5> p5, bool a5, Pool<TEntity, T6> p6, bool a6, Pool<TEntity, T7> p7, bool a7, Pool<TEntity, T8> p8, bool a8, Pool<TEntity, T9> p9, bool a9, Pool<TEntity, T10> p10, bool a10)
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
    {
        var ents = _pools[0].Entities;
        var s1 = p1.SparseSpan;
        var c1 = p1.Components;
        var s2 = p2.SparseSpan;
        var c2 = p2.Components;
        var s3 = p3.SparseSpan;
        var c3 = p3.Components;
        var s4 = p4.SparseSpan;
        var c4 = p4.Components;
        var s5 = p5.SparseSpan;
        var c5 = p5.Components;
        var s6 = p6.SparseSpan;
        var c6 = p6.Components;
        var s7 = p7.SparseSpan;
        var c7 = p7.Components;
        var s8 = p8.SparseSpan;
        var c8 = p8.Components;
        var s9 = p9.SparseSpan;
        var c9 = p9.Components;
        var s10 = p10.SparseSpan;
        var c10 = p10.Components;
        for (var i = Count - 1; i >= 0; i--)
        {
            var e = ents[i];
            var j1 = a1 ? i : SparseSet<TEntity>.Slot(s1, e.Index);
            var j2 = a2 ? i : SparseSet<TEntity>.Slot(s2, e.Index);
            var j3 = a3 ? i : SparseSet<TEntity>.Slot(s3, e.Index);
            var j4 = a4 ? i : SparseSet<TEntity>.Slot(s4, e.Index);
            var j5 = a5 ? i : SparseSet<TEntity>.Slot(s5, e.Index);
            var j6 = a6 ? i : SparseSet<TEntity>.Slot(s6, e.Index);
            var j7 = a7 ? i : SparseSet<TEntity>.Slot(s7, e.Index);
            var j8 = a8 ? i : SparseSet<TEntity>.Slot(s8, e.Index);
            var j9 = a9 ? i : SparseSet<TEntity>.Slot(s9, e.Index);
            var j10 = a10 ? i : SparseSet<TEntity>.Slot(s10, e.Index);
            fn.Invoke(e, ref c1[j1], ref c2[j2], ref c3[j3], ref c4[j4], ref c5[j5], ref c6[j6], ref c7[j7], ref c8[j8], ref c9[j9], ref c10[j10]);
        }
    }

    internal void Each<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> action)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
    {
        var adapter = new RefActionAdapter<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(action);
        Each<RefActionAdapter<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(ref adapter);
    }

    public GroupRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> Rows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>()
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        => new(Count, OwnedPool<T1>(), OwnedPool<T2>(), OwnedPool<T3>(), OwnedPool<T4>(), OwnedPool<T5>(), OwnedPool<T6>(), OwnedPool<T7>(), OwnedPool<T8>(), OwnedPool<T9>(), OwnedPool<T10>(), OwnedPool<T11>());

    // Owned types read aligned slots; With types are looked up.
    internal void Each<TFn, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(ref TFn fn)
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
    {
        var o1 = FindOwned<T1>();
        var o2 = FindOwned<T2>();
        var o3 = FindOwned<T3>();
        var o4 = FindOwned<T4>();
        var o5 = FindOwned<T5>();
        var o6 = FindOwned<T6>();
        var o7 = FindOwned<T7>();
        var o8 = FindOwned<T8>();
        var o9 = FindOwned<T9>();
        var o10 = FindOwned<T10>();
        var o11 = FindOwned<T11>();
        if (o1 is not null && o2 is not null && o3 is not null && o4 is not null && o5 is not null && o6 is not null && o7 is not null && o8 is not null && o9 is not null && o10 is not null && o11 is not null)
        {
            new GroupRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(Count, o1, o2, o3, o4, o5, o6, o7, o8, o9, o10, o11).Each(ref fn);
            return;
        }

        var p1 = o1 ?? Storage<T1>();
        var p2 = o2 ?? Storage<T2>();
        var p3 = o3 ?? Storage<T3>();
        var p4 = o4 ?? Storage<T4>();
        var p5 = o5 ?? Storage<T5>();
        var p6 = o6 ?? Storage<T6>();
        var p7 = o7 ?? Storage<T7>();
        var p8 = o8 ?? Storage<T8>();
        var p9 = o9 ?? Storage<T9>();
        var p10 = o10 ?? Storage<T10>();
        var p11 = o11 ?? Storage<T11>();
        var registry = Registry;
        using var scope = WalkScope<TEntity>.Enter(registry, [p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11]);
        EachLookedUp(ref fn, p1, o1 is not null, p2, o2 is not null, p3, o3 is not null, p4, o4 is not null, p5, o5 is not null, p6, o6 is not null, p7, o7 is not null, p8, o8 is not null, p9, o9 is not null, p10, o10 is not null, p11, o11 is not null);
    }

    private void EachLookedUp<TFn, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(ref TFn fn, Pool<TEntity, T1> p1, bool a1, Pool<TEntity, T2> p2, bool a2, Pool<TEntity, T3> p3, bool a3, Pool<TEntity, T4> p4, bool a4, Pool<TEntity, T5> p5, bool a5, Pool<TEntity, T6> p6, bool a6, Pool<TEntity, T7> p7, bool a7, Pool<TEntity, T8> p8, bool a8, Pool<TEntity, T9> p9, bool a9, Pool<TEntity, T10> p10, bool a10, Pool<TEntity, T11> p11, bool a11)
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
    {
        var ents = _pools[0].Entities;
        var s1 = p1.SparseSpan;
        var c1 = p1.Components;
        var s2 = p2.SparseSpan;
        var c2 = p2.Components;
        var s3 = p3.SparseSpan;
        var c3 = p3.Components;
        var s4 = p4.SparseSpan;
        var c4 = p4.Components;
        var s5 = p5.SparseSpan;
        var c5 = p5.Components;
        var s6 = p6.SparseSpan;
        var c6 = p6.Components;
        var s7 = p7.SparseSpan;
        var c7 = p7.Components;
        var s8 = p8.SparseSpan;
        var c8 = p8.Components;
        var s9 = p9.SparseSpan;
        var c9 = p9.Components;
        var s10 = p10.SparseSpan;
        var c10 = p10.Components;
        var s11 = p11.SparseSpan;
        var c11 = p11.Components;
        for (var i = Count - 1; i >= 0; i--)
        {
            var e = ents[i];
            var j1 = a1 ? i : SparseSet<TEntity>.Slot(s1, e.Index);
            var j2 = a2 ? i : SparseSet<TEntity>.Slot(s2, e.Index);
            var j3 = a3 ? i : SparseSet<TEntity>.Slot(s3, e.Index);
            var j4 = a4 ? i : SparseSet<TEntity>.Slot(s4, e.Index);
            var j5 = a5 ? i : SparseSet<TEntity>.Slot(s5, e.Index);
            var j6 = a6 ? i : SparseSet<TEntity>.Slot(s6, e.Index);
            var j7 = a7 ? i : SparseSet<TEntity>.Slot(s7, e.Index);
            var j8 = a8 ? i : SparseSet<TEntity>.Slot(s8, e.Index);
            var j9 = a9 ? i : SparseSet<TEntity>.Slot(s9, e.Index);
            var j10 = a10 ? i : SparseSet<TEntity>.Slot(s10, e.Index);
            var j11 = a11 ? i : SparseSet<TEntity>.Slot(s11, e.Index);
            fn.Invoke(e, ref c1[j1], ref c2[j2], ref c3[j3], ref c4[j4], ref c5[j5], ref c6[j6], ref c7[j7], ref c8[j8], ref c9[j9], ref c10[j10], ref c11[j11]);
        }
    }

    internal void Each<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> action)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
    {
        var adapter = new RefActionAdapter<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(action);
        Each<RefActionAdapter<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(ref adapter);
    }

    public GroupRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> Rows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>()
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent
        => new(Count, OwnedPool<T1>(), OwnedPool<T2>(), OwnedPool<T3>(), OwnedPool<T4>(), OwnedPool<T5>(), OwnedPool<T6>(), OwnedPool<T7>(), OwnedPool<T8>(), OwnedPool<T9>(), OwnedPool<T10>(), OwnedPool<T11>(), OwnedPool<T12>());

    // Owned types read aligned slots; With types are looked up.
    internal void Each<TFn, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(ref TFn fn)
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent
    {
        var o1 = FindOwned<T1>();
        var o2 = FindOwned<T2>();
        var o3 = FindOwned<T3>();
        var o4 = FindOwned<T4>();
        var o5 = FindOwned<T5>();
        var o6 = FindOwned<T6>();
        var o7 = FindOwned<T7>();
        var o8 = FindOwned<T8>();
        var o9 = FindOwned<T9>();
        var o10 = FindOwned<T10>();
        var o11 = FindOwned<T11>();
        var o12 = FindOwned<T12>();
        if (o1 is not null && o2 is not null && o3 is not null && o4 is not null && o5 is not null && o6 is not null && o7 is not null && o8 is not null && o9 is not null && o10 is not null && o11 is not null && o12 is not null)
        {
            new GroupRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(Count, o1, o2, o3, o4, o5, o6, o7, o8, o9, o10, o11, o12).Each(ref fn);
            return;
        }

        var p1 = o1 ?? Storage<T1>();
        var p2 = o2 ?? Storage<T2>();
        var p3 = o3 ?? Storage<T3>();
        var p4 = o4 ?? Storage<T4>();
        var p5 = o5 ?? Storage<T5>();
        var p6 = o6 ?? Storage<T6>();
        var p7 = o7 ?? Storage<T7>();
        var p8 = o8 ?? Storage<T8>();
        var p9 = o9 ?? Storage<T9>();
        var p10 = o10 ?? Storage<T10>();
        var p11 = o11 ?? Storage<T11>();
        var p12 = o12 ?? Storage<T12>();
        var registry = Registry;
        using var scope = WalkScope<TEntity>.Enter(registry, [p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12]);
        EachLookedUp(ref fn, p1, o1 is not null, p2, o2 is not null, p3, o3 is not null, p4, o4 is not null, p5, o5 is not null, p6, o6 is not null, p7, o7 is not null, p8, o8 is not null, p9, o9 is not null, p10, o10 is not null, p11, o11 is not null, p12, o12 is not null);
    }

    private void EachLookedUp<TFn, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(ref TFn fn, Pool<TEntity, T1> p1, bool a1, Pool<TEntity, T2> p2, bool a2, Pool<TEntity, T3> p3, bool a3, Pool<TEntity, T4> p4, bool a4, Pool<TEntity, T5> p5, bool a5, Pool<TEntity, T6> p6, bool a6, Pool<TEntity, T7> p7, bool a7, Pool<TEntity, T8> p8, bool a8, Pool<TEntity, T9> p9, bool a9, Pool<TEntity, T10> p10, bool a10, Pool<TEntity, T11> p11, bool a11, Pool<TEntity, T12> p12, bool a12)
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent
    {
        var ents = _pools[0].Entities;
        var s1 = p1.SparseSpan;
        var c1 = p1.Components;
        var s2 = p2.SparseSpan;
        var c2 = p2.Components;
        var s3 = p3.SparseSpan;
        var c3 = p3.Components;
        var s4 = p4.SparseSpan;
        var c4 = p4.Components;
        var s5 = p5.SparseSpan;
        var c5 = p5.Components;
        var s6 = p6.SparseSpan;
        var c6 = p6.Components;
        var s7 = p7.SparseSpan;
        var c7 = p7.Components;
        var s8 = p8.SparseSpan;
        var c8 = p8.Components;
        var s9 = p9.SparseSpan;
        var c9 = p9.Components;
        var s10 = p10.SparseSpan;
        var c10 = p10.Components;
        var s11 = p11.SparseSpan;
        var c11 = p11.Components;
        var s12 = p12.SparseSpan;
        var c12 = p12.Components;
        for (var i = Count - 1; i >= 0; i--)
        {
            var e = ents[i];
            var j1 = a1 ? i : SparseSet<TEntity>.Slot(s1, e.Index);
            var j2 = a2 ? i : SparseSet<TEntity>.Slot(s2, e.Index);
            var j3 = a3 ? i : SparseSet<TEntity>.Slot(s3, e.Index);
            var j4 = a4 ? i : SparseSet<TEntity>.Slot(s4, e.Index);
            var j5 = a5 ? i : SparseSet<TEntity>.Slot(s5, e.Index);
            var j6 = a6 ? i : SparseSet<TEntity>.Slot(s6, e.Index);
            var j7 = a7 ? i : SparseSet<TEntity>.Slot(s7, e.Index);
            var j8 = a8 ? i : SparseSet<TEntity>.Slot(s8, e.Index);
            var j9 = a9 ? i : SparseSet<TEntity>.Slot(s9, e.Index);
            var j10 = a10 ? i : SparseSet<TEntity>.Slot(s10, e.Index);
            var j11 = a11 ? i : SparseSet<TEntity>.Slot(s11, e.Index);
            var j12 = a12 ? i : SparseSet<TEntity>.Slot(s12, e.Index);
            fn.Invoke(e, ref c1[j1], ref c2[j2], ref c3[j3], ref c4[j4], ref c5[j5], ref c6[j6], ref c7[j7], ref c8[j8], ref c9[j9], ref c10[j10], ref c11[j11], ref c12[j12]);
        }
    }

    internal void Each<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> action)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent
    {
        var adapter = new RefActionAdapter<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(action);
        Each<RefActionAdapter<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(ref adapter);
    }

    public GroupRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> Rows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>()
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent
        where T13 : IComponent
        => new(Count, OwnedPool<T1>(), OwnedPool<T2>(), OwnedPool<T3>(), OwnedPool<T4>(), OwnedPool<T5>(), OwnedPool<T6>(), OwnedPool<T7>(), OwnedPool<T8>(), OwnedPool<T9>(), OwnedPool<T10>(), OwnedPool<T11>(), OwnedPool<T12>(), OwnedPool<T13>());

    // Owned types read aligned slots; With types are looked up.
    internal void Each<TFn, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(ref TFn fn)
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent
        where T13 : IComponent
    {
        var o1 = FindOwned<T1>();
        var o2 = FindOwned<T2>();
        var o3 = FindOwned<T3>();
        var o4 = FindOwned<T4>();
        var o5 = FindOwned<T5>();
        var o6 = FindOwned<T6>();
        var o7 = FindOwned<T7>();
        var o8 = FindOwned<T8>();
        var o9 = FindOwned<T9>();
        var o10 = FindOwned<T10>();
        var o11 = FindOwned<T11>();
        var o12 = FindOwned<T12>();
        var o13 = FindOwned<T13>();
        if (o1 is not null && o2 is not null && o3 is not null && o4 is not null && o5 is not null && o6 is not null && o7 is not null && o8 is not null && o9 is not null && o10 is not null && o11 is not null && o12 is not null && o13 is not null)
        {
            new GroupRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(Count, o1, o2, o3, o4, o5, o6, o7, o8, o9, o10, o11, o12, o13).Each(ref fn);
            return;
        }

        var p1 = o1 ?? Storage<T1>();
        var p2 = o2 ?? Storage<T2>();
        var p3 = o3 ?? Storage<T3>();
        var p4 = o4 ?? Storage<T4>();
        var p5 = o5 ?? Storage<T5>();
        var p6 = o6 ?? Storage<T6>();
        var p7 = o7 ?? Storage<T7>();
        var p8 = o8 ?? Storage<T8>();
        var p9 = o9 ?? Storage<T9>();
        var p10 = o10 ?? Storage<T10>();
        var p11 = o11 ?? Storage<T11>();
        var p12 = o12 ?? Storage<T12>();
        var p13 = o13 ?? Storage<T13>();
        var registry = Registry;
        using var scope = WalkScope<TEntity>.Enter(registry, [p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13]);
        EachLookedUp(ref fn, p1, o1 is not null, p2, o2 is not null, p3, o3 is not null, p4, o4 is not null, p5, o5 is not null, p6, o6 is not null, p7, o7 is not null, p8, o8 is not null, p9, o9 is not null, p10, o10 is not null, p11, o11 is not null, p12, o12 is not null, p13, o13 is not null);
    }

    private void EachLookedUp<TFn, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(ref TFn fn, Pool<TEntity, T1> p1, bool a1, Pool<TEntity, T2> p2, bool a2, Pool<TEntity, T3> p3, bool a3, Pool<TEntity, T4> p4, bool a4, Pool<TEntity, T5> p5, bool a5, Pool<TEntity, T6> p6, bool a6, Pool<TEntity, T7> p7, bool a7, Pool<TEntity, T8> p8, bool a8, Pool<TEntity, T9> p9, bool a9, Pool<TEntity, T10> p10, bool a10, Pool<TEntity, T11> p11, bool a11, Pool<TEntity, T12> p12, bool a12, Pool<TEntity, T13> p13, bool a13)
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent
        where T13 : IComponent
    {
        var ents = _pools[0].Entities;
        var s1 = p1.SparseSpan;
        var c1 = p1.Components;
        var s2 = p2.SparseSpan;
        var c2 = p2.Components;
        var s3 = p3.SparseSpan;
        var c3 = p3.Components;
        var s4 = p4.SparseSpan;
        var c4 = p4.Components;
        var s5 = p5.SparseSpan;
        var c5 = p5.Components;
        var s6 = p6.SparseSpan;
        var c6 = p6.Components;
        var s7 = p7.SparseSpan;
        var c7 = p7.Components;
        var s8 = p8.SparseSpan;
        var c8 = p8.Components;
        var s9 = p9.SparseSpan;
        var c9 = p9.Components;
        var s10 = p10.SparseSpan;
        var c10 = p10.Components;
        var s11 = p11.SparseSpan;
        var c11 = p11.Components;
        var s12 = p12.SparseSpan;
        var c12 = p12.Components;
        var s13 = p13.SparseSpan;
        var c13 = p13.Components;
        for (var i = Count - 1; i >= 0; i--)
        {
            var e = ents[i];
            var j1 = a1 ? i : SparseSet<TEntity>.Slot(s1, e.Index);
            var j2 = a2 ? i : SparseSet<TEntity>.Slot(s2, e.Index);
            var j3 = a3 ? i : SparseSet<TEntity>.Slot(s3, e.Index);
            var j4 = a4 ? i : SparseSet<TEntity>.Slot(s4, e.Index);
            var j5 = a5 ? i : SparseSet<TEntity>.Slot(s5, e.Index);
            var j6 = a6 ? i : SparseSet<TEntity>.Slot(s6, e.Index);
            var j7 = a7 ? i : SparseSet<TEntity>.Slot(s7, e.Index);
            var j8 = a8 ? i : SparseSet<TEntity>.Slot(s8, e.Index);
            var j9 = a9 ? i : SparseSet<TEntity>.Slot(s9, e.Index);
            var j10 = a10 ? i : SparseSet<TEntity>.Slot(s10, e.Index);
            var j11 = a11 ? i : SparseSet<TEntity>.Slot(s11, e.Index);
            var j12 = a12 ? i : SparseSet<TEntity>.Slot(s12, e.Index);
            var j13 = a13 ? i : SparseSet<TEntity>.Slot(s13, e.Index);
            fn.Invoke(e, ref c1[j1], ref c2[j2], ref c3[j3], ref c4[j4], ref c5[j5], ref c6[j6], ref c7[j7], ref c8[j8], ref c9[j9], ref c10[j10], ref c11[j11], ref c12[j12], ref c13[j13]);
        }
    }

    internal void Each<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> action)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent
        where T13 : IComponent
    {
        var adapter = new RefActionAdapter<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(action);
        Each<RefActionAdapter<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(ref adapter);
    }

    public GroupRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> Rows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>()
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent
        where T13 : IComponent
        where T14 : IComponent
        => new(Count, OwnedPool<T1>(), OwnedPool<T2>(), OwnedPool<T3>(), OwnedPool<T4>(), OwnedPool<T5>(), OwnedPool<T6>(), OwnedPool<T7>(), OwnedPool<T8>(), OwnedPool<T9>(), OwnedPool<T10>(), OwnedPool<T11>(), OwnedPool<T12>(), OwnedPool<T13>(), OwnedPool<T14>());

    // Owned types read aligned slots; With types are looked up.
    internal void Each<TFn, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(ref TFn fn)
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent
        where T13 : IComponent
        where T14 : IComponent
    {
        var o1 = FindOwned<T1>();
        var o2 = FindOwned<T2>();
        var o3 = FindOwned<T3>();
        var o4 = FindOwned<T4>();
        var o5 = FindOwned<T5>();
        var o6 = FindOwned<T6>();
        var o7 = FindOwned<T7>();
        var o8 = FindOwned<T8>();
        var o9 = FindOwned<T9>();
        var o10 = FindOwned<T10>();
        var o11 = FindOwned<T11>();
        var o12 = FindOwned<T12>();
        var o13 = FindOwned<T13>();
        var o14 = FindOwned<T14>();
        if (o1 is not null && o2 is not null && o3 is not null && o4 is not null && o5 is not null && o6 is not null && o7 is not null && o8 is not null && o9 is not null && o10 is not null && o11 is not null && o12 is not null && o13 is not null && o14 is not null)
        {
            new GroupRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(Count, o1, o2, o3, o4, o5, o6, o7, o8, o9, o10, o11, o12, o13, o14).Each(ref fn);
            return;
        }

        var p1 = o1 ?? Storage<T1>();
        var p2 = o2 ?? Storage<T2>();
        var p3 = o3 ?? Storage<T3>();
        var p4 = o4 ?? Storage<T4>();
        var p5 = o5 ?? Storage<T5>();
        var p6 = o6 ?? Storage<T6>();
        var p7 = o7 ?? Storage<T7>();
        var p8 = o8 ?? Storage<T8>();
        var p9 = o9 ?? Storage<T9>();
        var p10 = o10 ?? Storage<T10>();
        var p11 = o11 ?? Storage<T11>();
        var p12 = o12 ?? Storage<T12>();
        var p13 = o13 ?? Storage<T13>();
        var p14 = o14 ?? Storage<T14>();
        var registry = Registry;
        using var scope = WalkScope<TEntity>.Enter(registry, [p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14]);
        EachLookedUp(ref fn, p1, o1 is not null, p2, o2 is not null, p3, o3 is not null, p4, o4 is not null, p5, o5 is not null, p6, o6 is not null, p7, o7 is not null, p8, o8 is not null, p9, o9 is not null, p10, o10 is not null, p11, o11 is not null, p12, o12 is not null, p13, o13 is not null, p14, o14 is not null);
    }

    private void EachLookedUp<TFn, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(ref TFn fn, Pool<TEntity, T1> p1, bool a1, Pool<TEntity, T2> p2, bool a2, Pool<TEntity, T3> p3, bool a3, Pool<TEntity, T4> p4, bool a4, Pool<TEntity, T5> p5, bool a5, Pool<TEntity, T6> p6, bool a6, Pool<TEntity, T7> p7, bool a7, Pool<TEntity, T8> p8, bool a8, Pool<TEntity, T9> p9, bool a9, Pool<TEntity, T10> p10, bool a10, Pool<TEntity, T11> p11, bool a11, Pool<TEntity, T12> p12, bool a12, Pool<TEntity, T13> p13, bool a13, Pool<TEntity, T14> p14, bool a14)
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent
        where T13 : IComponent
        where T14 : IComponent
    {
        var ents = _pools[0].Entities;
        var s1 = p1.SparseSpan;
        var c1 = p1.Components;
        var s2 = p2.SparseSpan;
        var c2 = p2.Components;
        var s3 = p3.SparseSpan;
        var c3 = p3.Components;
        var s4 = p4.SparseSpan;
        var c4 = p4.Components;
        var s5 = p5.SparseSpan;
        var c5 = p5.Components;
        var s6 = p6.SparseSpan;
        var c6 = p6.Components;
        var s7 = p7.SparseSpan;
        var c7 = p7.Components;
        var s8 = p8.SparseSpan;
        var c8 = p8.Components;
        var s9 = p9.SparseSpan;
        var c9 = p9.Components;
        var s10 = p10.SparseSpan;
        var c10 = p10.Components;
        var s11 = p11.SparseSpan;
        var c11 = p11.Components;
        var s12 = p12.SparseSpan;
        var c12 = p12.Components;
        var s13 = p13.SparseSpan;
        var c13 = p13.Components;
        var s14 = p14.SparseSpan;
        var c14 = p14.Components;
        for (var i = Count - 1; i >= 0; i--)
        {
            var e = ents[i];
            var j1 = a1 ? i : SparseSet<TEntity>.Slot(s1, e.Index);
            var j2 = a2 ? i : SparseSet<TEntity>.Slot(s2, e.Index);
            var j3 = a3 ? i : SparseSet<TEntity>.Slot(s3, e.Index);
            var j4 = a4 ? i : SparseSet<TEntity>.Slot(s4, e.Index);
            var j5 = a5 ? i : SparseSet<TEntity>.Slot(s5, e.Index);
            var j6 = a6 ? i : SparseSet<TEntity>.Slot(s6, e.Index);
            var j7 = a7 ? i : SparseSet<TEntity>.Slot(s7, e.Index);
            var j8 = a8 ? i : SparseSet<TEntity>.Slot(s8, e.Index);
            var j9 = a9 ? i : SparseSet<TEntity>.Slot(s9, e.Index);
            var j10 = a10 ? i : SparseSet<TEntity>.Slot(s10, e.Index);
            var j11 = a11 ? i : SparseSet<TEntity>.Slot(s11, e.Index);
            var j12 = a12 ? i : SparseSet<TEntity>.Slot(s12, e.Index);
            var j13 = a13 ? i : SparseSet<TEntity>.Slot(s13, e.Index);
            var j14 = a14 ? i : SparseSet<TEntity>.Slot(s14, e.Index);
            fn.Invoke(e, ref c1[j1], ref c2[j2], ref c3[j3], ref c4[j4], ref c5[j5], ref c6[j6], ref c7[j7], ref c8[j8], ref c9[j9], ref c10[j10], ref c11[j11], ref c12[j12], ref c13[j13], ref c14[j14]);
        }
    }

    internal void Each<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> action)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent
        where T13 : IComponent
        where T14 : IComponent
    {
        var adapter = new RefActionAdapter<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(action);
        Each<RefActionAdapter<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(ref adapter);
    }

    public GroupRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> Rows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>()
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent
        where T13 : IComponent
        where T14 : IComponent
        where T15 : IComponent
        => new(Count, OwnedPool<T1>(), OwnedPool<T2>(), OwnedPool<T3>(), OwnedPool<T4>(), OwnedPool<T5>(), OwnedPool<T6>(), OwnedPool<T7>(), OwnedPool<T8>(), OwnedPool<T9>(), OwnedPool<T10>(), OwnedPool<T11>(), OwnedPool<T12>(), OwnedPool<T13>(), OwnedPool<T14>(), OwnedPool<T15>());

    // Owned types read aligned slots; With types are looked up.
    internal void Each<TFn, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(ref TFn fn)
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent
        where T13 : IComponent
        where T14 : IComponent
        where T15 : IComponent
    {
        var o1 = FindOwned<T1>();
        var o2 = FindOwned<T2>();
        var o3 = FindOwned<T3>();
        var o4 = FindOwned<T4>();
        var o5 = FindOwned<T5>();
        var o6 = FindOwned<T6>();
        var o7 = FindOwned<T7>();
        var o8 = FindOwned<T8>();
        var o9 = FindOwned<T9>();
        var o10 = FindOwned<T10>();
        var o11 = FindOwned<T11>();
        var o12 = FindOwned<T12>();
        var o13 = FindOwned<T13>();
        var o14 = FindOwned<T14>();
        var o15 = FindOwned<T15>();
        if (o1 is not null && o2 is not null && o3 is not null && o4 is not null && o5 is not null && o6 is not null && o7 is not null && o8 is not null && o9 is not null && o10 is not null && o11 is not null && o12 is not null && o13 is not null && o14 is not null && o15 is not null)
        {
            new GroupRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(Count, o1, o2, o3, o4, o5, o6, o7, o8, o9, o10, o11, o12, o13, o14, o15).Each(ref fn);
            return;
        }

        var p1 = o1 ?? Storage<T1>();
        var p2 = o2 ?? Storage<T2>();
        var p3 = o3 ?? Storage<T3>();
        var p4 = o4 ?? Storage<T4>();
        var p5 = o5 ?? Storage<T5>();
        var p6 = o6 ?? Storage<T6>();
        var p7 = o7 ?? Storage<T7>();
        var p8 = o8 ?? Storage<T8>();
        var p9 = o9 ?? Storage<T9>();
        var p10 = o10 ?? Storage<T10>();
        var p11 = o11 ?? Storage<T11>();
        var p12 = o12 ?? Storage<T12>();
        var p13 = o13 ?? Storage<T13>();
        var p14 = o14 ?? Storage<T14>();
        var p15 = o15 ?? Storage<T15>();
        var registry = Registry;
        using var scope = WalkScope<TEntity>.Enter(registry, [p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14, p15]);
        EachLookedUp(ref fn, p1, o1 is not null, p2, o2 is not null, p3, o3 is not null, p4, o4 is not null, p5, o5 is not null, p6, o6 is not null, p7, o7 is not null, p8, o8 is not null, p9, o9 is not null, p10, o10 is not null, p11, o11 is not null, p12, o12 is not null, p13, o13 is not null, p14, o14 is not null, p15, o15 is not null);
    }

    private void EachLookedUp<TFn, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(ref TFn fn, Pool<TEntity, T1> p1, bool a1, Pool<TEntity, T2> p2, bool a2, Pool<TEntity, T3> p3, bool a3, Pool<TEntity, T4> p4, bool a4, Pool<TEntity, T5> p5, bool a5, Pool<TEntity, T6> p6, bool a6, Pool<TEntity, T7> p7, bool a7, Pool<TEntity, T8> p8, bool a8, Pool<TEntity, T9> p9, bool a9, Pool<TEntity, T10> p10, bool a10, Pool<TEntity, T11> p11, bool a11, Pool<TEntity, T12> p12, bool a12, Pool<TEntity, T13> p13, bool a13, Pool<TEntity, T14> p14, bool a14, Pool<TEntity, T15> p15, bool a15)
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent
        where T13 : IComponent
        where T14 : IComponent
        where T15 : IComponent
    {
        var ents = _pools[0].Entities;
        var s1 = p1.SparseSpan;
        var c1 = p1.Components;
        var s2 = p2.SparseSpan;
        var c2 = p2.Components;
        var s3 = p3.SparseSpan;
        var c3 = p3.Components;
        var s4 = p4.SparseSpan;
        var c4 = p4.Components;
        var s5 = p5.SparseSpan;
        var c5 = p5.Components;
        var s6 = p6.SparseSpan;
        var c6 = p6.Components;
        var s7 = p7.SparseSpan;
        var c7 = p7.Components;
        var s8 = p8.SparseSpan;
        var c8 = p8.Components;
        var s9 = p9.SparseSpan;
        var c9 = p9.Components;
        var s10 = p10.SparseSpan;
        var c10 = p10.Components;
        var s11 = p11.SparseSpan;
        var c11 = p11.Components;
        var s12 = p12.SparseSpan;
        var c12 = p12.Components;
        var s13 = p13.SparseSpan;
        var c13 = p13.Components;
        var s14 = p14.SparseSpan;
        var c14 = p14.Components;
        var s15 = p15.SparseSpan;
        var c15 = p15.Components;
        for (var i = Count - 1; i >= 0; i--)
        {
            var e = ents[i];
            var j1 = a1 ? i : SparseSet<TEntity>.Slot(s1, e.Index);
            var j2 = a2 ? i : SparseSet<TEntity>.Slot(s2, e.Index);
            var j3 = a3 ? i : SparseSet<TEntity>.Slot(s3, e.Index);
            var j4 = a4 ? i : SparseSet<TEntity>.Slot(s4, e.Index);
            var j5 = a5 ? i : SparseSet<TEntity>.Slot(s5, e.Index);
            var j6 = a6 ? i : SparseSet<TEntity>.Slot(s6, e.Index);
            var j7 = a7 ? i : SparseSet<TEntity>.Slot(s7, e.Index);
            var j8 = a8 ? i : SparseSet<TEntity>.Slot(s8, e.Index);
            var j9 = a9 ? i : SparseSet<TEntity>.Slot(s9, e.Index);
            var j10 = a10 ? i : SparseSet<TEntity>.Slot(s10, e.Index);
            var j11 = a11 ? i : SparseSet<TEntity>.Slot(s11, e.Index);
            var j12 = a12 ? i : SparseSet<TEntity>.Slot(s12, e.Index);
            var j13 = a13 ? i : SparseSet<TEntity>.Slot(s13, e.Index);
            var j14 = a14 ? i : SparseSet<TEntity>.Slot(s14, e.Index);
            var j15 = a15 ? i : SparseSet<TEntity>.Slot(s15, e.Index);
            fn.Invoke(e, ref c1[j1], ref c2[j2], ref c3[j3], ref c4[j4], ref c5[j5], ref c6[j6], ref c7[j7], ref c8[j8], ref c9[j9], ref c10[j10], ref c11[j11], ref c12[j12], ref c13[j13], ref c14[j14], ref c15[j15]);
        }
    }

    internal void Each<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> action)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent
        where T13 : IComponent
        where T14 : IComponent
        where T15 : IComponent
    {
        var adapter = new RefActionAdapter<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(action);
        Each<RefActionAdapter<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(ref adapter);
    }

    public GroupRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> Rows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>()
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent
        where T13 : IComponent
        where T14 : IComponent
        where T15 : IComponent
        where T16 : IComponent
        => new(Count, OwnedPool<T1>(), OwnedPool<T2>(), OwnedPool<T3>(), OwnedPool<T4>(), OwnedPool<T5>(), OwnedPool<T6>(), OwnedPool<T7>(), OwnedPool<T8>(), OwnedPool<T9>(), OwnedPool<T10>(), OwnedPool<T11>(), OwnedPool<T12>(), OwnedPool<T13>(), OwnedPool<T14>(), OwnedPool<T15>(), OwnedPool<T16>());

    // Owned types read aligned slots; With types are looked up.
    internal void Each<TFn, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(ref TFn fn)
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent
        where T13 : IComponent
        where T14 : IComponent
        where T15 : IComponent
        where T16 : IComponent
    {
        var o1 = FindOwned<T1>();
        var o2 = FindOwned<T2>();
        var o3 = FindOwned<T3>();
        var o4 = FindOwned<T4>();
        var o5 = FindOwned<T5>();
        var o6 = FindOwned<T6>();
        var o7 = FindOwned<T7>();
        var o8 = FindOwned<T8>();
        var o9 = FindOwned<T9>();
        var o10 = FindOwned<T10>();
        var o11 = FindOwned<T11>();
        var o12 = FindOwned<T12>();
        var o13 = FindOwned<T13>();
        var o14 = FindOwned<T14>();
        var o15 = FindOwned<T15>();
        var o16 = FindOwned<T16>();
        if (o1 is not null && o2 is not null && o3 is not null && o4 is not null && o5 is not null && o6 is not null && o7 is not null && o8 is not null && o9 is not null && o10 is not null && o11 is not null && o12 is not null && o13 is not null && o14 is not null && o15 is not null && o16 is not null)
        {
            new GroupRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(Count, o1, o2, o3, o4, o5, o6, o7, o8, o9, o10, o11, o12, o13, o14, o15, o16).Each(ref fn);
            return;
        }

        var p1 = o1 ?? Storage<T1>();
        var p2 = o2 ?? Storage<T2>();
        var p3 = o3 ?? Storage<T3>();
        var p4 = o4 ?? Storage<T4>();
        var p5 = o5 ?? Storage<T5>();
        var p6 = o6 ?? Storage<T6>();
        var p7 = o7 ?? Storage<T7>();
        var p8 = o8 ?? Storage<T8>();
        var p9 = o9 ?? Storage<T9>();
        var p10 = o10 ?? Storage<T10>();
        var p11 = o11 ?? Storage<T11>();
        var p12 = o12 ?? Storage<T12>();
        var p13 = o13 ?? Storage<T13>();
        var p14 = o14 ?? Storage<T14>();
        var p15 = o15 ?? Storage<T15>();
        var p16 = o16 ?? Storage<T16>();
        var registry = Registry;
        using var scope = WalkScope<TEntity>.Enter(registry, [p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14, p15, p16]);
        EachLookedUp(ref fn, p1, o1 is not null, p2, o2 is not null, p3, o3 is not null, p4, o4 is not null, p5, o5 is not null, p6, o6 is not null, p7, o7 is not null, p8, o8 is not null, p9, o9 is not null, p10, o10 is not null, p11, o11 is not null, p12, o12 is not null, p13, o13 is not null, p14, o14 is not null, p15, o15 is not null, p16, o16 is not null);
    }

    private void EachLookedUp<TFn, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(ref TFn fn, Pool<TEntity, T1> p1, bool a1, Pool<TEntity, T2> p2, bool a2, Pool<TEntity, T3> p3, bool a3, Pool<TEntity, T4> p4, bool a4, Pool<TEntity, T5> p5, bool a5, Pool<TEntity, T6> p6, bool a6, Pool<TEntity, T7> p7, bool a7, Pool<TEntity, T8> p8, bool a8, Pool<TEntity, T9> p9, bool a9, Pool<TEntity, T10> p10, bool a10, Pool<TEntity, T11> p11, bool a11, Pool<TEntity, T12> p12, bool a12, Pool<TEntity, T13> p13, bool a13, Pool<TEntity, T14> p14, bool a14, Pool<TEntity, T15> p15, bool a15, Pool<TEntity, T16> p16, bool a16)
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent
        where T13 : IComponent
        where T14 : IComponent
        where T15 : IComponent
        where T16 : IComponent
    {
        var ents = _pools[0].Entities;
        var s1 = p1.SparseSpan;
        var c1 = p1.Components;
        var s2 = p2.SparseSpan;
        var c2 = p2.Components;
        var s3 = p3.SparseSpan;
        var c3 = p3.Components;
        var s4 = p4.SparseSpan;
        var c4 = p4.Components;
        var s5 = p5.SparseSpan;
        var c5 = p5.Components;
        var s6 = p6.SparseSpan;
        var c6 = p6.Components;
        var s7 = p7.SparseSpan;
        var c7 = p7.Components;
        var s8 = p8.SparseSpan;
        var c8 = p8.Components;
        var s9 = p9.SparseSpan;
        var c9 = p9.Components;
        var s10 = p10.SparseSpan;
        var c10 = p10.Components;
        var s11 = p11.SparseSpan;
        var c11 = p11.Components;
        var s12 = p12.SparseSpan;
        var c12 = p12.Components;
        var s13 = p13.SparseSpan;
        var c13 = p13.Components;
        var s14 = p14.SparseSpan;
        var c14 = p14.Components;
        var s15 = p15.SparseSpan;
        var c15 = p15.Components;
        var s16 = p16.SparseSpan;
        var c16 = p16.Components;
        for (var i = Count - 1; i >= 0; i--)
        {
            var e = ents[i];
            var j1 = a1 ? i : SparseSet<TEntity>.Slot(s1, e.Index);
            var j2 = a2 ? i : SparseSet<TEntity>.Slot(s2, e.Index);
            var j3 = a3 ? i : SparseSet<TEntity>.Slot(s3, e.Index);
            var j4 = a4 ? i : SparseSet<TEntity>.Slot(s4, e.Index);
            var j5 = a5 ? i : SparseSet<TEntity>.Slot(s5, e.Index);
            var j6 = a6 ? i : SparseSet<TEntity>.Slot(s6, e.Index);
            var j7 = a7 ? i : SparseSet<TEntity>.Slot(s7, e.Index);
            var j8 = a8 ? i : SparseSet<TEntity>.Slot(s8, e.Index);
            var j9 = a9 ? i : SparseSet<TEntity>.Slot(s9, e.Index);
            var j10 = a10 ? i : SparseSet<TEntity>.Slot(s10, e.Index);
            var j11 = a11 ? i : SparseSet<TEntity>.Slot(s11, e.Index);
            var j12 = a12 ? i : SparseSet<TEntity>.Slot(s12, e.Index);
            var j13 = a13 ? i : SparseSet<TEntity>.Slot(s13, e.Index);
            var j14 = a14 ? i : SparseSet<TEntity>.Slot(s14, e.Index);
            var j15 = a15 ? i : SparseSet<TEntity>.Slot(s15, e.Index);
            var j16 = a16 ? i : SparseSet<TEntity>.Slot(s16, e.Index);
            fn.Invoke(e, ref c1[j1], ref c2[j2], ref c3[j3], ref c4[j4], ref c5[j5], ref c6[j6], ref c7[j7], ref c8[j8], ref c9[j9], ref c10[j10], ref c11[j11], ref c12[j12], ref c13[j13], ref c14[j14], ref c15[j15], ref c16[j16]);
        }
    }

    internal void Each<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> action)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent
        where T13 : IComponent
        where T14 : IComponent
        where T15 : IComponent
        where T16 : IComponent
    {
        var adapter = new RefActionAdapter<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(action);
        Each<RefActionAdapter<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(ref adapter);
    }
}
