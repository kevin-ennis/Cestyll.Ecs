using Cestyll.Ecs.Collections;
using Cestyll.Ecs.Storage;

namespace Cestyll.Ecs.Views;

// View().Rows: Each and foreach with refs to the components.
public readonly struct ViewRows<TEntity, T1>
    where TEntity : unmanaged, IEntity<TEntity>
    where T1 : IComponent
{
    private readonly ViewQuery<TEntity> _query;
    private readonly Pool<TEntity, T1> _p1;

    internal ViewRows(ViewQuery<TEntity> query, Pool<TEntity, T1> p1)
    {
        _query = query;
        _p1 = p1;
    }

    public void Each<TFn>(ref TFn fn) where TFn : struct, IEach<TFn, TEntity, T1>
    {
        var walker = new Walker<TFn>(this, ref fn);
        _query.Run(ref walker, _p1);
    }

    public void Each(RefAction<TEntity, T1> action)
    {
        var adapter = new RefActionAdapter<TEntity, T1>(action);
        Each(ref adapter);
    }

    // foreach: same entities and rules as Each, which with a struct callback is a little faster.
    public Enumerator GetEnumerator() => new(this);

    public readonly ref struct Row
    {
        public readonly TEntity Entity;
        public readonly ref T1 C1;

        internal Row(TEntity entity, ref T1 c1)
        {
            Entity = entity;
            C1 = ref c1;
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

            throw new InvalidOperationException($"This view has no {typeof(T).Name}");
        }
    }

    public ref struct Enumerator
    {
        private readonly Walk _w;
        private int _slot; // the current entity's, in the driving set
        private int _j1; // its slot in the pool: _slot unless members or sources drive

        internal Enumerator(ViewRows<TEntity, T1> view)
        {
            var w = Walk.Rent();
            w.Bind(view._query, [view._p1]);
            w.S1 = view._p1.SparseArray;
            w.D1 = view._p1.DataArray;
            _w = w;
            _slot = w.Count;
            _j1 = 0;
        }

        public readonly Row Current
        {
            get
            {
                var w = _w;
                return new(w.Packed[_slot], ref w.D1[_j1]);
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
            var slot = _slot;
            if (w.Plain)
            {
                // The pool drives and nothing filters: the step is a countdown.
                slot--;
                _slot = slot;
                _j1 = slot;
                return slot >= 0;
            }

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

                _slot = slot;
                _j1 = j1;
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
        public T1[] D1 = [];

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
            D1 = [];
            _spare = this;
        }
    }

    private readonly ref struct Walker<TFn> : IViewWalker<TEntity>
        where TFn : struct, IEach<TFn, TEntity, T1>
    {
        private readonly ViewRows<TEntity, T1> _view;
        private readonly ref TFn _fn;

        public Walker(ViewRows<TEntity, T1> view, ref TFn fn)
        {
            _view = view;
            _fn = ref fn;
        }

        public int MinCount => _view._p1.Count;

        public void Unfiltered(ReadOnlySpan<TEntity> driven)
        {
            if (_view._query.Driven)
            {
                GroupDriven(driven, default, MaskMode.None, default, false);
                return;
            }

            ref var fn = ref _fn;
            var ents = _view._p1.Entities;
            var c1 = _view._p1.Components;
            for (var i = ents.Length - 1; i >= 0; i--)
            {
                fn.Invoke(ents[i], ref c1[i]);
            }
        }

        public void PoolDriven(
            ReadOnlySpan<TEntity> driven, ReadOnlySpan<ulong> mask, MaskMode mode, ViewFilters<TEntity> filters,
            bool componentFilters)
        {
            if (_view._query.Driven)
            {
                GroupDriven(driven, mask, mode, filters, componentFilters);
                return;
            }

            ref var fn = ref _fn;
            var include = mode == MaskMode.Include;
            var masked = mode != MaskMode.None;
            var ents = _view._p1.Entities;
            var c1 = _view._p1.Components;
            for (var i = ents.Length - 1; i >= 0; i--)
            {
                var e = ents[i];
                if (masked && WordOps.Test(mask, e.Index) != include)
                {
                    continue;
                }

                if (!componentFilters || filters.PassComponents(e))
                {
                    fn.Invoke(e, ref c1[i]);
                }
            }
        }

        // Use(group) or WithRelationTarget(target): ents are the members or sources, and the pool is probed for each.
        private void GroupDriven(
            ReadOnlySpan<TEntity> ents, ReadOnlySpan<ulong> mask, MaskMode mode, ViewFilters<TEntity> filters,
            bool componentFilters)
        {
            ref var fn = ref _fn;
            var include = mode == MaskMode.Include;
            var masked = mode != MaskMode.None;
            var s1 = _view._p1.SparseSpan;
            var c1 = _view._p1.Components;
            for (var i = ents.Length - 1; i >= 0; i--)
            {
                var e = ents[i];
                if (masked && WordOps.Test(mask, e.Index) != include)
                {
                    continue;
                }

                var j1 = SparseSet<TEntity>.Slot(s1, e.Index);
                if ((uint)j1 >= (uint)c1.Length)
                {
                    continue;
                }

                if (!componentFilters || filters.PassComponents(e))
                {
                    fn.Invoke(e, ref c1[j1]);
                }
            }
        }

        public void Masked(ReadOnlySpan<ulong> mask, ViewFilters<TEntity> filters, bool componentFilters)
        {
            ref var fn = ref _fn;
            var ents = _view._p1.Entities;
            var s1 = _view._p1.SparseSpan;
            var c1 = _view._p1.Components;

            // Words past the pools' sparse arrays only miss. The last word can be partial.
            var wordEnd = Math.Min(mask.Length, (s1.Length + 63) >> 6);
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

                    var e = ents[j1];
                    if (!componentFilters || filters.PassComponents(e))
                    {
                        fn.Invoke(e, ref c1[j1]);
                    }
                }
            }
        }
    }
}
