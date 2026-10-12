using Cestyll.Ecs.Collections;
using Cestyll.Ecs.Storage;

namespace Cestyll.Ecs.Views;

// View().Rows: Each and foreach with refs to the components.
public readonly struct ViewRows<TEntity, T1, T2>
    where TEntity : unmanaged, IEntity<TEntity>
    where T1 : IComponent
    where T2 : IComponent
{
    private readonly ViewQuery<TEntity> _query;
    private readonly Pool<TEntity, T1> _p1;
    private readonly Pool<TEntity, T2> _p2;

    internal ViewRows(ViewQuery<TEntity> query, Pool<TEntity, T1> p1, Pool<TEntity, T2> p2)
    {
        _query = query;
        _p1 = p1;
        _p2 = p2;
    }

    public void Each<TFn>(ref TFn fn) where TFn : struct, IEach<TFn, TEntity, T1, T2>
    {
        var walker = new Walker<TFn>(this, ref fn);
        _query.Run(ref walker, _p1, _p2);
    }

    public void Each(RefAction<TEntity, T1, T2> action)
    {
        var adapter = new RefActionAdapter<TEntity, T1, T2>(action);
        Each(ref adapter);
    }

    // foreach: same entities and rules as Each, which with a struct callback is a little faster.
    public Enumerator GetEnumerator() => new(this);

    public readonly ref struct Row
    {
        public readonly TEntity Entity;
        public readonly ref T1 C1;
        public readonly ref T2 C2;

        internal Row(TEntity entity, ref T1 c1, ref T2 c2)
        {
            Entity = entity;
            C1 = ref c1;
            C2 = ref c2;
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

            throw new InvalidOperationException($"This view has no {typeof(T).Name}");
        }
    }

    public ref struct Enumerator
    {
        private readonly Walk _w;
        private int _slot; // the current entity's, in the driving set
        private int _j1, _j2; // its slots in the pools

        internal Enumerator(ViewRows<TEntity, T1, T2> view)
        {
            var w = Walk.Rent();
            w.Bind(view._query, [view._p1, view._p2]);
            w.S1 = view._p1.SparseArray;
            w.S2 = view._p2.SparseArray;
            w.D1 = view._p1.DataArray;
            w.D2 = view._p2.DataArray;
            _w = w;
            _slot = w.Count;
            _j1 = 0;
            _j2 = 0;
        }

        public readonly Row Current
        {
            get
            {
                var w = _w;
                return new(w.Packed[_slot], ref w.D1[_j1], ref w.D2[_j2]);
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

                _slot = slot;
                _j1 = j1;
                _j2 = j2;
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
        public T1[] D1 = [];
        public T2[] D2 = [];

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
            D1 = [];
            D2 = [];
            _spare = this;
        }
    }

    private readonly ref struct Walker<TFn> : IViewWalker<TEntity>
        where TFn : struct, IEach<TFn, TEntity, T1, T2>
    {
        private readonly ViewRows<TEntity, T1, T2> _view;
        private readonly ref TFn _fn;

        public Walker(ViewRows<TEntity, T1, T2> view, ref TFn fn)
        {
            _view = view;
            _fn = ref fn;
        }

        public int MinCount => Math.Min(_view._p1.Count, _view._p2.Count);

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
                default:
                    Unfiltered<Drive2>(_view._p2.Entities);
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
                default:
                    PoolDriven<Drive2>(_view._p2.Entities, mask, mode, filters, componentFilters);
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

                fn.Invoke(e, ref c1[j1], ref c2[j2]);
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

                if (!componentFilters || filters.PassComponents(e))
                {
                    fn.Invoke(e, ref c1[j1], ref c2[j2]);
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

            // Words past the pools' sparse arrays only miss. The last word can be partial.
            var wordEnd = Math.Min(mask.Length, (Math.Min(s1.Length, s2.Length) + 63) >> 6);
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

                    var e = ents[j1];
                    if (!componentFilters || filters.PassComponents(e))
                    {
                        fn.Invoke(e, ref c1[j1], ref c2[j2]);
                    }
                }
            }
        }
    }
}
