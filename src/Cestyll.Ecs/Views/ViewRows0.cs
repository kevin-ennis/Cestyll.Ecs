using Cestyll.Ecs.Storage;

namespace Cestyll.Ecs.Views;

// View().Rows(): foreach over the entities alone. A tags-only view walks in ascending index order.
public readonly struct ViewRows<TEntity>
    where TEntity : unmanaged, IEntity<TEntity>
{
    private readonly ViewQuery<TEntity> _query;
    private readonly SparseSet<TEntity>? _pin;
    private readonly bool _joined;

    internal ViewRows(ViewQuery<TEntity> query, SparseSet<TEntity>? pin, bool joined)
    {
        _query = query;
        _pin = pin;
        _joined = joined;
    }

    public Enumerator GetEnumerator() => new(this);

    public ref struct Enumerator
    {
        private readonly Walk _w;
        private int _slot; // in the driving set

        // Tags only: the mask's bits are the entities, walked here like a bitset.
        private readonly ReadOnlySpan<ulong> _mask;
        private readonly uint[] _versions;
        private readonly bool _tagsOnly;
        private int _word;
        private ulong _bits;

        internal Enumerator(ViewRows<TEntity> view)
        {
            // Picked per walk, so a store made after Rows() can still drive.
            var query = view._query;
            var set = view._pin ?? query.DriveSet ?? (query.Driven ? null : query.SmallestRequiredSet());
            ReadOnlySpan<SparseSet<TEntity>> sets = set is null ? [] : [set];
            if (view._joined)
            {
                query.CheckJoin(sets);
            }

            var w = Walk.Rent();
            w.Bind(query, sets);
            _w = w;
            _slot = w.Count;
            _tagsOnly = w.TagsOnly;
            _mask = w.TagsOnly ? w.Mask : default;
            _versions = query.Registry.Versions;
            _word = -1;
        }

        public TEntity Current { get; private set; }

        public bool MoveNext()
        {
            if (_tagsOnly)
            {
                return NextTagged();
            }

            var w = _w;
            _slot = w.Simple ? _slot - 1 : w.Next();
            if (_slot < 0)
            {
                return false;
            }

            Current = w.Packed[_slot];
            return true;
        }

        private bool NextTagged()
        {
            while (true)
            {
                while (_bits == 0)
                {
                    if (++_word >= _mask.Length)
                    {
                        return false;
                    }

                    _bits = _mask[_word];
                }

                var index = (_word << 6) + BitOperations.TrailingZeroCount(_bits);
                _bits &= _bits - 1;
                var e = TEntity.Make(index, _versions[index]);
                if (!_w.ComponentFilters || _w.PassComponents(e))
                {
                    Current = e;
                    return true;
                }
            }
        }

        public void Dispose() => _w.Return();
    }

    // Pooled walk state, one spare per thread; a nested foreach allocates its own.
    private sealed class Walk : ViewCursor<TEntity>
    {
        [ThreadStatic]
        private static Walk? _spare;

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
            _spare = this;
        }
    }
}
