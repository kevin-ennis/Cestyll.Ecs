namespace Cestyll.Ecs;

public delegate void RefAction<T>(ref T value);

// incoming is an argument so the callback can be a static lambda.
public delegate void MergeAction<T>(ref T current, in T incoming);

// Each callbacks: the entity, then a ref per fetched component.
public delegate void RefAction<TEntity, T1>(TEntity e, ref T1 c1);

public delegate void RefAction<TEntity, T1, T2>(TEntity e, ref T1 c1, ref T2 c2);

public delegate void RefAction<TEntity, T1, T2, T3>(TEntity e, ref T1 c1, ref T2 c2, ref T3 c3);

public delegate void RefAction<TEntity, T1, T2, T3, T4>(TEntity e, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4);

public delegate void RefAction<TEntity, T1, T2, T3, T4, T5>(
    TEntity e, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5);

public delegate void RefAction<TEntity, T1, T2, T3, T4, T5, T6>(
    TEntity e, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6);

public delegate void RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7>(
    TEntity e, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6, ref T7 c7);

public delegate void RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7, T8>(
    TEntity e, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6, ref T7 c7, ref T8 c8);

public delegate void RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9>(
    TEntity e, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6, ref T7 c7, ref T8 c8, ref T9 c9);

public delegate void RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(
    TEntity e, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6, ref T7 c7, ref T8 c8, ref T9 c9, ref T10 c10);

public delegate void RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(
    TEntity e, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6, ref T7 c7, ref T8 c8, ref T9 c9, ref T10 c10, ref T11 c11);

public delegate void RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(
    TEntity e, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6, ref T7 c7, ref T8 c8, ref T9 c9, ref T10 c10, ref T11 c11, ref T12 c12);

public delegate void RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(
    TEntity e, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6, ref T7 c7, ref T8 c8, ref T9 c9, ref T10 c10, ref T11 c11, ref T12 c12, ref T13 c13);

public delegate void RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(
    TEntity e, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6, ref T7 c7, ref T8 c8, ref T9 c9, ref T10 c10, ref T11 c11, ref T12 c12, ref T13 c13, ref T14 c14);

public delegate void RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(
    TEntity e, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6, ref T7 c7, ref T8 c8, ref T9 c9, ref T10 c10, ref T11 c11, ref T12 c12, ref T13 c13, ref T14 c14, ref T15 c15);

public delegate void RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(
    TEntity e, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6, ref T7 c7, ref T8 c8, ref T9 c9, ref T10 c10, ref T11 c11, ref T12 c12, ref T13 c13, ref T14 c14, ref T15 c15, ref T16 c16);
