namespace Cestyll.Ecs.Views;

// Each callbacks; the struct form inlines. Walks run last slot first, so the callback may edit,
// strip or destroy only the current entity. Adds, other entities, sorts and ShrinkToFit need
// Defer/AutoDefer (DEBUG catches some). Mid-walk tag changes may or may not be seen unless live.

// Lets Each(ref fn) find the callback's arity, which C# won't infer from a constraint.
public interface IEach<TSelf, TEntity>
    where TSelf : struct, IEach<TSelf, TEntity>
    where TEntity : unmanaged, IEntity<TEntity>
{
    static abstract void Run(in View<TEntity> view, ref TSelf fn);

    static abstract void Run(Groups.Group<TEntity> group, ref TSelf fn);
}

public interface IEach<TSelf, TEntity, T1> : IEach<TSelf, TEntity>
    where TSelf : struct, IEach<TSelf, TEntity, T1>
    where TEntity : unmanaged, IEntity<TEntity>
    where T1 : IComponent
{
    void Invoke(TEntity e, ref T1 c1);

    static void IEach<TSelf, TEntity>.Run(in View<TEntity> view, ref TSelf fn) => view.Each<TSelf, T1>(ref fn);

    static void IEach<TSelf, TEntity>.Run(Groups.Group<TEntity> group, ref TSelf fn) => group.Each<TSelf, T1>(ref fn);
}

public interface IEach<TSelf, TEntity, T1, T2> : IEach<TSelf, TEntity>
    where TSelf : struct, IEach<TSelf, TEntity, T1, T2>
    where TEntity : unmanaged, IEntity<TEntity>
    where T1 : IComponent
    where T2 : IComponent
{
    void Invoke(TEntity e, ref T1 c1, ref T2 c2);

    static void IEach<TSelf, TEntity>.Run(in View<TEntity> view, ref TSelf fn) => view.Each<TSelf, T1, T2>(ref fn);

    static void IEach<TSelf, TEntity>.Run(Groups.Group<TEntity> group, ref TSelf fn) => group.Each<TSelf, T1, T2>(ref fn);
}

public interface IEach<TSelf, TEntity, T1, T2, T3> : IEach<TSelf, TEntity>
    where TSelf : struct, IEach<TSelf, TEntity, T1, T2, T3>
    where TEntity : unmanaged, IEntity<TEntity>
    where T1 : IComponent
    where T2 : IComponent
    where T3 : IComponent
{
    void Invoke(TEntity e, ref T1 c1, ref T2 c2, ref T3 c3);

    static void IEach<TSelf, TEntity>.Run(in View<TEntity> view, ref TSelf fn) => view.Each<TSelf, T1, T2, T3>(ref fn);

    static void IEach<TSelf, TEntity>.Run(Groups.Group<TEntity> group, ref TSelf fn) => group.Each<TSelf, T1, T2, T3>(ref fn);
}

public interface IEach<TSelf, TEntity, T1, T2, T3, T4> : IEach<TSelf, TEntity>
    where TSelf : struct, IEach<TSelf, TEntity, T1, T2, T3, T4>
    where TEntity : unmanaged, IEntity<TEntity>
    where T1 : IComponent
    where T2 : IComponent
    where T3 : IComponent
    where T4 : IComponent
{
    void Invoke(TEntity e, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4);

    static void IEach<TSelf, TEntity>.Run(in View<TEntity> view, ref TSelf fn) => view.Each<TSelf, T1, T2, T3, T4>(ref fn);

    static void IEach<TSelf, TEntity>.Run(Groups.Group<TEntity> group, ref TSelf fn) => group.Each<TSelf, T1, T2, T3, T4>(ref fn);
}

public interface IEach<TSelf, TEntity, T1, T2, T3, T4, T5> : IEach<TSelf, TEntity>
    where TSelf : struct, IEach<TSelf, TEntity, T1, T2, T3, T4, T5>
    where TEntity : unmanaged, IEntity<TEntity>
    where T1 : IComponent
    where T2 : IComponent
    where T3 : IComponent
    where T4 : IComponent
    where T5 : IComponent
{
    void Invoke(TEntity e, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5);

    static void IEach<TSelf, TEntity>.Run(in View<TEntity> view, ref TSelf fn) => view.Each<TSelf, T1, T2, T3, T4, T5>(ref fn);

    static void IEach<TSelf, TEntity>.Run(Groups.Group<TEntity> group, ref TSelf fn) => group.Each<TSelf, T1, T2, T3, T4, T5>(ref fn);
}

public interface IEach<TSelf, TEntity, T1, T2, T3, T4, T5, T6> : IEach<TSelf, TEntity>
    where TSelf : struct, IEach<TSelf, TEntity, T1, T2, T3, T4, T5, T6>
    where TEntity : unmanaged, IEntity<TEntity>
    where T1 : IComponent
    where T2 : IComponent
    where T3 : IComponent
    where T4 : IComponent
    where T5 : IComponent
    where T6 : IComponent
{
    void Invoke(TEntity e, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6);

    static void IEach<TSelf, TEntity>.Run(in View<TEntity> view, ref TSelf fn) => view.Each<TSelf, T1, T2, T3, T4, T5, T6>(ref fn);

    static void IEach<TSelf, TEntity>.Run(Groups.Group<TEntity> group, ref TSelf fn) => group.Each<TSelf, T1, T2, T3, T4, T5, T6>(ref fn);
}

public interface IEach<TSelf, TEntity, T1, T2, T3, T4, T5, T6, T7> : IEach<TSelf, TEntity>
    where TSelf : struct, IEach<TSelf, TEntity, T1, T2, T3, T4, T5, T6, T7>
    where TEntity : unmanaged, IEntity<TEntity>
    where T1 : IComponent
    where T2 : IComponent
    where T3 : IComponent
    where T4 : IComponent
    where T5 : IComponent
    where T6 : IComponent
    where T7 : IComponent
{
    void Invoke(TEntity e, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6, ref T7 c7);

    static void IEach<TSelf, TEntity>.Run(in View<TEntity> view, ref TSelf fn) => view.Each<TSelf, T1, T2, T3, T4, T5, T6, T7>(ref fn);

    static void IEach<TSelf, TEntity>.Run(Groups.Group<TEntity> group, ref TSelf fn) => group.Each<TSelf, T1, T2, T3, T4, T5, T6, T7>(ref fn);
}

public interface IEach<TSelf, TEntity, T1, T2, T3, T4, T5, T6, T7, T8> : IEach<TSelf, TEntity>
    where TSelf : struct, IEach<TSelf, TEntity, T1, T2, T3, T4, T5, T6, T7, T8>
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
    void Invoke(TEntity e, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6, ref T7 c7, ref T8 c8);

    static void IEach<TSelf, TEntity>.Run(in View<TEntity> view, ref TSelf fn) => view.Each<TSelf, T1, T2, T3, T4, T5, T6, T7, T8>(ref fn);

    static void IEach<TSelf, TEntity>.Run(Groups.Group<TEntity> group, ref TSelf fn) => group.Each<TSelf, T1, T2, T3, T4, T5, T6, T7, T8>(ref fn);
}

public interface IEach<TSelf, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9> : IEach<TSelf, TEntity>
    where TSelf : struct, IEach<TSelf, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9>
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
    void Invoke(TEntity e, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6, ref T7 c7, ref T8 c8, ref T9 c9);

    static void IEach<TSelf, TEntity>.Run(in View<TEntity> view, ref TSelf fn) => view.Each<TSelf, T1, T2, T3, T4, T5, T6, T7, T8, T9>(ref fn);

    static void IEach<TSelf, TEntity>.Run(Groups.Group<TEntity> group, ref TSelf fn) => group.Each<TSelf, T1, T2, T3, T4, T5, T6, T7, T8, T9>(ref fn);
}

public interface IEach<TSelf, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : IEach<TSelf, TEntity>
    where TSelf : struct, IEach<TSelf, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
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
    void Invoke(TEntity e, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6, ref T7 c7, ref T8 c8, ref T9 c9, ref T10 c10);

    static void IEach<TSelf, TEntity>.Run(in View<TEntity> view, ref TSelf fn) => view.Each<TSelf, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(ref fn);

    static void IEach<TSelf, TEntity>.Run(Groups.Group<TEntity> group, ref TSelf fn) => group.Each<TSelf, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(ref fn);
}

public interface IEach<TSelf, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> : IEach<TSelf, TEntity>
    where TSelf : struct, IEach<TSelf, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>
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
    void Invoke(TEntity e, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6, ref T7 c7, ref T8 c8, ref T9 c9, ref T10 c10, ref T11 c11);

    static void IEach<TSelf, TEntity>.Run(in View<TEntity> view, ref TSelf fn) => view.Each<TSelf, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(ref fn);

    static void IEach<TSelf, TEntity>.Run(Groups.Group<TEntity> group, ref TSelf fn) => group.Each<TSelf, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(ref fn);
}

public interface IEach<TSelf, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> : IEach<TSelf, TEntity>
    where TSelf : struct, IEach<TSelf, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>
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
    void Invoke(TEntity e, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6, ref T7 c7, ref T8 c8, ref T9 c9, ref T10 c10, ref T11 c11, ref T12 c12);

    static void IEach<TSelf, TEntity>.Run(in View<TEntity> view, ref TSelf fn) => view.Each<TSelf, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(ref fn);

    static void IEach<TSelf, TEntity>.Run(Groups.Group<TEntity> group, ref TSelf fn) => group.Each<TSelf, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(ref fn);
}

public interface IEach<TSelf, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> : IEach<TSelf, TEntity>
    where TSelf : struct, IEach<TSelf, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>
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
    void Invoke(TEntity e, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6, ref T7 c7, ref T8 c8, ref T9 c9, ref T10 c10, ref T11 c11, ref T12 c12, ref T13 c13);

    static void IEach<TSelf, TEntity>.Run(in View<TEntity> view, ref TSelf fn) => view.Each<TSelf, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(ref fn);

    static void IEach<TSelf, TEntity>.Run(Groups.Group<TEntity> group, ref TSelf fn) => group.Each<TSelf, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(ref fn);
}

public interface IEach<TSelf, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> : IEach<TSelf, TEntity>
    where TSelf : struct, IEach<TSelf, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>
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
    void Invoke(TEntity e, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6, ref T7 c7, ref T8 c8, ref T9 c9, ref T10 c10, ref T11 c11, ref T12 c12, ref T13 c13, ref T14 c14);

    static void IEach<TSelf, TEntity>.Run(in View<TEntity> view, ref TSelf fn) => view.Each<TSelf, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(ref fn);

    static void IEach<TSelf, TEntity>.Run(Groups.Group<TEntity> group, ref TSelf fn) => group.Each<TSelf, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(ref fn);
}

public interface IEach<TSelf, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> : IEach<TSelf, TEntity>
    where TSelf : struct, IEach<TSelf, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>
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
    void Invoke(TEntity e, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6, ref T7 c7, ref T8 c8, ref T9 c9, ref T10 c10, ref T11 c11, ref T12 c12, ref T13 c13, ref T14 c14, ref T15 c15);

    static void IEach<TSelf, TEntity>.Run(in View<TEntity> view, ref TSelf fn) => view.Each<TSelf, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(ref fn);

    static void IEach<TSelf, TEntity>.Run(Groups.Group<TEntity> group, ref TSelf fn) => group.Each<TSelf, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(ref fn);
}

public interface IEach<TSelf, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> : IEach<TSelf, TEntity>
    where TSelf : struct, IEach<TSelf, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>
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
    void Invoke(TEntity e, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6, ref T7 c7, ref T8 c8, ref T9 c9, ref T10 c10, ref T11 c11, ref T12 c12, ref T13 c13, ref T14 c14, ref T15 c15, ref T16 c16);

    static void IEach<TSelf, TEntity>.Run(in View<TEntity> view, ref TSelf fn) => view.Each<TSelf, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(ref fn);

    static void IEach<TSelf, TEntity>.Run(Groups.Group<TEntity> group, ref TSelf fn) => group.Each<TSelf, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(ref fn);
}
