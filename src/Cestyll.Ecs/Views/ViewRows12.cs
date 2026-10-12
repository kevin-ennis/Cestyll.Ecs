using Cestyll.Ecs.Collections;
using Cestyll.Ecs.Storage;

namespace Cestyll.Ecs.Views;

// View().Rows: Each and foreach with refs to the components.
public readonly struct ViewRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>
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
    private readonly ViewQuery<TEntity> _query;
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

    internal ViewRows(
        ViewQuery<TEntity> query, Pool<TEntity, T1> p1, Pool<TEntity, T2> p2, Pool<TEntity, T3> p3, Pool<TEntity, T4> p4, Pool<TEntity, T5> p5, Pool<TEntity, T6> p6, Pool<TEntity, T7> p7, Pool<TEntity, T8> p8, Pool<TEntity, T9> p9, Pool<TEntity, T10> p10, Pool<TEntity, T11> p11, Pool<TEntity, T12> p12)
    {
        _query = query;
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

    public void Each<TFn>(ref TFn fn) where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>
    {
        var walker = new Walker<TFn>(this, ref fn);
        _query.Run(ref walker, _p1, _p2, _p3, _p4, _p5, _p6, _p7, _p8, _p9, _p10, _p11, _p12);
    }

    public void Each(RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> action)
    {
        var adapter = new RefActionAdapter<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(action);
        Each(ref adapter);
    }

    // foreach: same entities and rules as Each, which with a struct callback is a little faster.
    public Enumerator GetEnumerator() => new(this);

    public readonly ref struct Row
    {
        public readonly TEntity Entity;
        public readonly ref T1 C1;
        public readonly ref T2 C2;
        public readonly ref T3 C3;
        public readonly ref T4 C4;
        public readonly ref T5 C5;
        public readonly ref T6 C6;
        public readonly ref T7 C7;
        public readonly ref T8 C8;
        public readonly ref T9 C9;
        public readonly ref T10 C10;
        public readonly ref T11 C11;
        public readonly ref T12 C12;

        internal Row(TEntity entity, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6, ref T7 c7, ref T8 c8, ref T9 c9, ref T10 c10, ref T11 c11, ref T12 c12)
        {
            Entity = entity;
            C1 = ref c1;
            C2 = ref c2;
            C3 = ref c3;
            C4 = ref c4;
            C5 = ref c5;
            C6 = ref c6;
            C7 = ref c7;
            C8 = ref c8;
            C9 = ref c9;
            C10 = ref c10;
            C11 = ref c11;
            C12 = ref c12;
        }

        // By type rather than position; costs a field read. With two of T (a join), the first.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Get<T>()
            where T : IComponent
        {
            if (typeof(T) == typeof(T1))
            {
                return ref Unsafe.As<T1, T>(ref C1);
            }

            if (typeof(T) == typeof(T2))
            {
                return ref Unsafe.As<T2, T>(ref C2);
            }

            if (typeof(T) == typeof(T3))
            {
                return ref Unsafe.As<T3, T>(ref C3);
            }

            if (typeof(T) == typeof(T4))
            {
                return ref Unsafe.As<T4, T>(ref C4);
            }

            if (typeof(T) == typeof(T5))
            {
                return ref Unsafe.As<T5, T>(ref C5);
            }

            if (typeof(T) == typeof(T6))
            {
                return ref Unsafe.As<T6, T>(ref C6);
            }

            if (typeof(T) == typeof(T7))
            {
                return ref Unsafe.As<T7, T>(ref C7);
            }

            if (typeof(T) == typeof(T8))
            {
                return ref Unsafe.As<T8, T>(ref C8);
            }

            if (typeof(T) == typeof(T9))
            {
                return ref Unsafe.As<T9, T>(ref C9);
            }

            if (typeof(T) == typeof(T10))
            {
                return ref Unsafe.As<T10, T>(ref C10);
            }

            if (typeof(T) == typeof(T11))
            {
                return ref Unsafe.As<T11, T>(ref C11);
            }

            if (typeof(T) == typeof(T12))
            {
                return ref Unsafe.As<T12, T>(ref C12);
            }

            throw new InvalidOperationException($"This view has no {typeof(T).Name}");
        }
    }

    public ref struct Enumerator
    {
        private readonly Walk _w;
        private int _slot; // the current entity's, in the driving set
        // Slots live in _w.J1 to _w.J12, so the JIT promotes the two fields in full.

        internal Enumerator(ViewRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> view)
        {
            var w = Walk.Rent();
            w.Bind(view._query, [view._p1, view._p2, view._p3, view._p4, view._p5, view._p6, view._p7, view._p8, view._p9, view._p10, view._p11, view._p12]);
            w.S1 = view._p1.SparseArray;
            w.S2 = view._p2.SparseArray;
            w.S3 = view._p3.SparseArray;
            w.S4 = view._p4.SparseArray;
            w.S5 = view._p5.SparseArray;
            w.S6 = view._p6.SparseArray;
            w.S7 = view._p7.SparseArray;
            w.S8 = view._p8.SparseArray;
            w.S9 = view._p9.SparseArray;
            w.S10 = view._p10.SparseArray;
            w.S11 = view._p11.SparseArray;
            w.S12 = view._p12.SparseArray;
            w.D1 = view._p1.DataArray;
            w.D2 = view._p2.DataArray;
            w.D3 = view._p3.DataArray;
            w.D4 = view._p4.DataArray;
            w.D5 = view._p5.DataArray;
            w.D6 = view._p6.DataArray;
            w.D7 = view._p7.DataArray;
            w.D8 = view._p8.DataArray;
            w.D9 = view._p9.DataArray;
            w.D10 = view._p10.DataArray;
            w.D11 = view._p11.DataArray;
            w.D12 = view._p12.DataArray;
            _w = w;
            _slot = w.Count;
        }

        public readonly Row Current
        {
            get
            {
                var w = _w;
                return new(w.Packed[_slot], ref w.D1[w.J1], ref w.D2[w.J2], ref w.D3[w.J3], ref w.D4[w.J4], ref w.D5[w.J5], ref w.D6[w.J6], ref w.D7[w.J7], ref w.D8[w.J8], ref w.D9[w.J9], ref w.D10[w.J10], ref w.D11[w.J11], ref w.D12[w.J12]);
            }
        }

        // Locals, not fields, so a step that skips an entity writes nothing back.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var w = _w;
            var d = w.Driver;
            var packed = w.Packed;
            var s1 = w.S1;
            var s2 = w.S2;
            var s3 = w.S3;
            var s4 = w.S4;
            var s5 = w.S5;
            var s6 = w.S6;
            var s7 = w.S7;
            var s8 = w.S8;
            var s9 = w.S9;
            var s10 = w.S10;
            var s11 = w.S11;
            var s12 = w.S12;
            var slot = _slot;
            while (true)
            {
                if (w.Simple)
                {
                    slot--;
                    if (slot < 0)
                    {
                        _slot = slot;
                        return false;
                    }
                }
                else
                {
                    slot = w.Next();
                    if (slot < 0)
                    {
                        return false;
                    }
                }

                var e = packed[slot];
                var j1 = d == 0 ? slot : SparseSet<TEntity>.Slot(s1, e.Index);
                if (j1 < 0)
                {
                    continue;
                }

                var j2 = d == 1 ? slot : SparseSet<TEntity>.Slot(s2, e.Index);
                if (j2 < 0)
                {
                    continue;
                }

                var j3 = d == 2 ? slot : SparseSet<TEntity>.Slot(s3, e.Index);
                if (j3 < 0)
                {
                    continue;
                }

                var j4 = d == 3 ? slot : SparseSet<TEntity>.Slot(s4, e.Index);
                if (j4 < 0)
                {
                    continue;
                }

                var j5 = d == 4 ? slot : SparseSet<TEntity>.Slot(s5, e.Index);
                if (j5 < 0)
                {
                    continue;
                }

                var j6 = d == 5 ? slot : SparseSet<TEntity>.Slot(s6, e.Index);
                if (j6 < 0)
                {
                    continue;
                }

                var j7 = d == 6 ? slot : SparseSet<TEntity>.Slot(s7, e.Index);
                if (j7 < 0)
                {
                    continue;
                }

                var j8 = d == 7 ? slot : SparseSet<TEntity>.Slot(s8, e.Index);
                if (j8 < 0)
                {
                    continue;
                }

                var j9 = d == 8 ? slot : SparseSet<TEntity>.Slot(s9, e.Index);
                if (j9 < 0)
                {
                    continue;
                }

                var j10 = d == 9 ? slot : SparseSet<TEntity>.Slot(s10, e.Index);
                if (j10 < 0)
                {
                    continue;
                }

                var j11 = d == 10 ? slot : SparseSet<TEntity>.Slot(s11, e.Index);
                if (j11 < 0)
                {
                    continue;
                }

                var j12 = d == 11 ? slot : SparseSet<TEntity>.Slot(s12, e.Index);
                if (j12 < 0)
                {
                    continue;
                }

                _slot = slot;
                w.J1 = j1;
                w.J2 = j2;
                w.J3 = j3;
                w.J4 = j4;
                w.J5 = j5;
                w.J6 = j6;
                w.J7 = j7;
                w.J8 = j8;
                w.J9 = j9;
                w.J10 = j10;
                w.J11 = j11;
                w.J12 = j12;
                return true;
            }
        }

        public void Dispose() => _w.Return();
    }

    // Pooled walk state, one spare per thread; a nested foreach allocates its own.
    private sealed class Walk : ViewCursor<TEntity>
    {
        [ThreadStatic]
        private static Walk? _spare;

        public TEntity[] S1 = [];
        public TEntity[] S2 = [];
        public TEntity[] S3 = [];
        public TEntity[] S4 = [];
        public TEntity[] S5 = [];
        public TEntity[] S6 = [];
        public TEntity[] S7 = [];
        public TEntity[] S8 = [];
        public TEntity[] S9 = [];
        public TEntity[] S10 = [];
        public TEntity[] S11 = [];
        public TEntity[] S12 = [];
        public T1[] D1 = [];
        public T2[] D2 = [];
        public T3[] D3 = [];
        public T4[] D4 = [];
        public T5[] D5 = [];
        public T6[] D6 = [];
        public T7[] D7 = [];
        public T8[] D8 = [];
        public T9[] D9 = [];
        public T10[] D10 = [];
        public T11[] D11 = [];
        public T12[] D12 = [];

        public static Walk Rent()
        {
            var w = _spare;
            if (w is null)
            {
                return new();
            }

            _spare = null;
            return w;
        }

        // Out of line: runs once per foreach, from the loop's finally.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Return()
        {
            Dispose();
            S1 = [];
            S2 = [];
            S3 = [];
            S4 = [];
            S5 = [];
            S6 = [];
            S7 = [];
            S8 = [];
            S9 = [];
            S10 = [];
            S11 = [];
            S12 = [];
            D1 = [];
            D2 = [];
            D3 = [];
            D4 = [];
            D5 = [];
            D6 = [];
            D7 = [];
            D8 = [];
            D9 = [];
            D10 = [];
            D11 = [];
            D12 = [];
            _spare = this;
        }
    }

    private readonly ref struct Walker<TFn> : IViewWalker<TEntity>
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>
    {
        private readonly ViewRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> _view;
        private readonly ref TFn _fn;

        public Walker(ViewRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> view, ref TFn fn)
        {
            _view = view;
            _fn = ref fn;
        }

        public int MinCount => Math.Min(_view._p1.Count, Math.Min(_view._p2.Count, Math.Min(_view._p3.Count, Math.Min(_view._p4.Count, Math.Min(_view._p5.Count, Math.Min(_view._p6.Count, Math.Min(_view._p7.Count, Math.Min(_view._p8.Count, Math.Min(_view._p9.Count, Math.Min(_view._p10.Count, Math.Min(_view._p11.Count, _view._p12.Count)))))))))));

        // The pinned pool, else the smallest, first on ties.
        private int Driver()
        {
            if (_view._query.Driver >= 0)
            {
                return _view._query.Driver;
            }

            if (_view._query.Driven)
            {
                return -1;
            }

            var d = 0;
            var min = _view._p1.Count;
            if (_view._p2.Count < min)
            {
                d = 1;
                min = _view._p2.Count;
            }

            if (_view._p3.Count < min)
            {
                d = 2;
                min = _view._p3.Count;
            }

            if (_view._p4.Count < min)
            {
                d = 3;
                min = _view._p4.Count;
            }

            if (_view._p5.Count < min)
            {
                d = 4;
                min = _view._p5.Count;
            }

            if (_view._p6.Count < min)
            {
                d = 5;
                min = _view._p6.Count;
            }

            if (_view._p7.Count < min)
            {
                d = 6;
                min = _view._p7.Count;
            }

            if (_view._p8.Count < min)
            {
                d = 7;
                min = _view._p8.Count;
            }

            if (_view._p9.Count < min)
            {
                d = 8;
                min = _view._p9.Count;
            }

            if (_view._p10.Count < min)
            {
                d = 9;
                min = _view._p10.Count;
            }

            if (_view._p11.Count < min)
            {
                d = 10;
                min = _view._p11.Count;
            }

            if (_view._p12.Count < min)
            {
                d = 11;
                min = _view._p12.Count;
            }

            return d;
        }

        public void Unfiltered(ReadOnlySpan<TEntity> driven)
        {
            switch (Driver())
            {
                case -1:
                    Unfiltered<DriveNone>(driven);
                    return;
                case 0:
                    Unfiltered<Drive1>(_view._p1.Entities);
                    return;
                case 1:
                    Unfiltered<Drive2>(_view._p2.Entities);
                    return;
                case 2:
                    Unfiltered<Drive3>(_view._p3.Entities);
                    return;
                case 3:
                    Unfiltered<Drive4>(_view._p4.Entities);
                    return;
                case 4:
                    Unfiltered<Drive5>(_view._p5.Entities);
                    return;
                case 5:
                    Unfiltered<Drive6>(_view._p6.Entities);
                    return;
                case 6:
                    Unfiltered<Drive7>(_view._p7.Entities);
                    return;
                case 7:
                    Unfiltered<Drive8>(_view._p8.Entities);
                    return;
                case 8:
                    Unfiltered<Drive9>(_view._p9.Entities);
                    return;
                case 9:
                    Unfiltered<Drive10>(_view._p10.Entities);
                    return;
                case 10:
                    Unfiltered<Drive11>(_view._p11.Entities);
                    return;
                default:
                    Unfiltered<Drive12>(_view._p12.Entities);
                    return;
            }
        }

        public void PoolDriven(
            ReadOnlySpan<TEntity> driven, ReadOnlySpan<ulong> mask, MaskMode mode, ViewFilters<TEntity> filters,
            bool componentFilters)
        {
            switch (Driver())
            {
                case -1:
                    PoolDriven<DriveNone>(driven, mask, mode, filters, componentFilters);
                    return;
                case 0:
                    PoolDriven<Drive1>(_view._p1.Entities, mask, mode, filters, componentFilters);
                    return;
                case 1:
                    PoolDriven<Drive2>(_view._p2.Entities, mask, mode, filters, componentFilters);
                    return;
                case 2:
                    PoolDriven<Drive3>(_view._p3.Entities, mask, mode, filters, componentFilters);
                    return;
                case 3:
                    PoolDriven<Drive4>(_view._p4.Entities, mask, mode, filters, componentFilters);
                    return;
                case 4:
                    PoolDriven<Drive5>(_view._p5.Entities, mask, mode, filters, componentFilters);
                    return;
                case 5:
                    PoolDriven<Drive6>(_view._p6.Entities, mask, mode, filters, componentFilters);
                    return;
                case 6:
                    PoolDriven<Drive7>(_view._p7.Entities, mask, mode, filters, componentFilters);
                    return;
                case 7:
                    PoolDriven<Drive8>(_view._p8.Entities, mask, mode, filters, componentFilters);
                    return;
                case 8:
                    PoolDriven<Drive9>(_view._p9.Entities, mask, mode, filters, componentFilters);
                    return;
                case 9:
                    PoolDriven<Drive10>(_view._p10.Entities, mask, mode, filters, componentFilters);
                    return;
                case 10:
                    PoolDriven<Drive11>(_view._p11.Entities, mask, mode, filters, componentFilters);
                    return;
                default:
                    PoolDriven<Drive12>(_view._p12.Entities, mask, mode, filters, componentFilters);
                    return;
            }
        }

        // typeof tests fold per TDrive. Out of line: PGO inlining it into the switch ran 2-3x slower.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Unfiltered<TDrive>(ReadOnlySpan<TEntity> ents) where TDrive : struct
        {
            ref var fn = ref _fn;
            var s1 = _view._p1.SparseSpan;
            var c1 = _view._p1.Components;
            var s2 = _view._p2.SparseSpan;
            var c2 = _view._p2.Components;
            var s3 = _view._p3.SparseSpan;
            var c3 = _view._p3.Components;
            var s4 = _view._p4.SparseSpan;
            var c4 = _view._p4.Components;
            var s5 = _view._p5.SparseSpan;
            var c5 = _view._p5.Components;
            var s6 = _view._p6.SparseSpan;
            var c6 = _view._p6.Components;
            var s7 = _view._p7.SparseSpan;
            var c7 = _view._p7.Components;
            var s8 = _view._p8.SparseSpan;
            var c8 = _view._p8.Components;
            var s9 = _view._p9.SparseSpan;
            var c9 = _view._p9.Components;
            var s10 = _view._p10.SparseSpan;
            var c10 = _view._p10.Components;
            var s11 = _view._p11.SparseSpan;
            var c11 = _view._p11.Components;
            var s12 = _view._p12.SparseSpan;
            var c12 = _view._p12.Components;
            for (var i = ents.Length - 1; i >= 0; i--)
            {
                var e = ents[i];
                var j1 = typeof(TDrive) == typeof(Drive1) ? i : SparseSet<TEntity>.Slot(s1, e.Index);
                if ((uint)j1 >= (uint)c1.Length)
                {
                    continue;
                }

                var j2 = typeof(TDrive) == typeof(Drive2) ? i : SparseSet<TEntity>.Slot(s2, e.Index);
                if ((uint)j2 >= (uint)c2.Length)
                {
                    continue;
                }

                var j3 = typeof(TDrive) == typeof(Drive3) ? i : SparseSet<TEntity>.Slot(s3, e.Index);
                if ((uint)j3 >= (uint)c3.Length)
                {
                    continue;
                }

                var j4 = typeof(TDrive) == typeof(Drive4) ? i : SparseSet<TEntity>.Slot(s4, e.Index);
                if ((uint)j4 >= (uint)c4.Length)
                {
                    continue;
                }

                var j5 = typeof(TDrive) == typeof(Drive5) ? i : SparseSet<TEntity>.Slot(s5, e.Index);
                if ((uint)j5 >= (uint)c5.Length)
                {
                    continue;
                }

                var j6 = typeof(TDrive) == typeof(Drive6) ? i : SparseSet<TEntity>.Slot(s6, e.Index);
                if ((uint)j6 >= (uint)c6.Length)
                {
                    continue;
                }

                var j7 = typeof(TDrive) == typeof(Drive7) ? i : SparseSet<TEntity>.Slot(s7, e.Index);
                if ((uint)j7 >= (uint)c7.Length)
                {
                    continue;
                }

                var j8 = typeof(TDrive) == typeof(Drive8) ? i : SparseSet<TEntity>.Slot(s8, e.Index);
                if ((uint)j8 >= (uint)c8.Length)
                {
                    continue;
                }

                var j9 = typeof(TDrive) == typeof(Drive9) ? i : SparseSet<TEntity>.Slot(s9, e.Index);
                if ((uint)j9 >= (uint)c9.Length)
                {
                    continue;
                }

                var j10 = typeof(TDrive) == typeof(Drive10) ? i : SparseSet<TEntity>.Slot(s10, e.Index);
                if ((uint)j10 >= (uint)c10.Length)
                {
                    continue;
                }

                var j11 = typeof(TDrive) == typeof(Drive11) ? i : SparseSet<TEntity>.Slot(s11, e.Index);
                if ((uint)j11 >= (uint)c11.Length)
                {
                    continue;
                }

                var j12 = typeof(TDrive) == typeof(Drive12) ? i : SparseSet<TEntity>.Slot(s12, e.Index);
                if ((uint)j12 >= (uint)c12.Length)
                {
                    continue;
                }

                fn.Invoke(e, ref c1[j1], ref c2[j2], ref c3[j3], ref c4[j4], ref c5[j5], ref c6[j6], ref c7[j7], ref c8[j8], ref c9[j9], ref c10[j10], ref c11[j11], ref c12[j12]);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void PoolDriven<TDrive>(
            ReadOnlySpan<TEntity> ents, ReadOnlySpan<ulong> mask, MaskMode mode, ViewFilters<TEntity> filters,
            bool componentFilters)
            where TDrive : struct
        {
            ref var fn = ref _fn;
            var include = mode == MaskMode.Include;
            var masked = mode != MaskMode.None;
            var s1 = _view._p1.SparseSpan;
            var c1 = _view._p1.Components;
            var s2 = _view._p2.SparseSpan;
            var c2 = _view._p2.Components;
            var s3 = _view._p3.SparseSpan;
            var c3 = _view._p3.Components;
            var s4 = _view._p4.SparseSpan;
            var c4 = _view._p4.Components;
            var s5 = _view._p5.SparseSpan;
            var c5 = _view._p5.Components;
            var s6 = _view._p6.SparseSpan;
            var c6 = _view._p6.Components;
            var s7 = _view._p7.SparseSpan;
            var c7 = _view._p7.Components;
            var s8 = _view._p8.SparseSpan;
            var c8 = _view._p8.Components;
            var s9 = _view._p9.SparseSpan;
            var c9 = _view._p9.Components;
            var s10 = _view._p10.SparseSpan;
            var c10 = _view._p10.Components;
            var s11 = _view._p11.SparseSpan;
            var c11 = _view._p11.Components;
            var s12 = _view._p12.SparseSpan;
            var c12 = _view._p12.Components;
            for (var i = ents.Length - 1; i >= 0; i--)
            {
                var e = ents[i];
                if (masked && WordOps.Test(mask, e.Index) != include)
                {
                    continue;
                }

                var j1 = typeof(TDrive) == typeof(Drive1) ? i : SparseSet<TEntity>.Slot(s1, e.Index);
                if ((uint)j1 >= (uint)c1.Length)
                {
                    continue;
                }

                var j2 = typeof(TDrive) == typeof(Drive2) ? i : SparseSet<TEntity>.Slot(s2, e.Index);
                if ((uint)j2 >= (uint)c2.Length)
                {
                    continue;
                }

                var j3 = typeof(TDrive) == typeof(Drive3) ? i : SparseSet<TEntity>.Slot(s3, e.Index);
                if ((uint)j3 >= (uint)c3.Length)
                {
                    continue;
                }

                var j4 = typeof(TDrive) == typeof(Drive4) ? i : SparseSet<TEntity>.Slot(s4, e.Index);
                if ((uint)j4 >= (uint)c4.Length)
                {
                    continue;
                }

                var j5 = typeof(TDrive) == typeof(Drive5) ? i : SparseSet<TEntity>.Slot(s5, e.Index);
                if ((uint)j5 >= (uint)c5.Length)
                {
                    continue;
                }

                var j6 = typeof(TDrive) == typeof(Drive6) ? i : SparseSet<TEntity>.Slot(s6, e.Index);
                if ((uint)j6 >= (uint)c6.Length)
                {
                    continue;
                }

                var j7 = typeof(TDrive) == typeof(Drive7) ? i : SparseSet<TEntity>.Slot(s7, e.Index);
                if ((uint)j7 >= (uint)c7.Length)
                {
                    continue;
                }

                var j8 = typeof(TDrive) == typeof(Drive8) ? i : SparseSet<TEntity>.Slot(s8, e.Index);
                if ((uint)j8 >= (uint)c8.Length)
                {
                    continue;
                }

                var j9 = typeof(TDrive) == typeof(Drive9) ? i : SparseSet<TEntity>.Slot(s9, e.Index);
                if ((uint)j9 >= (uint)c9.Length)
                {
                    continue;
                }

                var j10 = typeof(TDrive) == typeof(Drive10) ? i : SparseSet<TEntity>.Slot(s10, e.Index);
                if ((uint)j10 >= (uint)c10.Length)
                {
                    continue;
                }

                var j11 = typeof(TDrive) == typeof(Drive11) ? i : SparseSet<TEntity>.Slot(s11, e.Index);
                if ((uint)j11 >= (uint)c11.Length)
                {
                    continue;
                }

                var j12 = typeof(TDrive) == typeof(Drive12) ? i : SparseSet<TEntity>.Slot(s12, e.Index);
                if ((uint)j12 >= (uint)c12.Length)
                {
                    continue;
                }

                if (!componentFilters || filters.PassComponents(e))
                {
                    fn.Invoke(e, ref c1[j1], ref c2[j2], ref c3[j3], ref c4[j4], ref c5[j5], ref c6[j6], ref c7[j7], ref c8[j8], ref c9[j9], ref c10[j10], ref c11[j11], ref c12[j12]);
                }
            }
        }

        public void Masked(ReadOnlySpan<ulong> mask, ViewFilters<TEntity> filters, bool componentFilters)
        {
            ref var fn = ref _fn;
            var ents = _view._p1.Entities;
            var s1 = _view._p1.SparseSpan;
            var c1 = _view._p1.Components;
            var s2 = _view._p2.SparseSpan;
            var c2 = _view._p2.Components;
            var s3 = _view._p3.SparseSpan;
            var c3 = _view._p3.Components;
            var s4 = _view._p4.SparseSpan;
            var c4 = _view._p4.Components;
            var s5 = _view._p5.SparseSpan;
            var c5 = _view._p5.Components;
            var s6 = _view._p6.SparseSpan;
            var c6 = _view._p6.Components;
            var s7 = _view._p7.SparseSpan;
            var c7 = _view._p7.Components;
            var s8 = _view._p8.SparseSpan;
            var c8 = _view._p8.Components;
            var s9 = _view._p9.SparseSpan;
            var c9 = _view._p9.Components;
            var s10 = _view._p10.SparseSpan;
            var c10 = _view._p10.Components;
            var s11 = _view._p11.SparseSpan;
            var c11 = _view._p11.Components;
            var s12 = _view._p12.SparseSpan;
            var c12 = _view._p12.Components;

            // Words past the pools' sparse arrays only miss. The last word can be partial.
            var wordEnd = Math.Min(mask.Length, (Math.Min(s1.Length, Math.Min(s2.Length, Math.Min(s3.Length, Math.Min(s4.Length, Math.Min(s5.Length, Math.Min(s6.Length, Math.Min(s7.Length, Math.Min(s8.Length, Math.Min(s9.Length, Math.Min(s10.Length, Math.Min(s11.Length, s12.Length))))))))))) + 63) >> 6);
            for (var w = 0; w < wordEnd; w++)
            {
                var bits = mask[w];
                while (bits != 0)
                {
                    var index = (w << 6) + BitOperations.TrailingZeroCount(bits);
                    bits &= bits - 1;

                    var j1 = SparseSet<TEntity>.Slot(s1, index);
                    if ((uint)j1 >= (uint)c1.Length)
                    {
                        continue;
                    }

                    var j2 = SparseSet<TEntity>.Slot(s2, index);
                    if ((uint)j2 >= (uint)c2.Length)
                    {
                        continue;
                    }

                    var j3 = SparseSet<TEntity>.Slot(s3, index);
                    if ((uint)j3 >= (uint)c3.Length)
                    {
                        continue;
                    }

                    var j4 = SparseSet<TEntity>.Slot(s4, index);
                    if ((uint)j4 >= (uint)c4.Length)
                    {
                        continue;
                    }

                    var j5 = SparseSet<TEntity>.Slot(s5, index);
                    if ((uint)j5 >= (uint)c5.Length)
                    {
                        continue;
                    }

                    var j6 = SparseSet<TEntity>.Slot(s6, index);
                    if ((uint)j6 >= (uint)c6.Length)
                    {
                        continue;
                    }

                    var j7 = SparseSet<TEntity>.Slot(s7, index);
                    if ((uint)j7 >= (uint)c7.Length)
                    {
                        continue;
                    }

                    var j8 = SparseSet<TEntity>.Slot(s8, index);
                    if ((uint)j8 >= (uint)c8.Length)
                    {
                        continue;
                    }

                    var j9 = SparseSet<TEntity>.Slot(s9, index);
                    if ((uint)j9 >= (uint)c9.Length)
                    {
                        continue;
                    }

                    var j10 = SparseSet<TEntity>.Slot(s10, index);
                    if ((uint)j10 >= (uint)c10.Length)
                    {
                        continue;
                    }

                    var j11 = SparseSet<TEntity>.Slot(s11, index);
                    if ((uint)j11 >= (uint)c11.Length)
                    {
                        continue;
                    }

                    var j12 = SparseSet<TEntity>.Slot(s12, index);
                    if ((uint)j12 >= (uint)c12.Length)
                    {
                        continue;
                    }

                    var e = ents[j1];
                    if (!componentFilters || filters.PassComponents(e))
                    {
                        fn.Invoke(e, ref c1[j1], ref c2[j2], ref c3[j3], ref c4[j4], ref c5[j5], ref c6[j6], ref c7[j7], ref c8[j8], ref c9[j9], ref c10[j10], ref c11[j11], ref c12[j12]);
                    }
                }
            }
        }
    }
}
