namespace Cestyll.Ecs.Collections;

// One bit per entity index. Hot operations lower to BMI (tzcnt, blsr, andn, popcnt).
internal sealed class EntityBitset
{
    private ulong[] _words = new ulong[16];

    public ReadOnlySpan<ulong> Words => _words;
    internal ulong[] WordsArray => _words;

    public void Set(int index)
    {
        var w = index >> 6;
        if (w >= _words.Length)
        {
            Array.Resize(ref _words, Math.Max(w + 1, _words.Length * 2));
        }

        _words[w] |= 1UL << index; // shift count is masked to 6 bits
    }

    // Sets [from, to). Grows first, so a failed allocation changes nothing.
    public void SetRange(int from, int to)
    {
        if (from >= to)
        {
            return;
        }

        var last = (to - 1) >> 6;
        if (last >= _words.Length)
        {
            Array.Resize(ref _words, (int)Math.Min(Array.MaxLength, Math.Max(last + 1L, _words.Length * 2L)));
        }

        var first = from >> 6;
        var head = ~0UL << from;         // bits from `from` up, within its word
        var tail = ~0UL >> (63 - ((to - 1) & 63)); // bits up to to - 1, within its word
        if (first == last)
        {
            _words[first] |= head & tail;
            return;
        }

        _words[first] |= head;
        _words.AsSpan(first + 1, last - first - 1).Fill(~0UL);
        _words[last] |= tail;
    }

    public void Unset(int index)
    {
        var w = index >> 6;
        if (w < _words.Length)
        {
            _words[w] &= ~(1UL << index);
        }
    }

    public bool Test(int index) => WordOps.Test(_words, index);

    // Clears and returns the lowest set index, or -1. Every word below hint must be zero.
    public int TakeLowest(ref int hint)
    {
        for (var w = hint; w < _words.Length; w++)
        {
            var bits = _words[w];
            if (bits == 0)
            {
                continue;
            }

            hint = w;
            _words[w] = bits & (bits - 1); // blsr
            return (w << 6) + BitOperations.TrailingZeroCount(bits); // tzcnt
        }
        hint = _words.Length;
        return -1;
    }

    public int Count() => WordOps.PopCount(_words);

    public void Clear() => Array.Clear(_words);

    // Trims to the highest set bit, 16 words at least.
    public void ShrinkToFit()
    {
        var last = _words.LastIndexOfAnyExcept(0UL);
        var length = Math.Max(16, last + 1);
        if (length < _words.Length)
        {
            Array.Resize(ref _words, length);
        }
    }

    public BitEnumerator GetEnumerator() => new(_words);
}

// Yields set indices in ascending order: tzcnt to find, blsr to consume.
internal ref struct BitEnumerator
{
    private readonly ulong[] _words;
    private int _word = -1;
    private ulong _bits;

    internal BitEnumerator(ulong[] words) => _words = words;

    public int Current { get; private set; }
    public readonly BitEnumerator GetEnumerator() => this;

    public bool MoveNext()
    {
        while (_bits == 0)
        {
            if (++_word >= _words.Length)
            {
                return false;
            }

            _bits = _words[_word];
        }
        Current = (_word << 6) + BitOperations.TrailingZeroCount(_bits);
        _bits &= _bits - 1;
        return true;
    }
}
