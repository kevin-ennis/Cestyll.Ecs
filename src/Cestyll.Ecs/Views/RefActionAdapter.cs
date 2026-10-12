namespace Cestyll.Ecs.Views;

// RefAction as an IEach, for the delegate overloads of Each.

internal readonly struct RefActionAdapter<TEntity, T1>(RefAction<TEntity, T1> action)
    : IEach<RefActionAdapter<TEntity, T1>, TEntity, T1>
    where TEntity : unmanaged, IEntity<TEntity>
    where T1 : IComponent
{
    public void Invoke(TEntity e, ref T1 c1) => action(e, ref c1);
}

internal readonly struct RefActionAdapter<TEntity, T1, T2>(RefAction<TEntity, T1, T2> action)
    : IEach<RefActionAdapter<TEntity, T1, T2>, TEntity, T1, T2>
    where TEntity : unmanaged, IEntity<TEntity>
    where T1 : IComponent
    where T2 : IComponent
{
    public void Invoke(TEntity e, ref T1 c1, ref T2 c2) => action(e, ref c1, ref c2);
}

internal readonly struct RefActionAdapter<TEntity, T1, T2, T3>(RefAction<TEntity, T1, T2, T3> action)
    : IEach<RefActionAdapter<TEntity, T1, T2, T3>, TEntity, T1, T2, T3>
    where TEntity : unmanaged, IEntity<TEntity>
    where T1 : IComponent
    where T2 : IComponent
    where T3 : IComponent
{
    public void Invoke(TEntity e, ref T1 c1, ref T2 c2, ref T3 c3) => action(e, ref c1, ref c2, ref c3);
}

internal readonly struct RefActionAdapter<TEntity, T1, T2, T3, T4>(RefAction<TEntity, T1, T2, T3, T4> action)
    : IEach<RefActionAdapter<TEntity, T1, T2, T3, T4>, TEntity, T1, T2, T3, T4>
    where TEntity : unmanaged, IEntity<TEntity>
    where T1 : IComponent
    where T2 : IComponent
    where T3 : IComponent
    where T4 : IComponent
{
    public void Invoke(TEntity e, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4) => action(e, ref c1, ref c2, ref c3, ref c4);
}

internal readonly struct RefActionAdapter<TEntity, T1, T2, T3, T4, T5>(RefAction<TEntity, T1, T2, T3, T4, T5> action)
    : IEach<RefActionAdapter<TEntity, T1, T2, T3, T4, T5>, TEntity, T1, T2, T3, T4, T5>
    where TEntity : unmanaged, IEntity<TEntity>
    where T1 : IComponent
    where T2 : IComponent
    where T3 : IComponent
    where T4 : IComponent
    where T5 : IComponent
{
    public void Invoke(TEntity e, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5) => action(e, ref c1, ref c2, ref c3, ref c4, ref c5);
}

internal readonly struct RefActionAdapter<TEntity, T1, T2, T3, T4, T5, T6>(RefAction<TEntity, T1, T2, T3, T4, T5, T6> action)
    : IEach<RefActionAdapter<TEntity, T1, T2, T3, T4, T5, T6>, TEntity, T1, T2, T3, T4, T5, T6>
    where TEntity : unmanaged, IEntity<TEntity>
    where T1 : IComponent
    where T2 : IComponent
    where T3 : IComponent
    where T4 : IComponent
    where T5 : IComponent
    where T6 : IComponent
{
    public void Invoke(TEntity e, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6) => action(e, ref c1, ref c2, ref c3, ref c4, ref c5, ref c6);
}

internal readonly struct RefActionAdapter<TEntity, T1, T2, T3, T4, T5, T6, T7>(RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7> action)
    : IEach<RefActionAdapter<TEntity, T1, T2, T3, T4, T5, T6, T7>, TEntity, T1, T2, T3, T4, T5, T6, T7>
    where TEntity : unmanaged, IEntity<TEntity>
    where T1 : IComponent
    where T2 : IComponent
    where T3 : IComponent
    where T4 : IComponent
    where T5 : IComponent
    where T6 : IComponent
    where T7 : IComponent
{
    public void Invoke(TEntity e, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6, ref T7 c7) => action(e, ref c1, ref c2, ref c3, ref c4, ref c5, ref c6, ref c7);
}

internal readonly struct RefActionAdapter<TEntity, T1, T2, T3, T4, T5, T6, T7, T8>(RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7, T8> action)
    : IEach<RefActionAdapter<TEntity, T1, T2, T3, T4, T5, T6, T7, T8>, TEntity, T1, T2, T3, T4, T5, T6, T7, T8>
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
    public void Invoke(TEntity e, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6, ref T7 c7, ref T8 c8) => action(e, ref c1, ref c2, ref c3, ref c4, ref c5, ref c6, ref c7, ref c8);
}

internal readonly struct RefActionAdapter<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9>(RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9> action)
    : IEach<RefActionAdapter<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9>, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9>
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
    public void Invoke(TEntity e, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6, ref T7 c7, ref T8 c8, ref T9 c9) => action(e, ref c1, ref c2, ref c3, ref c4, ref c5, ref c6, ref c7, ref c8, ref c9);
}

internal readonly struct RefActionAdapter<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> action)
    : IEach<RefActionAdapter<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
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
    public void Invoke(TEntity e, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6, ref T7 c7, ref T8 c8, ref T9 c9, ref T10 c10) => action(e, ref c1, ref c2, ref c3, ref c4, ref c5, ref c6, ref c7, ref c8, ref c9, ref c10);
}

internal readonly struct RefActionAdapter<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> action)
    : IEach<RefActionAdapter<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>
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
    public void Invoke(TEntity e, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6, ref T7 c7, ref T8 c8, ref T9 c9, ref T10 c10, ref T11 c11) => action(e, ref c1, ref c2, ref c3, ref c4, ref c5, ref c6, ref c7, ref c8, ref c9, ref c10, ref c11);
}

internal readonly struct RefActionAdapter<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> action)
    : IEach<RefActionAdapter<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>
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
    public void Invoke(TEntity e, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6, ref T7 c7, ref T8 c8, ref T9 c9, ref T10 c10, ref T11 c11, ref T12 c12) => action(e, ref c1, ref c2, ref c3, ref c4, ref c5, ref c6, ref c7, ref c8, ref c9, ref c10, ref c11, ref c12);
}

internal readonly struct RefActionAdapter<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> action)
    : IEach<RefActionAdapter<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>
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
    public void Invoke(TEntity e, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6, ref T7 c7, ref T8 c8, ref T9 c9, ref T10 c10, ref T11 c11, ref T12 c12, ref T13 c13) => action(e, ref c1, ref c2, ref c3, ref c4, ref c5, ref c6, ref c7, ref c8, ref c9, ref c10, ref c11, ref c12, ref c13);
}

internal readonly struct RefActionAdapter<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> action)
    : IEach<RefActionAdapter<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>
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
    public void Invoke(TEntity e, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6, ref T7 c7, ref T8 c8, ref T9 c9, ref T10 c10, ref T11 c11, ref T12 c12, ref T13 c13, ref T14 c14) => action(e, ref c1, ref c2, ref c3, ref c4, ref c5, ref c6, ref c7, ref c8, ref c9, ref c10, ref c11, ref c12, ref c13, ref c14);
}

internal readonly struct RefActionAdapter<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> action)
    : IEach<RefActionAdapter<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>
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
    public void Invoke(TEntity e, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6, ref T7 c7, ref T8 c8, ref T9 c9, ref T10 c10, ref T11 c11, ref T12 c12, ref T13 c13, ref T14 c14, ref T15 c15) => action(e, ref c1, ref c2, ref c3, ref c4, ref c5, ref c6, ref c7, ref c8, ref c9, ref c10, ref c11, ref c12, ref c13, ref c14, ref c15);
}

internal readonly struct RefActionAdapter<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> action)
    : IEach<RefActionAdapter<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>
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
    public void Invoke(TEntity e, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6, ref T7 c7, ref T8 c8, ref T9 c9, ref T10 c10, ref T11 c11, ref T12 c12, ref T13 c13, ref T14 c14, ref T15 c15, ref T16 c16) => action(e, ref c1, ref c2, ref c3, ref c4, ref c5, ref c6, ref c7, ref c8, ref c9, ref c10, ref c11, ref c12, ref c13, ref c14, ref c15, ref c16);
}
