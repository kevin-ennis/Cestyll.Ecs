namespace Cestyll.Ecs;

// Handles, and Create overloads with initial components; inside a defer scope they record them.
public partial class Registry<TEntity>
    where TEntity : unmanaged, IEntity<TEntity>
{
    // Unchecked; each handle call checks liveness.
    public EntityHandle<TEntity> Handle(TEntity e) => new(this, e);

    public EntityHandle<TEntity> Create<T1>(T1 c1)
        where T1 : IComponent
    {
        if (_deferDepth > 0)
        {
            return new(this, DeferredBuffer().Create(c1).Entity);
        }

        var e = CreateEntity();
        Storage<T1>().Add(e, c1);
        return new(this, e);
    }

    public EntityHandle<TEntity> Create<T1, T2>(T1 c1, T2 c2)
        where T1 : IComponent
        where T2 : IComponent
    {
        if (_deferDepth > 0)
        {
            return new(this, DeferredBuffer().Create(c1, c2).Entity);
        }

        var e = CreateEntity();
        Storage<T1>().Add(e, c1);
        Storage<T2>().Add(e, c2);
        return new(this, e);
    }

    public EntityHandle<TEntity> Create<T1, T2, T3>(T1 c1, T2 c2, T3 c3)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
    {
        if (_deferDepth > 0)
        {
            return new(this, DeferredBuffer().Create(c1, c2, c3).Entity);
        }

        var e = CreateEntity();
        Storage<T1>().Add(e, c1);
        Storage<T2>().Add(e, c2);
        Storage<T3>().Add(e, c3);
        return new(this, e);
    }

    public EntityHandle<TEntity> Create<T1, T2, T3, T4>(T1 c1, T2 c2, T3 c3, T4 c4)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
    {
        if (_deferDepth > 0)
        {
            return new(this, DeferredBuffer().Create(c1, c2, c3, c4).Entity);
        }

        var e = CreateEntity();
        Storage<T1>().Add(e, c1);
        Storage<T2>().Add(e, c2);
        Storage<T3>().Add(e, c3);
        Storage<T4>().Add(e, c4);
        return new(this, e);
    }

    public EntityHandle<TEntity> Create<T1, T2, T3, T4, T5>(T1 c1, T2 c2, T3 c3, T4 c4, T5 c5)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
    {
        if (_deferDepth > 0)
        {
            return new(this, DeferredBuffer().Create(c1, c2, c3, c4, c5).Entity);
        }

        var e = CreateEntity();
        Storage<T1>().Add(e, c1);
        Storage<T2>().Add(e, c2);
        Storage<T3>().Add(e, c3);
        Storage<T4>().Add(e, c4);
        Storage<T5>().Add(e, c5);
        return new(this, e);
    }

    public EntityHandle<TEntity> Create<T1, T2, T3, T4, T5, T6>(T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
    {
        if (_deferDepth > 0)
        {
            return new(this, DeferredBuffer().Create(c1, c2, c3, c4, c5, c6).Entity);
        }

        var e = CreateEntity();
        Storage<T1>().Add(e, c1);
        Storage<T2>().Add(e, c2);
        Storage<T3>().Add(e, c3);
        Storage<T4>().Add(e, c4);
        Storage<T5>().Add(e, c5);
        Storage<T6>().Add(e, c6);
        return new(this, e);
    }

    public EntityHandle<TEntity> Create<T1, T2, T3, T4, T5, T6, T7>(T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
    {
        if (_deferDepth > 0)
        {
            return new(this, DeferredBuffer().Create(c1, c2, c3, c4, c5, c6, c7).Entity);
        }

        var e = CreateEntity();
        Storage<T1>().Add(e, c1);
        Storage<T2>().Add(e, c2);
        Storage<T3>().Add(e, c3);
        Storage<T4>().Add(e, c4);
        Storage<T5>().Add(e, c5);
        Storage<T6>().Add(e, c6);
        Storage<T7>().Add(e, c7);
        return new(this, e);
    }

    public EntityHandle<TEntity> Create<T1, T2, T3, T4, T5, T6, T7, T8>(
        T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
    {
        if (_deferDepth > 0)
        {
            return new(this, DeferredBuffer().Create(c1, c2, c3, c4, c5, c6, c7, c8).Entity);
        }

        var e = CreateEntity();
        Storage<T1>().Add(e, c1);
        Storage<T2>().Add(e, c2);
        Storage<T3>().Add(e, c3);
        Storage<T4>().Add(e, c4);
        Storage<T5>().Add(e, c5);
        Storage<T6>().Add(e, c6);
        Storage<T7>().Add(e, c7);
        Storage<T8>().Add(e, c8);
        return new(this, e);
    }

    public EntityHandle<TEntity> Create<T1, T2, T3, T4, T5, T6, T7, T8, T9>(
        T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9)
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
        if (_deferDepth > 0)
        {
            return new(this, DeferredBuffer().Create(c1, c2, c3, c4, c5, c6, c7, c8, c9).Entity);
        }

        var e = CreateEntity();
        Storage<T1>().Add(e, c1);
        Storage<T2>().Add(e, c2);
        Storage<T3>().Add(e, c3);
        Storage<T4>().Add(e, c4);
        Storage<T5>().Add(e, c5);
        Storage<T6>().Add(e, c6);
        Storage<T7>().Add(e, c7);
        Storage<T8>().Add(e, c8);
        Storage<T9>().Add(e, c9);
        return new(this, e);
    }

    public EntityHandle<TEntity> Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(
        T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10)
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
        if (_deferDepth > 0)
        {
            return new(this, DeferredBuffer().Create(c1, c2, c3, c4, c5, c6, c7, c8, c9, c10).Entity);
        }

        var e = CreateEntity();
        Storage<T1>().Add(e, c1);
        Storage<T2>().Add(e, c2);
        Storage<T3>().Add(e, c3);
        Storage<T4>().Add(e, c4);
        Storage<T5>().Add(e, c5);
        Storage<T6>().Add(e, c6);
        Storage<T7>().Add(e, c7);
        Storage<T8>().Add(e, c8);
        Storage<T9>().Add(e, c9);
        Storage<T10>().Add(e, c10);
        return new(this, e);
    }

    public EntityHandle<TEntity> Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(
        T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10, T11 c11)
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
        if (_deferDepth > 0)
        {
            return new(this, DeferredBuffer().Create(c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11).Entity);
        }

        var e = CreateEntity();
        Storage<T1>().Add(e, c1);
        Storage<T2>().Add(e, c2);
        Storage<T3>().Add(e, c3);
        Storage<T4>().Add(e, c4);
        Storage<T5>().Add(e, c5);
        Storage<T6>().Add(e, c6);
        Storage<T7>().Add(e, c7);
        Storage<T8>().Add(e, c8);
        Storage<T9>().Add(e, c9);
        Storage<T10>().Add(e, c10);
        Storage<T11>().Add(e, c11);
        return new(this, e);
    }

    public EntityHandle<TEntity> Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(
        T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10, T11 c11, T12 c12)
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
        if (_deferDepth > 0)
        {
            return new(this, DeferredBuffer().Create(c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12).Entity);
        }

        var e = CreateEntity();
        Storage<T1>().Add(e, c1);
        Storage<T2>().Add(e, c2);
        Storage<T3>().Add(e, c3);
        Storage<T4>().Add(e, c4);
        Storage<T5>().Add(e, c5);
        Storage<T6>().Add(e, c6);
        Storage<T7>().Add(e, c7);
        Storage<T8>().Add(e, c8);
        Storage<T9>().Add(e, c9);
        Storage<T10>().Add(e, c10);
        Storage<T11>().Add(e, c11);
        Storage<T12>().Add(e, c12);
        return new(this, e);
    }

    public EntityHandle<TEntity> Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(
        T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10, T11 c11, T12 c12, T13 c13)
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
        if (_deferDepth > 0)
        {
            return new(this, DeferredBuffer().Create(c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12, c13).Entity);
        }

        var e = CreateEntity();
        Storage<T1>().Add(e, c1);
        Storage<T2>().Add(e, c2);
        Storage<T3>().Add(e, c3);
        Storage<T4>().Add(e, c4);
        Storage<T5>().Add(e, c5);
        Storage<T6>().Add(e, c6);
        Storage<T7>().Add(e, c7);
        Storage<T8>().Add(e, c8);
        Storage<T9>().Add(e, c9);
        Storage<T10>().Add(e, c10);
        Storage<T11>().Add(e, c11);
        Storage<T12>().Add(e, c12);
        Storage<T13>().Add(e, c13);
        return new(this, e);
    }

    public EntityHandle<TEntity> Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(
        T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10, T11 c11, T12 c12, T13 c13, T14 c14)
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
        if (_deferDepth > 0)
        {
            return new(this, DeferredBuffer().Create(c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12, c13, c14).Entity);
        }

        var e = CreateEntity();
        Storage<T1>().Add(e, c1);
        Storage<T2>().Add(e, c2);
        Storage<T3>().Add(e, c3);
        Storage<T4>().Add(e, c4);
        Storage<T5>().Add(e, c5);
        Storage<T6>().Add(e, c6);
        Storage<T7>().Add(e, c7);
        Storage<T8>().Add(e, c8);
        Storage<T9>().Add(e, c9);
        Storage<T10>().Add(e, c10);
        Storage<T11>().Add(e, c11);
        Storage<T12>().Add(e, c12);
        Storage<T13>().Add(e, c13);
        Storage<T14>().Add(e, c14);
        return new(this, e);
    }

    public EntityHandle<TEntity> Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(
        T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10, T11 c11, T12 c12, T13 c13, T14 c14, T15 c15)
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
        if (_deferDepth > 0)
        {
            return new(this, DeferredBuffer().Create(c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12, c13, c14, c15).Entity);
        }

        var e = CreateEntity();
        Storage<T1>().Add(e, c1);
        Storage<T2>().Add(e, c2);
        Storage<T3>().Add(e, c3);
        Storage<T4>().Add(e, c4);
        Storage<T5>().Add(e, c5);
        Storage<T6>().Add(e, c6);
        Storage<T7>().Add(e, c7);
        Storage<T8>().Add(e, c8);
        Storage<T9>().Add(e, c9);
        Storage<T10>().Add(e, c10);
        Storage<T11>().Add(e, c11);
        Storage<T12>().Add(e, c12);
        Storage<T13>().Add(e, c13);
        Storage<T14>().Add(e, c14);
        Storage<T15>().Add(e, c15);
        return new(this, e);
    }

    public EntityHandle<TEntity> Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(
        T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10, T11 c11, T12 c12, T13 c13, T14 c14, T15 c15, T16 c16)
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
        if (_deferDepth > 0)
        {
            return new(this, DeferredBuffer().Create(c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12, c13, c14, c15, c16).Entity);
        }

        var e = CreateEntity();
        Storage<T1>().Add(e, c1);
        Storage<T2>().Add(e, c2);
        Storage<T3>().Add(e, c3);
        Storage<T4>().Add(e, c4);
        Storage<T5>().Add(e, c5);
        Storage<T6>().Add(e, c6);
        Storage<T7>().Add(e, c7);
        Storage<T8>().Add(e, c8);
        Storage<T9>().Add(e, c9);
        Storage<T10>().Add(e, c10);
        Storage<T11>().Add(e, c11);
        Storage<T12>().Add(e, c12);
        Storage<T13>().Add(e, c13);
        Storage<T14>().Add(e, c14);
        Storage<T15>().Add(e, c15);
        Storage<T16>().Add(e, c16);
        return new(this, e);
    }
}
