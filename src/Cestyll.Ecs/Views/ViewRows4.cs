using Cestyll.Ecs.Collections;
using Cestyll.Ecs.Storage;

namespace Cestyll.Ecs.Views;

// View().Rows: Each and foreach with refs to the components.
public readonly struct ViewRows<TEntity, T1, T2, T3, T4>
    where TEntity : unmanaged, IEntity<TEntity>
    where T1 : IComponent
    where T2 : IComponent
    where T3 : IComponent
    where T4 : IComponent
{
    private readonly ViewQuery<TEntity> _query;
    private readonly Pool<TEntity, T1> _p1;
    private readonly Pool<TEntity, T2> _p2;
    private readonly Pool<TEntity, T3> _p3;
    private readonly Pool<TEntity, T4> _p4;

    internal ViewRows(
        ViewQuery<TEntity> query, Pool<TEntity, T1> p1, Pool<TEntity, T2> p2, Pool<TEntity, T3> p3, Pool<TEntity, T4> p4)
    {
        _query = query;
        _p1 = p1;
        _p2 = p2;
        _p3 = p3;
        _p4 = p4;
    }

    public void Each<TFn>(ref TFn fn) where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4>
    {
        var walker = new Walker<TFn>(this, ref fn);
        _query.Run(ref walker, _p1, _p2, _p3, _p4);
    }

    public void Each(RefAction<TEntity, T1, T2, T3, T4> action)
    {
        var adapter = new RefActionAdapter<TEntity, T1, T2, T3, T4>(action);
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

        internal Row(TEntity entity, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4)
        {
            Entity = entity;
            C1 = ref c1;
            C2 = ref c2;
            C3 = ref c3;
            C4 = ref c4;
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

            throw new InvalidOperationException($"This view has no {typeof(T).Name}");
        }
    }

    public ref struct Enumerator
    {
        private readonly Walk _w;
        private int _slot; // the current entity's, in the driving set
        // Slots live in _w.J1 to _w.J4, so the JIT promotes the two fields in full.

        internal Enumerator(ViewRows<TEntity, T1, T2, T3, T4> view)
        {
            var w = Walk.Rent();
            w.Bind(view._query, [view._p1, view._p2, view._p3, view._p4]);
            w.S1 = view._p1.SparseArray;
            w.S2 = view._p2.SparseArray;
            w.S3 = view._p3.SparseArray;
            w.S4 = view._p4.SparseArray;
            w.D1 = view._p1.DataArray;
            w.D2 = view._p2.DataArray;
            w.D3 = view._p3.DataArray;
            w.D4 = view._p4.DataArray;
            _w = w;
            _slot = w.Count;
        }

        public readonly Row Current
        {
            get
            {
                var w = _w;
                return new(w.Packed[_slot], ref w.D1[w.J1], ref w.D2[w.J2], ref w.D3[w.J3], ref w.D4[w.J4]);
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

                _slot = slot;
                w.J1 = j1;
                w.J2 = j2;
                w.J3 = j3;
                w.J4 = j4;
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
        public T1[] D1 = [];
        public T2[] D2 = [];
        public T3[] D3 = [];
        public T4[] D4 = [];

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
            D1 = [];
            D2 = [];
            D3 = [];
            D4 = [];
            _spare = this;
        }
    }

    private readonly ref struct Walker<TFn> : IViewWalker<TEntity>
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4>
    {
        private readonly ViewRows<TEntity, T1, T2, T3, T4> _view;
        private readonly ref TFn _fn;

        public Walker(ViewRows<TEntity, T1, T2, T3, T4> view, ref TFn fn)
        {
            _view = view;
            _fn = ref fn;
        }

        public int MinCount => Math.Min(_view._p1.Count, Math.Min(_view._p2.Count, Math.Min(_view._p3.Count, _view._p4.Count)));

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
                default:
                    Unfiltered<Drive4>(_view._p4.Entities);
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
                default:
                    PoolDriven<Drive4>(_view._p4.Entities, mask, mode, filters, componentFilters);
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

                fn.Invoke(e, ref c1[j1], ref c2[j2], ref c3[j3], ref c4[j4]);
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

                if (!componentFilters || filters.PassComponents(e))
                {
                    fn.Invoke(e, ref c1[j1], ref c2[j2], ref c3[j3], ref c4[j4]);
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

            // Words past the pools' sparse arrays only miss. The last word can be partial.
            var wordEnd = Math.Min(mask.Length, (Math.Min(s1.Length, Math.Min(s2.Length, Math.Min(s3.Length, s4.Length))) + 63) >> 6);
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

                    var e = ents[j1];
                    if (!componentFilters || filters.PassComponents(e))
                    {
                        fn.Invoke(e, ref c1[j1], ref c2[j2], ref c3[j3], ref c4[j4]);
                    }
                }
            }
        }
    }
}
