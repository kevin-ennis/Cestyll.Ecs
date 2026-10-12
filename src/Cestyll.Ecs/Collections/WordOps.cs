namespace Cestyll.Ecs.Collections;

// Word-level bitset ops for query masks, via Vector256 when accelerated, with a scalar tail.
internal enum MaskMode : byte { None, Include, Exclude }

internal static class WordOps
{
    public static void Or(Span<ulong> dst, ReadOnlySpan<ulong> src)
    {
        var n = Math.Min(dst.Length, src.Length);
        var i = 0;
        if (Vector256.IsHardwareAccelerated)
        {
            var d = MemoryMarshal.Cast<ulong, Vector256<ulong>>(dst[..n]);
            var x = MemoryMarshal.Cast<ulong, Vector256<ulong>>(src[..n]);
            for (var v = 0; v < d.Length; v++)
            {
                d[v] |= x[v];
            }

            i = d.Length * 4;
        }

        for (; i < n; i++)
        {
            dst[i] |= src[i];
        }
    }

    public static void And(Span<ulong> dst, ReadOnlySpan<ulong> src)
    {
        var n = Math.Min(dst.Length, src.Length);
        var i = 0;
        if (Vector256.IsHardwareAccelerated)
        {
            var d = MemoryMarshal.Cast<ulong, Vector256<ulong>>(dst[..n]);
            var x = MemoryMarshal.Cast<ulong, Vector256<ulong>>(src[..n]);
            for (var v = 0; v < d.Length; v++)
            {
                d[v] &= x[v];
            }

            i = d.Length * 4;
        }

        for (; i < n; i++)
        {
            dst[i] &= src[i];
        }

        dst[n..].Clear(); // words past the end of src are zero in src
    }

    public static void AndNot(Span<ulong> dst, ReadOnlySpan<ulong> excl)
    {
        var n = Math.Min(dst.Length, excl.Length);
        var i = 0;
        if (Vector256.IsHardwareAccelerated)
        {
            var d = MemoryMarshal.Cast<ulong, Vector256<ulong>>(dst[..n]);
            var x = MemoryMarshal.Cast<ulong, Vector256<ulong>>(excl[..n]);
            for (var v = 0; v < d.Length; v++)
            {
                d[v] = Vector256.AndNot(d[v], x[v]);
            }

            i = d.Length * 4;
        }

        for (; i < n; i++)
        {
            dst[i] &= ~excl[i];
        }
    }

    public static int PopCount(ReadOnlySpan<ulong> words)
    {
        var n = 0;
        foreach (var w in words)
        {
            n += BitOperations.PopCount(w);
        }

        return n;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Test(ReadOnlySpan<ulong> words, int index)
    {
        var w = index >> 6;
        return (uint)w < (uint)words.Length && (words[w] & (1UL << index)) != 0;
    }
}
