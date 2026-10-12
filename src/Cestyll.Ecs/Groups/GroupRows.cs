using Cestyll.Ecs.Storage;
using Cestyll.Ecs.Views;

namespace Cestyll.Ecs.Groups;

// Members and owned spans as of the Rows call. Each walks last first, like a view.

public readonly struct GroupRows<TEntity, T1>
    where TEntity : unmanaged, IEntity<TEntity>
    where T1 : IComponent
{
    private readonly int _count;
    private readonly Pool<TEntity, T1> _p1;

    internal GroupRows(int count, Pool<TEntity, T1> p1)
    {
        _count = count;
        _p1 = p1;
    }

    public int Count => _count;
    public ReadOnlySpan<TEntity> Entities => _p1.Entities[.._count];
    public Span<T1> C1 => _p1.Components[.._count];

    internal Pool<TEntity, T1> Pool1 => _p1;

    public void Each<TFn>(ref TFn fn) where TFn : struct, IEach<TFn, TEntity, T1>
    {
        var registry = _p1.OwnerRegistry!;
#if !DEBUG
        // Nothing to close afterwards: no scope, and Release counts no pools.
        if (!registry.AutoDefer)
        {
            Walk(ref fn);
            return;
        }
#endif

        using var scope = WalkScope<TEntity>.Enter(registry, [_p1]);
        Walk(ref fn);
    }

    public void Each(RefAction<TEntity, T1> action)
    {
        var adapter = new RefActionAdapter<TEntity, T1>(action);
        Each(ref adapter);
    }

    private void Walk<TFn>(ref TFn fn) where TFn : struct, IEach<TFn, TEntity, T1>
    {
        var ents = _p1.Entities;
        var c1 = _p1.Components;
        for (var i = _count - 1; i >= 0; i--)
        {
            fn.Invoke(ents[i], ref c1[i]);
        }
    }
}

public readonly struct GroupRows<TEntity, T1, T2>
    where TEntity : unmanaged, IEntity<TEntity>
    where T1 : IComponent
    where T2 : IComponent
{
    private readonly int _count;
    private readonly Pool<TEntity, T1> _p1;
    private readonly Pool<TEntity, T2> _p2;

    internal GroupRows(int count, Pool<TEntity, T1> p1, Pool<TEntity, T2> p2)
    {
        _count = count;
        _p1 = p1;
        _p2 = p2;
    }

    public int Count => _count;
    public ReadOnlySpan<TEntity> Entities => _p1.Entities[.._count];
    public Span<T1> C1 => _p1.Components[.._count];
    public Span<T2> C2 => _p2.Components[.._count];

    internal Pool<TEntity, T1> Pool1 => _p1;
    internal Pool<TEntity, T2> Pool2 => _p2;

    public void Each<TFn>(ref TFn fn) where TFn : struct, IEach<TFn, TEntity, T1, T2>
    {
        var registry = _p1.OwnerRegistry!;
#if !DEBUG
        // Nothing to close afterwards: no scope, and Release counts no pools.
        if (!registry.AutoDefer)
        {
            Walk(ref fn);
            return;
        }
#endif

        using var scope = WalkScope<TEntity>.Enter(registry, [_p1, _p2]);
        Walk(ref fn);
    }

    public void Each(RefAction<TEntity, T1, T2> action)
    {
        var adapter = new RefActionAdapter<TEntity, T1, T2>(action);
        Each(ref adapter);
    }

    private void Walk<TFn>(ref TFn fn) where TFn : struct, IEach<TFn, TEntity, T1, T2>
    {
        var ents = _p1.Entities;
        var c1 = _p1.Components;
        var c2 = _p2.Components;
        for (var i = _count - 1; i >= 0; i--)
        {
            fn.Invoke(ents[i], ref c1[i], ref c2[i]);
        }
    }
}

public readonly struct GroupRows<TEntity, T1, T2, T3>
    where TEntity : unmanaged, IEntity<TEntity>
    where T1 : IComponent
    where T2 : IComponent
    where T3 : IComponent
{
    private readonly int _count;
    private readonly Pool<TEntity, T1> _p1;
    private readonly Pool<TEntity, T2> _p2;
    private readonly Pool<TEntity, T3> _p3;

    internal GroupRows(int count, Pool<TEntity, T1> p1, Pool<TEntity, T2> p2, Pool<TEntity, T3> p3)
    {
        _count = count;
        _p1 = p1;
        _p2 = p2;
        _p3 = p3;
    }

    public int Count => _count;
    public ReadOnlySpan<TEntity> Entities => _p1.Entities[.._count];
    public Span<T1> C1 => _p1.Components[.._count];
    public Span<T2> C2 => _p2.Components[.._count];
    public Span<T3> C3 => _p3.Components[.._count];

    internal Pool<TEntity, T1> Pool1 => _p1;
    internal Pool<TEntity, T2> Pool2 => _p2;
    internal Pool<TEntity, T3> Pool3 => _p3;

    public void Each<TFn>(ref TFn fn) where TFn : struct, IEach<TFn, TEntity, T1, T2, T3>
    {
        var registry = _p1.OwnerRegistry!;
#if !DEBUG
        // Nothing to close afterwards: no scope, and Release counts no pools.
        if (!registry.AutoDefer)
        {
            Walk(ref fn);
            return;
        }
#endif

        using var scope = WalkScope<TEntity>.Enter(registry, [_p1, _p2, _p3]);
        Walk(ref fn);
    }

    public void Each(RefAction<TEntity, T1, T2, T3> action)
    {
        var adapter = new RefActionAdapter<TEntity, T1, T2, T3>(action);
        Each(ref adapter);
    }

    private void Walk<TFn>(ref TFn fn) where TFn : struct, IEach<TFn, TEntity, T1, T2, T3>
    {
        var ents = _p1.Entities;
        var c1 = _p1.Components;
        var c2 = _p2.Components;
        var c3 = _p3.Components;
        for (var i = _count - 1; i >= 0; i--)
        {
            fn.Invoke(ents[i], ref c1[i], ref c2[i], ref c3[i]);
        }
    }
}

public readonly struct GroupRows<TEntity, T1, T2, T3, T4>
    where TEntity : unmanaged, IEntity<TEntity>
    where T1 : IComponent
    where T2 : IComponent
    where T3 : IComponent
    where T4 : IComponent
{
    private readonly int _count;
    private readonly Pool<TEntity, T1> _p1;
    private readonly Pool<TEntity, T2> _p2;
    private readonly Pool<TEntity, T3> _p3;
    private readonly Pool<TEntity, T4> _p4;

    internal GroupRows(int count, Pool<TEntity, T1> p1, Pool<TEntity, T2> p2, Pool<TEntity, T3> p3, Pool<TEntity, T4> p4)
    {
        _count = count;
        _p1 = p1;
        _p2 = p2;
        _p3 = p3;
        _p4 = p4;
    }

    public int Count => _count;
    public ReadOnlySpan<TEntity> Entities => _p1.Entities[.._count];
    public Span<T1> C1 => _p1.Components[.._count];
    public Span<T2> C2 => _p2.Components[.._count];
    public Span<T3> C3 => _p3.Components[.._count];
    public Span<T4> C4 => _p4.Components[.._count];

    internal Pool<TEntity, T1> Pool1 => _p1;
    internal Pool<TEntity, T2> Pool2 => _p2;
    internal Pool<TEntity, T3> Pool3 => _p3;
    internal Pool<TEntity, T4> Pool4 => _p4;

    public void Each<TFn>(ref TFn fn) where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4>
    {
        var registry = _p1.OwnerRegistry!;
#if !DEBUG
        // Nothing to close afterwards: no scope, and Release counts no pools.
        if (!registry.AutoDefer)
        {
            Walk(ref fn);
            return;
        }
#endif

        using var scope = WalkScope<TEntity>.Enter(registry, [_p1, _p2, _p3, _p4]);
        Walk(ref fn);
    }

    public void Each(RefAction<TEntity, T1, T2, T3, T4> action)
    {
        var adapter = new RefActionAdapter<TEntity, T1, T2, T3, T4>(action);
        Each(ref adapter);
    }

    private void Walk<TFn>(ref TFn fn) where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4>
    {
        var ents = _p1.Entities;
        var c1 = _p1.Components;
        var c2 = _p2.Components;
        var c3 = _p3.Components;
        var c4 = _p4.Components;
        for (var i = _count - 1; i >= 0; i--)
        {
            fn.Invoke(ents[i], ref c1[i], ref c2[i], ref c3[i], ref c4[i]);
        }
    }
}

public readonly struct GroupRows<TEntity, T1, T2, T3, T4, T5>
    where TEntity : unmanaged, IEntity<TEntity>
    where T1 : IComponent
    where T2 : IComponent
    where T3 : IComponent
    where T4 : IComponent
    where T5 : IComponent
{
    private readonly int _count;
    private readonly Pool<TEntity, T1> _p1;
    private readonly Pool<TEntity, T2> _p2;
    private readonly Pool<TEntity, T3> _p3;
    private readonly Pool<TEntity, T4> _p4;
    private readonly Pool<TEntity, T5> _p5;

    internal GroupRows(int count, Pool<TEntity, T1> p1, Pool<TEntity, T2> p2, Pool<TEntity, T3> p3, Pool<TEntity, T4> p4, Pool<TEntity, T5> p5)
    {
        _count = count;
        _p1 = p1;
        _p2 = p2;
        _p3 = p3;
        _p4 = p4;
        _p5 = p5;
    }

    public int Count => _count;
    public ReadOnlySpan<TEntity> Entities => _p1.Entities[.._count];
    public Span<T1> C1 => _p1.Components[.._count];
    public Span<T2> C2 => _p2.Components[.._count];
    public Span<T3> C3 => _p3.Components[.._count];
    public Span<T4> C4 => _p4.Components[.._count];
    public Span<T5> C5 => _p5.Components[.._count];

    internal Pool<TEntity, T1> Pool1 => _p1;
    internal Pool<TEntity, T2> Pool2 => _p2;
    internal Pool<TEntity, T3> Pool3 => _p3;
    internal Pool<TEntity, T4> Pool4 => _p4;
    internal Pool<TEntity, T5> Pool5 => _p5;

    public void Each<TFn>(ref TFn fn) where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5>
    {
        var registry = _p1.OwnerRegistry!;
#if !DEBUG
        // Nothing to close afterwards: no scope, and Release counts no pools.
        if (!registry.AutoDefer)
        {
            Walk(ref fn);
            return;
        }
#endif

        using var scope = WalkScope<TEntity>.Enter(registry, [_p1, _p2, _p3, _p4, _p5]);
        Walk(ref fn);
    }

    public void Each(RefAction<TEntity, T1, T2, T3, T4, T5> action)
    {
        var adapter = new RefActionAdapter<TEntity, T1, T2, T3, T4, T5>(action);
        Each(ref adapter);
    }

    private void Walk<TFn>(ref TFn fn) where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5>
    {
        var ents = _p1.Entities;
        var c1 = _p1.Components;
        var c2 = _p2.Components;
        var c3 = _p3.Components;
        var c4 = _p4.Components;
        var c5 = _p5.Components;
        for (var i = _count - 1; i >= 0; i--)
        {
            fn.Invoke(ents[i], ref c1[i], ref c2[i], ref c3[i], ref c4[i], ref c5[i]);
        }
    }
}

public readonly struct GroupRows<TEntity, T1, T2, T3, T4, T5, T6>
    where TEntity : unmanaged, IEntity<TEntity>
    where T1 : IComponent
    where T2 : IComponent
    where T3 : IComponent
    where T4 : IComponent
    where T5 : IComponent
    where T6 : IComponent
{
    private readonly int _count;
    private readonly Pool<TEntity, T1> _p1;
    private readonly Pool<TEntity, T2> _p2;
    private readonly Pool<TEntity, T3> _p3;
    private readonly Pool<TEntity, T4> _p4;
    private readonly Pool<TEntity, T5> _p5;
    private readonly Pool<TEntity, T6> _p6;

    internal GroupRows(int count, Pool<TEntity, T1> p1, Pool<TEntity, T2> p2, Pool<TEntity, T3> p3, Pool<TEntity, T4> p4, Pool<TEntity, T5> p5, Pool<TEntity, T6> p6)
    {
        _count = count;
        _p1 = p1;
        _p2 = p2;
        _p3 = p3;
        _p4 = p4;
        _p5 = p5;
        _p6 = p6;
    }

    public int Count => _count;
    public ReadOnlySpan<TEntity> Entities => _p1.Entities[.._count];
    public Span<T1> C1 => _p1.Components[.._count];
    public Span<T2> C2 => _p2.Components[.._count];
    public Span<T3> C3 => _p3.Components[.._count];
    public Span<T4> C4 => _p4.Components[.._count];
    public Span<T5> C5 => _p5.Components[.._count];
    public Span<T6> C6 => _p6.Components[.._count];

    internal Pool<TEntity, T1> Pool1 => _p1;
    internal Pool<TEntity, T2> Pool2 => _p2;
    internal Pool<TEntity, T3> Pool3 => _p3;
    internal Pool<TEntity, T4> Pool4 => _p4;
    internal Pool<TEntity, T5> Pool5 => _p5;
    internal Pool<TEntity, T6> Pool6 => _p6;

    public void Each<TFn>(ref TFn fn) where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6>
    {
        var registry = _p1.OwnerRegistry!;
#if !DEBUG
        // Nothing to close afterwards: no scope, and Release counts no pools.
        if (!registry.AutoDefer)
        {
            Walk(ref fn);
            return;
        }
#endif

        using var scope = WalkScope<TEntity>.Enter(registry, [_p1, _p2, _p3, _p4, _p5, _p6]);
        Walk(ref fn);
    }

    public void Each(RefAction<TEntity, T1, T2, T3, T4, T5, T6> action)
    {
        var adapter = new RefActionAdapter<TEntity, T1, T2, T3, T4, T5, T6>(action);
        Each(ref adapter);
    }

    private void Walk<TFn>(ref TFn fn) where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6>
    {
        var ents = _p1.Entities;
        var c1 = _p1.Components;
        var c2 = _p2.Components;
        var c3 = _p3.Components;
        var c4 = _p4.Components;
        var c5 = _p5.Components;
        var c6 = _p6.Components;
        for (var i = _count - 1; i >= 0; i--)
        {
            fn.Invoke(ents[i], ref c1[i], ref c2[i], ref c3[i], ref c4[i], ref c5[i], ref c6[i]);
        }
    }
}

public readonly struct GroupRows<TEntity, T1, T2, T3, T4, T5, T6, T7>
    where TEntity : unmanaged, IEntity<TEntity>
    where T1 : IComponent
    where T2 : IComponent
    where T3 : IComponent
    where T4 : IComponent
    where T5 : IComponent
    where T6 : IComponent
    where T7 : IComponent
{
    private readonly int _count;
    private readonly Pool<TEntity, T1> _p1;
    private readonly Pool<TEntity, T2> _p2;
    private readonly Pool<TEntity, T3> _p3;
    private readonly Pool<TEntity, T4> _p4;
    private readonly Pool<TEntity, T5> _p5;
    private readonly Pool<TEntity, T6> _p6;
    private readonly Pool<TEntity, T7> _p7;

    internal GroupRows(int count, Pool<TEntity, T1> p1, Pool<TEntity, T2> p2, Pool<TEntity, T3> p3, Pool<TEntity, T4> p4, Pool<TEntity, T5> p5, Pool<TEntity, T6> p6, Pool<TEntity, T7> p7)
    {
        _count = count;
        _p1 = p1;
        _p2 = p2;
        _p3 = p3;
        _p4 = p4;
        _p5 = p5;
        _p6 = p6;
        _p7 = p7;
    }

    public int Count => _count;
    public ReadOnlySpan<TEntity> Entities => _p1.Entities[.._count];
    public Span<T1> C1 => _p1.Components[.._count];
    public Span<T2> C2 => _p2.Components[.._count];
    public Span<T3> C3 => _p3.Components[.._count];
    public Span<T4> C4 => _p4.Components[.._count];
    public Span<T5> C5 => _p5.Components[.._count];
    public Span<T6> C6 => _p6.Components[.._count];
    public Span<T7> C7 => _p7.Components[.._count];

    internal Pool<TEntity, T1> Pool1 => _p1;
    internal Pool<TEntity, T2> Pool2 => _p2;
    internal Pool<TEntity, T3> Pool3 => _p3;
    internal Pool<TEntity, T4> Pool4 => _p4;
    internal Pool<TEntity, T5> Pool5 => _p5;
    internal Pool<TEntity, T6> Pool6 => _p6;
    internal Pool<TEntity, T7> Pool7 => _p7;

    public void Each<TFn>(ref TFn fn) where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7>
    {
        var registry = _p1.OwnerRegistry!;
#if !DEBUG
        // Nothing to close afterwards: no scope, and Release counts no pools.
        if (!registry.AutoDefer)
        {
            Walk(ref fn);
            return;
        }
#endif

        using var scope = WalkScope<TEntity>.Enter(registry, [_p1, _p2, _p3, _p4, _p5, _p6, _p7]);
        Walk(ref fn);
    }

    public void Each(RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7> action)
    {
        var adapter = new RefActionAdapter<TEntity, T1, T2, T3, T4, T5, T6, T7>(action);
        Each(ref adapter);
    }

    private void Walk<TFn>(ref TFn fn) where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7>
    {
        var ents = _p1.Entities;
        var c1 = _p1.Components;
        var c2 = _p2.Components;
        var c3 = _p3.Components;
        var c4 = _p4.Components;
        var c5 = _p5.Components;
        var c6 = _p6.Components;
        var c7 = _p7.Components;
        for (var i = _count - 1; i >= 0; i--)
        {
            fn.Invoke(ents[i], ref c1[i], ref c2[i], ref c3[i], ref c4[i], ref c5[i], ref c6[i], ref c7[i]);
        }
    }
}

public readonly struct GroupRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8>
    where TEntity : unmanaged, IEntity<TEntity>
    where T1 : IComponent
    where T2 : IComponent
    where T3 : IComponent
    where T4 : IComponent
    where T5 : IComponent
    where T6 : IComponent
    where T7 : IComponent
    where T8 : IComponent
{
    private readonly int _count;
    private readonly Pool<TEntity, T1> _p1;
    private readonly Pool<TEntity, T2> _p2;
    private readonly Pool<TEntity, T3> _p3;
    private readonly Pool<TEntity, T4> _p4;
    private readonly Pool<TEntity, T5> _p5;
    private readonly Pool<TEntity, T6> _p6;
    private readonly Pool<TEntity, T7> _p7;
    private readonly Pool<TEntity, T8> _p8;

    internal GroupRows(int count, Pool<TEntity, T1> p1, Pool<TEntity, T2> p2, Pool<TEntity, T3> p3, Pool<TEntity, T4> p4, Pool<TEntity, T5> p5, Pool<TEntity, T6> p6, Pool<TEntity, T7> p7, Pool<TEntity, T8> p8)
    {
        _count = count;
        _p1 = p1;
        _p2 = p2;
        _p3 = p3;
        _p4 = p4;
        _p5 = p5;
        _p6 = p6;
        _p7 = p7;
        _p8 = p8;
    }

    public int Count => _count;
    public ReadOnlySpan<TEntity> Entities => _p1.Entities[.._count];
    public Span<T1> C1 => _p1.Components[.._count];
    public Span<T2> C2 => _p2.Components[.._count];
    public Span<T3> C3 => _p3.Components[.._count];
    public Span<T4> C4 => _p4.Components[.._count];
    public Span<T5> C5 => _p5.Components[.._count];
    public Span<T6> C6 => _p6.Components[.._count];
    public Span<T7> C7 => _p7.Components[.._count];
    public Span<T8> C8 => _p8.Components[.._count];

    internal Pool<TEntity, T1> Pool1 => _p1;
    internal Pool<TEntity, T2> Pool2 => _p2;
    internal Pool<TEntity, T3> Pool3 => _p3;
    internal Pool<TEntity, T4> Pool4 => _p4;
    internal Pool<TEntity, T5> Pool5 => _p5;
    internal Pool<TEntity, T6> Pool6 => _p6;
    internal Pool<TEntity, T7> Pool7 => _p7;
    internal Pool<TEntity, T8> Pool8 => _p8;

    public void Each<TFn>(ref TFn fn) where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7, T8>
    {
        var registry = _p1.OwnerRegistry!;
#if !DEBUG
        // Nothing to close afterwards: no scope, and Release counts no pools.
        if (!registry.AutoDefer)
        {
            Walk(ref fn);
            return;
        }
#endif

        using var scope = WalkScope<TEntity>.Enter(registry, [_p1, _p2, _p3, _p4, _p5, _p6, _p7, _p8]);
        Walk(ref fn);
    }

    public void Each(RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7, T8> action)
    {
        var adapter = new RefActionAdapter<TEntity, T1, T2, T3, T4, T5, T6, T7, T8>(action);
        Each(ref adapter);
    }

    private void Walk<TFn>(ref TFn fn) where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7, T8>
    {
        var ents = _p1.Entities;
        var c1 = _p1.Components;
        var c2 = _p2.Components;
        var c3 = _p3.Components;
        var c4 = _p4.Components;
        var c5 = _p5.Components;
        var c6 = _p6.Components;
        var c7 = _p7.Components;
        var c8 = _p8.Components;
        for (var i = _count - 1; i >= 0; i--)
        {
            fn.Invoke(ents[i], ref c1[i], ref c2[i], ref c3[i], ref c4[i], ref c5[i], ref c6[i], ref c7[i], ref c8[i]);
        }
    }
}

public readonly struct GroupRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9>
    where TEntity : unmanaged, IEntity<TEntity>
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
    private readonly int _count;
    private readonly Pool<TEntity, T1> _p1;
    private readonly Pool<TEntity, T2> _p2;
    private readonly Pool<TEntity, T3> _p3;
    private readonly Pool<TEntity, T4> _p4;
    private readonly Pool<TEntity, T5> _p5;
    private readonly Pool<TEntity, T6> _p6;
    private readonly Pool<TEntity, T7> _p7;
    private readonly Pool<TEntity, T8> _p8;
    private readonly Pool<TEntity, T9> _p9;

    internal GroupRows(int count, Pool<TEntity, T1> p1, Pool<TEntity, T2> p2, Pool<TEntity, T3> p3, Pool<TEntity, T4> p4, Pool<TEntity, T5> p5, Pool<TEntity, T6> p6, Pool<TEntity, T7> p7, Pool<TEntity, T8> p8, Pool<TEntity, T9> p9)
    {
        _count = count;
        _p1 = p1;
        _p2 = p2;
        _p3 = p3;
        _p4 = p4;
        _p5 = p5;
        _p6 = p6;
        _p7 = p7;
        _p8 = p8;
        _p9 = p9;
    }

    public int Count => _count;
    public ReadOnlySpan<TEntity> Entities => _p1.Entities[.._count];
    public Span<T1> C1 => _p1.Components[.._count];
    public Span<T2> C2 => _p2.Components[.._count];
    public Span<T3> C3 => _p3.Components[.._count];
    public Span<T4> C4 => _p4.Components[.._count];
    public Span<T5> C5 => _p5.Components[.._count];
    public Span<T6> C6 => _p6.Components[.._count];
    public Span<T7> C7 => _p7.Components[.._count];
    public Span<T8> C8 => _p8.Components[.._count];
    public Span<T9> C9 => _p9.Components[.._count];

    internal Pool<TEntity, T1> Pool1 => _p1;
    internal Pool<TEntity, T2> Pool2 => _p2;
    internal Pool<TEntity, T3> Pool3 => _p3;
    internal Pool<TEntity, T4> Pool4 => _p4;
    internal Pool<TEntity, T5> Pool5 => _p5;
    internal Pool<TEntity, T6> Pool6 => _p6;
    internal Pool<TEntity, T7> Pool7 => _p7;
    internal Pool<TEntity, T8> Pool8 => _p8;
    internal Pool<TEntity, T9> Pool9 => _p9;

    public void Each<TFn>(ref TFn fn) where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9>
    {
        var registry = _p1.OwnerRegistry!;
#if !DEBUG
        // Nothing to close afterwards: no scope, and Release counts no pools.
        if (!registry.AutoDefer)
        {
            Walk(ref fn);
            return;
        }
#endif

        using var scope = WalkScope<TEntity>.Enter(registry, [_p1, _p2, _p3, _p4, _p5, _p6, _p7, _p8, _p9]);
        Walk(ref fn);
    }

    public void Each(RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9> action)
    {
        var adapter = new RefActionAdapter<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9>(action);
        Each(ref adapter);
    }

    private void Walk<TFn>(ref TFn fn) where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9>
    {
        var ents = _p1.Entities;
        var c1 = _p1.Components;
        var c2 = _p2.Components;
        var c3 = _p3.Components;
        var c4 = _p4.Components;
        var c5 = _p5.Components;
        var c6 = _p6.Components;
        var c7 = _p7.Components;
        var c8 = _p8.Components;
        var c9 = _p9.Components;
        for (var i = _count - 1; i >= 0; i--)
        {
            fn.Invoke(ents[i], ref c1[i], ref c2[i], ref c3[i], ref c4[i], ref c5[i], ref c6[i], ref c7[i], ref c8[i], ref c9[i]);
        }
    }
}

public readonly struct GroupRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
    where TEntity : unmanaged, IEntity<TEntity>
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
    private readonly int _count;
    private readonly Pool<TEntity, T1> _p1;
    private readonly Pool<TEntity, T2> _p2;
    private readonly Pool<TEntity, T3> _p3;
    private readonly Pool<TEntity, T4> _p4;
    private readonly Pool<TEntity, T5> _p5;
    private readonly Pool<TEntity, T6> _p6;
    private readonly Pool<TEntity, T7> _p7;
    private readonly Pool<TEntity, T8> _p8;
    private readonly Pool<TEntity, T9> _p9;
    private readonly Pool<TEntity, T10> _p10;

    internal GroupRows(int count, Pool<TEntity, T1> p1, Pool<TEntity, T2> p2, Pool<TEntity, T3> p3, Pool<TEntity, T4> p4, Pool<TEntity, T5> p5, Pool<TEntity, T6> p6, Pool<TEntity, T7> p7, Pool<TEntity, T8> p8, Pool<TEntity, T9> p9, Pool<TEntity, T10> p10)
    {
        _count = count;
        _p1 = p1;
        _p2 = p2;
        _p3 = p3;
        _p4 = p4;
        _p5 = p5;
        _p6 = p6;
        _p7 = p7;
        _p8 = p8;
        _p9 = p9;
        _p10 = p10;
    }

    public int Count => _count;
    public ReadOnlySpan<TEntity> Entities => _p1.Entities[.._count];
    public Span<T1> C1 => _p1.Components[.._count];
    public Span<T2> C2 => _p2.Components[.._count];
    public Span<T3> C3 => _p3.Components[.._count];
    public Span<T4> C4 => _p4.Components[.._count];
    public Span<T5> C5 => _p5.Components[.._count];
    public Span<T6> C6 => _p6.Components[.._count];
    public Span<T7> C7 => _p7.Components[.._count];
    public Span<T8> C8 => _p8.Components[.._count];
    public Span<T9> C9 => _p9.Components[.._count];
    public Span<T10> C10 => _p10.Components[.._count];

    internal Pool<TEntity, T1> Pool1 => _p1;
    internal Pool<TEntity, T2> Pool2 => _p2;
    internal Pool<TEntity, T3> Pool3 => _p3;
    internal Pool<TEntity, T4> Pool4 => _p4;
    internal Pool<TEntity, T5> Pool5 => _p5;
    internal Pool<TEntity, T6> Pool6 => _p6;
    internal Pool<TEntity, T7> Pool7 => _p7;
    internal Pool<TEntity, T8> Pool8 => _p8;
    internal Pool<TEntity, T9> Pool9 => _p9;
    internal Pool<TEntity, T10> Pool10 => _p10;

    public void Each<TFn>(ref TFn fn) where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
    {
        var registry = _p1.OwnerRegistry!;
#if !DEBUG
        // Nothing to close afterwards: no scope, and Release counts no pools.
        if (!registry.AutoDefer)
        {
            Walk(ref fn);
            return;
        }
#endif

        using var scope = WalkScope<TEntity>.Enter(registry, [_p1, _p2, _p3, _p4, _p5, _p6, _p7, _p8, _p9, _p10]);
        Walk(ref fn);
    }

    public void Each(RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> action)
    {
        var adapter = new RefActionAdapter<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(action);
        Each(ref adapter);
    }

    private void Walk<TFn>(ref TFn fn) where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
    {
        var ents = _p1.Entities;
        var c1 = _p1.Components;
        var c2 = _p2.Components;
        var c3 = _p3.Components;
        var c4 = _p4.Components;
        var c5 = _p5.Components;
        var c6 = _p6.Components;
        var c7 = _p7.Components;
        var c8 = _p8.Components;
        var c9 = _p9.Components;
        var c10 = _p10.Components;
        for (var i = _count - 1; i >= 0; i--)
        {
            fn.Invoke(ents[i], ref c1[i], ref c2[i], ref c3[i], ref c4[i], ref c5[i], ref c6[i], ref c7[i], ref c8[i], ref c9[i], ref c10[i]);
        }
    }
}

public readonly struct GroupRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>
    where TEntity : unmanaged, IEntity<TEntity>
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
    private readonly int _count;
    private readonly Pool<TEntity, T1> _p1;
    private readonly Pool<TEntity, T2> _p2;
    private readonly Pool<TEntity, T3> _p3;
    private readonly Pool<TEntity, T4> _p4;
    private readonly Pool<TEntity, T5> _p5;
    private readonly Pool<TEntity, T6> _p6;
    private readonly Pool<TEntity, T7> _p7;
    private readonly Pool<TEntity, T8> _p8;
    private readonly Pool<TEntity, T9> _p9;
    private readonly Pool<TEntity, T10> _p10;
    private readonly Pool<TEntity, T11> _p11;

    internal GroupRows(int count, Pool<TEntity, T1> p1, Pool<TEntity, T2> p2, Pool<TEntity, T3> p3, Pool<TEntity, T4> p4, Pool<TEntity, T5> p5, Pool<TEntity, T6> p6, Pool<TEntity, T7> p7, Pool<TEntity, T8> p8, Pool<TEntity, T9> p9, Pool<TEntity, T10> p10, Pool<TEntity, T11> p11)
    {
        _count = count;
        _p1 = p1;
        _p2 = p2;
        _p3 = p3;
        _p4 = p4;
        _p5 = p5;
        _p6 = p6;
        _p7 = p7;
        _p8 = p8;
        _p9 = p9;
        _p10 = p10;
        _p11 = p11;
    }

    public int Count => _count;
    public ReadOnlySpan<TEntity> Entities => _p1.Entities[.._count];
    public Span<T1> C1 => _p1.Components[.._count];
    public Span<T2> C2 => _p2.Components[.._count];
    public Span<T3> C3 => _p3.Components[.._count];
    public Span<T4> C4 => _p4.Components[.._count];
    public Span<T5> C5 => _p5.Components[.._count];
    public Span<T6> C6 => _p6.Components[.._count];
    public Span<T7> C7 => _p7.Components[.._count];
    public Span<T8> C8 => _p8.Components[.._count];
    public Span<T9> C9 => _p9.Components[.._count];
    public Span<T10> C10 => _p10.Components[.._count];
    public Span<T11> C11 => _p11.Components[.._count];

    internal Pool<TEntity, T1> Pool1 => _p1;
    internal Pool<TEntity, T2> Pool2 => _p2;
    internal Pool<TEntity, T3> Pool3 => _p3;
    internal Pool<TEntity, T4> Pool4 => _p4;
    internal Pool<TEntity, T5> Pool5 => _p5;
    internal Pool<TEntity, T6> Pool6 => _p6;
    internal Pool<TEntity, T7> Pool7 => _p7;
    internal Pool<TEntity, T8> Pool8 => _p8;
    internal Pool<TEntity, T9> Pool9 => _p9;
    internal Pool<TEntity, T10> Pool10 => _p10;
    internal Pool<TEntity, T11> Pool11 => _p11;

    public void Each<TFn>(ref TFn fn) where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>
    {
        var registry = _p1.OwnerRegistry!;
#if !DEBUG
        // Nothing to close afterwards: no scope, and Release counts no pools.
        if (!registry.AutoDefer)
        {
            Walk(ref fn);
            return;
        }
#endif

        using var scope = WalkScope<TEntity>.Enter(registry, [_p1, _p2, _p3, _p4, _p5, _p6, _p7, _p8, _p9, _p10, _p11]);
        Walk(ref fn);
    }

    public void Each(RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> action)
    {
        var adapter = new RefActionAdapter<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(action);
        Each(ref adapter);
    }

    private void Walk<TFn>(ref TFn fn) where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>
    {
        var ents = _p1.Entities;
        var c1 = _p1.Components;
        var c2 = _p2.Components;
        var c3 = _p3.Components;
        var c4 = _p4.Components;
        var c5 = _p5.Components;
        var c6 = _p6.Components;
        var c7 = _p7.Components;
        var c8 = _p8.Components;
        var c9 = _p9.Components;
        var c10 = _p10.Components;
        var c11 = _p11.Components;
        for (var i = _count - 1; i >= 0; i--)
        {
            fn.Invoke(ents[i], ref c1[i], ref c2[i], ref c3[i], ref c4[i], ref c5[i], ref c6[i], ref c7[i], ref c8[i], ref c9[i], ref c10[i], ref c11[i]);
        }
    }
}

public readonly struct GroupRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>
    where TEntity : unmanaged, IEntity<TEntity>
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
    private readonly int _count;
    private readonly Pool<TEntity, T1> _p1;
    private readonly Pool<TEntity, T2> _p2;
    private readonly Pool<TEntity, T3> _p3;
    private readonly Pool<TEntity, T4> _p4;
    private readonly Pool<TEntity, T5> _p5;
    private readonly Pool<TEntity, T6> _p6;
    private readonly Pool<TEntity, T7> _p7;
    private readonly Pool<TEntity, T8> _p8;
    private readonly Pool<TEntity, T9> _p9;
    private readonly Pool<TEntity, T10> _p10;
    private readonly Pool<TEntity, T11> _p11;
    private readonly Pool<TEntity, T12> _p12;

    internal GroupRows(int count, Pool<TEntity, T1> p1, Pool<TEntity, T2> p2, Pool<TEntity, T3> p3, Pool<TEntity, T4> p4, Pool<TEntity, T5> p5, Pool<TEntity, T6> p6, Pool<TEntity, T7> p7, Pool<TEntity, T8> p8, Pool<TEntity, T9> p9, Pool<TEntity, T10> p10, Pool<TEntity, T11> p11, Pool<TEntity, T12> p12)
    {
        _count = count;
        _p1 = p1;
        _p2 = p2;
        _p3 = p3;
        _p4 = p4;
        _p5 = p5;
        _p6 = p6;
        _p7 = p7;
        _p8 = p8;
        _p9 = p9;
        _p10 = p10;
        _p11 = p11;
        _p12 = p12;
    }

    public int Count => _count;
    public ReadOnlySpan<TEntity> Entities => _p1.Entities[.._count];
    public Span<T1> C1 => _p1.Components[.._count];
    public Span<T2> C2 => _p2.Components[.._count];
    public Span<T3> C3 => _p3.Components[.._count];
    public Span<T4> C4 => _p4.Components[.._count];
    public Span<T5> C5 => _p5.Components[.._count];
    public Span<T6> C6 => _p6.Components[.._count];
    public Span<T7> C7 => _p7.Components[.._count];
    public Span<T8> C8 => _p8.Components[.._count];
    public Span<T9> C9 => _p9.Components[.._count];
    public Span<T10> C10 => _p10.Components[.._count];
    public Span<T11> C11 => _p11.Components[.._count];
    public Span<T12> C12 => _p12.Components[.._count];

    internal Pool<TEntity, T1> Pool1 => _p1;
    internal Pool<TEntity, T2> Pool2 => _p2;
    internal Pool<TEntity, T3> Pool3 => _p3;
    internal Pool<TEntity, T4> Pool4 => _p4;
    internal Pool<TEntity, T5> Pool5 => _p5;
    internal Pool<TEntity, T6> Pool6 => _p6;
    internal Pool<TEntity, T7> Pool7 => _p7;
    internal Pool<TEntity, T8> Pool8 => _p8;
    internal Pool<TEntity, T9> Pool9 => _p9;
    internal Pool<TEntity, T10> Pool10 => _p10;
    internal Pool<TEntity, T11> Pool11 => _p11;
    internal Pool<TEntity, T12> Pool12 => _p12;

    public void Each<TFn>(ref TFn fn) where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>
    {
        var registry = _p1.OwnerRegistry!;
#if !DEBUG
        // Nothing to close afterwards: no scope, and Release counts no pools.
        if (!registry.AutoDefer)
        {
            Walk(ref fn);
            return;
        }
#endif

        using var scope = WalkScope<TEntity>.Enter(registry, [_p1, _p2, _p3, _p4, _p5, _p6, _p7, _p8, _p9, _p10, _p11, _p12]);
        Walk(ref fn);
    }

    public void Each(RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> action)
    {
        var adapter = new RefActionAdapter<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(action);
        Each(ref adapter);
    }

    private void Walk<TFn>(ref TFn fn) where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>
    {
        var ents = _p1.Entities;
        var c1 = _p1.Components;
        var c2 = _p2.Components;
        var c3 = _p3.Components;
        var c4 = _p4.Components;
        var c5 = _p5.Components;
        var c6 = _p6.Components;
        var c7 = _p7.Components;
        var c8 = _p8.Components;
        var c9 = _p9.Components;
        var c10 = _p10.Components;
        var c11 = _p11.Components;
        var c12 = _p12.Components;
        for (var i = _count - 1; i >= 0; i--)
        {
            fn.Invoke(ents[i], ref c1[i], ref c2[i], ref c3[i], ref c4[i], ref c5[i], ref c6[i], ref c7[i], ref c8[i], ref c9[i], ref c10[i], ref c11[i], ref c12[i]);
        }
    }
}

public readonly struct GroupRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>
    where TEntity : unmanaged, IEntity<TEntity>
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
    private readonly int _count;
    private readonly Pool<TEntity, T1> _p1;
    private readonly Pool<TEntity, T2> _p2;
    private readonly Pool<TEntity, T3> _p3;
    private readonly Pool<TEntity, T4> _p4;
    private readonly Pool<TEntity, T5> _p5;
    private readonly Pool<TEntity, T6> _p6;
    private readonly Pool<TEntity, T7> _p7;
    private readonly Pool<TEntity, T8> _p8;
    private readonly Pool<TEntity, T9> _p9;
    private readonly Pool<TEntity, T10> _p10;
    private readonly Pool<TEntity, T11> _p11;
    private readonly Pool<TEntity, T12> _p12;
    private readonly Pool<TEntity, T13> _p13;

    internal GroupRows(int count, Pool<TEntity, T1> p1, Pool<TEntity, T2> p2, Pool<TEntity, T3> p3, Pool<TEntity, T4> p4, Pool<TEntity, T5> p5, Pool<TEntity, T6> p6, Pool<TEntity, T7> p7, Pool<TEntity, T8> p8, Pool<TEntity, T9> p9, Pool<TEntity, T10> p10, Pool<TEntity, T11> p11, Pool<TEntity, T12> p12, Pool<TEntity, T13> p13)
    {
        _count = count;
        _p1 = p1;
        _p2 = p2;
        _p3 = p3;
        _p4 = p4;
        _p5 = p5;
        _p6 = p6;
        _p7 = p7;
        _p8 = p8;
        _p9 = p9;
        _p10 = p10;
        _p11 = p11;
        _p12 = p12;
        _p13 = p13;
    }

    public int Count => _count;
    public ReadOnlySpan<TEntity> Entities => _p1.Entities[.._count];
    public Span<T1> C1 => _p1.Components[.._count];
    public Span<T2> C2 => _p2.Components[.._count];
    public Span<T3> C3 => _p3.Components[.._count];
    public Span<T4> C4 => _p4.Components[.._count];
    public Span<T5> C5 => _p5.Components[.._count];
    public Span<T6> C6 => _p6.Components[.._count];
    public Span<T7> C7 => _p7.Components[.._count];
    public Span<T8> C8 => _p8.Components[.._count];
    public Span<T9> C9 => _p9.Components[.._count];
    public Span<T10> C10 => _p10.Components[.._count];
    public Span<T11> C11 => _p11.Components[.._count];
    public Span<T12> C12 => _p12.Components[.._count];
    public Span<T13> C13 => _p13.Components[.._count];

    internal Pool<TEntity, T1> Pool1 => _p1;
    internal Pool<TEntity, T2> Pool2 => _p2;
    internal Pool<TEntity, T3> Pool3 => _p3;
    internal Pool<TEntity, T4> Pool4 => _p4;
    internal Pool<TEntity, T5> Pool5 => _p5;
    internal Pool<TEntity, T6> Pool6 => _p6;
    internal Pool<TEntity, T7> Pool7 => _p7;
    internal Pool<TEntity, T8> Pool8 => _p8;
    internal Pool<TEntity, T9> Pool9 => _p9;
    internal Pool<TEntity, T10> Pool10 => _p10;
    internal Pool<TEntity, T11> Pool11 => _p11;
    internal Pool<TEntity, T12> Pool12 => _p12;
    internal Pool<TEntity, T13> Pool13 => _p13;

    public void Each<TFn>(ref TFn fn) where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>
    {
        var registry = _p1.OwnerRegistry!;
#if !DEBUG
        // Nothing to close afterwards: no scope, and Release counts no pools.
        if (!registry.AutoDefer)
        {
            Walk(ref fn);
            return;
        }
#endif

        using var scope = WalkScope<TEntity>.Enter(registry, [_p1, _p2, _p3, _p4, _p5, _p6, _p7, _p8, _p9, _p10, _p11, _p12, _p13]);
        Walk(ref fn);
    }

    public void Each(RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> action)
    {
        var adapter = new RefActionAdapter<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(action);
        Each(ref adapter);
    }

    private void Walk<TFn>(ref TFn fn) where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>
    {
        var ents = _p1.Entities;
        var c1 = _p1.Components;
        var c2 = _p2.Components;
        var c3 = _p3.Components;
        var c4 = _p4.Components;
        var c5 = _p5.Components;
        var c6 = _p6.Components;
        var c7 = _p7.Components;
        var c8 = _p8.Components;
        var c9 = _p9.Components;
        var c10 = _p10.Components;
        var c11 = _p11.Components;
        var c12 = _p12.Components;
        var c13 = _p13.Components;
        for (var i = _count - 1; i >= 0; i--)
        {
            fn.Invoke(ents[i], ref c1[i], ref c2[i], ref c3[i], ref c4[i], ref c5[i], ref c6[i], ref c7[i], ref c8[i], ref c9[i], ref c10[i], ref c11[i], ref c12[i], ref c13[i]);
        }
    }
}

public readonly struct GroupRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>
    where TEntity : unmanaged, IEntity<TEntity>
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
    private readonly int _count;
    private readonly Pool<TEntity, T1> _p1;
    private readonly Pool<TEntity, T2> _p2;
    private readonly Pool<TEntity, T3> _p3;
    private readonly Pool<TEntity, T4> _p4;
    private readonly Pool<TEntity, T5> _p5;
    private readonly Pool<TEntity, T6> _p6;
    private readonly Pool<TEntity, T7> _p7;
    private readonly Pool<TEntity, T8> _p8;
    private readonly Pool<TEntity, T9> _p9;
    private readonly Pool<TEntity, T10> _p10;
    private readonly Pool<TEntity, T11> _p11;
    private readonly Pool<TEntity, T12> _p12;
    private readonly Pool<TEntity, T13> _p13;
    private readonly Pool<TEntity, T14> _p14;

    internal GroupRows(int count, Pool<TEntity, T1> p1, Pool<TEntity, T2> p2, Pool<TEntity, T3> p3, Pool<TEntity, T4> p4, Pool<TEntity, T5> p5, Pool<TEntity, T6> p6, Pool<TEntity, T7> p7, Pool<TEntity, T8> p8, Pool<TEntity, T9> p9, Pool<TEntity, T10> p10, Pool<TEntity, T11> p11, Pool<TEntity, T12> p12, Pool<TEntity, T13> p13, Pool<TEntity, T14> p14)
    {
        _count = count;
        _p1 = p1;
        _p2 = p2;
        _p3 = p3;
        _p4 = p4;
        _p5 = p5;
        _p6 = p6;
        _p7 = p7;
        _p8 = p8;
        _p9 = p9;
        _p10 = p10;
        _p11 = p11;
        _p12 = p12;
        _p13 = p13;
        _p14 = p14;
    }

    public int Count => _count;
    public ReadOnlySpan<TEntity> Entities => _p1.Entities[.._count];
    public Span<T1> C1 => _p1.Components[.._count];
    public Span<T2> C2 => _p2.Components[.._count];
    public Span<T3> C3 => _p3.Components[.._count];
    public Span<T4> C4 => _p4.Components[.._count];
    public Span<T5> C5 => _p5.Components[.._count];
    public Span<T6> C6 => _p6.Components[.._count];
    public Span<T7> C7 => _p7.Components[.._count];
    public Span<T8> C8 => _p8.Components[.._count];
    public Span<T9> C9 => _p9.Components[.._count];
    public Span<T10> C10 => _p10.Components[.._count];
    public Span<T11> C11 => _p11.Components[.._count];
    public Span<T12> C12 => _p12.Components[.._count];
    public Span<T13> C13 => _p13.Components[.._count];
    public Span<T14> C14 => _p14.Components[.._count];

    internal Pool<TEntity, T1> Pool1 => _p1;
    internal Pool<TEntity, T2> Pool2 => _p2;
    internal Pool<TEntity, T3> Pool3 => _p3;
    internal Pool<TEntity, T4> Pool4 => _p4;
    internal Pool<TEntity, T5> Pool5 => _p5;
    internal Pool<TEntity, T6> Pool6 => _p6;
    internal Pool<TEntity, T7> Pool7 => _p7;
    internal Pool<TEntity, T8> Pool8 => _p8;
    internal Pool<TEntity, T9> Pool9 => _p9;
    internal Pool<TEntity, T10> Pool10 => _p10;
    internal Pool<TEntity, T11> Pool11 => _p11;
    internal Pool<TEntity, T12> Pool12 => _p12;
    internal Pool<TEntity, T13> Pool13 => _p13;
    internal Pool<TEntity, T14> Pool14 => _p14;

    public void Each<TFn>(ref TFn fn) where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>
    {
        var registry = _p1.OwnerRegistry!;
#if !DEBUG
        // Nothing to close afterwards: no scope, and Release counts no pools.
        if (!registry.AutoDefer)
        {
            Walk(ref fn);
            return;
        }
#endif

        using var scope = WalkScope<TEntity>.Enter(registry, [_p1, _p2, _p3, _p4, _p5, _p6, _p7, _p8, _p9, _p10, _p11, _p12, _p13, _p14]);
        Walk(ref fn);
    }

    public void Each(RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> action)
    {
        var adapter = new RefActionAdapter<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(action);
        Each(ref adapter);
    }

    private void Walk<TFn>(ref TFn fn) where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>
    {
        var ents = _p1.Entities;
        var c1 = _p1.Components;
        var c2 = _p2.Components;
        var c3 = _p3.Components;
        var c4 = _p4.Components;
        var c5 = _p5.Components;
        var c6 = _p6.Components;
        var c7 = _p7.Components;
        var c8 = _p8.Components;
        var c9 = _p9.Components;
        var c10 = _p10.Components;
        var c11 = _p11.Components;
        var c12 = _p12.Components;
        var c13 = _p13.Components;
        var c14 = _p14.Components;
        for (var i = _count - 1; i >= 0; i--)
        {
            fn.Invoke(ents[i], ref c1[i], ref c2[i], ref c3[i], ref c4[i], ref c5[i], ref c6[i], ref c7[i], ref c8[i], ref c9[i], ref c10[i], ref c11[i], ref c12[i], ref c13[i], ref c14[i]);
        }
    }
}

public readonly struct GroupRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>
    where TEntity : unmanaged, IEntity<TEntity>
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
    private readonly int _count;
    private readonly Pool<TEntity, T1> _p1;
    private readonly Pool<TEntity, T2> _p2;
    private readonly Pool<TEntity, T3> _p3;
    private readonly Pool<TEntity, T4> _p4;
    private readonly Pool<TEntity, T5> _p5;
    private readonly Pool<TEntity, T6> _p6;
    private readonly Pool<TEntity, T7> _p7;
    private readonly Pool<TEntity, T8> _p8;
    private readonly Pool<TEntity, T9> _p9;
    private readonly Pool<TEntity, T10> _p10;
    private readonly Pool<TEntity, T11> _p11;
    private readonly Pool<TEntity, T12> _p12;
    private readonly Pool<TEntity, T13> _p13;
    private readonly Pool<TEntity, T14> _p14;
    private readonly Pool<TEntity, T15> _p15;

    internal GroupRows(int count, Pool<TEntity, T1> p1, Pool<TEntity, T2> p2, Pool<TEntity, T3> p3, Pool<TEntity, T4> p4, Pool<TEntity, T5> p5, Pool<TEntity, T6> p6, Pool<TEntity, T7> p7, Pool<TEntity, T8> p8, Pool<TEntity, T9> p9, Pool<TEntity, T10> p10, Pool<TEntity, T11> p11, Pool<TEntity, T12> p12, Pool<TEntity, T13> p13, Pool<TEntity, T14> p14, Pool<TEntity, T15> p15)
    {
        _count = count;
        _p1 = p1;
        _p2 = p2;
        _p3 = p3;
        _p4 = p4;
        _p5 = p5;
        _p6 = p6;
        _p7 = p7;
        _p8 = p8;
        _p9 = p9;
        _p10 = p10;
        _p11 = p11;
        _p12 = p12;
        _p13 = p13;
        _p14 = p14;
        _p15 = p15;
    }

    public int Count => _count;
    public ReadOnlySpan<TEntity> Entities => _p1.Entities[.._count];
    public Span<T1> C1 => _p1.Components[.._count];
    public Span<T2> C2 => _p2.Components[.._count];
    public Span<T3> C3 => _p3.Components[.._count];
    public Span<T4> C4 => _p4.Components[.._count];
    public Span<T5> C5 => _p5.Components[.._count];
    public Span<T6> C6 => _p6.Components[.._count];
    public Span<T7> C7 => _p7.Components[.._count];
    public Span<T8> C8 => _p8.Components[.._count];
    public Span<T9> C9 => _p9.Components[.._count];
    public Span<T10> C10 => _p10.Components[.._count];
    public Span<T11> C11 => _p11.Components[.._count];
    public Span<T12> C12 => _p12.Components[.._count];
    public Span<T13> C13 => _p13.Components[.._count];
    public Span<T14> C14 => _p14.Components[.._count];
    public Span<T15> C15 => _p15.Components[.._count];

    internal Pool<TEntity, T1> Pool1 => _p1;
    internal Pool<TEntity, T2> Pool2 => _p2;
    internal Pool<TEntity, T3> Pool3 => _p3;
    internal Pool<TEntity, T4> Pool4 => _p4;
    internal Pool<TEntity, T5> Pool5 => _p5;
    internal Pool<TEntity, T6> Pool6 => _p6;
    internal Pool<TEntity, T7> Pool7 => _p7;
    internal Pool<TEntity, T8> Pool8 => _p8;
    internal Pool<TEntity, T9> Pool9 => _p9;
    internal Pool<TEntity, T10> Pool10 => _p10;
    internal Pool<TEntity, T11> Pool11 => _p11;
    internal Pool<TEntity, T12> Pool12 => _p12;
    internal Pool<TEntity, T13> Pool13 => _p13;
    internal Pool<TEntity, T14> Pool14 => _p14;
    internal Pool<TEntity, T15> Pool15 => _p15;

    public void Each<TFn>(ref TFn fn) where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>
    {
        var registry = _p1.OwnerRegistry!;
#if !DEBUG
        // Nothing to close afterwards: no scope, and Release counts no pools.
        if (!registry.AutoDefer)
        {
            Walk(ref fn);
            return;
        }
#endif

        using var scope = WalkScope<TEntity>.Enter(registry, [_p1, _p2, _p3, _p4, _p5, _p6, _p7, _p8, _p9, _p10, _p11, _p12, _p13, _p14, _p15]);
        Walk(ref fn);
    }

    public void Each(RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> action)
    {
        var adapter = new RefActionAdapter<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(action);
        Each(ref adapter);
    }

    private void Walk<TFn>(ref TFn fn) where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>
    {
        var ents = _p1.Entities;
        var c1 = _p1.Components;
        var c2 = _p2.Components;
        var c3 = _p3.Components;
        var c4 = _p4.Components;
        var c5 = _p5.Components;
        var c6 = _p6.Components;
        var c7 = _p7.Components;
        var c8 = _p8.Components;
        var c9 = _p9.Components;
        var c10 = _p10.Components;
        var c11 = _p11.Components;
        var c12 = _p12.Components;
        var c13 = _p13.Components;
        var c14 = _p14.Components;
        var c15 = _p15.Components;
        for (var i = _count - 1; i >= 0; i--)
        {
            fn.Invoke(ents[i], ref c1[i], ref c2[i], ref c3[i], ref c4[i], ref c5[i], ref c6[i], ref c7[i], ref c8[i], ref c9[i], ref c10[i], ref c11[i], ref c12[i], ref c13[i], ref c14[i], ref c15[i]);
        }
    }
}

public readonly struct GroupRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>
    where TEntity : unmanaged, IEntity<TEntity>
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
    private readonly int _count;
    private readonly Pool<TEntity, T1> _p1;
    private readonly Pool<TEntity, T2> _p2;
    private readonly Pool<TEntity, T3> _p3;
    private readonly Pool<TEntity, T4> _p4;
    private readonly Pool<TEntity, T5> _p5;
    private readonly Pool<TEntity, T6> _p6;
    private readonly Pool<TEntity, T7> _p7;
    private readonly Pool<TEntity, T8> _p8;
    private readonly Pool<TEntity, T9> _p9;
    private readonly Pool<TEntity, T10> _p10;
    private readonly Pool<TEntity, T11> _p11;
    private readonly Pool<TEntity, T12> _p12;
    private readonly Pool<TEntity, T13> _p13;
    private readonly Pool<TEntity, T14> _p14;
    private readonly Pool<TEntity, T15> _p15;
    private readonly Pool<TEntity, T16> _p16;

    internal GroupRows(int count, Pool<TEntity, T1> p1, Pool<TEntity, T2> p2, Pool<TEntity, T3> p3, Pool<TEntity, T4> p4, Pool<TEntity, T5> p5, Pool<TEntity, T6> p6, Pool<TEntity, T7> p7, Pool<TEntity, T8> p8, Pool<TEntity, T9> p9, Pool<TEntity, T10> p10, Pool<TEntity, T11> p11, Pool<TEntity, T12> p12, Pool<TEntity, T13> p13, Pool<TEntity, T14> p14, Pool<TEntity, T15> p15, Pool<TEntity, T16> p16)
    {
        _count = count;
        _p1 = p1;
        _p2 = p2;
        _p3 = p3;
        _p4 = p4;
        _p5 = p5;
        _p6 = p6;
        _p7 = p7;
        _p8 = p8;
        _p9 = p9;
        _p10 = p10;
        _p11 = p11;
        _p12 = p12;
        _p13 = p13;
        _p14 = p14;
        _p15 = p15;
        _p16 = p16;
    }

    public int Count => _count;
    public ReadOnlySpan<TEntity> Entities => _p1.Entities[.._count];
    public Span<T1> C1 => _p1.Components[.._count];
    public Span<T2> C2 => _p2.Components[.._count];
    public Span<T3> C3 => _p3.Components[.._count];
    public Span<T4> C4 => _p4.Components[.._count];
    public Span<T5> C5 => _p5.Components[.._count];
    public Span<T6> C6 => _p6.Components[.._count];
    public Span<T7> C7 => _p7.Components[.._count];
    public Span<T8> C8 => _p8.Components[.._count];
    public Span<T9> C9 => _p9.Components[.._count];
    public Span<T10> C10 => _p10.Components[.._count];
    public Span<T11> C11 => _p11.Components[.._count];
    public Span<T12> C12 => _p12.Components[.._count];
    public Span<T13> C13 => _p13.Components[.._count];
    public Span<T14> C14 => _p14.Components[.._count];
    public Span<T15> C15 => _p15.Components[.._count];
    public Span<T16> C16 => _p16.Components[.._count];

    internal Pool<TEntity, T1> Pool1 => _p1;
    internal Pool<TEntity, T2> Pool2 => _p2;
    internal Pool<TEntity, T3> Pool3 => _p3;
    internal Pool<TEntity, T4> Pool4 => _p4;
    internal Pool<TEntity, T5> Pool5 => _p5;
    internal Pool<TEntity, T6> Pool6 => _p6;
    internal Pool<TEntity, T7> Pool7 => _p7;
    internal Pool<TEntity, T8> Pool8 => _p8;
    internal Pool<TEntity, T9> Pool9 => _p9;
    internal Pool<TEntity, T10> Pool10 => _p10;
    internal Pool<TEntity, T11> Pool11 => _p11;
    internal Pool<TEntity, T12> Pool12 => _p12;
    internal Pool<TEntity, T13> Pool13 => _p13;
    internal Pool<TEntity, T14> Pool14 => _p14;
    internal Pool<TEntity, T15> Pool15 => _p15;
    internal Pool<TEntity, T16> Pool16 => _p16;

    public void Each<TFn>(ref TFn fn) where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>
    {
        var registry = _p1.OwnerRegistry!;
#if !DEBUG
        // Nothing to close afterwards: no scope, and Release counts no pools.
        if (!registry.AutoDefer)
        {
            Walk(ref fn);
            return;
        }
#endif

        using var scope = WalkScope<TEntity>.Enter(registry, [_p1, _p2, _p3, _p4, _p5, _p6, _p7, _p8, _p9, _p10, _p11, _p12, _p13, _p14, _p15, _p16]);
        Walk(ref fn);
    }

    public void Each(RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> action)
    {
        var adapter = new RefActionAdapter<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(action);
        Each(ref adapter);
    }

    private void Walk<TFn>(ref TFn fn) where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>
    {
        var ents = _p1.Entities;
        var c1 = _p1.Components;
        var c2 = _p2.Components;
        var c3 = _p3.Components;
        var c4 = _p4.Components;
        var c5 = _p5.Components;
        var c6 = _p6.Components;
        var c7 = _p7.Components;
        var c8 = _p8.Components;
        var c9 = _p9.Components;
        var c10 = _p10.Components;
        var c11 = _p11.Components;
        var c12 = _p12.Components;
        var c13 = _p13.Components;
        var c14 = _p14.Components;
        var c15 = _p15.Components;
        var c16 = _p16.Components;
        for (var i = _count - 1; i >= 0; i--)
        {
            fn.Invoke(ents[i], ref c1[i], ref c2[i], ref c3[i], ref c4[i], ref c5[i], ref c6[i], ref c7[i], ref c8[i], ref c9[i], ref c10[i], ref c11[i], ref c12[i], ref c13[i], ref c14[i], ref c15[i], ref c16[i]);
        }
    }
}
