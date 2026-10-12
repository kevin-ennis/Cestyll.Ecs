namespace Cestyll.Ecs.Commands;

// Create overloads that record the initial components.
public sealed partial class CommandBuffer<TEntity>
    where TEntity : unmanaged, IEntity<TEntity>
{
    public CommandHandle<TEntity> Create<T1>(T1 c1)
        where T1 : IComponent
    {
        var e = Created(_registry.CreateEntityNow());
        Add(e, c1);
        return new(this, e);
    }

    public CommandHandle<TEntity> Create<T1, T2>(T1 c1, T2 c2)
        where T1 : IComponent
        where T2 : IComponent
    {
        var e = Created(_registry.CreateEntityNow());
        Add(e, c1);
        Add(e, c2);
        return new(this, e);
    }

    public CommandHandle<TEntity> Create<T1, T2, T3>(T1 c1, T2 c2, T3 c3)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
    {
        var e = Created(_registry.CreateEntityNow());
        Add(e, c1);
        Add(e, c2);
        Add(e, c3);
        return new(this, e);
    }

    public CommandHandle<TEntity> Create<T1, T2, T3, T4>(T1 c1, T2 c2, T3 c3, T4 c4)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
    {
        var e = Created(_registry.CreateEntityNow());
        Add(e, c1);
        Add(e, c2);
        Add(e, c3);
        Add(e, c4);
        return new(this, e);
    }

    public CommandHandle<TEntity> Create<T1, T2, T3, T4, T5>(
        T1 c1, T2 c2, T3 c3, T4 c4, T5 c5)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
    {
        var e = Created(_registry.CreateEntityNow());
        Add(e, c1);
        Add(e, c2);
        Add(e, c3);
        Add(e, c4);
        Add(e, c5);
        return new(this, e);
    }

    public CommandHandle<TEntity> Create<T1, T2, T3, T4, T5, T6>(
        T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
    {
        var e = Created(_registry.CreateEntityNow());
        Add(e, c1);
        Add(e, c2);
        Add(e, c3);
        Add(e, c4);
        Add(e, c5);
        Add(e, c6);
        return new(this, e);
    }

    public CommandHandle<TEntity> Create<T1, T2, T3, T4, T5, T6, T7>(
        T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
    {
        var e = Created(_registry.CreateEntityNow());
        Add(e, c1);
        Add(e, c2);
        Add(e, c3);
        Add(e, c4);
        Add(e, c5);
        Add(e, c6);
        Add(e, c7);
        return new(this, e);
    }

    public CommandHandle<TEntity> Create<T1, T2, T3, T4, T5, T6, T7, T8>(
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
        var e = Created(_registry.CreateEntityNow());
        Add(e, c1);
        Add(e, c2);
        Add(e, c3);
        Add(e, c4);
        Add(e, c5);
        Add(e, c6);
        Add(e, c7);
        Add(e, c8);
        return new(this, e);
    }

    public CommandHandle<TEntity> Create<T1, T2, T3, T4, T5, T6, T7, T8, T9>(
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
        var e = Created(_registry.CreateEntityNow());
        Add(e, c1);
        Add(e, c2);
        Add(e, c3);
        Add(e, c4);
        Add(e, c5);
        Add(e, c6);
        Add(e, c7);
        Add(e, c8);
        Add(e, c9);
        return new(this, e);
    }

    public CommandHandle<TEntity> Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(
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
        var e = Created(_registry.CreateEntityNow());
        Add(e, c1);
        Add(e, c2);
        Add(e, c3);
        Add(e, c4);
        Add(e, c5);
        Add(e, c6);
        Add(e, c7);
        Add(e, c8);
        Add(e, c9);
        Add(e, c10);
        return new(this, e);
    }

    public CommandHandle<TEntity> Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(
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
        var e = Created(_registry.CreateEntityNow());
        Add(e, c1);
        Add(e, c2);
        Add(e, c3);
        Add(e, c4);
        Add(e, c5);
        Add(e, c6);
        Add(e, c7);
        Add(e, c8);
        Add(e, c9);
        Add(e, c10);
        Add(e, c11);
        return new(this, e);
    }

    public CommandHandle<TEntity> Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(
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
        var e = Created(_registry.CreateEntityNow());
        Add(e, c1);
        Add(e, c2);
        Add(e, c3);
        Add(e, c4);
        Add(e, c5);
        Add(e, c6);
        Add(e, c7);
        Add(e, c8);
        Add(e, c9);
        Add(e, c10);
        Add(e, c11);
        Add(e, c12);
        return new(this, e);
    }

    public CommandHandle<TEntity> Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(
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
        var e = Created(_registry.CreateEntityNow());
        Add(e, c1);
        Add(e, c2);
        Add(e, c3);
        Add(e, c4);
        Add(e, c5);
        Add(e, c6);
        Add(e, c7);
        Add(e, c8);
        Add(e, c9);
        Add(e, c10);
        Add(e, c11);
        Add(e, c12);
        Add(e, c13);
        return new(this, e);
    }

    public CommandHandle<TEntity> Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(
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
        var e = Created(_registry.CreateEntityNow());
        Add(e, c1);
        Add(e, c2);
        Add(e, c3);
        Add(e, c4);
        Add(e, c5);
        Add(e, c6);
        Add(e, c7);
        Add(e, c8);
        Add(e, c9);
        Add(e, c10);
        Add(e, c11);
        Add(e, c12);
        Add(e, c13);
        Add(e, c14);
        return new(this, e);
    }

    public CommandHandle<TEntity> Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(
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
        var e = Created(_registry.CreateEntityNow());
        Add(e, c1);
        Add(e, c2);
        Add(e, c3);
        Add(e, c4);
        Add(e, c5);
        Add(e, c6);
        Add(e, c7);
        Add(e, c8);
        Add(e, c9);
        Add(e, c10);
        Add(e, c11);
        Add(e, c12);
        Add(e, c13);
        Add(e, c14);
        Add(e, c15);
        return new(this, e);
    }

    public CommandHandle<TEntity> Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(
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
        var e = Created(_registry.CreateEntityNow());
        Add(e, c1);
        Add(e, c2);
        Add(e, c3);
        Add(e, c4);
        Add(e, c5);
        Add(e, c6);
        Add(e, c7);
        Add(e, c8);
        Add(e, c9);
        Add(e, c10);
        Add(e, c11);
        Add(e, c12);
        Add(e, c13);
        Add(e, c14);
        Add(e, c15);
        Add(e, c16);
        return new(this, e);
    }
}
