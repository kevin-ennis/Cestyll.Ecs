using Cestyll.Ecs.Groups;
using Cestyll.Ecs.Storage;

namespace Cestyll.Ecs;

// Pools and owning groups walked as Vector256 rows; lanes are the components' raw bytes.
// Components walked together share one size that divides or is a multiple of 32 bytes.
// A partial last vector is a zero-padded buffer row, copied back when the loop ends.

// The lane type as a value: an extension can't take its own type argument while inferring the receiver's.
public readonly struct Lane<TLane>
    where TLane : unmanaged
{
}

public static class Lanes
{
    public static Lane<float> Float => default;
    public static Lane<double> Double => default;
    public static Lane<int> Int => default;
    public static Lane<uint> UInt => default;
    public static Lane<long> Long => default;
    public static Lane<ulong> ULong => default;
    public static Lane<short> Short => default;
    public static Lane<ushort> UShort => default;
    public static Lane<byte> Byte => default;
    public static Lane<sbyte> SByte => default;
}

internal static class VectorSizes
{
    public const int Bytes = 32;

    public static int Check(int size, Type type)
    {
        if (Bytes % size != 0 && size % Bytes != 0)
        {
            throw new ArgumentException(
                $"{type.Name} is {size} bytes, which neither divides nor is a multiple of the 32 bytes of a "
                + "Vector256, so a component would straddle two vectors");
        }

        return size;
    }

    public static void Same(int size, int other, Type type, Type otherType)
    {
        if (size != other)
        {
            throw new ArgumentException(
                $"{type.Name} is {size} bytes and {otherType.Name} is {other}: components walked as vectors "
                + "together must be the same size, so vector i of each covers the same entities");
        }
    }
}

// A tail row's blocks, one Vector256 per component array; each arity adds its typed arrays.
internal abstract class VectorTail(int blocks)
{
    private readonly byte[] _blocks = new byte[blocks * VectorSizes.Bytes];
    private int _start;
    private int _length;

    // Set when the tail row is handed out, so Flush knows to write back.
    protected bool Filled { get; set; }

    protected void Begin(int start, int length)
    {
        _start = start;
        _length = length;
        Filled = false;
    }

    protected Span<byte> Block(int k) => _blocks.AsSpan(k * VectorSizes.Bytes, VectorSizes.Bytes);

    protected void FillBlock<T>(T[] array, int k)
        where T : unmanaged
    {
        var block = MemoryMarshal.Cast<byte, T>(Block(k));
        block.Clear();
        array.AsSpan(_start, _length).CopyTo(block);
    }

    protected void FlushBlock<T>(T[] array, int k)
        where T : unmanaged =>
        MemoryMarshal.Cast<byte, T>(Block(k))[.._length].CopyTo(array.AsSpan(_start, _length));
}

public static class VectorWalks
{
    // tail is the components past the last full vector, for the caller to finish.
    extension<T>(Span<T> components)
        where T : unmanaged
    {
        public Span<Vector256<TLane>> Vectors256<TLane>(Lane<TLane> lane, out Span<T> tail)
            where TLane : unmanaged
        {
            var size = VectorSizes.Check(Unsafe.SizeOf<T>(), typeof(T));
            var vectors = MemoryMarshal.Cast<T, Vector256<TLane>>(components);
            var covered = (int)((long)vectors.Length * VectorSizes.Bytes / size);
            tail = components[covered..];
            return vectors;
        }
    }

    extension<TEntity, T>(Pool<TEntity, T> pool)
        where TEntity : unmanaged, IEntity<TEntity>
        where T : unmanaged, IComponent
    {
        public VectorRows<T, TLane> Vectors256<TLane>(Lane<TLane> lane)
            where TLane : unmanaged =>
            new(pool.Count, pool.DataArray);
    }

    extension<TEntity, T1>(GroupRows<TEntity, T1> owning)
        where TEntity : unmanaged, IEntity<TEntity>
        where T1 : unmanaged, IComponent
    {
        public VectorRows<T1, TLane> Vectors256<TLane>(Lane<TLane> lane)
            where TLane : unmanaged =>
            new(owning.Count, owning.Pool1.DataArray);
    }

    extension<TEntity, T1, T2>(GroupRows<TEntity, T1, T2> owning)
        where TEntity : unmanaged, IEntity<TEntity>
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
    {
        public VectorRows<T1, T2, TLane> Vectors256<TLane>(Lane<TLane> lane)
            where TLane : unmanaged =>
            new(owning.Count, owning.Pool1.DataArray, owning.Pool2.DataArray);
    }

    extension<TEntity, T1, T2, T3>(GroupRows<TEntity, T1, T2, T3> owning)
        where TEntity : unmanaged, IEntity<TEntity>
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
    {
        public VectorRows<T1, T2, T3, TLane> Vectors256<TLane>(Lane<TLane> lane)
            where TLane : unmanaged =>
            new(owning.Count, owning.Pool1.DataArray, owning.Pool2.DataArray, owning.Pool3.DataArray);
    }

    extension<TEntity, T1, T2, T3, T4>(GroupRows<TEntity, T1, T2, T3, T4> owning)
        where TEntity : unmanaged, IEntity<TEntity>
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
        where T4 : unmanaged, IComponent
    {
        public VectorRows<T1, T2, T3, T4, TLane> Vectors256<TLane>(Lane<TLane> lane)
            where TLane : unmanaged =>
            new(owning.Count, owning.Pool1.DataArray, owning.Pool2.DataArray, owning.Pool3.DataArray, owning.Pool4.DataArray);
    }

    extension<TEntity, T1, T2, T3, T4, T5>(GroupRows<TEntity, T1, T2, T3, T4, T5> owning)
        where TEntity : unmanaged, IEntity<TEntity>
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
        where T4 : unmanaged, IComponent
        where T5 : unmanaged, IComponent
    {
        public VectorRows<T1, T2, T3, T4, T5, TLane> Vectors256<TLane>(Lane<TLane> lane)
            where TLane : unmanaged =>
            new(owning.Count, owning.Pool1.DataArray, owning.Pool2.DataArray, owning.Pool3.DataArray, owning.Pool4.DataArray, owning.Pool5.DataArray);
    }

    extension<TEntity, T1, T2, T3, T4, T5, T6>(GroupRows<TEntity, T1, T2, T3, T4, T5, T6> owning)
        where TEntity : unmanaged, IEntity<TEntity>
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
        where T4 : unmanaged, IComponent
        where T5 : unmanaged, IComponent
        where T6 : unmanaged, IComponent
    {
        public VectorRows<T1, T2, T3, T4, T5, T6, TLane> Vectors256<TLane>(Lane<TLane> lane)
            where TLane : unmanaged =>
            new(owning.Count, owning.Pool1.DataArray, owning.Pool2.DataArray, owning.Pool3.DataArray, owning.Pool4.DataArray, owning.Pool5.DataArray, owning.Pool6.DataArray);
    }

    extension<TEntity, T1, T2, T3, T4, T5, T6, T7>(GroupRows<TEntity, T1, T2, T3, T4, T5, T6, T7> owning)
        where TEntity : unmanaged, IEntity<TEntity>
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
        where T4 : unmanaged, IComponent
        where T5 : unmanaged, IComponent
        where T6 : unmanaged, IComponent
        where T7 : unmanaged, IComponent
    {
        public VectorRows<T1, T2, T3, T4, T5, T6, T7, TLane> Vectors256<TLane>(Lane<TLane> lane)
            where TLane : unmanaged =>
            new(owning.Count, owning.Pool1.DataArray, owning.Pool2.DataArray, owning.Pool3.DataArray, owning.Pool4.DataArray, owning.Pool5.DataArray, owning.Pool6.DataArray, owning.Pool7.DataArray);
    }

    extension<TEntity, T1, T2, T3, T4, T5, T6, T7, T8>(GroupRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8> owning)
        where TEntity : unmanaged, IEntity<TEntity>
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
        where T4 : unmanaged, IComponent
        where T5 : unmanaged, IComponent
        where T6 : unmanaged, IComponent
        where T7 : unmanaged, IComponent
        where T8 : unmanaged, IComponent
    {
        public VectorRows<T1, T2, T3, T4, T5, T6, T7, T8, TLane> Vectors256<TLane>(Lane<TLane> lane)
            where TLane : unmanaged =>
            new(owning.Count, owning.Pool1.DataArray, owning.Pool2.DataArray, owning.Pool3.DataArray, owning.Pool4.DataArray, owning.Pool5.DataArray, owning.Pool6.DataArray, owning.Pool7.DataArray, owning.Pool8.DataArray);
    }

    extension<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9>(GroupRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9> owning)
        where TEntity : unmanaged, IEntity<TEntity>
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
        where T4 : unmanaged, IComponent
        where T5 : unmanaged, IComponent
        where T6 : unmanaged, IComponent
        where T7 : unmanaged, IComponent
        where T8 : unmanaged, IComponent
        where T9 : unmanaged, IComponent
    {
        public VectorRows<T1, T2, T3, T4, T5, T6, T7, T8, T9, TLane> Vectors256<TLane>(Lane<TLane> lane)
            where TLane : unmanaged =>
            new(owning.Count, owning.Pool1.DataArray, owning.Pool2.DataArray, owning.Pool3.DataArray, owning.Pool4.DataArray, owning.Pool5.DataArray, owning.Pool6.DataArray, owning.Pool7.DataArray, owning.Pool8.DataArray, owning.Pool9.DataArray);
    }

    extension<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(GroupRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> owning)
        where TEntity : unmanaged, IEntity<TEntity>
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
        where T4 : unmanaged, IComponent
        where T5 : unmanaged, IComponent
        where T6 : unmanaged, IComponent
        where T7 : unmanaged, IComponent
        where T8 : unmanaged, IComponent
        where T9 : unmanaged, IComponent
        where T10 : unmanaged, IComponent
    {
        public VectorRows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TLane> Vectors256<TLane>(Lane<TLane> lane)
            where TLane : unmanaged =>
            new(owning.Count, owning.Pool1.DataArray, owning.Pool2.DataArray, owning.Pool3.DataArray, owning.Pool4.DataArray, owning.Pool5.DataArray, owning.Pool6.DataArray, owning.Pool7.DataArray, owning.Pool8.DataArray, owning.Pool9.DataArray, owning.Pool10.DataArray);
    }

    extension<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(GroupRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> owning)
        where TEntity : unmanaged, IEntity<TEntity>
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
        where T4 : unmanaged, IComponent
        where T5 : unmanaged, IComponent
        where T6 : unmanaged, IComponent
        where T7 : unmanaged, IComponent
        where T8 : unmanaged, IComponent
        where T9 : unmanaged, IComponent
        where T10 : unmanaged, IComponent
        where T11 : unmanaged, IComponent
    {
        public VectorRows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TLane> Vectors256<TLane>(Lane<TLane> lane)
            where TLane : unmanaged =>
            new(owning.Count, owning.Pool1.DataArray, owning.Pool2.DataArray, owning.Pool3.DataArray, owning.Pool4.DataArray, owning.Pool5.DataArray, owning.Pool6.DataArray, owning.Pool7.DataArray, owning.Pool8.DataArray, owning.Pool9.DataArray, owning.Pool10.DataArray, owning.Pool11.DataArray);
    }

    extension<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(GroupRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> owning)
        where TEntity : unmanaged, IEntity<TEntity>
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
        where T4 : unmanaged, IComponent
        where T5 : unmanaged, IComponent
        where T6 : unmanaged, IComponent
        where T7 : unmanaged, IComponent
        where T8 : unmanaged, IComponent
        where T9 : unmanaged, IComponent
        where T10 : unmanaged, IComponent
        where T11 : unmanaged, IComponent
        where T12 : unmanaged, IComponent
    {
        public VectorRows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TLane> Vectors256<TLane>(Lane<TLane> lane)
            where TLane : unmanaged =>
            new(owning.Count, owning.Pool1.DataArray, owning.Pool2.DataArray, owning.Pool3.DataArray, owning.Pool4.DataArray, owning.Pool5.DataArray, owning.Pool6.DataArray, owning.Pool7.DataArray, owning.Pool8.DataArray, owning.Pool9.DataArray, owning.Pool10.DataArray, owning.Pool11.DataArray, owning.Pool12.DataArray);
    }

    extension<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(GroupRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> owning)
        where TEntity : unmanaged, IEntity<TEntity>
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
        where T4 : unmanaged, IComponent
        where T5 : unmanaged, IComponent
        where T6 : unmanaged, IComponent
        where T7 : unmanaged, IComponent
        where T8 : unmanaged, IComponent
        where T9 : unmanaged, IComponent
        where T10 : unmanaged, IComponent
        where T11 : unmanaged, IComponent
        where T12 : unmanaged, IComponent
        where T13 : unmanaged, IComponent
    {
        public VectorRows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TLane> Vectors256<TLane>(Lane<TLane> lane)
            where TLane : unmanaged =>
            new(owning.Count, owning.Pool1.DataArray, owning.Pool2.DataArray, owning.Pool3.DataArray, owning.Pool4.DataArray, owning.Pool5.DataArray, owning.Pool6.DataArray, owning.Pool7.DataArray, owning.Pool8.DataArray, owning.Pool9.DataArray, owning.Pool10.DataArray, owning.Pool11.DataArray, owning.Pool12.DataArray, owning.Pool13.DataArray);
    }

    extension<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(GroupRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> owning)
        where TEntity : unmanaged, IEntity<TEntity>
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
        where T4 : unmanaged, IComponent
        where T5 : unmanaged, IComponent
        where T6 : unmanaged, IComponent
        where T7 : unmanaged, IComponent
        where T8 : unmanaged, IComponent
        where T9 : unmanaged, IComponent
        where T10 : unmanaged, IComponent
        where T11 : unmanaged, IComponent
        where T12 : unmanaged, IComponent
        where T13 : unmanaged, IComponent
        where T14 : unmanaged, IComponent
    {
        public VectorRows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TLane> Vectors256<TLane>(Lane<TLane> lane)
            where TLane : unmanaged =>
            new(owning.Count, owning.Pool1.DataArray, owning.Pool2.DataArray, owning.Pool3.DataArray, owning.Pool4.DataArray, owning.Pool5.DataArray, owning.Pool6.DataArray, owning.Pool7.DataArray, owning.Pool8.DataArray, owning.Pool9.DataArray, owning.Pool10.DataArray, owning.Pool11.DataArray, owning.Pool12.DataArray, owning.Pool13.DataArray, owning.Pool14.DataArray);
    }

    extension<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(GroupRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> owning)
        where TEntity : unmanaged, IEntity<TEntity>
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
        where T4 : unmanaged, IComponent
        where T5 : unmanaged, IComponent
        where T6 : unmanaged, IComponent
        where T7 : unmanaged, IComponent
        where T8 : unmanaged, IComponent
        where T9 : unmanaged, IComponent
        where T10 : unmanaged, IComponent
        where T11 : unmanaged, IComponent
        where T12 : unmanaged, IComponent
        where T13 : unmanaged, IComponent
        where T14 : unmanaged, IComponent
        where T15 : unmanaged, IComponent
    {
        public VectorRows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TLane> Vectors256<TLane>(Lane<TLane> lane)
            where TLane : unmanaged =>
            new(owning.Count, owning.Pool1.DataArray, owning.Pool2.DataArray, owning.Pool3.DataArray, owning.Pool4.DataArray, owning.Pool5.DataArray, owning.Pool6.DataArray, owning.Pool7.DataArray, owning.Pool8.DataArray, owning.Pool9.DataArray, owning.Pool10.DataArray, owning.Pool11.DataArray, owning.Pool12.DataArray, owning.Pool13.DataArray, owning.Pool14.DataArray, owning.Pool15.DataArray);
    }

    extension<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(GroupRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> owning)
        where TEntity : unmanaged, IEntity<TEntity>
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
        where T4 : unmanaged, IComponent
        where T5 : unmanaged, IComponent
        where T6 : unmanaged, IComponent
        where T7 : unmanaged, IComponent
        where T8 : unmanaged, IComponent
        where T9 : unmanaged, IComponent
        where T10 : unmanaged, IComponent
        where T11 : unmanaged, IComponent
        where T12 : unmanaged, IComponent
        where T13 : unmanaged, IComponent
        where T14 : unmanaged, IComponent
        where T15 : unmanaged, IComponent
        where T16 : unmanaged, IComponent
    {
        public VectorRows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TLane> Vectors256<TLane>(Lane<TLane> lane)
            where TLane : unmanaged =>
            new(owning.Count, owning.Pool1.DataArray, owning.Pool2.DataArray, owning.Pool3.DataArray, owning.Pool4.DataArray, owning.Pool5.DataArray, owning.Pool6.DataArray, owning.Pool7.DataArray, owning.Pool8.DataArray, owning.Pool9.DataArray, owning.Pool10.DataArray, owning.Pool11.DataArray, owning.Pool12.DataArray, owning.Pool13.DataArray, owning.Pool14.DataArray, owning.Pool15.DataArray, owning.Pool16.DataArray);
    }
}

public readonly ref struct VectorRows<T1, TLane>
    where T1 : unmanaged, IComponent
    where TLane : unmanaged
{
    private readonly int _count;
    private readonly T1[] _a1;

    internal VectorRows(int count, T1[] a1)
    {
        VectorSizes.Check(Unsafe.SizeOf<T1>(), typeof(T1));
        _count = count;
        _a1 = a1;
    }

    public Enumerator GetEnumerator() => new(this);

    public readonly ref struct Row
    {
        public readonly ref Vector256<TLane> V1;

        internal Row(ref Vector256<TLane> v1)
        {
            V1 = ref v1;
        }
    }

    // Few fields, and Dispose reads only the tail, so the JIT keeps spans and index in registers.
    public ref struct Enumerator
    {
        private readonly Span<Vector256<TLane>> _v1;
        private int _i; // rows handed out so far; the tail row is one past the vectors
        private readonly Tail? _tail; // null when the count fills the vectors exactly

        internal Enumerator(VectorRows<T1, TLane> rows)
        {
            var count = rows._count;
            _v1 = MemoryMarshal.Cast<T1, Vector256<TLane>>(rows._a1.AsSpan(0, count));
            var covered = (int)((long)_v1.Length * VectorSizes.Bytes / Unsafe.SizeOf<T1>());
            _i = 0;
            _tail = null;
            if (covered < count)
            {
                var tail = Tail.Rent();
                tail.Set(covered, count - covered, rows._a1);
                _tail = tail;
            }
        }

        // The spans share a length, so one tail test serves all.
        public readonly Row Current
        {
            get
            {
                var i = _i - 1;
                var tail = i >= _v1.Length;
                return new(ref (tail ? ref _tail!.Vector1() : ref _v1[i]));
            }
        }

        public bool MoveNext()
        {
            var i = _i;
            if (i < _v1.Length)
            {
                _i = i + 1;
                return true;
            }

            var tail = _tail;
            if (tail is null || i > _v1.Length)
            {
                tail?.Flush();
                return false;
            }

            tail.Fill(); // the leftover components, spare lanes zeroed, as one more row
            _i = i + 1;
            return true;
        }

        public void Dispose() => _tail?.Flush();
    }

    // The last row's buffer, on the heap so refs can point at it; one spare per thread.
    private sealed class Tail() : VectorTail(1)
    {
        [ThreadStatic]
        private static Tail? _spare;

        private T1[] _a1 = [];

        public static Tail Rent()
        {
            var t = _spare;
            if (t is null)
            {
                return new();
            }

            _spare = null;
            return t;
        }

        public void Set(int start, int length, T1[] a1)
        {
            Begin(start, length);
            _a1 = a1;
        }

        public ref Vector256<TLane> Vector1() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(0))[0];

        public void Fill()
        {
            FillBlock(_a1, 0);
            Filled = true;
        }

        // Writes back only if the tail row was handed out.
        public void Flush()
        {
            if (Filled)
            {
                Filled = false;
                FlushBlock(_a1, 0);
            }

            _a1 = [];
            _spare = this;
        }
    }
}

public readonly ref struct VectorRows<T1, T2, TLane>
    where T1 : unmanaged, IComponent
    where T2 : unmanaged, IComponent
    where TLane : unmanaged
{
    private readonly int _count;
    private readonly T1[] _a1;
    private readonly T2[] _a2;

    internal VectorRows(int count, T1[] a1, T2[] a2)
    {
        var size = VectorSizes.Check(Unsafe.SizeOf<T1>(), typeof(T1));
        VectorSizes.Same(size, Unsafe.SizeOf<T2>(), typeof(T1), typeof(T2));
        _count = count;
        _a1 = a1;
        _a2 = a2;
    }

    public Enumerator GetEnumerator() => new(this);

    public readonly ref struct Row
    {
        public readonly ref Vector256<TLane> V1;
        public readonly ref Vector256<TLane> V2;

        internal Row(ref Vector256<TLane> v1, ref Vector256<TLane> v2)
        {
            V1 = ref v1;
            V2 = ref v2;
        }
    }

    // Few fields, and Dispose reads only the tail, so the JIT keeps spans and index in registers.
    public ref struct Enumerator
    {
        private readonly Span<Vector256<TLane>> _v1;
        private readonly Span<Vector256<TLane>> _v2;
        private int _i; // rows handed out so far; the tail row is one past the vectors
        private readonly Tail? _tail; // null when the count fills the vectors exactly

        internal Enumerator(VectorRows<T1, T2, TLane> rows)
        {
            var count = rows._count;
            _v1 = MemoryMarshal.Cast<T1, Vector256<TLane>>(rows._a1.AsSpan(0, count));
            _v2 = MemoryMarshal.Cast<T2, Vector256<TLane>>(rows._a2.AsSpan(0, count));
            var covered = (int)((long)_v1.Length * VectorSizes.Bytes / Unsafe.SizeOf<T1>());
            _i = 0;
            _tail = null;
            if (covered < count)
            {
                var tail = Tail.Rent();
                tail.Set(covered, count - covered, rows._a1, rows._a2);
                _tail = tail;
            }
        }

        // The spans share a length, so one tail test serves all.
        public readonly Row Current
        {
            get
            {
                var i = _i - 1;
                var tail = i >= _v1.Length;
                return new(ref (tail ? ref _tail!.Vector1() : ref _v1[i]), ref (tail ? ref _tail!.Vector2() : ref _v2[i]));
            }
        }

        public bool MoveNext()
        {
            var i = _i;
            if (i < _v1.Length)
            {
                _i = i + 1;
                return true;
            }

            var tail = _tail;
            if (tail is null || i > _v1.Length)
            {
                tail?.Flush();
                return false;
            }

            tail.Fill(); // the leftover components, spare lanes zeroed, as one more row
            _i = i + 1;
            return true;
        }

        public void Dispose() => _tail?.Flush();
    }

    // The last row's buffer, on the heap so refs can point at it; one spare per thread.
    private sealed class Tail() : VectorTail(2)
    {
        [ThreadStatic]
        private static Tail? _spare;

        private T1[] _a1 = [];
        private T2[] _a2 = [];

        public static Tail Rent()
        {
            var t = _spare;
            if (t is null)
            {
                return new();
            }

            _spare = null;
            return t;
        }

        public void Set(int start, int length, T1[] a1, T2[] a2)
        {
            Begin(start, length);
            _a1 = a1;
            _a2 = a2;
        }

        public ref Vector256<TLane> Vector1() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(0))[0];
        public ref Vector256<TLane> Vector2() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(1))[0];

        public void Fill()
        {
            FillBlock(_a1, 0);
            FillBlock(_a2, 1);
            Filled = true;
        }

        // Writes back only if the tail row was handed out.
        public void Flush()
        {
            if (Filled)
            {
                Filled = false;
                FlushBlock(_a1, 0);
                FlushBlock(_a2, 1);
            }

            _a1 = [];
            _a2 = [];
            _spare = this;
        }
    }
}

public readonly ref struct VectorRows<T1, T2, T3, TLane>
    where T1 : unmanaged, IComponent
    where T2 : unmanaged, IComponent
    where T3 : unmanaged, IComponent
    where TLane : unmanaged
{
    private readonly int _count;
    private readonly T1[] _a1;
    private readonly T2[] _a2;
    private readonly T3[] _a3;

    internal VectorRows(int count, T1[] a1, T2[] a2, T3[] a3)
    {
        var size = VectorSizes.Check(Unsafe.SizeOf<T1>(), typeof(T1));
        VectorSizes.Same(size, Unsafe.SizeOf<T2>(), typeof(T1), typeof(T2));
        VectorSizes.Same(size, Unsafe.SizeOf<T3>(), typeof(T1), typeof(T3));
        _count = count;
        _a1 = a1;
        _a2 = a2;
        _a3 = a3;
    }

    public Enumerator GetEnumerator() => new(this);

    public readonly ref struct Row
    {
        public readonly ref Vector256<TLane> V1;
        public readonly ref Vector256<TLane> V2;
        public readonly ref Vector256<TLane> V3;

        internal Row(ref Vector256<TLane> v1, ref Vector256<TLane> v2, ref Vector256<TLane> v3)
        {
            V1 = ref v1;
            V2 = ref v2;
            V3 = ref v3;
        }
    }

    // Few fields, and Dispose reads only the tail, so the JIT keeps spans and index in registers.
    public ref struct Enumerator
    {
        private readonly Span<Vector256<TLane>> _v1;
        private readonly Span<Vector256<TLane>> _v2;
        private readonly Span<Vector256<TLane>> _v3;
        private int _i; // rows handed out so far; the tail row is one past the vectors
        private readonly Tail? _tail; // null when the count fills the vectors exactly

        internal Enumerator(VectorRows<T1, T2, T3, TLane> rows)
        {
            var count = rows._count;
            _v1 = MemoryMarshal.Cast<T1, Vector256<TLane>>(rows._a1.AsSpan(0, count));
            _v2 = MemoryMarshal.Cast<T2, Vector256<TLane>>(rows._a2.AsSpan(0, count));
            _v3 = MemoryMarshal.Cast<T3, Vector256<TLane>>(rows._a3.AsSpan(0, count));
            var covered = (int)((long)_v1.Length * VectorSizes.Bytes / Unsafe.SizeOf<T1>());
            _i = 0;
            _tail = null;
            if (covered < count)
            {
                var tail = Tail.Rent();
                tail.Set(covered, count - covered, rows._a1, rows._a2, rows._a3);
                _tail = tail;
            }
        }

        // The spans share a length, so one tail test serves all.
        public readonly Row Current
        {
            get
            {
                var i = _i - 1;
                var tail = i >= _v1.Length;
                return new(ref (tail ? ref _tail!.Vector1() : ref _v1[i]), ref (tail ? ref _tail!.Vector2() : ref _v2[i]), ref (tail ? ref _tail!.Vector3() : ref _v3[i]));
            }
        }

        public bool MoveNext()
        {
            var i = _i;
            if (i < _v1.Length)
            {
                _i = i + 1;
                return true;
            }

            var tail = _tail;
            if (tail is null || i > _v1.Length)
            {
                tail?.Flush();
                return false;
            }

            tail.Fill(); // the leftover components, spare lanes zeroed, as one more row
            _i = i + 1;
            return true;
        }

        public void Dispose() => _tail?.Flush();
    }

    // The last row's buffer, on the heap so refs can point at it; one spare per thread.
    private sealed class Tail() : VectorTail(3)
    {
        [ThreadStatic]
        private static Tail? _spare;

        private T1[] _a1 = [];
        private T2[] _a2 = [];
        private T3[] _a3 = [];

        public static Tail Rent()
        {
            var t = _spare;
            if (t is null)
            {
                return new();
            }

            _spare = null;
            return t;
        }

        public void Set(int start, int length, T1[] a1, T2[] a2, T3[] a3)
        {
            Begin(start, length);
            _a1 = a1;
            _a2 = a2;
            _a3 = a3;
        }

        public ref Vector256<TLane> Vector1() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(0))[0];
        public ref Vector256<TLane> Vector2() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(1))[0];
        public ref Vector256<TLane> Vector3() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(2))[0];

        public void Fill()
        {
            FillBlock(_a1, 0);
            FillBlock(_a2, 1);
            FillBlock(_a3, 2);
            Filled = true;
        }

        // Writes back only if the tail row was handed out.
        public void Flush()
        {
            if (Filled)
            {
                Filled = false;
                FlushBlock(_a1, 0);
                FlushBlock(_a2, 1);
                FlushBlock(_a3, 2);
            }

            _a1 = [];
            _a2 = [];
            _a3 = [];
            _spare = this;
        }
    }
}

public readonly ref struct VectorRows<T1, T2, T3, T4, TLane>
    where T1 : unmanaged, IComponent
    where T2 : unmanaged, IComponent
    where T3 : unmanaged, IComponent
    where T4 : unmanaged, IComponent
    where TLane : unmanaged
{
    private readonly int _count;
    private readonly T1[] _a1;
    private readonly T2[] _a2;
    private readonly T3[] _a3;
    private readonly T4[] _a4;

    internal VectorRows(int count, T1[] a1, T2[] a2, T3[] a3, T4[] a4)
    {
        var size = VectorSizes.Check(Unsafe.SizeOf<T1>(), typeof(T1));
        VectorSizes.Same(size, Unsafe.SizeOf<T2>(), typeof(T1), typeof(T2));
        VectorSizes.Same(size, Unsafe.SizeOf<T3>(), typeof(T1), typeof(T3));
        VectorSizes.Same(size, Unsafe.SizeOf<T4>(), typeof(T1), typeof(T4));
        _count = count;
        _a1 = a1;
        _a2 = a2;
        _a3 = a3;
        _a4 = a4;
    }

    public Enumerator GetEnumerator() => new(this);

    public readonly ref struct Row
    {
        public readonly ref Vector256<TLane> V1;
        public readonly ref Vector256<TLane> V2;
        public readonly ref Vector256<TLane> V3;
        public readonly ref Vector256<TLane> V4;

        internal Row(ref Vector256<TLane> v1, ref Vector256<TLane> v2, ref Vector256<TLane> v3, ref Vector256<TLane> v4)
        {
            V1 = ref v1;
            V2 = ref v2;
            V3 = ref v3;
            V4 = ref v4;
        }
    }

    // Few fields, and Dispose reads only the tail, so the JIT keeps spans and index in registers.
    public ref struct Enumerator
    {
        private readonly Span<Vector256<TLane>> _v1;
        private readonly Span<Vector256<TLane>> _v2;
        private readonly Span<Vector256<TLane>> _v3;
        private readonly Span<Vector256<TLane>> _v4;
        private int _i; // rows handed out so far; the tail row is one past the vectors
        private readonly Tail? _tail; // null when the count fills the vectors exactly

        internal Enumerator(VectorRows<T1, T2, T3, T4, TLane> rows)
        {
            var count = rows._count;
            _v1 = MemoryMarshal.Cast<T1, Vector256<TLane>>(rows._a1.AsSpan(0, count));
            _v2 = MemoryMarshal.Cast<T2, Vector256<TLane>>(rows._a2.AsSpan(0, count));
            _v3 = MemoryMarshal.Cast<T3, Vector256<TLane>>(rows._a3.AsSpan(0, count));
            _v4 = MemoryMarshal.Cast<T4, Vector256<TLane>>(rows._a4.AsSpan(0, count));
            var covered = (int)((long)_v1.Length * VectorSizes.Bytes / Unsafe.SizeOf<T1>());
            _i = 0;
            _tail = null;
            if (covered < count)
            {
                var tail = Tail.Rent();
                tail.Set(covered, count - covered, rows._a1, rows._a2, rows._a3, rows._a4);
                _tail = tail;
            }
        }

        // The spans share a length, so one tail test serves all.
        public readonly Row Current
        {
            get
            {
                var i = _i - 1;
                var tail = i >= _v1.Length;
                return new(ref (tail ? ref _tail!.Vector1() : ref _v1[i]), ref (tail ? ref _tail!.Vector2() : ref _v2[i]), ref (tail ? ref _tail!.Vector3() : ref _v3[i]), ref (tail ? ref _tail!.Vector4() : ref _v4[i]));
            }
        }

        public bool MoveNext()
        {
            var i = _i;
            if (i < _v1.Length)
            {
                _i = i + 1;
                return true;
            }

            var tail = _tail;
            if (tail is null || i > _v1.Length)
            {
                tail?.Flush();
                return false;
            }

            tail.Fill(); // the leftover components, spare lanes zeroed, as one more row
            _i = i + 1;
            return true;
        }

        public void Dispose() => _tail?.Flush();
    }

    // The last row's buffer, on the heap so refs can point at it; one spare per thread.
    private sealed class Tail() : VectorTail(4)
    {
        [ThreadStatic]
        private static Tail? _spare;

        private T1[] _a1 = [];
        private T2[] _a2 = [];
        private T3[] _a3 = [];
        private T4[] _a4 = [];

        public static Tail Rent()
        {
            var t = _spare;
            if (t is null)
            {
                return new();
            }

            _spare = null;
            return t;
        }

        public void Set(int start, int length, T1[] a1, T2[] a2, T3[] a3, T4[] a4)
        {
            Begin(start, length);
            _a1 = a1;
            _a2 = a2;
            _a3 = a3;
            _a4 = a4;
        }

        public ref Vector256<TLane> Vector1() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(0))[0];
        public ref Vector256<TLane> Vector2() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(1))[0];
        public ref Vector256<TLane> Vector3() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(2))[0];
        public ref Vector256<TLane> Vector4() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(3))[0];

        public void Fill()
        {
            FillBlock(_a1, 0);
            FillBlock(_a2, 1);
            FillBlock(_a3, 2);
            FillBlock(_a4, 3);
            Filled = true;
        }

        // Writes back only if the tail row was handed out.
        public void Flush()
        {
            if (Filled)
            {
                Filled = false;
                FlushBlock(_a1, 0);
                FlushBlock(_a2, 1);
                FlushBlock(_a3, 2);
                FlushBlock(_a4, 3);
            }

            _a1 = [];
            _a2 = [];
            _a3 = [];
            _a4 = [];
            _spare = this;
        }
    }
}

public readonly ref struct VectorRows<T1, T2, T3, T4, T5, TLane>
    where T1 : unmanaged, IComponent
    where T2 : unmanaged, IComponent
    where T3 : unmanaged, IComponent
    where T4 : unmanaged, IComponent
    where T5 : unmanaged, IComponent
    where TLane : unmanaged
{
    private readonly int _count;
    private readonly T1[] _a1;
    private readonly T2[] _a2;
    private readonly T3[] _a3;
    private readonly T4[] _a4;
    private readonly T5[] _a5;

    internal VectorRows(int count, T1[] a1, T2[] a2, T3[] a3, T4[] a4, T5[] a5)
    {
        var size = VectorSizes.Check(Unsafe.SizeOf<T1>(), typeof(T1));
        VectorSizes.Same(size, Unsafe.SizeOf<T2>(), typeof(T1), typeof(T2));
        VectorSizes.Same(size, Unsafe.SizeOf<T3>(), typeof(T1), typeof(T3));
        VectorSizes.Same(size, Unsafe.SizeOf<T4>(), typeof(T1), typeof(T4));
        VectorSizes.Same(size, Unsafe.SizeOf<T5>(), typeof(T1), typeof(T5));
        _count = count;
        _a1 = a1;
        _a2 = a2;
        _a3 = a3;
        _a4 = a4;
        _a5 = a5;
    }

    public Enumerator GetEnumerator() => new(this);

    public readonly ref struct Row
    {
        public readonly ref Vector256<TLane> V1;
        public readonly ref Vector256<TLane> V2;
        public readonly ref Vector256<TLane> V3;
        public readonly ref Vector256<TLane> V4;
        public readonly ref Vector256<TLane> V5;

        internal Row(ref Vector256<TLane> v1, ref Vector256<TLane> v2, ref Vector256<TLane> v3, ref Vector256<TLane> v4, ref Vector256<TLane> v5)
        {
            V1 = ref v1;
            V2 = ref v2;
            V3 = ref v3;
            V4 = ref v4;
            V5 = ref v5;
        }
    }

    // Few fields, and Dispose reads only the tail, so the JIT keeps spans and index in registers.
    public ref struct Enumerator
    {
        private readonly Span<Vector256<TLane>> _v1;
        private readonly Span<Vector256<TLane>> _v2;
        private readonly Span<Vector256<TLane>> _v3;
        private readonly Span<Vector256<TLane>> _v4;
        private readonly Span<Vector256<TLane>> _v5;
        private int _i; // rows handed out so far; the tail row is one past the vectors
        private readonly Tail? _tail; // null when the count fills the vectors exactly

        internal Enumerator(VectorRows<T1, T2, T3, T4, T5, TLane> rows)
        {
            var count = rows._count;
            _v1 = MemoryMarshal.Cast<T1, Vector256<TLane>>(rows._a1.AsSpan(0, count));
            _v2 = MemoryMarshal.Cast<T2, Vector256<TLane>>(rows._a2.AsSpan(0, count));
            _v3 = MemoryMarshal.Cast<T3, Vector256<TLane>>(rows._a3.AsSpan(0, count));
            _v4 = MemoryMarshal.Cast<T4, Vector256<TLane>>(rows._a4.AsSpan(0, count));
            _v5 = MemoryMarshal.Cast<T5, Vector256<TLane>>(rows._a5.AsSpan(0, count));
            var covered = (int)((long)_v1.Length * VectorSizes.Bytes / Unsafe.SizeOf<T1>());
            _i = 0;
            _tail = null;
            if (covered < count)
            {
                var tail = Tail.Rent();
                tail.Set(covered, count - covered, rows._a1, rows._a2, rows._a3, rows._a4, rows._a5);
                _tail = tail;
            }
        }

        // The spans share a length, so one tail test serves all.
        public readonly Row Current
        {
            get
            {
                var i = _i - 1;
                var tail = i >= _v1.Length;
                return new(ref (tail ? ref _tail!.Vector1() : ref _v1[i]), ref (tail ? ref _tail!.Vector2() : ref _v2[i]), ref (tail ? ref _tail!.Vector3() : ref _v3[i]), ref (tail ? ref _tail!.Vector4() : ref _v4[i]), ref (tail ? ref _tail!.Vector5() : ref _v5[i]));
            }
        }

        public bool MoveNext()
        {
            var i = _i;
            if (i < _v1.Length)
            {
                _i = i + 1;
                return true;
            }

            var tail = _tail;
            if (tail is null || i > _v1.Length)
            {
                tail?.Flush();
                return false;
            }

            tail.Fill(); // the leftover components, spare lanes zeroed, as one more row
            _i = i + 1;
            return true;
        }

        public void Dispose() => _tail?.Flush();
    }

    // The last row's buffer, on the heap so refs can point at it; one spare per thread.
    private sealed class Tail() : VectorTail(5)
    {
        [ThreadStatic]
        private static Tail? _spare;

        private T1[] _a1 = [];
        private T2[] _a2 = [];
        private T3[] _a3 = [];
        private T4[] _a4 = [];
        private T5[] _a5 = [];

        public static Tail Rent()
        {
            var t = _spare;
            if (t is null)
            {
                return new();
            }

            _spare = null;
            return t;
        }

        public void Set(int start, int length, T1[] a1, T2[] a2, T3[] a3, T4[] a4, T5[] a5)
        {
            Begin(start, length);
            _a1 = a1;
            _a2 = a2;
            _a3 = a3;
            _a4 = a4;
            _a5 = a5;
        }

        public ref Vector256<TLane> Vector1() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(0))[0];
        public ref Vector256<TLane> Vector2() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(1))[0];
        public ref Vector256<TLane> Vector3() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(2))[0];
        public ref Vector256<TLane> Vector4() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(3))[0];
        public ref Vector256<TLane> Vector5() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(4))[0];

        public void Fill()
        {
            FillBlock(_a1, 0);
            FillBlock(_a2, 1);
            FillBlock(_a3, 2);
            FillBlock(_a4, 3);
            FillBlock(_a5, 4);
            Filled = true;
        }

        // Writes back only if the tail row was handed out.
        public void Flush()
        {
            if (Filled)
            {
                Filled = false;
                FlushBlock(_a1, 0);
                FlushBlock(_a2, 1);
                FlushBlock(_a3, 2);
                FlushBlock(_a4, 3);
                FlushBlock(_a5, 4);
            }

            _a1 = [];
            _a2 = [];
            _a3 = [];
            _a4 = [];
            _a5 = [];
            _spare = this;
        }
    }
}

public readonly ref struct VectorRows<T1, T2, T3, T4, T5, T6, TLane>
    where T1 : unmanaged, IComponent
    where T2 : unmanaged, IComponent
    where T3 : unmanaged, IComponent
    where T4 : unmanaged, IComponent
    where T5 : unmanaged, IComponent
    where T6 : unmanaged, IComponent
    where TLane : unmanaged
{
    private readonly int _count;
    private readonly T1[] _a1;
    private readonly T2[] _a2;
    private readonly T3[] _a3;
    private readonly T4[] _a4;
    private readonly T5[] _a5;
    private readonly T6[] _a6;

    internal VectorRows(int count, T1[] a1, T2[] a2, T3[] a3, T4[] a4, T5[] a5, T6[] a6)
    {
        var size = VectorSizes.Check(Unsafe.SizeOf<T1>(), typeof(T1));
        VectorSizes.Same(size, Unsafe.SizeOf<T2>(), typeof(T1), typeof(T2));
        VectorSizes.Same(size, Unsafe.SizeOf<T3>(), typeof(T1), typeof(T3));
        VectorSizes.Same(size, Unsafe.SizeOf<T4>(), typeof(T1), typeof(T4));
        VectorSizes.Same(size, Unsafe.SizeOf<T5>(), typeof(T1), typeof(T5));
        VectorSizes.Same(size, Unsafe.SizeOf<T6>(), typeof(T1), typeof(T6));
        _count = count;
        _a1 = a1;
        _a2 = a2;
        _a3 = a3;
        _a4 = a4;
        _a5 = a5;
        _a6 = a6;
    }

    public Enumerator GetEnumerator() => new(this);

    public readonly ref struct Row
    {
        public readonly ref Vector256<TLane> V1;
        public readonly ref Vector256<TLane> V2;
        public readonly ref Vector256<TLane> V3;
        public readonly ref Vector256<TLane> V4;
        public readonly ref Vector256<TLane> V5;
        public readonly ref Vector256<TLane> V6;

        internal Row(ref Vector256<TLane> v1, ref Vector256<TLane> v2, ref Vector256<TLane> v3, ref Vector256<TLane> v4, ref Vector256<TLane> v5, ref Vector256<TLane> v6)
        {
            V1 = ref v1;
            V2 = ref v2;
            V3 = ref v3;
            V4 = ref v4;
            V5 = ref v5;
            V6 = ref v6;
        }
    }

    // Few fields, and Dispose reads only the tail, so the JIT keeps spans and index in registers.
    public ref struct Enumerator
    {
        private readonly Span<Vector256<TLane>> _v1;
        private readonly Span<Vector256<TLane>> _v2;
        private readonly Span<Vector256<TLane>> _v3;
        private readonly Span<Vector256<TLane>> _v4;
        private readonly Span<Vector256<TLane>> _v5;
        private readonly Span<Vector256<TLane>> _v6;
        private int _i; // rows handed out so far; the tail row is one past the vectors
        private readonly Tail? _tail; // null when the count fills the vectors exactly

        internal Enumerator(VectorRows<T1, T2, T3, T4, T5, T6, TLane> rows)
        {
            var count = rows._count;
            _v1 = MemoryMarshal.Cast<T1, Vector256<TLane>>(rows._a1.AsSpan(0, count));
            _v2 = MemoryMarshal.Cast<T2, Vector256<TLane>>(rows._a2.AsSpan(0, count));
            _v3 = MemoryMarshal.Cast<T3, Vector256<TLane>>(rows._a3.AsSpan(0, count));
            _v4 = MemoryMarshal.Cast<T4, Vector256<TLane>>(rows._a4.AsSpan(0, count));
            _v5 = MemoryMarshal.Cast<T5, Vector256<TLane>>(rows._a5.AsSpan(0, count));
            _v6 = MemoryMarshal.Cast<T6, Vector256<TLane>>(rows._a6.AsSpan(0, count));
            var covered = (int)((long)_v1.Length * VectorSizes.Bytes / Unsafe.SizeOf<T1>());
            _i = 0;
            _tail = null;
            if (covered < count)
            {
                var tail = Tail.Rent();
                tail.Set(covered, count - covered, rows._a1, rows._a2, rows._a3, rows._a4, rows._a5, rows._a6);
                _tail = tail;
            }
        }

        // The spans share a length, so one tail test serves all.
        public readonly Row Current
        {
            get
            {
                var i = _i - 1;
                var tail = i >= _v1.Length;
                return new(ref (tail ? ref _tail!.Vector1() : ref _v1[i]), ref (tail ? ref _tail!.Vector2() : ref _v2[i]), ref (tail ? ref _tail!.Vector3() : ref _v3[i]), ref (tail ? ref _tail!.Vector4() : ref _v4[i]), ref (tail ? ref _tail!.Vector5() : ref _v5[i]), ref (tail ? ref _tail!.Vector6() : ref _v6[i]));
            }
        }

        public bool MoveNext()
        {
            var i = _i;
            if (i < _v1.Length)
            {
                _i = i + 1;
                return true;
            }

            var tail = _tail;
            if (tail is null || i > _v1.Length)
            {
                tail?.Flush();
                return false;
            }

            tail.Fill(); // the leftover components, spare lanes zeroed, as one more row
            _i = i + 1;
            return true;
        }

        public void Dispose() => _tail?.Flush();
    }

    // The last row's buffer, on the heap so refs can point at it; one spare per thread.
    private sealed class Tail() : VectorTail(6)
    {
        [ThreadStatic]
        private static Tail? _spare;

        private T1[] _a1 = [];
        private T2[] _a2 = [];
        private T3[] _a3 = [];
        private T4[] _a4 = [];
        private T5[] _a5 = [];
        private T6[] _a6 = [];

        public static Tail Rent()
        {
            var t = _spare;
            if (t is null)
            {
                return new();
            }

            _spare = null;
            return t;
        }

        public void Set(int start, int length, T1[] a1, T2[] a2, T3[] a3, T4[] a4, T5[] a5, T6[] a6)
        {
            Begin(start, length);
            _a1 = a1;
            _a2 = a2;
            _a3 = a3;
            _a4 = a4;
            _a5 = a5;
            _a6 = a6;
        }

        public ref Vector256<TLane> Vector1() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(0))[0];
        public ref Vector256<TLane> Vector2() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(1))[0];
        public ref Vector256<TLane> Vector3() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(2))[0];
        public ref Vector256<TLane> Vector4() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(3))[0];
        public ref Vector256<TLane> Vector5() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(4))[0];
        public ref Vector256<TLane> Vector6() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(5))[0];

        public void Fill()
        {
            FillBlock(_a1, 0);
            FillBlock(_a2, 1);
            FillBlock(_a3, 2);
            FillBlock(_a4, 3);
            FillBlock(_a5, 4);
            FillBlock(_a6, 5);
            Filled = true;
        }

        // Writes back only if the tail row was handed out.
        public void Flush()
        {
            if (Filled)
            {
                Filled = false;
                FlushBlock(_a1, 0);
                FlushBlock(_a2, 1);
                FlushBlock(_a3, 2);
                FlushBlock(_a4, 3);
                FlushBlock(_a5, 4);
                FlushBlock(_a6, 5);
            }

            _a1 = [];
            _a2 = [];
            _a3 = [];
            _a4 = [];
            _a5 = [];
            _a6 = [];
            _spare = this;
        }
    }
}

public readonly ref struct VectorRows<T1, T2, T3, T4, T5, T6, T7, TLane>
    where T1 : unmanaged, IComponent
    where T2 : unmanaged, IComponent
    where T3 : unmanaged, IComponent
    where T4 : unmanaged, IComponent
    where T5 : unmanaged, IComponent
    where T6 : unmanaged, IComponent
    where T7 : unmanaged, IComponent
    where TLane : unmanaged
{
    private readonly int _count;
    private readonly T1[] _a1;
    private readonly T2[] _a2;
    private readonly T3[] _a3;
    private readonly T4[] _a4;
    private readonly T5[] _a5;
    private readonly T6[] _a6;
    private readonly T7[] _a7;

    internal VectorRows(int count, T1[] a1, T2[] a2, T3[] a3, T4[] a4, T5[] a5, T6[] a6, T7[] a7)
    {
        var size = VectorSizes.Check(Unsafe.SizeOf<T1>(), typeof(T1));
        VectorSizes.Same(size, Unsafe.SizeOf<T2>(), typeof(T1), typeof(T2));
        VectorSizes.Same(size, Unsafe.SizeOf<T3>(), typeof(T1), typeof(T3));
        VectorSizes.Same(size, Unsafe.SizeOf<T4>(), typeof(T1), typeof(T4));
        VectorSizes.Same(size, Unsafe.SizeOf<T5>(), typeof(T1), typeof(T5));
        VectorSizes.Same(size, Unsafe.SizeOf<T6>(), typeof(T1), typeof(T6));
        VectorSizes.Same(size, Unsafe.SizeOf<T7>(), typeof(T1), typeof(T7));
        _count = count;
        _a1 = a1;
        _a2 = a2;
        _a3 = a3;
        _a4 = a4;
        _a5 = a5;
        _a6 = a6;
        _a7 = a7;
    }

    public Enumerator GetEnumerator() => new(this);

    public readonly ref struct Row
    {
        public readonly ref Vector256<TLane> V1;
        public readonly ref Vector256<TLane> V2;
        public readonly ref Vector256<TLane> V3;
        public readonly ref Vector256<TLane> V4;
        public readonly ref Vector256<TLane> V5;
        public readonly ref Vector256<TLane> V6;
        public readonly ref Vector256<TLane> V7;

        internal Row(ref Vector256<TLane> v1, ref Vector256<TLane> v2, ref Vector256<TLane> v3, ref Vector256<TLane> v4, ref Vector256<TLane> v5, ref Vector256<TLane> v6, ref Vector256<TLane> v7)
        {
            V1 = ref v1;
            V2 = ref v2;
            V3 = ref v3;
            V4 = ref v4;
            V5 = ref v5;
            V6 = ref v6;
            V7 = ref v7;
        }
    }

    // Few fields, and Dispose reads only the tail, so the JIT keeps spans and index in registers.
    public ref struct Enumerator
    {
        private readonly Span<Vector256<TLane>> _v1;
        private readonly Span<Vector256<TLane>> _v2;
        private readonly Span<Vector256<TLane>> _v3;
        private readonly Span<Vector256<TLane>> _v4;
        private readonly Span<Vector256<TLane>> _v5;
        private readonly Span<Vector256<TLane>> _v6;
        private readonly Span<Vector256<TLane>> _v7;
        private int _i; // rows handed out so far; the tail row is one past the vectors
        private readonly Tail? _tail; // null when the count fills the vectors exactly

        internal Enumerator(VectorRows<T1, T2, T3, T4, T5, T6, T7, TLane> rows)
        {
            var count = rows._count;
            _v1 = MemoryMarshal.Cast<T1, Vector256<TLane>>(rows._a1.AsSpan(0, count));
            _v2 = MemoryMarshal.Cast<T2, Vector256<TLane>>(rows._a2.AsSpan(0, count));
            _v3 = MemoryMarshal.Cast<T3, Vector256<TLane>>(rows._a3.AsSpan(0, count));
            _v4 = MemoryMarshal.Cast<T4, Vector256<TLane>>(rows._a4.AsSpan(0, count));
            _v5 = MemoryMarshal.Cast<T5, Vector256<TLane>>(rows._a5.AsSpan(0, count));
            _v6 = MemoryMarshal.Cast<T6, Vector256<TLane>>(rows._a6.AsSpan(0, count));
            _v7 = MemoryMarshal.Cast<T7, Vector256<TLane>>(rows._a7.AsSpan(0, count));
            var covered = (int)((long)_v1.Length * VectorSizes.Bytes / Unsafe.SizeOf<T1>());
            _i = 0;
            _tail = null;
            if (covered < count)
            {
                var tail = Tail.Rent();
                tail.Set(covered, count - covered, rows._a1, rows._a2, rows._a3, rows._a4, rows._a5, rows._a6, rows._a7);
                _tail = tail;
            }
        }

        // The spans share a length, so one tail test serves all.
        public readonly Row Current
        {
            get
            {
                var i = _i - 1;
                var tail = i >= _v1.Length;
                return new(ref (tail ? ref _tail!.Vector1() : ref _v1[i]), ref (tail ? ref _tail!.Vector2() : ref _v2[i]), ref (tail ? ref _tail!.Vector3() : ref _v3[i]), ref (tail ? ref _tail!.Vector4() : ref _v4[i]), ref (tail ? ref _tail!.Vector5() : ref _v5[i]), ref (tail ? ref _tail!.Vector6() : ref _v6[i]), ref (tail ? ref _tail!.Vector7() : ref _v7[i]));
            }
        }

        public bool MoveNext()
        {
            var i = _i;
            if (i < _v1.Length)
            {
                _i = i + 1;
                return true;
            }

            var tail = _tail;
            if (tail is null || i > _v1.Length)
            {
                tail?.Flush();
                return false;
            }

            tail.Fill(); // the leftover components, spare lanes zeroed, as one more row
            _i = i + 1;
            return true;
        }

        public void Dispose() => _tail?.Flush();
    }

    // The last row's buffer, on the heap so refs can point at it; one spare per thread.
    private sealed class Tail() : VectorTail(7)
    {
        [ThreadStatic]
        private static Tail? _spare;

        private T1[] _a1 = [];
        private T2[] _a2 = [];
        private T3[] _a3 = [];
        private T4[] _a4 = [];
        private T5[] _a5 = [];
        private T6[] _a6 = [];
        private T7[] _a7 = [];

        public static Tail Rent()
        {
            var t = _spare;
            if (t is null)
            {
                return new();
            }

            _spare = null;
            return t;
        }

        public void Set(int start, int length, T1[] a1, T2[] a2, T3[] a3, T4[] a4, T5[] a5, T6[] a6, T7[] a7)
        {
            Begin(start, length);
            _a1 = a1;
            _a2 = a2;
            _a3 = a3;
            _a4 = a4;
            _a5 = a5;
            _a6 = a6;
            _a7 = a7;
        }

        public ref Vector256<TLane> Vector1() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(0))[0];
        public ref Vector256<TLane> Vector2() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(1))[0];
        public ref Vector256<TLane> Vector3() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(2))[0];
        public ref Vector256<TLane> Vector4() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(3))[0];
        public ref Vector256<TLane> Vector5() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(4))[0];
        public ref Vector256<TLane> Vector6() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(5))[0];
        public ref Vector256<TLane> Vector7() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(6))[0];

        public void Fill()
        {
            FillBlock(_a1, 0);
            FillBlock(_a2, 1);
            FillBlock(_a3, 2);
            FillBlock(_a4, 3);
            FillBlock(_a5, 4);
            FillBlock(_a6, 5);
            FillBlock(_a7, 6);
            Filled = true;
        }

        // Writes back only if the tail row was handed out.
        public void Flush()
        {
            if (Filled)
            {
                Filled = false;
                FlushBlock(_a1, 0);
                FlushBlock(_a2, 1);
                FlushBlock(_a3, 2);
                FlushBlock(_a4, 3);
                FlushBlock(_a5, 4);
                FlushBlock(_a6, 5);
                FlushBlock(_a7, 6);
            }

            _a1 = [];
            _a2 = [];
            _a3 = [];
            _a4 = [];
            _a5 = [];
            _a6 = [];
            _a7 = [];
            _spare = this;
        }
    }
}

public readonly ref struct VectorRows<T1, T2, T3, T4, T5, T6, T7, T8, TLane>
    where T1 : unmanaged, IComponent
    where T2 : unmanaged, IComponent
    where T3 : unmanaged, IComponent
    where T4 : unmanaged, IComponent
    where T5 : unmanaged, IComponent
    where T6 : unmanaged, IComponent
    where T7 : unmanaged, IComponent
    where T8 : unmanaged, IComponent
    where TLane : unmanaged
{
    private readonly int _count;
    private readonly T1[] _a1;
    private readonly T2[] _a2;
    private readonly T3[] _a3;
    private readonly T4[] _a4;
    private readonly T5[] _a5;
    private readonly T6[] _a6;
    private readonly T7[] _a7;
    private readonly T8[] _a8;

    internal VectorRows(int count, T1[] a1, T2[] a2, T3[] a3, T4[] a4, T5[] a5, T6[] a6, T7[] a7, T8[] a8)
    {
        var size = VectorSizes.Check(Unsafe.SizeOf<T1>(), typeof(T1));
        VectorSizes.Same(size, Unsafe.SizeOf<T2>(), typeof(T1), typeof(T2));
        VectorSizes.Same(size, Unsafe.SizeOf<T3>(), typeof(T1), typeof(T3));
        VectorSizes.Same(size, Unsafe.SizeOf<T4>(), typeof(T1), typeof(T4));
        VectorSizes.Same(size, Unsafe.SizeOf<T5>(), typeof(T1), typeof(T5));
        VectorSizes.Same(size, Unsafe.SizeOf<T6>(), typeof(T1), typeof(T6));
        VectorSizes.Same(size, Unsafe.SizeOf<T7>(), typeof(T1), typeof(T7));
        VectorSizes.Same(size, Unsafe.SizeOf<T8>(), typeof(T1), typeof(T8));
        _count = count;
        _a1 = a1;
        _a2 = a2;
        _a3 = a3;
        _a4 = a4;
        _a5 = a5;
        _a6 = a6;
        _a7 = a7;
        _a8 = a8;
    }

    public Enumerator GetEnumerator() => new(this);

    public readonly ref struct Row
    {
        public readonly ref Vector256<TLane> V1;
        public readonly ref Vector256<TLane> V2;
        public readonly ref Vector256<TLane> V3;
        public readonly ref Vector256<TLane> V4;
        public readonly ref Vector256<TLane> V5;
        public readonly ref Vector256<TLane> V6;
        public readonly ref Vector256<TLane> V7;
        public readonly ref Vector256<TLane> V8;

        internal Row(ref Vector256<TLane> v1, ref Vector256<TLane> v2, ref Vector256<TLane> v3, ref Vector256<TLane> v4, ref Vector256<TLane> v5, ref Vector256<TLane> v6, ref Vector256<TLane> v7, ref Vector256<TLane> v8)
        {
            V1 = ref v1;
            V2 = ref v2;
            V3 = ref v3;
            V4 = ref v4;
            V5 = ref v5;
            V6 = ref v6;
            V7 = ref v7;
            V8 = ref v8;
        }
    }

    // Few fields, and Dispose reads only the tail, so the JIT keeps spans and index in registers.
    public ref struct Enumerator
    {
        private readonly Span<Vector256<TLane>> _v1;
        private readonly Span<Vector256<TLane>> _v2;
        private readonly Span<Vector256<TLane>> _v3;
        private readonly Span<Vector256<TLane>> _v4;
        private readonly Span<Vector256<TLane>> _v5;
        private readonly Span<Vector256<TLane>> _v6;
        private readonly Span<Vector256<TLane>> _v7;
        private readonly Span<Vector256<TLane>> _v8;
        private int _i; // rows handed out so far; the tail row is one past the vectors
        private readonly Tail? _tail; // null when the count fills the vectors exactly

        internal Enumerator(VectorRows<T1, T2, T3, T4, T5, T6, T7, T8, TLane> rows)
        {
            var count = rows._count;
            _v1 = MemoryMarshal.Cast<T1, Vector256<TLane>>(rows._a1.AsSpan(0, count));
            _v2 = MemoryMarshal.Cast<T2, Vector256<TLane>>(rows._a2.AsSpan(0, count));
            _v3 = MemoryMarshal.Cast<T3, Vector256<TLane>>(rows._a3.AsSpan(0, count));
            _v4 = MemoryMarshal.Cast<T4, Vector256<TLane>>(rows._a4.AsSpan(0, count));
            _v5 = MemoryMarshal.Cast<T5, Vector256<TLane>>(rows._a5.AsSpan(0, count));
            _v6 = MemoryMarshal.Cast<T6, Vector256<TLane>>(rows._a6.AsSpan(0, count));
            _v7 = MemoryMarshal.Cast<T7, Vector256<TLane>>(rows._a7.AsSpan(0, count));
            _v8 = MemoryMarshal.Cast<T8, Vector256<TLane>>(rows._a8.AsSpan(0, count));
            var covered = (int)((long)_v1.Length * VectorSizes.Bytes / Unsafe.SizeOf<T1>());
            _i = 0;
            _tail = null;
            if (covered < count)
            {
                var tail = Tail.Rent();
                tail.Set(covered, count - covered, rows._a1, rows._a2, rows._a3, rows._a4, rows._a5, rows._a6, rows._a7, rows._a8);
                _tail = tail;
            }
        }

        // The spans share a length, so one tail test serves all.
        public readonly Row Current
        {
            get
            {
                var i = _i - 1;
                var tail = i >= _v1.Length;
                return new(ref (tail ? ref _tail!.Vector1() : ref _v1[i]), ref (tail ? ref _tail!.Vector2() : ref _v2[i]), ref (tail ? ref _tail!.Vector3() : ref _v3[i]), ref (tail ? ref _tail!.Vector4() : ref _v4[i]), ref (tail ? ref _tail!.Vector5() : ref _v5[i]), ref (tail ? ref _tail!.Vector6() : ref _v6[i]), ref (tail ? ref _tail!.Vector7() : ref _v7[i]), ref (tail ? ref _tail!.Vector8() : ref _v8[i]));
            }
        }

        public bool MoveNext()
        {
            var i = _i;
            if (i < _v1.Length)
            {
                _i = i + 1;
                return true;
            }

            var tail = _tail;
            if (tail is null || i > _v1.Length)
            {
                tail?.Flush();
                return false;
            }

            tail.Fill(); // the leftover components, spare lanes zeroed, as one more row
            _i = i + 1;
            return true;
        }

        public void Dispose() => _tail?.Flush();
    }

    // The last row's buffer, on the heap so refs can point at it; one spare per thread.
    private sealed class Tail() : VectorTail(8)
    {
        [ThreadStatic]
        private static Tail? _spare;

        private T1[] _a1 = [];
        private T2[] _a2 = [];
        private T3[] _a3 = [];
        private T4[] _a4 = [];
        private T5[] _a5 = [];
        private T6[] _a6 = [];
        private T7[] _a7 = [];
        private T8[] _a8 = [];

        public static Tail Rent()
        {
            var t = _spare;
            if (t is null)
            {
                return new();
            }

            _spare = null;
            return t;
        }

        public void Set(int start, int length, T1[] a1, T2[] a2, T3[] a3, T4[] a4, T5[] a5, T6[] a6, T7[] a7, T8[] a8)
        {
            Begin(start, length);
            _a1 = a1;
            _a2 = a2;
            _a3 = a3;
            _a4 = a4;
            _a5 = a5;
            _a6 = a6;
            _a7 = a7;
            _a8 = a8;
        }

        public ref Vector256<TLane> Vector1() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(0))[0];
        public ref Vector256<TLane> Vector2() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(1))[0];
        public ref Vector256<TLane> Vector3() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(2))[0];
        public ref Vector256<TLane> Vector4() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(3))[0];
        public ref Vector256<TLane> Vector5() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(4))[0];
        public ref Vector256<TLane> Vector6() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(5))[0];
        public ref Vector256<TLane> Vector7() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(6))[0];
        public ref Vector256<TLane> Vector8() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(7))[0];

        public void Fill()
        {
            FillBlock(_a1, 0);
            FillBlock(_a2, 1);
            FillBlock(_a3, 2);
            FillBlock(_a4, 3);
            FillBlock(_a5, 4);
            FillBlock(_a6, 5);
            FillBlock(_a7, 6);
            FillBlock(_a8, 7);
            Filled = true;
        }

        // Writes back only if the tail row was handed out.
        public void Flush()
        {
            if (Filled)
            {
                Filled = false;
                FlushBlock(_a1, 0);
                FlushBlock(_a2, 1);
                FlushBlock(_a3, 2);
                FlushBlock(_a4, 3);
                FlushBlock(_a5, 4);
                FlushBlock(_a6, 5);
                FlushBlock(_a7, 6);
                FlushBlock(_a8, 7);
            }

            _a1 = [];
            _a2 = [];
            _a3 = [];
            _a4 = [];
            _a5 = [];
            _a6 = [];
            _a7 = [];
            _a8 = [];
            _spare = this;
        }
    }
}

public readonly ref struct VectorRows<T1, T2, T3, T4, T5, T6, T7, T8, T9, TLane>
    where T1 : unmanaged, IComponent
    where T2 : unmanaged, IComponent
    where T3 : unmanaged, IComponent
    where T4 : unmanaged, IComponent
    where T5 : unmanaged, IComponent
    where T6 : unmanaged, IComponent
    where T7 : unmanaged, IComponent
    where T8 : unmanaged, IComponent
    where T9 : unmanaged, IComponent
    where TLane : unmanaged
{
    private readonly int _count;
    private readonly T1[] _a1;
    private readonly T2[] _a2;
    private readonly T3[] _a3;
    private readonly T4[] _a4;
    private readonly T5[] _a5;
    private readonly T6[] _a6;
    private readonly T7[] _a7;
    private readonly T8[] _a8;
    private readonly T9[] _a9;

    internal VectorRows(int count, T1[] a1, T2[] a2, T3[] a3, T4[] a4, T5[] a5, T6[] a6, T7[] a7, T8[] a8, T9[] a9)
    {
        var size = VectorSizes.Check(Unsafe.SizeOf<T1>(), typeof(T1));
        VectorSizes.Same(size, Unsafe.SizeOf<T2>(), typeof(T1), typeof(T2));
        VectorSizes.Same(size, Unsafe.SizeOf<T3>(), typeof(T1), typeof(T3));
        VectorSizes.Same(size, Unsafe.SizeOf<T4>(), typeof(T1), typeof(T4));
        VectorSizes.Same(size, Unsafe.SizeOf<T5>(), typeof(T1), typeof(T5));
        VectorSizes.Same(size, Unsafe.SizeOf<T6>(), typeof(T1), typeof(T6));
        VectorSizes.Same(size, Unsafe.SizeOf<T7>(), typeof(T1), typeof(T7));
        VectorSizes.Same(size, Unsafe.SizeOf<T8>(), typeof(T1), typeof(T8));
        VectorSizes.Same(size, Unsafe.SizeOf<T9>(), typeof(T1), typeof(T9));
        _count = count;
        _a1 = a1;
        _a2 = a2;
        _a3 = a3;
        _a4 = a4;
        _a5 = a5;
        _a6 = a6;
        _a7 = a7;
        _a8 = a8;
        _a9 = a9;
    }

    public Enumerator GetEnumerator() => new(this);

    public readonly ref struct Row
    {
        public readonly ref Vector256<TLane> V1;
        public readonly ref Vector256<TLane> V2;
        public readonly ref Vector256<TLane> V3;
        public readonly ref Vector256<TLane> V4;
        public readonly ref Vector256<TLane> V5;
        public readonly ref Vector256<TLane> V6;
        public readonly ref Vector256<TLane> V7;
        public readonly ref Vector256<TLane> V8;
        public readonly ref Vector256<TLane> V9;

        internal Row(ref Vector256<TLane> v1, ref Vector256<TLane> v2, ref Vector256<TLane> v3, ref Vector256<TLane> v4, ref Vector256<TLane> v5, ref Vector256<TLane> v6, ref Vector256<TLane> v7, ref Vector256<TLane> v8, ref Vector256<TLane> v9)
        {
            V1 = ref v1;
            V2 = ref v2;
            V3 = ref v3;
            V4 = ref v4;
            V5 = ref v5;
            V6 = ref v6;
            V7 = ref v7;
            V8 = ref v8;
            V9 = ref v9;
        }
    }

    // Few fields, and Dispose reads only the tail, so the JIT keeps spans and index in registers.
    public ref struct Enumerator
    {
        private readonly Span<Vector256<TLane>> _v1;
        private readonly Span<Vector256<TLane>> _v2;
        private readonly Span<Vector256<TLane>> _v3;
        private readonly Span<Vector256<TLane>> _v4;
        private readonly Span<Vector256<TLane>> _v5;
        private readonly Span<Vector256<TLane>> _v6;
        private readonly Span<Vector256<TLane>> _v7;
        private readonly Span<Vector256<TLane>> _v8;
        private readonly Span<Vector256<TLane>> _v9;
        private int _i; // rows handed out so far; the tail row is one past the vectors
        private readonly Tail? _tail; // null when the count fills the vectors exactly

        internal Enumerator(VectorRows<T1, T2, T3, T4, T5, T6, T7, T8, T9, TLane> rows)
        {
            var count = rows._count;
            _v1 = MemoryMarshal.Cast<T1, Vector256<TLane>>(rows._a1.AsSpan(0, count));
            _v2 = MemoryMarshal.Cast<T2, Vector256<TLane>>(rows._a2.AsSpan(0, count));
            _v3 = MemoryMarshal.Cast<T3, Vector256<TLane>>(rows._a3.AsSpan(0, count));
            _v4 = MemoryMarshal.Cast<T4, Vector256<TLane>>(rows._a4.AsSpan(0, count));
            _v5 = MemoryMarshal.Cast<T5, Vector256<TLane>>(rows._a5.AsSpan(0, count));
            _v6 = MemoryMarshal.Cast<T6, Vector256<TLane>>(rows._a6.AsSpan(0, count));
            _v7 = MemoryMarshal.Cast<T7, Vector256<TLane>>(rows._a7.AsSpan(0, count));
            _v8 = MemoryMarshal.Cast<T8, Vector256<TLane>>(rows._a8.AsSpan(0, count));
            _v9 = MemoryMarshal.Cast<T9, Vector256<TLane>>(rows._a9.AsSpan(0, count));
            var covered = (int)((long)_v1.Length * VectorSizes.Bytes / Unsafe.SizeOf<T1>());
            _i = 0;
            _tail = null;
            if (covered < count)
            {
                var tail = Tail.Rent();
                tail.Set(covered, count - covered, rows._a1, rows._a2, rows._a3, rows._a4, rows._a5, rows._a6, rows._a7, rows._a8, rows._a9);
                _tail = tail;
            }
        }

        // The spans share a length, so one tail test serves all.
        public readonly Row Current
        {
            get
            {
                var i = _i - 1;
                var tail = i >= _v1.Length;
                return new(ref (tail ? ref _tail!.Vector1() : ref _v1[i]), ref (tail ? ref _tail!.Vector2() : ref _v2[i]), ref (tail ? ref _tail!.Vector3() : ref _v3[i]), ref (tail ? ref _tail!.Vector4() : ref _v4[i]), ref (tail ? ref _tail!.Vector5() : ref _v5[i]), ref (tail ? ref _tail!.Vector6() : ref _v6[i]), ref (tail ? ref _tail!.Vector7() : ref _v7[i]), ref (tail ? ref _tail!.Vector8() : ref _v8[i]), ref (tail ? ref _tail!.Vector9() : ref _v9[i]));
            }
        }

        public bool MoveNext()
        {
            var i = _i;
            if (i < _v1.Length)
            {
                _i = i + 1;
                return true;
            }

            var tail = _tail;
            if (tail is null || i > _v1.Length)
            {
                tail?.Flush();
                return false;
            }

            tail.Fill(); // the leftover components, spare lanes zeroed, as one more row
            _i = i + 1;
            return true;
        }

        public void Dispose() => _tail?.Flush();
    }

    // The last row's buffer, on the heap so refs can point at it; one spare per thread.
    private sealed class Tail() : VectorTail(9)
    {
        [ThreadStatic]
        private static Tail? _spare;

        private T1[] _a1 = [];
        private T2[] _a2 = [];
        private T3[] _a3 = [];
        private T4[] _a4 = [];
        private T5[] _a5 = [];
        private T6[] _a6 = [];
        private T7[] _a7 = [];
        private T8[] _a8 = [];
        private T9[] _a9 = [];

        public static Tail Rent()
        {
            var t = _spare;
            if (t is null)
            {
                return new();
            }

            _spare = null;
            return t;
        }

        public void Set(int start, int length, T1[] a1, T2[] a2, T3[] a3, T4[] a4, T5[] a5, T6[] a6, T7[] a7, T8[] a8, T9[] a9)
        {
            Begin(start, length);
            _a1 = a1;
            _a2 = a2;
            _a3 = a3;
            _a4 = a4;
            _a5 = a5;
            _a6 = a6;
            _a7 = a7;
            _a8 = a8;
            _a9 = a9;
        }

        public ref Vector256<TLane> Vector1() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(0))[0];
        public ref Vector256<TLane> Vector2() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(1))[0];
        public ref Vector256<TLane> Vector3() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(2))[0];
        public ref Vector256<TLane> Vector4() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(3))[0];
        public ref Vector256<TLane> Vector5() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(4))[0];
        public ref Vector256<TLane> Vector6() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(5))[0];
        public ref Vector256<TLane> Vector7() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(6))[0];
        public ref Vector256<TLane> Vector8() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(7))[0];
        public ref Vector256<TLane> Vector9() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(8))[0];

        public void Fill()
        {
            FillBlock(_a1, 0);
            FillBlock(_a2, 1);
            FillBlock(_a3, 2);
            FillBlock(_a4, 3);
            FillBlock(_a5, 4);
            FillBlock(_a6, 5);
            FillBlock(_a7, 6);
            FillBlock(_a8, 7);
            FillBlock(_a9, 8);
            Filled = true;
        }

        // Writes back only if the tail row was handed out.
        public void Flush()
        {
            if (Filled)
            {
                Filled = false;
                FlushBlock(_a1, 0);
                FlushBlock(_a2, 1);
                FlushBlock(_a3, 2);
                FlushBlock(_a4, 3);
                FlushBlock(_a5, 4);
                FlushBlock(_a6, 5);
                FlushBlock(_a7, 6);
                FlushBlock(_a8, 7);
                FlushBlock(_a9, 8);
            }

            _a1 = [];
            _a2 = [];
            _a3 = [];
            _a4 = [];
            _a5 = [];
            _a6 = [];
            _a7 = [];
            _a8 = [];
            _a9 = [];
            _spare = this;
        }
    }
}

public readonly ref struct VectorRows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TLane>
    where T1 : unmanaged, IComponent
    where T2 : unmanaged, IComponent
    where T3 : unmanaged, IComponent
    where T4 : unmanaged, IComponent
    where T5 : unmanaged, IComponent
    where T6 : unmanaged, IComponent
    where T7 : unmanaged, IComponent
    where T8 : unmanaged, IComponent
    where T9 : unmanaged, IComponent
    where T10 : unmanaged, IComponent
    where TLane : unmanaged
{
    private readonly int _count;
    private readonly T1[] _a1;
    private readonly T2[] _a2;
    private readonly T3[] _a3;
    private readonly T4[] _a4;
    private readonly T5[] _a5;
    private readonly T6[] _a6;
    private readonly T7[] _a7;
    private readonly T8[] _a8;
    private readonly T9[] _a9;
    private readonly T10[] _a10;

    internal VectorRows(int count, T1[] a1, T2[] a2, T3[] a3, T4[] a4, T5[] a5, T6[] a6, T7[] a7, T8[] a8, T9[] a9, T10[] a10)
    {
        var size = VectorSizes.Check(Unsafe.SizeOf<T1>(), typeof(T1));
        VectorSizes.Same(size, Unsafe.SizeOf<T2>(), typeof(T1), typeof(T2));
        VectorSizes.Same(size, Unsafe.SizeOf<T3>(), typeof(T1), typeof(T3));
        VectorSizes.Same(size, Unsafe.SizeOf<T4>(), typeof(T1), typeof(T4));
        VectorSizes.Same(size, Unsafe.SizeOf<T5>(), typeof(T1), typeof(T5));
        VectorSizes.Same(size, Unsafe.SizeOf<T6>(), typeof(T1), typeof(T6));
        VectorSizes.Same(size, Unsafe.SizeOf<T7>(), typeof(T1), typeof(T7));
        VectorSizes.Same(size, Unsafe.SizeOf<T8>(), typeof(T1), typeof(T8));
        VectorSizes.Same(size, Unsafe.SizeOf<T9>(), typeof(T1), typeof(T9));
        VectorSizes.Same(size, Unsafe.SizeOf<T10>(), typeof(T1), typeof(T10));
        _count = count;
        _a1 = a1;
        _a2 = a2;
        _a3 = a3;
        _a4 = a4;
        _a5 = a5;
        _a6 = a6;
        _a7 = a7;
        _a8 = a8;
        _a9 = a9;
        _a10 = a10;
    }

    public Enumerator GetEnumerator() => new(this);

    public readonly ref struct Row
    {
        public readonly ref Vector256<TLane> V1;
        public readonly ref Vector256<TLane> V2;
        public readonly ref Vector256<TLane> V3;
        public readonly ref Vector256<TLane> V4;
        public readonly ref Vector256<TLane> V5;
        public readonly ref Vector256<TLane> V6;
        public readonly ref Vector256<TLane> V7;
        public readonly ref Vector256<TLane> V8;
        public readonly ref Vector256<TLane> V9;
        public readonly ref Vector256<TLane> V10;

        internal Row(ref Vector256<TLane> v1, ref Vector256<TLane> v2, ref Vector256<TLane> v3, ref Vector256<TLane> v4, ref Vector256<TLane> v5, ref Vector256<TLane> v6, ref Vector256<TLane> v7, ref Vector256<TLane> v8, ref Vector256<TLane> v9, ref Vector256<TLane> v10)
        {
            V1 = ref v1;
            V2 = ref v2;
            V3 = ref v3;
            V4 = ref v4;
            V5 = ref v5;
            V6 = ref v6;
            V7 = ref v7;
            V8 = ref v8;
            V9 = ref v9;
            V10 = ref v10;
        }
    }

    // Few fields, and Dispose reads only the tail, so the JIT keeps spans and index in registers.
    public ref struct Enumerator
    {
        private readonly Span<Vector256<TLane>> _v1;
        private readonly Span<Vector256<TLane>> _v2;
        private readonly Span<Vector256<TLane>> _v3;
        private readonly Span<Vector256<TLane>> _v4;
        private readonly Span<Vector256<TLane>> _v5;
        private readonly Span<Vector256<TLane>> _v6;
        private readonly Span<Vector256<TLane>> _v7;
        private readonly Span<Vector256<TLane>> _v8;
        private readonly Span<Vector256<TLane>> _v9;
        private readonly Span<Vector256<TLane>> _v10;
        private int _i; // rows handed out so far; the tail row is one past the vectors
        private readonly Tail? _tail; // null when the count fills the vectors exactly

        internal Enumerator(VectorRows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TLane> rows)
        {
            var count = rows._count;
            _v1 = MemoryMarshal.Cast<T1, Vector256<TLane>>(rows._a1.AsSpan(0, count));
            _v2 = MemoryMarshal.Cast<T2, Vector256<TLane>>(rows._a2.AsSpan(0, count));
            _v3 = MemoryMarshal.Cast<T3, Vector256<TLane>>(rows._a3.AsSpan(0, count));
            _v4 = MemoryMarshal.Cast<T4, Vector256<TLane>>(rows._a4.AsSpan(0, count));
            _v5 = MemoryMarshal.Cast<T5, Vector256<TLane>>(rows._a5.AsSpan(0, count));
            _v6 = MemoryMarshal.Cast<T6, Vector256<TLane>>(rows._a6.AsSpan(0, count));
            _v7 = MemoryMarshal.Cast<T7, Vector256<TLane>>(rows._a7.AsSpan(0, count));
            _v8 = MemoryMarshal.Cast<T8, Vector256<TLane>>(rows._a8.AsSpan(0, count));
            _v9 = MemoryMarshal.Cast<T9, Vector256<TLane>>(rows._a9.AsSpan(0, count));
            _v10 = MemoryMarshal.Cast<T10, Vector256<TLane>>(rows._a10.AsSpan(0, count));
            var covered = (int)((long)_v1.Length * VectorSizes.Bytes / Unsafe.SizeOf<T1>());
            _i = 0;
            _tail = null;
            if (covered < count)
            {
                var tail = Tail.Rent();
                tail.Set(covered, count - covered, rows._a1, rows._a2, rows._a3, rows._a4, rows._a5, rows._a6, rows._a7, rows._a8, rows._a9, rows._a10);
                _tail = tail;
            }
        }

        // The spans share a length, so one tail test serves all.
        public readonly Row Current
        {
            get
            {
                var i = _i - 1;
                var tail = i >= _v1.Length;
                return new(ref (tail ? ref _tail!.Vector1() : ref _v1[i]), ref (tail ? ref _tail!.Vector2() : ref _v2[i]), ref (tail ? ref _tail!.Vector3() : ref _v3[i]), ref (tail ? ref _tail!.Vector4() : ref _v4[i]), ref (tail ? ref _tail!.Vector5() : ref _v5[i]), ref (tail ? ref _tail!.Vector6() : ref _v6[i]), ref (tail ? ref _tail!.Vector7() : ref _v7[i]), ref (tail ? ref _tail!.Vector8() : ref _v8[i]), ref (tail ? ref _tail!.Vector9() : ref _v9[i]), ref (tail ? ref _tail!.Vector10() : ref _v10[i]));
            }
        }

        public bool MoveNext()
        {
            var i = _i;
            if (i < _v1.Length)
            {
                _i = i + 1;
                return true;
            }

            var tail = _tail;
            if (tail is null || i > _v1.Length)
            {
                tail?.Flush();
                return false;
            }

            tail.Fill(); // the leftover components, spare lanes zeroed, as one more row
            _i = i + 1;
            return true;
        }

        public void Dispose() => _tail?.Flush();
    }

    // The last row's buffer, on the heap so refs can point at it; one spare per thread.
    private sealed class Tail() : VectorTail(10)
    {
        [ThreadStatic]
        private static Tail? _spare;

        private T1[] _a1 = [];
        private T2[] _a2 = [];
        private T3[] _a3 = [];
        private T4[] _a4 = [];
        private T5[] _a5 = [];
        private T6[] _a6 = [];
        private T7[] _a7 = [];
        private T8[] _a8 = [];
        private T9[] _a9 = [];
        private T10[] _a10 = [];

        public static Tail Rent()
        {
            var t = _spare;
            if (t is null)
            {
                return new();
            }

            _spare = null;
            return t;
        }

        public void Set(int start, int length, T1[] a1, T2[] a2, T3[] a3, T4[] a4, T5[] a5, T6[] a6, T7[] a7, T8[] a8, T9[] a9, T10[] a10)
        {
            Begin(start, length);
            _a1 = a1;
            _a2 = a2;
            _a3 = a3;
            _a4 = a4;
            _a5 = a5;
            _a6 = a6;
            _a7 = a7;
            _a8 = a8;
            _a9 = a9;
            _a10 = a10;
        }

        public ref Vector256<TLane> Vector1() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(0))[0];
        public ref Vector256<TLane> Vector2() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(1))[0];
        public ref Vector256<TLane> Vector3() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(2))[0];
        public ref Vector256<TLane> Vector4() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(3))[0];
        public ref Vector256<TLane> Vector5() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(4))[0];
        public ref Vector256<TLane> Vector6() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(5))[0];
        public ref Vector256<TLane> Vector7() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(6))[0];
        public ref Vector256<TLane> Vector8() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(7))[0];
        public ref Vector256<TLane> Vector9() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(8))[0];
        public ref Vector256<TLane> Vector10() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(9))[0];

        public void Fill()
        {
            FillBlock(_a1, 0);
            FillBlock(_a2, 1);
            FillBlock(_a3, 2);
            FillBlock(_a4, 3);
            FillBlock(_a5, 4);
            FillBlock(_a6, 5);
            FillBlock(_a7, 6);
            FillBlock(_a8, 7);
            FillBlock(_a9, 8);
            FillBlock(_a10, 9);
            Filled = true;
        }

        // Writes back only if the tail row was handed out.
        public void Flush()
        {
            if (Filled)
            {
                Filled = false;
                FlushBlock(_a1, 0);
                FlushBlock(_a2, 1);
                FlushBlock(_a3, 2);
                FlushBlock(_a4, 3);
                FlushBlock(_a5, 4);
                FlushBlock(_a6, 5);
                FlushBlock(_a7, 6);
                FlushBlock(_a8, 7);
                FlushBlock(_a9, 8);
                FlushBlock(_a10, 9);
            }

            _a1 = [];
            _a2 = [];
            _a3 = [];
            _a4 = [];
            _a5 = [];
            _a6 = [];
            _a7 = [];
            _a8 = [];
            _a9 = [];
            _a10 = [];
            _spare = this;
        }
    }
}

public readonly ref struct VectorRows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TLane>
    where T1 : unmanaged, IComponent
    where T2 : unmanaged, IComponent
    where T3 : unmanaged, IComponent
    where T4 : unmanaged, IComponent
    where T5 : unmanaged, IComponent
    where T6 : unmanaged, IComponent
    where T7 : unmanaged, IComponent
    where T8 : unmanaged, IComponent
    where T9 : unmanaged, IComponent
    where T10 : unmanaged, IComponent
    where T11 : unmanaged, IComponent
    where TLane : unmanaged
{
    private readonly int _count;
    private readonly T1[] _a1;
    private readonly T2[] _a2;
    private readonly T3[] _a3;
    private readonly T4[] _a4;
    private readonly T5[] _a5;
    private readonly T6[] _a6;
    private readonly T7[] _a7;
    private readonly T8[] _a8;
    private readonly T9[] _a9;
    private readonly T10[] _a10;
    private readonly T11[] _a11;

    internal VectorRows(int count, T1[] a1, T2[] a2, T3[] a3, T4[] a4, T5[] a5, T6[] a6, T7[] a7, T8[] a8, T9[] a9, T10[] a10, T11[] a11)
    {
        var size = VectorSizes.Check(Unsafe.SizeOf<T1>(), typeof(T1));
        VectorSizes.Same(size, Unsafe.SizeOf<T2>(), typeof(T1), typeof(T2));
        VectorSizes.Same(size, Unsafe.SizeOf<T3>(), typeof(T1), typeof(T3));
        VectorSizes.Same(size, Unsafe.SizeOf<T4>(), typeof(T1), typeof(T4));
        VectorSizes.Same(size, Unsafe.SizeOf<T5>(), typeof(T1), typeof(T5));
        VectorSizes.Same(size, Unsafe.SizeOf<T6>(), typeof(T1), typeof(T6));
        VectorSizes.Same(size, Unsafe.SizeOf<T7>(), typeof(T1), typeof(T7));
        VectorSizes.Same(size, Unsafe.SizeOf<T8>(), typeof(T1), typeof(T8));
        VectorSizes.Same(size, Unsafe.SizeOf<T9>(), typeof(T1), typeof(T9));
        VectorSizes.Same(size, Unsafe.SizeOf<T10>(), typeof(T1), typeof(T10));
        VectorSizes.Same(size, Unsafe.SizeOf<T11>(), typeof(T1), typeof(T11));
        _count = count;
        _a1 = a1;
        _a2 = a2;
        _a3 = a3;
        _a4 = a4;
        _a5 = a5;
        _a6 = a6;
        _a7 = a7;
        _a8 = a8;
        _a9 = a9;
        _a10 = a10;
        _a11 = a11;
    }

    public Enumerator GetEnumerator() => new(this);

    public readonly ref struct Row
    {
        public readonly ref Vector256<TLane> V1;
        public readonly ref Vector256<TLane> V2;
        public readonly ref Vector256<TLane> V3;
        public readonly ref Vector256<TLane> V4;
        public readonly ref Vector256<TLane> V5;
        public readonly ref Vector256<TLane> V6;
        public readonly ref Vector256<TLane> V7;
        public readonly ref Vector256<TLane> V8;
        public readonly ref Vector256<TLane> V9;
        public readonly ref Vector256<TLane> V10;
        public readonly ref Vector256<TLane> V11;

        internal Row(ref Vector256<TLane> v1, ref Vector256<TLane> v2, ref Vector256<TLane> v3, ref Vector256<TLane> v4, ref Vector256<TLane> v5, ref Vector256<TLane> v6, ref Vector256<TLane> v7, ref Vector256<TLane> v8, ref Vector256<TLane> v9, ref Vector256<TLane> v10, ref Vector256<TLane> v11)
        {
            V1 = ref v1;
            V2 = ref v2;
            V3 = ref v3;
            V4 = ref v4;
            V5 = ref v5;
            V6 = ref v6;
            V7 = ref v7;
            V8 = ref v8;
            V9 = ref v9;
            V10 = ref v10;
            V11 = ref v11;
        }
    }

    // Few fields, and Dispose reads only the tail, so the JIT keeps spans and index in registers.
    public ref struct Enumerator
    {
        private readonly Span<Vector256<TLane>> _v1;
        private readonly Span<Vector256<TLane>> _v2;
        private readonly Span<Vector256<TLane>> _v3;
        private readonly Span<Vector256<TLane>> _v4;
        private readonly Span<Vector256<TLane>> _v5;
        private readonly Span<Vector256<TLane>> _v6;
        private readonly Span<Vector256<TLane>> _v7;
        private readonly Span<Vector256<TLane>> _v8;
        private readonly Span<Vector256<TLane>> _v9;
        private readonly Span<Vector256<TLane>> _v10;
        private readonly Span<Vector256<TLane>> _v11;
        private int _i; // rows handed out so far; the tail row is one past the vectors
        private readonly Tail? _tail; // null when the count fills the vectors exactly

        internal Enumerator(VectorRows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TLane> rows)
        {
            var count = rows._count;
            _v1 = MemoryMarshal.Cast<T1, Vector256<TLane>>(rows._a1.AsSpan(0, count));
            _v2 = MemoryMarshal.Cast<T2, Vector256<TLane>>(rows._a2.AsSpan(0, count));
            _v3 = MemoryMarshal.Cast<T3, Vector256<TLane>>(rows._a3.AsSpan(0, count));
            _v4 = MemoryMarshal.Cast<T4, Vector256<TLane>>(rows._a4.AsSpan(0, count));
            _v5 = MemoryMarshal.Cast<T5, Vector256<TLane>>(rows._a5.AsSpan(0, count));
            _v6 = MemoryMarshal.Cast<T6, Vector256<TLane>>(rows._a6.AsSpan(0, count));
            _v7 = MemoryMarshal.Cast<T7, Vector256<TLane>>(rows._a7.AsSpan(0, count));
            _v8 = MemoryMarshal.Cast<T8, Vector256<TLane>>(rows._a8.AsSpan(0, count));
            _v9 = MemoryMarshal.Cast<T9, Vector256<TLane>>(rows._a9.AsSpan(0, count));
            _v10 = MemoryMarshal.Cast<T10, Vector256<TLane>>(rows._a10.AsSpan(0, count));
            _v11 = MemoryMarshal.Cast<T11, Vector256<TLane>>(rows._a11.AsSpan(0, count));
            var covered = (int)((long)_v1.Length * VectorSizes.Bytes / Unsafe.SizeOf<T1>());
            _i = 0;
            _tail = null;
            if (covered < count)
            {
                var tail = Tail.Rent();
                tail.Set(covered, count - covered, rows._a1, rows._a2, rows._a3, rows._a4, rows._a5, rows._a6, rows._a7, rows._a8, rows._a9, rows._a10, rows._a11);
                _tail = tail;
            }
        }

        // The spans share a length, so one tail test serves all.
        public readonly Row Current
        {
            get
            {
                var i = _i - 1;
                var tail = i >= _v1.Length;
                return new(ref (tail ? ref _tail!.Vector1() : ref _v1[i]), ref (tail ? ref _tail!.Vector2() : ref _v2[i]), ref (tail ? ref _tail!.Vector3() : ref _v3[i]), ref (tail ? ref _tail!.Vector4() : ref _v4[i]), ref (tail ? ref _tail!.Vector5() : ref _v5[i]), ref (tail ? ref _tail!.Vector6() : ref _v6[i]), ref (tail ? ref _tail!.Vector7() : ref _v7[i]), ref (tail ? ref _tail!.Vector8() : ref _v8[i]), ref (tail ? ref _tail!.Vector9() : ref _v9[i]), ref (tail ? ref _tail!.Vector10() : ref _v10[i]), ref (tail ? ref _tail!.Vector11() : ref _v11[i]));
            }
        }

        public bool MoveNext()
        {
            var i = _i;
            if (i < _v1.Length)
            {
                _i = i + 1;
                return true;
            }

            var tail = _tail;
            if (tail is null || i > _v1.Length)
            {
                tail?.Flush();
                return false;
            }

            tail.Fill(); // the leftover components, spare lanes zeroed, as one more row
            _i = i + 1;
            return true;
        }

        public void Dispose() => _tail?.Flush();
    }

    // The last row's buffer, on the heap so refs can point at it; one spare per thread.
    private sealed class Tail() : VectorTail(11)
    {
        [ThreadStatic]
        private static Tail? _spare;

        private T1[] _a1 = [];
        private T2[] _a2 = [];
        private T3[] _a3 = [];
        private T4[] _a4 = [];
        private T5[] _a5 = [];
        private T6[] _a6 = [];
        private T7[] _a7 = [];
        private T8[] _a8 = [];
        private T9[] _a9 = [];
        private T10[] _a10 = [];
        private T11[] _a11 = [];

        public static Tail Rent()
        {
            var t = _spare;
            if (t is null)
            {
                return new();
            }

            _spare = null;
            return t;
        }

        public void Set(int start, int length, T1[] a1, T2[] a2, T3[] a3, T4[] a4, T5[] a5, T6[] a6, T7[] a7, T8[] a8, T9[] a9, T10[] a10, T11[] a11)
        {
            Begin(start, length);
            _a1 = a1;
            _a2 = a2;
            _a3 = a3;
            _a4 = a4;
            _a5 = a5;
            _a6 = a6;
            _a7 = a7;
            _a8 = a8;
            _a9 = a9;
            _a10 = a10;
            _a11 = a11;
        }

        public ref Vector256<TLane> Vector1() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(0))[0];
        public ref Vector256<TLane> Vector2() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(1))[0];
        public ref Vector256<TLane> Vector3() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(2))[0];
        public ref Vector256<TLane> Vector4() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(3))[0];
        public ref Vector256<TLane> Vector5() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(4))[0];
        public ref Vector256<TLane> Vector6() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(5))[0];
        public ref Vector256<TLane> Vector7() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(6))[0];
        public ref Vector256<TLane> Vector8() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(7))[0];
        public ref Vector256<TLane> Vector9() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(8))[0];
        public ref Vector256<TLane> Vector10() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(9))[0];
        public ref Vector256<TLane> Vector11() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(10))[0];

        public void Fill()
        {
            FillBlock(_a1, 0);
            FillBlock(_a2, 1);
            FillBlock(_a3, 2);
            FillBlock(_a4, 3);
            FillBlock(_a5, 4);
            FillBlock(_a6, 5);
            FillBlock(_a7, 6);
            FillBlock(_a8, 7);
            FillBlock(_a9, 8);
            FillBlock(_a10, 9);
            FillBlock(_a11, 10);
            Filled = true;
        }

        // Writes back only if the tail row was handed out.
        public void Flush()
        {
            if (Filled)
            {
                Filled = false;
                FlushBlock(_a1, 0);
                FlushBlock(_a2, 1);
                FlushBlock(_a3, 2);
                FlushBlock(_a4, 3);
                FlushBlock(_a5, 4);
                FlushBlock(_a6, 5);
                FlushBlock(_a7, 6);
                FlushBlock(_a8, 7);
                FlushBlock(_a9, 8);
                FlushBlock(_a10, 9);
                FlushBlock(_a11, 10);
            }

            _a1 = [];
            _a2 = [];
            _a3 = [];
            _a4 = [];
            _a5 = [];
            _a6 = [];
            _a7 = [];
            _a8 = [];
            _a9 = [];
            _a10 = [];
            _a11 = [];
            _spare = this;
        }
    }
}

public readonly ref struct VectorRows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TLane>
    where T1 : unmanaged, IComponent
    where T2 : unmanaged, IComponent
    where T3 : unmanaged, IComponent
    where T4 : unmanaged, IComponent
    where T5 : unmanaged, IComponent
    where T6 : unmanaged, IComponent
    where T7 : unmanaged, IComponent
    where T8 : unmanaged, IComponent
    where T9 : unmanaged, IComponent
    where T10 : unmanaged, IComponent
    where T11 : unmanaged, IComponent
    where T12 : unmanaged, IComponent
    where TLane : unmanaged
{
    private readonly int _count;
    private readonly T1[] _a1;
    private readonly T2[] _a2;
    private readonly T3[] _a3;
    private readonly T4[] _a4;
    private readonly T5[] _a5;
    private readonly T6[] _a6;
    private readonly T7[] _a7;
    private readonly T8[] _a8;
    private readonly T9[] _a9;
    private readonly T10[] _a10;
    private readonly T11[] _a11;
    private readonly T12[] _a12;

    internal VectorRows(int count, T1[] a1, T2[] a2, T3[] a3, T4[] a4, T5[] a5, T6[] a6, T7[] a7, T8[] a8, T9[] a9, T10[] a10, T11[] a11, T12[] a12)
    {
        var size = VectorSizes.Check(Unsafe.SizeOf<T1>(), typeof(T1));
        VectorSizes.Same(size, Unsafe.SizeOf<T2>(), typeof(T1), typeof(T2));
        VectorSizes.Same(size, Unsafe.SizeOf<T3>(), typeof(T1), typeof(T3));
        VectorSizes.Same(size, Unsafe.SizeOf<T4>(), typeof(T1), typeof(T4));
        VectorSizes.Same(size, Unsafe.SizeOf<T5>(), typeof(T1), typeof(T5));
        VectorSizes.Same(size, Unsafe.SizeOf<T6>(), typeof(T1), typeof(T6));
        VectorSizes.Same(size, Unsafe.SizeOf<T7>(), typeof(T1), typeof(T7));
        VectorSizes.Same(size, Unsafe.SizeOf<T8>(), typeof(T1), typeof(T8));
        VectorSizes.Same(size, Unsafe.SizeOf<T9>(), typeof(T1), typeof(T9));
        VectorSizes.Same(size, Unsafe.SizeOf<T10>(), typeof(T1), typeof(T10));
        VectorSizes.Same(size, Unsafe.SizeOf<T11>(), typeof(T1), typeof(T11));
        VectorSizes.Same(size, Unsafe.SizeOf<T12>(), typeof(T1), typeof(T12));
        _count = count;
        _a1 = a1;
        _a2 = a2;
        _a3 = a3;
        _a4 = a4;
        _a5 = a5;
        _a6 = a6;
        _a7 = a7;
        _a8 = a8;
        _a9 = a9;
        _a10 = a10;
        _a11 = a11;
        _a12 = a12;
    }

    public Enumerator GetEnumerator() => new(this);

    public readonly ref struct Row
    {
        public readonly ref Vector256<TLane> V1;
        public readonly ref Vector256<TLane> V2;
        public readonly ref Vector256<TLane> V3;
        public readonly ref Vector256<TLane> V4;
        public readonly ref Vector256<TLane> V5;
        public readonly ref Vector256<TLane> V6;
        public readonly ref Vector256<TLane> V7;
        public readonly ref Vector256<TLane> V8;
        public readonly ref Vector256<TLane> V9;
        public readonly ref Vector256<TLane> V10;
        public readonly ref Vector256<TLane> V11;
        public readonly ref Vector256<TLane> V12;

        internal Row(ref Vector256<TLane> v1, ref Vector256<TLane> v2, ref Vector256<TLane> v3, ref Vector256<TLane> v4, ref Vector256<TLane> v5, ref Vector256<TLane> v6, ref Vector256<TLane> v7, ref Vector256<TLane> v8, ref Vector256<TLane> v9, ref Vector256<TLane> v10, ref Vector256<TLane> v11, ref Vector256<TLane> v12)
        {
            V1 = ref v1;
            V2 = ref v2;
            V3 = ref v3;
            V4 = ref v4;
            V5 = ref v5;
            V6 = ref v6;
            V7 = ref v7;
            V8 = ref v8;
            V9 = ref v9;
            V10 = ref v10;
            V11 = ref v11;
            V12 = ref v12;
        }
    }

    // Few fields, and Dispose reads only the tail, so the JIT keeps spans and index in registers.
    public ref struct Enumerator
    {
        private readonly Span<Vector256<TLane>> _v1;
        private readonly Span<Vector256<TLane>> _v2;
        private readonly Span<Vector256<TLane>> _v3;
        private readonly Span<Vector256<TLane>> _v4;
        private readonly Span<Vector256<TLane>> _v5;
        private readonly Span<Vector256<TLane>> _v6;
        private readonly Span<Vector256<TLane>> _v7;
        private readonly Span<Vector256<TLane>> _v8;
        private readonly Span<Vector256<TLane>> _v9;
        private readonly Span<Vector256<TLane>> _v10;
        private readonly Span<Vector256<TLane>> _v11;
        private readonly Span<Vector256<TLane>> _v12;
        private int _i; // rows handed out so far; the tail row is one past the vectors
        private readonly Tail? _tail; // null when the count fills the vectors exactly

        internal Enumerator(VectorRows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TLane> rows)
        {
            var count = rows._count;
            _v1 = MemoryMarshal.Cast<T1, Vector256<TLane>>(rows._a1.AsSpan(0, count));
            _v2 = MemoryMarshal.Cast<T2, Vector256<TLane>>(rows._a2.AsSpan(0, count));
            _v3 = MemoryMarshal.Cast<T3, Vector256<TLane>>(rows._a3.AsSpan(0, count));
            _v4 = MemoryMarshal.Cast<T4, Vector256<TLane>>(rows._a4.AsSpan(0, count));
            _v5 = MemoryMarshal.Cast<T5, Vector256<TLane>>(rows._a5.AsSpan(0, count));
            _v6 = MemoryMarshal.Cast<T6, Vector256<TLane>>(rows._a6.AsSpan(0, count));
            _v7 = MemoryMarshal.Cast<T7, Vector256<TLane>>(rows._a7.AsSpan(0, count));
            _v8 = MemoryMarshal.Cast<T8, Vector256<TLane>>(rows._a8.AsSpan(0, count));
            _v9 = MemoryMarshal.Cast<T9, Vector256<TLane>>(rows._a9.AsSpan(0, count));
            _v10 = MemoryMarshal.Cast<T10, Vector256<TLane>>(rows._a10.AsSpan(0, count));
            _v11 = MemoryMarshal.Cast<T11, Vector256<TLane>>(rows._a11.AsSpan(0, count));
            _v12 = MemoryMarshal.Cast<T12, Vector256<TLane>>(rows._a12.AsSpan(0, count));
            var covered = (int)((long)_v1.Length * VectorSizes.Bytes / Unsafe.SizeOf<T1>());
            _i = 0;
            _tail = null;
            if (covered < count)
            {
                var tail = Tail.Rent();
                tail.Set(covered, count - covered, rows._a1, rows._a2, rows._a3, rows._a4, rows._a5, rows._a6, rows._a7, rows._a8, rows._a9, rows._a10, rows._a11, rows._a12);
                _tail = tail;
            }
        }

        // The spans share a length, so one tail test serves all.
        public readonly Row Current
        {
            get
            {
                var i = _i - 1;
                var tail = i >= _v1.Length;
                return new(ref (tail ? ref _tail!.Vector1() : ref _v1[i]), ref (tail ? ref _tail!.Vector2() : ref _v2[i]), ref (tail ? ref _tail!.Vector3() : ref _v3[i]), ref (tail ? ref _tail!.Vector4() : ref _v4[i]), ref (tail ? ref _tail!.Vector5() : ref _v5[i]), ref (tail ? ref _tail!.Vector6() : ref _v6[i]), ref (tail ? ref _tail!.Vector7() : ref _v7[i]), ref (tail ? ref _tail!.Vector8() : ref _v8[i]), ref (tail ? ref _tail!.Vector9() : ref _v9[i]), ref (tail ? ref _tail!.Vector10() : ref _v10[i]), ref (tail ? ref _tail!.Vector11() : ref _v11[i]), ref (tail ? ref _tail!.Vector12() : ref _v12[i]));
            }
        }

        public bool MoveNext()
        {
            var i = _i;
            if (i < _v1.Length)
            {
                _i = i + 1;
                return true;
            }

            var tail = _tail;
            if (tail is null || i > _v1.Length)
            {
                tail?.Flush();
                return false;
            }

            tail.Fill(); // the leftover components, spare lanes zeroed, as one more row
            _i = i + 1;
            return true;
        }

        public void Dispose() => _tail?.Flush();
    }

    // The last row's buffer, on the heap so refs can point at it; one spare per thread.
    private sealed class Tail() : VectorTail(12)
    {
        [ThreadStatic]
        private static Tail? _spare;

        private T1[] _a1 = [];
        private T2[] _a2 = [];
        private T3[] _a3 = [];
        private T4[] _a4 = [];
        private T5[] _a5 = [];
        private T6[] _a6 = [];
        private T7[] _a7 = [];
        private T8[] _a8 = [];
        private T9[] _a9 = [];
        private T10[] _a10 = [];
        private T11[] _a11 = [];
        private T12[] _a12 = [];

        public static Tail Rent()
        {
            var t = _spare;
            if (t is null)
            {
                return new();
            }

            _spare = null;
            return t;
        }

        public void Set(int start, int length, T1[] a1, T2[] a2, T3[] a3, T4[] a4, T5[] a5, T6[] a6, T7[] a7, T8[] a8, T9[] a9, T10[] a10, T11[] a11, T12[] a12)
        {
            Begin(start, length);
            _a1 = a1;
            _a2 = a2;
            _a3 = a3;
            _a4 = a4;
            _a5 = a5;
            _a6 = a6;
            _a7 = a7;
            _a8 = a8;
            _a9 = a9;
            _a10 = a10;
            _a11 = a11;
            _a12 = a12;
        }

        public ref Vector256<TLane> Vector1() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(0))[0];
        public ref Vector256<TLane> Vector2() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(1))[0];
        public ref Vector256<TLane> Vector3() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(2))[0];
        public ref Vector256<TLane> Vector4() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(3))[0];
        public ref Vector256<TLane> Vector5() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(4))[0];
        public ref Vector256<TLane> Vector6() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(5))[0];
        public ref Vector256<TLane> Vector7() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(6))[0];
        public ref Vector256<TLane> Vector8() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(7))[0];
        public ref Vector256<TLane> Vector9() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(8))[0];
        public ref Vector256<TLane> Vector10() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(9))[0];
        public ref Vector256<TLane> Vector11() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(10))[0];
        public ref Vector256<TLane> Vector12() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(11))[0];

        public void Fill()
        {
            FillBlock(_a1, 0);
            FillBlock(_a2, 1);
            FillBlock(_a3, 2);
            FillBlock(_a4, 3);
            FillBlock(_a5, 4);
            FillBlock(_a6, 5);
            FillBlock(_a7, 6);
            FillBlock(_a8, 7);
            FillBlock(_a9, 8);
            FillBlock(_a10, 9);
            FillBlock(_a11, 10);
            FillBlock(_a12, 11);
            Filled = true;
        }

        // Writes back only if the tail row was handed out.
        public void Flush()
        {
            if (Filled)
            {
                Filled = false;
                FlushBlock(_a1, 0);
                FlushBlock(_a2, 1);
                FlushBlock(_a3, 2);
                FlushBlock(_a4, 3);
                FlushBlock(_a5, 4);
                FlushBlock(_a6, 5);
                FlushBlock(_a7, 6);
                FlushBlock(_a8, 7);
                FlushBlock(_a9, 8);
                FlushBlock(_a10, 9);
                FlushBlock(_a11, 10);
                FlushBlock(_a12, 11);
            }

            _a1 = [];
            _a2 = [];
            _a3 = [];
            _a4 = [];
            _a5 = [];
            _a6 = [];
            _a7 = [];
            _a8 = [];
            _a9 = [];
            _a10 = [];
            _a11 = [];
            _a12 = [];
            _spare = this;
        }
    }
}

public readonly ref struct VectorRows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TLane>
    where T1 : unmanaged, IComponent
    where T2 : unmanaged, IComponent
    where T3 : unmanaged, IComponent
    where T4 : unmanaged, IComponent
    where T5 : unmanaged, IComponent
    where T6 : unmanaged, IComponent
    where T7 : unmanaged, IComponent
    where T8 : unmanaged, IComponent
    where T9 : unmanaged, IComponent
    where T10 : unmanaged, IComponent
    where T11 : unmanaged, IComponent
    where T12 : unmanaged, IComponent
    where T13 : unmanaged, IComponent
    where TLane : unmanaged
{
    private readonly int _count;
    private readonly T1[] _a1;
    private readonly T2[] _a2;
    private readonly T3[] _a3;
    private readonly T4[] _a4;
    private readonly T5[] _a5;
    private readonly T6[] _a6;
    private readonly T7[] _a7;
    private readonly T8[] _a8;
    private readonly T9[] _a9;
    private readonly T10[] _a10;
    private readonly T11[] _a11;
    private readonly T12[] _a12;
    private readonly T13[] _a13;

    internal VectorRows(int count, T1[] a1, T2[] a2, T3[] a3, T4[] a4, T5[] a5, T6[] a6, T7[] a7, T8[] a8, T9[] a9, T10[] a10, T11[] a11, T12[] a12, T13[] a13)
    {
        var size = VectorSizes.Check(Unsafe.SizeOf<T1>(), typeof(T1));
        VectorSizes.Same(size, Unsafe.SizeOf<T2>(), typeof(T1), typeof(T2));
        VectorSizes.Same(size, Unsafe.SizeOf<T3>(), typeof(T1), typeof(T3));
        VectorSizes.Same(size, Unsafe.SizeOf<T4>(), typeof(T1), typeof(T4));
        VectorSizes.Same(size, Unsafe.SizeOf<T5>(), typeof(T1), typeof(T5));
        VectorSizes.Same(size, Unsafe.SizeOf<T6>(), typeof(T1), typeof(T6));
        VectorSizes.Same(size, Unsafe.SizeOf<T7>(), typeof(T1), typeof(T7));
        VectorSizes.Same(size, Unsafe.SizeOf<T8>(), typeof(T1), typeof(T8));
        VectorSizes.Same(size, Unsafe.SizeOf<T9>(), typeof(T1), typeof(T9));
        VectorSizes.Same(size, Unsafe.SizeOf<T10>(), typeof(T1), typeof(T10));
        VectorSizes.Same(size, Unsafe.SizeOf<T11>(), typeof(T1), typeof(T11));
        VectorSizes.Same(size, Unsafe.SizeOf<T12>(), typeof(T1), typeof(T12));
        VectorSizes.Same(size, Unsafe.SizeOf<T13>(), typeof(T1), typeof(T13));
        _count = count;
        _a1 = a1;
        _a2 = a2;
        _a3 = a3;
        _a4 = a4;
        _a5 = a5;
        _a6 = a6;
        _a7 = a7;
        _a8 = a8;
        _a9 = a9;
        _a10 = a10;
        _a11 = a11;
        _a12 = a12;
        _a13 = a13;
    }

    public Enumerator GetEnumerator() => new(this);

    public readonly ref struct Row
    {
        public readonly ref Vector256<TLane> V1;
        public readonly ref Vector256<TLane> V2;
        public readonly ref Vector256<TLane> V3;
        public readonly ref Vector256<TLane> V4;
        public readonly ref Vector256<TLane> V5;
        public readonly ref Vector256<TLane> V6;
        public readonly ref Vector256<TLane> V7;
        public readonly ref Vector256<TLane> V8;
        public readonly ref Vector256<TLane> V9;
        public readonly ref Vector256<TLane> V10;
        public readonly ref Vector256<TLane> V11;
        public readonly ref Vector256<TLane> V12;
        public readonly ref Vector256<TLane> V13;

        internal Row(ref Vector256<TLane> v1, ref Vector256<TLane> v2, ref Vector256<TLane> v3, ref Vector256<TLane> v4, ref Vector256<TLane> v5, ref Vector256<TLane> v6, ref Vector256<TLane> v7, ref Vector256<TLane> v8, ref Vector256<TLane> v9, ref Vector256<TLane> v10, ref Vector256<TLane> v11, ref Vector256<TLane> v12, ref Vector256<TLane> v13)
        {
            V1 = ref v1;
            V2 = ref v2;
            V3 = ref v3;
            V4 = ref v4;
            V5 = ref v5;
            V6 = ref v6;
            V7 = ref v7;
            V8 = ref v8;
            V9 = ref v9;
            V10 = ref v10;
            V11 = ref v11;
            V12 = ref v12;
            V13 = ref v13;
        }
    }

    // Few fields, and Dispose reads only the tail, so the JIT keeps spans and index in registers.
    public ref struct Enumerator
    {
        private readonly Span<Vector256<TLane>> _v1;
        private readonly Span<Vector256<TLane>> _v2;
        private readonly Span<Vector256<TLane>> _v3;
        private readonly Span<Vector256<TLane>> _v4;
        private readonly Span<Vector256<TLane>> _v5;
        private readonly Span<Vector256<TLane>> _v6;
        private readonly Span<Vector256<TLane>> _v7;
        private readonly Span<Vector256<TLane>> _v8;
        private readonly Span<Vector256<TLane>> _v9;
        private readonly Span<Vector256<TLane>> _v10;
        private readonly Span<Vector256<TLane>> _v11;
        private readonly Span<Vector256<TLane>> _v12;
        private readonly Span<Vector256<TLane>> _v13;
        private int _i; // rows handed out so far; the tail row is one past the vectors
        private readonly Tail? _tail; // null when the count fills the vectors exactly

        internal Enumerator(VectorRows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TLane> rows)
        {
            var count = rows._count;
            _v1 = MemoryMarshal.Cast<T1, Vector256<TLane>>(rows._a1.AsSpan(0, count));
            _v2 = MemoryMarshal.Cast<T2, Vector256<TLane>>(rows._a2.AsSpan(0, count));
            _v3 = MemoryMarshal.Cast<T3, Vector256<TLane>>(rows._a3.AsSpan(0, count));
            _v4 = MemoryMarshal.Cast<T4, Vector256<TLane>>(rows._a4.AsSpan(0, count));
            _v5 = MemoryMarshal.Cast<T5, Vector256<TLane>>(rows._a5.AsSpan(0, count));
            _v6 = MemoryMarshal.Cast<T6, Vector256<TLane>>(rows._a6.AsSpan(0, count));
            _v7 = MemoryMarshal.Cast<T7, Vector256<TLane>>(rows._a7.AsSpan(0, count));
            _v8 = MemoryMarshal.Cast<T8, Vector256<TLane>>(rows._a8.AsSpan(0, count));
            _v9 = MemoryMarshal.Cast<T9, Vector256<TLane>>(rows._a9.AsSpan(0, count));
            _v10 = MemoryMarshal.Cast<T10, Vector256<TLane>>(rows._a10.AsSpan(0, count));
            _v11 = MemoryMarshal.Cast<T11, Vector256<TLane>>(rows._a11.AsSpan(0, count));
            _v12 = MemoryMarshal.Cast<T12, Vector256<TLane>>(rows._a12.AsSpan(0, count));
            _v13 = MemoryMarshal.Cast<T13, Vector256<TLane>>(rows._a13.AsSpan(0, count));
            var covered = (int)((long)_v1.Length * VectorSizes.Bytes / Unsafe.SizeOf<T1>());
            _i = 0;
            _tail = null;
            if (covered < count)
            {
                var tail = Tail.Rent();
                tail.Set(covered, count - covered, rows._a1, rows._a2, rows._a3, rows._a4, rows._a5, rows._a6, rows._a7, rows._a8, rows._a9, rows._a10, rows._a11, rows._a12, rows._a13);
                _tail = tail;
            }
        }

        // The spans share a length, so one tail test serves all.
        public readonly Row Current
        {
            get
            {
                var i = _i - 1;
                var tail = i >= _v1.Length;
                return new(ref (tail ? ref _tail!.Vector1() : ref _v1[i]), ref (tail ? ref _tail!.Vector2() : ref _v2[i]), ref (tail ? ref _tail!.Vector3() : ref _v3[i]), ref (tail ? ref _tail!.Vector4() : ref _v4[i]), ref (tail ? ref _tail!.Vector5() : ref _v5[i]), ref (tail ? ref _tail!.Vector6() : ref _v6[i]), ref (tail ? ref _tail!.Vector7() : ref _v7[i]), ref (tail ? ref _tail!.Vector8() : ref _v8[i]), ref (tail ? ref _tail!.Vector9() : ref _v9[i]), ref (tail ? ref _tail!.Vector10() : ref _v10[i]), ref (tail ? ref _tail!.Vector11() : ref _v11[i]), ref (tail ? ref _tail!.Vector12() : ref _v12[i]), ref (tail ? ref _tail!.Vector13() : ref _v13[i]));
            }
        }

        public bool MoveNext()
        {
            var i = _i;
            if (i < _v1.Length)
            {
                _i = i + 1;
                return true;
            }

            var tail = _tail;
            if (tail is null || i > _v1.Length)
            {
                tail?.Flush();
                return false;
            }

            tail.Fill(); // the leftover components, spare lanes zeroed, as one more row
            _i = i + 1;
            return true;
        }

        public void Dispose() => _tail?.Flush();
    }

    // The last row's buffer, on the heap so refs can point at it; one spare per thread.
    private sealed class Tail() : VectorTail(13)
    {
        [ThreadStatic]
        private static Tail? _spare;

        private T1[] _a1 = [];
        private T2[] _a2 = [];
        private T3[] _a3 = [];
        private T4[] _a4 = [];
        private T5[] _a5 = [];
        private T6[] _a6 = [];
        private T7[] _a7 = [];
        private T8[] _a8 = [];
        private T9[] _a9 = [];
        private T10[] _a10 = [];
        private T11[] _a11 = [];
        private T12[] _a12 = [];
        private T13[] _a13 = [];

        public static Tail Rent()
        {
            var t = _spare;
            if (t is null)
            {
                return new();
            }

            _spare = null;
            return t;
        }

        public void Set(int start, int length, T1[] a1, T2[] a2, T3[] a3, T4[] a4, T5[] a5, T6[] a6, T7[] a7, T8[] a8, T9[] a9, T10[] a10, T11[] a11, T12[] a12, T13[] a13)
        {
            Begin(start, length);
            _a1 = a1;
            _a2 = a2;
            _a3 = a3;
            _a4 = a4;
            _a5 = a5;
            _a6 = a6;
            _a7 = a7;
            _a8 = a8;
            _a9 = a9;
            _a10 = a10;
            _a11 = a11;
            _a12 = a12;
            _a13 = a13;
        }

        public ref Vector256<TLane> Vector1() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(0))[0];
        public ref Vector256<TLane> Vector2() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(1))[0];
        public ref Vector256<TLane> Vector3() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(2))[0];
        public ref Vector256<TLane> Vector4() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(3))[0];
        public ref Vector256<TLane> Vector5() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(4))[0];
        public ref Vector256<TLane> Vector6() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(5))[0];
        public ref Vector256<TLane> Vector7() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(6))[0];
        public ref Vector256<TLane> Vector8() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(7))[0];
        public ref Vector256<TLane> Vector9() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(8))[0];
        public ref Vector256<TLane> Vector10() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(9))[0];
        public ref Vector256<TLane> Vector11() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(10))[0];
        public ref Vector256<TLane> Vector12() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(11))[0];
        public ref Vector256<TLane> Vector13() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(12))[0];

        public void Fill()
        {
            FillBlock(_a1, 0);
            FillBlock(_a2, 1);
            FillBlock(_a3, 2);
            FillBlock(_a4, 3);
            FillBlock(_a5, 4);
            FillBlock(_a6, 5);
            FillBlock(_a7, 6);
            FillBlock(_a8, 7);
            FillBlock(_a9, 8);
            FillBlock(_a10, 9);
            FillBlock(_a11, 10);
            FillBlock(_a12, 11);
            FillBlock(_a13, 12);
            Filled = true;
        }

        // Writes back only if the tail row was handed out.
        public void Flush()
        {
            if (Filled)
            {
                Filled = false;
                FlushBlock(_a1, 0);
                FlushBlock(_a2, 1);
                FlushBlock(_a3, 2);
                FlushBlock(_a4, 3);
                FlushBlock(_a5, 4);
                FlushBlock(_a6, 5);
                FlushBlock(_a7, 6);
                FlushBlock(_a8, 7);
                FlushBlock(_a9, 8);
                FlushBlock(_a10, 9);
                FlushBlock(_a11, 10);
                FlushBlock(_a12, 11);
                FlushBlock(_a13, 12);
            }

            _a1 = [];
            _a2 = [];
            _a3 = [];
            _a4 = [];
            _a5 = [];
            _a6 = [];
            _a7 = [];
            _a8 = [];
            _a9 = [];
            _a10 = [];
            _a11 = [];
            _a12 = [];
            _a13 = [];
            _spare = this;
        }
    }
}

public readonly ref struct VectorRows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TLane>
    where T1 : unmanaged, IComponent
    where T2 : unmanaged, IComponent
    where T3 : unmanaged, IComponent
    where T4 : unmanaged, IComponent
    where T5 : unmanaged, IComponent
    where T6 : unmanaged, IComponent
    where T7 : unmanaged, IComponent
    where T8 : unmanaged, IComponent
    where T9 : unmanaged, IComponent
    where T10 : unmanaged, IComponent
    where T11 : unmanaged, IComponent
    where T12 : unmanaged, IComponent
    where T13 : unmanaged, IComponent
    where T14 : unmanaged, IComponent
    where TLane : unmanaged
{
    private readonly int _count;
    private readonly T1[] _a1;
    private readonly T2[] _a2;
    private readonly T3[] _a3;
    private readonly T4[] _a4;
    private readonly T5[] _a5;
    private readonly T6[] _a6;
    private readonly T7[] _a7;
    private readonly T8[] _a8;
    private readonly T9[] _a9;
    private readonly T10[] _a10;
    private readonly T11[] _a11;
    private readonly T12[] _a12;
    private readonly T13[] _a13;
    private readonly T14[] _a14;

    internal VectorRows(int count, T1[] a1, T2[] a2, T3[] a3, T4[] a4, T5[] a5, T6[] a6, T7[] a7, T8[] a8, T9[] a9, T10[] a10, T11[] a11, T12[] a12, T13[] a13, T14[] a14)
    {
        var size = VectorSizes.Check(Unsafe.SizeOf<T1>(), typeof(T1));
        VectorSizes.Same(size, Unsafe.SizeOf<T2>(), typeof(T1), typeof(T2));
        VectorSizes.Same(size, Unsafe.SizeOf<T3>(), typeof(T1), typeof(T3));
        VectorSizes.Same(size, Unsafe.SizeOf<T4>(), typeof(T1), typeof(T4));
        VectorSizes.Same(size, Unsafe.SizeOf<T5>(), typeof(T1), typeof(T5));
        VectorSizes.Same(size, Unsafe.SizeOf<T6>(), typeof(T1), typeof(T6));
        VectorSizes.Same(size, Unsafe.SizeOf<T7>(), typeof(T1), typeof(T7));
        VectorSizes.Same(size, Unsafe.SizeOf<T8>(), typeof(T1), typeof(T8));
        VectorSizes.Same(size, Unsafe.SizeOf<T9>(), typeof(T1), typeof(T9));
        VectorSizes.Same(size, Unsafe.SizeOf<T10>(), typeof(T1), typeof(T10));
        VectorSizes.Same(size, Unsafe.SizeOf<T11>(), typeof(T1), typeof(T11));
        VectorSizes.Same(size, Unsafe.SizeOf<T12>(), typeof(T1), typeof(T12));
        VectorSizes.Same(size, Unsafe.SizeOf<T13>(), typeof(T1), typeof(T13));
        VectorSizes.Same(size, Unsafe.SizeOf<T14>(), typeof(T1), typeof(T14));
        _count = count;
        _a1 = a1;
        _a2 = a2;
        _a3 = a3;
        _a4 = a4;
        _a5 = a5;
        _a6 = a6;
        _a7 = a7;
        _a8 = a8;
        _a9 = a9;
        _a10 = a10;
        _a11 = a11;
        _a12 = a12;
        _a13 = a13;
        _a14 = a14;
    }

    public Enumerator GetEnumerator() => new(this);

    public readonly ref struct Row
    {
        public readonly ref Vector256<TLane> V1;
        public readonly ref Vector256<TLane> V2;
        public readonly ref Vector256<TLane> V3;
        public readonly ref Vector256<TLane> V4;
        public readonly ref Vector256<TLane> V5;
        public readonly ref Vector256<TLane> V6;
        public readonly ref Vector256<TLane> V7;
        public readonly ref Vector256<TLane> V8;
        public readonly ref Vector256<TLane> V9;
        public readonly ref Vector256<TLane> V10;
        public readonly ref Vector256<TLane> V11;
        public readonly ref Vector256<TLane> V12;
        public readonly ref Vector256<TLane> V13;
        public readonly ref Vector256<TLane> V14;

        internal Row(ref Vector256<TLane> v1, ref Vector256<TLane> v2, ref Vector256<TLane> v3, ref Vector256<TLane> v4, ref Vector256<TLane> v5, ref Vector256<TLane> v6, ref Vector256<TLane> v7, ref Vector256<TLane> v8, ref Vector256<TLane> v9, ref Vector256<TLane> v10, ref Vector256<TLane> v11, ref Vector256<TLane> v12, ref Vector256<TLane> v13, ref Vector256<TLane> v14)
        {
            V1 = ref v1;
            V2 = ref v2;
            V3 = ref v3;
            V4 = ref v4;
            V5 = ref v5;
            V6 = ref v6;
            V7 = ref v7;
            V8 = ref v8;
            V9 = ref v9;
            V10 = ref v10;
            V11 = ref v11;
            V12 = ref v12;
            V13 = ref v13;
            V14 = ref v14;
        }
    }

    // Few fields, and Dispose reads only the tail, so the JIT keeps spans and index in registers.
    public ref struct Enumerator
    {
        private readonly Span<Vector256<TLane>> _v1;
        private readonly Span<Vector256<TLane>> _v2;
        private readonly Span<Vector256<TLane>> _v3;
        private readonly Span<Vector256<TLane>> _v4;
        private readonly Span<Vector256<TLane>> _v5;
        private readonly Span<Vector256<TLane>> _v6;
        private readonly Span<Vector256<TLane>> _v7;
        private readonly Span<Vector256<TLane>> _v8;
        private readonly Span<Vector256<TLane>> _v9;
        private readonly Span<Vector256<TLane>> _v10;
        private readonly Span<Vector256<TLane>> _v11;
        private readonly Span<Vector256<TLane>> _v12;
        private readonly Span<Vector256<TLane>> _v13;
        private readonly Span<Vector256<TLane>> _v14;
        private int _i; // rows handed out so far; the tail row is one past the vectors
        private readonly Tail? _tail; // null when the count fills the vectors exactly

        internal Enumerator(VectorRows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TLane> rows)
        {
            var count = rows._count;
            _v1 = MemoryMarshal.Cast<T1, Vector256<TLane>>(rows._a1.AsSpan(0, count));
            _v2 = MemoryMarshal.Cast<T2, Vector256<TLane>>(rows._a2.AsSpan(0, count));
            _v3 = MemoryMarshal.Cast<T3, Vector256<TLane>>(rows._a3.AsSpan(0, count));
            _v4 = MemoryMarshal.Cast<T4, Vector256<TLane>>(rows._a4.AsSpan(0, count));
            _v5 = MemoryMarshal.Cast<T5, Vector256<TLane>>(rows._a5.AsSpan(0, count));
            _v6 = MemoryMarshal.Cast<T6, Vector256<TLane>>(rows._a6.AsSpan(0, count));
            _v7 = MemoryMarshal.Cast<T7, Vector256<TLane>>(rows._a7.AsSpan(0, count));
            _v8 = MemoryMarshal.Cast<T8, Vector256<TLane>>(rows._a8.AsSpan(0, count));
            _v9 = MemoryMarshal.Cast<T9, Vector256<TLane>>(rows._a9.AsSpan(0, count));
            _v10 = MemoryMarshal.Cast<T10, Vector256<TLane>>(rows._a10.AsSpan(0, count));
            _v11 = MemoryMarshal.Cast<T11, Vector256<TLane>>(rows._a11.AsSpan(0, count));
            _v12 = MemoryMarshal.Cast<T12, Vector256<TLane>>(rows._a12.AsSpan(0, count));
            _v13 = MemoryMarshal.Cast<T13, Vector256<TLane>>(rows._a13.AsSpan(0, count));
            _v14 = MemoryMarshal.Cast<T14, Vector256<TLane>>(rows._a14.AsSpan(0, count));
            var covered = (int)((long)_v1.Length * VectorSizes.Bytes / Unsafe.SizeOf<T1>());
            _i = 0;
            _tail = null;
            if (covered < count)
            {
                var tail = Tail.Rent();
                tail.Set(covered, count - covered, rows._a1, rows._a2, rows._a3, rows._a4, rows._a5, rows._a6, rows._a7, rows._a8, rows._a9, rows._a10, rows._a11, rows._a12, rows._a13, rows._a14);
                _tail = tail;
            }
        }

        // The spans share a length, so one tail test serves all.
        public readonly Row Current
        {
            get
            {
                var i = _i - 1;
                var tail = i >= _v1.Length;
                return new(ref (tail ? ref _tail!.Vector1() : ref _v1[i]), ref (tail ? ref _tail!.Vector2() : ref _v2[i]), ref (tail ? ref _tail!.Vector3() : ref _v3[i]), ref (tail ? ref _tail!.Vector4() : ref _v4[i]), ref (tail ? ref _tail!.Vector5() : ref _v5[i]), ref (tail ? ref _tail!.Vector6() : ref _v6[i]), ref (tail ? ref _tail!.Vector7() : ref _v7[i]), ref (tail ? ref _tail!.Vector8() : ref _v8[i]), ref (tail ? ref _tail!.Vector9() : ref _v9[i]), ref (tail ? ref _tail!.Vector10() : ref _v10[i]), ref (tail ? ref _tail!.Vector11() : ref _v11[i]), ref (tail ? ref _tail!.Vector12() : ref _v12[i]), ref (tail ? ref _tail!.Vector13() : ref _v13[i]), ref (tail ? ref _tail!.Vector14() : ref _v14[i]));
            }
        }

        public bool MoveNext()
        {
            var i = _i;
            if (i < _v1.Length)
            {
                _i = i + 1;
                return true;
            }

            var tail = _tail;
            if (tail is null || i > _v1.Length)
            {
                tail?.Flush();
                return false;
            }

            tail.Fill(); // the leftover components, spare lanes zeroed, as one more row
            _i = i + 1;
            return true;
        }

        public void Dispose() => _tail?.Flush();
    }

    // The last row's buffer, on the heap so refs can point at it; one spare per thread.
    private sealed class Tail() : VectorTail(14)
    {
        [ThreadStatic]
        private static Tail? _spare;

        private T1[] _a1 = [];
        private T2[] _a2 = [];
        private T3[] _a3 = [];
        private T4[] _a4 = [];
        private T5[] _a5 = [];
        private T6[] _a6 = [];
        private T7[] _a7 = [];
        private T8[] _a8 = [];
        private T9[] _a9 = [];
        private T10[] _a10 = [];
        private T11[] _a11 = [];
        private T12[] _a12 = [];
        private T13[] _a13 = [];
        private T14[] _a14 = [];

        public static Tail Rent()
        {
            var t = _spare;
            if (t is null)
            {
                return new();
            }

            _spare = null;
            return t;
        }

        public void Set(int start, int length, T1[] a1, T2[] a2, T3[] a3, T4[] a4, T5[] a5, T6[] a6, T7[] a7, T8[] a8, T9[] a9, T10[] a10, T11[] a11, T12[] a12, T13[] a13, T14[] a14)
        {
            Begin(start, length);
            _a1 = a1;
            _a2 = a2;
            _a3 = a3;
            _a4 = a4;
            _a5 = a5;
            _a6 = a6;
            _a7 = a7;
            _a8 = a8;
            _a9 = a9;
            _a10 = a10;
            _a11 = a11;
            _a12 = a12;
            _a13 = a13;
            _a14 = a14;
        }

        public ref Vector256<TLane> Vector1() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(0))[0];
        public ref Vector256<TLane> Vector2() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(1))[0];
        public ref Vector256<TLane> Vector3() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(2))[0];
        public ref Vector256<TLane> Vector4() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(3))[0];
        public ref Vector256<TLane> Vector5() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(4))[0];
        public ref Vector256<TLane> Vector6() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(5))[0];
        public ref Vector256<TLane> Vector7() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(6))[0];
        public ref Vector256<TLane> Vector8() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(7))[0];
        public ref Vector256<TLane> Vector9() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(8))[0];
        public ref Vector256<TLane> Vector10() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(9))[0];
        public ref Vector256<TLane> Vector11() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(10))[0];
        public ref Vector256<TLane> Vector12() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(11))[0];
        public ref Vector256<TLane> Vector13() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(12))[0];
        public ref Vector256<TLane> Vector14() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(13))[0];

        public void Fill()
        {
            FillBlock(_a1, 0);
            FillBlock(_a2, 1);
            FillBlock(_a3, 2);
            FillBlock(_a4, 3);
            FillBlock(_a5, 4);
            FillBlock(_a6, 5);
            FillBlock(_a7, 6);
            FillBlock(_a8, 7);
            FillBlock(_a9, 8);
            FillBlock(_a10, 9);
            FillBlock(_a11, 10);
            FillBlock(_a12, 11);
            FillBlock(_a13, 12);
            FillBlock(_a14, 13);
            Filled = true;
        }

        // Writes back only if the tail row was handed out.
        public void Flush()
        {
            if (Filled)
            {
                Filled = false;
                FlushBlock(_a1, 0);
                FlushBlock(_a2, 1);
                FlushBlock(_a3, 2);
                FlushBlock(_a4, 3);
                FlushBlock(_a5, 4);
                FlushBlock(_a6, 5);
                FlushBlock(_a7, 6);
                FlushBlock(_a8, 7);
                FlushBlock(_a9, 8);
                FlushBlock(_a10, 9);
                FlushBlock(_a11, 10);
                FlushBlock(_a12, 11);
                FlushBlock(_a13, 12);
                FlushBlock(_a14, 13);
            }

            _a1 = [];
            _a2 = [];
            _a3 = [];
            _a4 = [];
            _a5 = [];
            _a6 = [];
            _a7 = [];
            _a8 = [];
            _a9 = [];
            _a10 = [];
            _a11 = [];
            _a12 = [];
            _a13 = [];
            _a14 = [];
            _spare = this;
        }
    }
}

public readonly ref struct VectorRows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TLane>
    where T1 : unmanaged, IComponent
    where T2 : unmanaged, IComponent
    where T3 : unmanaged, IComponent
    where T4 : unmanaged, IComponent
    where T5 : unmanaged, IComponent
    where T6 : unmanaged, IComponent
    where T7 : unmanaged, IComponent
    where T8 : unmanaged, IComponent
    where T9 : unmanaged, IComponent
    where T10 : unmanaged, IComponent
    where T11 : unmanaged, IComponent
    where T12 : unmanaged, IComponent
    where T13 : unmanaged, IComponent
    where T14 : unmanaged, IComponent
    where T15 : unmanaged, IComponent
    where TLane : unmanaged
{
    private readonly int _count;
    private readonly T1[] _a1;
    private readonly T2[] _a2;
    private readonly T3[] _a3;
    private readonly T4[] _a4;
    private readonly T5[] _a5;
    private readonly T6[] _a6;
    private readonly T7[] _a7;
    private readonly T8[] _a8;
    private readonly T9[] _a9;
    private readonly T10[] _a10;
    private readonly T11[] _a11;
    private readonly T12[] _a12;
    private readonly T13[] _a13;
    private readonly T14[] _a14;
    private readonly T15[] _a15;

    internal VectorRows(int count, T1[] a1, T2[] a2, T3[] a3, T4[] a4, T5[] a5, T6[] a6, T7[] a7, T8[] a8, T9[] a9, T10[] a10, T11[] a11, T12[] a12, T13[] a13, T14[] a14, T15[] a15)
    {
        var size = VectorSizes.Check(Unsafe.SizeOf<T1>(), typeof(T1));
        VectorSizes.Same(size, Unsafe.SizeOf<T2>(), typeof(T1), typeof(T2));
        VectorSizes.Same(size, Unsafe.SizeOf<T3>(), typeof(T1), typeof(T3));
        VectorSizes.Same(size, Unsafe.SizeOf<T4>(), typeof(T1), typeof(T4));
        VectorSizes.Same(size, Unsafe.SizeOf<T5>(), typeof(T1), typeof(T5));
        VectorSizes.Same(size, Unsafe.SizeOf<T6>(), typeof(T1), typeof(T6));
        VectorSizes.Same(size, Unsafe.SizeOf<T7>(), typeof(T1), typeof(T7));
        VectorSizes.Same(size, Unsafe.SizeOf<T8>(), typeof(T1), typeof(T8));
        VectorSizes.Same(size, Unsafe.SizeOf<T9>(), typeof(T1), typeof(T9));
        VectorSizes.Same(size, Unsafe.SizeOf<T10>(), typeof(T1), typeof(T10));
        VectorSizes.Same(size, Unsafe.SizeOf<T11>(), typeof(T1), typeof(T11));
        VectorSizes.Same(size, Unsafe.SizeOf<T12>(), typeof(T1), typeof(T12));
        VectorSizes.Same(size, Unsafe.SizeOf<T13>(), typeof(T1), typeof(T13));
        VectorSizes.Same(size, Unsafe.SizeOf<T14>(), typeof(T1), typeof(T14));
        VectorSizes.Same(size, Unsafe.SizeOf<T15>(), typeof(T1), typeof(T15));
        _count = count;
        _a1 = a1;
        _a2 = a2;
        _a3 = a3;
        _a4 = a4;
        _a5 = a5;
        _a6 = a6;
        _a7 = a7;
        _a8 = a8;
        _a9 = a9;
        _a10 = a10;
        _a11 = a11;
        _a12 = a12;
        _a13 = a13;
        _a14 = a14;
        _a15 = a15;
    }

    public Enumerator GetEnumerator() => new(this);

    public readonly ref struct Row
    {
        public readonly ref Vector256<TLane> V1;
        public readonly ref Vector256<TLane> V2;
        public readonly ref Vector256<TLane> V3;
        public readonly ref Vector256<TLane> V4;
        public readonly ref Vector256<TLane> V5;
        public readonly ref Vector256<TLane> V6;
        public readonly ref Vector256<TLane> V7;
        public readonly ref Vector256<TLane> V8;
        public readonly ref Vector256<TLane> V9;
        public readonly ref Vector256<TLane> V10;
        public readonly ref Vector256<TLane> V11;
        public readonly ref Vector256<TLane> V12;
        public readonly ref Vector256<TLane> V13;
        public readonly ref Vector256<TLane> V14;
        public readonly ref Vector256<TLane> V15;

        internal Row(ref Vector256<TLane> v1, ref Vector256<TLane> v2, ref Vector256<TLane> v3, ref Vector256<TLane> v4, ref Vector256<TLane> v5, ref Vector256<TLane> v6, ref Vector256<TLane> v7, ref Vector256<TLane> v8, ref Vector256<TLane> v9, ref Vector256<TLane> v10, ref Vector256<TLane> v11, ref Vector256<TLane> v12, ref Vector256<TLane> v13, ref Vector256<TLane> v14, ref Vector256<TLane> v15)
        {
            V1 = ref v1;
            V2 = ref v2;
            V3 = ref v3;
            V4 = ref v4;
            V5 = ref v5;
            V6 = ref v6;
            V7 = ref v7;
            V8 = ref v8;
            V9 = ref v9;
            V10 = ref v10;
            V11 = ref v11;
            V12 = ref v12;
            V13 = ref v13;
            V14 = ref v14;
            V15 = ref v15;
        }
    }

    // Few fields, and Dispose reads only the tail, so the JIT keeps spans and index in registers.
    public ref struct Enumerator
    {
        private readonly Span<Vector256<TLane>> _v1;
        private readonly Span<Vector256<TLane>> _v2;
        private readonly Span<Vector256<TLane>> _v3;
        private readonly Span<Vector256<TLane>> _v4;
        private readonly Span<Vector256<TLane>> _v5;
        private readonly Span<Vector256<TLane>> _v6;
        private readonly Span<Vector256<TLane>> _v7;
        private readonly Span<Vector256<TLane>> _v8;
        private readonly Span<Vector256<TLane>> _v9;
        private readonly Span<Vector256<TLane>> _v10;
        private readonly Span<Vector256<TLane>> _v11;
        private readonly Span<Vector256<TLane>> _v12;
        private readonly Span<Vector256<TLane>> _v13;
        private readonly Span<Vector256<TLane>> _v14;
        private readonly Span<Vector256<TLane>> _v15;
        private int _i; // rows handed out so far; the tail row is one past the vectors
        private readonly Tail? _tail; // null when the count fills the vectors exactly

        internal Enumerator(VectorRows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TLane> rows)
        {
            var count = rows._count;
            _v1 = MemoryMarshal.Cast<T1, Vector256<TLane>>(rows._a1.AsSpan(0, count));
            _v2 = MemoryMarshal.Cast<T2, Vector256<TLane>>(rows._a2.AsSpan(0, count));
            _v3 = MemoryMarshal.Cast<T3, Vector256<TLane>>(rows._a3.AsSpan(0, count));
            _v4 = MemoryMarshal.Cast<T4, Vector256<TLane>>(rows._a4.AsSpan(0, count));
            _v5 = MemoryMarshal.Cast<T5, Vector256<TLane>>(rows._a5.AsSpan(0, count));
            _v6 = MemoryMarshal.Cast<T6, Vector256<TLane>>(rows._a6.AsSpan(0, count));
            _v7 = MemoryMarshal.Cast<T7, Vector256<TLane>>(rows._a7.AsSpan(0, count));
            _v8 = MemoryMarshal.Cast<T8, Vector256<TLane>>(rows._a8.AsSpan(0, count));
            _v9 = MemoryMarshal.Cast<T9, Vector256<TLane>>(rows._a9.AsSpan(0, count));
            _v10 = MemoryMarshal.Cast<T10, Vector256<TLane>>(rows._a10.AsSpan(0, count));
            _v11 = MemoryMarshal.Cast<T11, Vector256<TLane>>(rows._a11.AsSpan(0, count));
            _v12 = MemoryMarshal.Cast<T12, Vector256<TLane>>(rows._a12.AsSpan(0, count));
            _v13 = MemoryMarshal.Cast<T13, Vector256<TLane>>(rows._a13.AsSpan(0, count));
            _v14 = MemoryMarshal.Cast<T14, Vector256<TLane>>(rows._a14.AsSpan(0, count));
            _v15 = MemoryMarshal.Cast<T15, Vector256<TLane>>(rows._a15.AsSpan(0, count));
            var covered = (int)((long)_v1.Length * VectorSizes.Bytes / Unsafe.SizeOf<T1>());
            _i = 0;
            _tail = null;
            if (covered < count)
            {
                var tail = Tail.Rent();
                tail.Set(covered, count - covered, rows._a1, rows._a2, rows._a3, rows._a4, rows._a5, rows._a6, rows._a7, rows._a8, rows._a9, rows._a10, rows._a11, rows._a12, rows._a13, rows._a14, rows._a15);
                _tail = tail;
            }
        }

        // The spans share a length, so one tail test serves all.
        public readonly Row Current
        {
            get
            {
                var i = _i - 1;
                var tail = i >= _v1.Length;
                return new(ref (tail ? ref _tail!.Vector1() : ref _v1[i]), ref (tail ? ref _tail!.Vector2() : ref _v2[i]), ref (tail ? ref _tail!.Vector3() : ref _v3[i]), ref (tail ? ref _tail!.Vector4() : ref _v4[i]), ref (tail ? ref _tail!.Vector5() : ref _v5[i]), ref (tail ? ref _tail!.Vector6() : ref _v6[i]), ref (tail ? ref _tail!.Vector7() : ref _v7[i]), ref (tail ? ref _tail!.Vector8() : ref _v8[i]), ref (tail ? ref _tail!.Vector9() : ref _v9[i]), ref (tail ? ref _tail!.Vector10() : ref _v10[i]), ref (tail ? ref _tail!.Vector11() : ref _v11[i]), ref (tail ? ref _tail!.Vector12() : ref _v12[i]), ref (tail ? ref _tail!.Vector13() : ref _v13[i]), ref (tail ? ref _tail!.Vector14() : ref _v14[i]), ref (tail ? ref _tail!.Vector15() : ref _v15[i]));
            }
        }

        public bool MoveNext()
        {
            var i = _i;
            if (i < _v1.Length)
            {
                _i = i + 1;
                return true;
            }

            var tail = _tail;
            if (tail is null || i > _v1.Length)
            {
                tail?.Flush();
                return false;
            }

            tail.Fill(); // the leftover components, spare lanes zeroed, as one more row
            _i = i + 1;
            return true;
        }

        public void Dispose() => _tail?.Flush();
    }

    // The last row's buffer, on the heap so refs can point at it; one spare per thread.
    private sealed class Tail() : VectorTail(15)
    {
        [ThreadStatic]
        private static Tail? _spare;

        private T1[] _a1 = [];
        private T2[] _a2 = [];
        private T3[] _a3 = [];
        private T4[] _a4 = [];
        private T5[] _a5 = [];
        private T6[] _a6 = [];
        private T7[] _a7 = [];
        private T8[] _a8 = [];
        private T9[] _a9 = [];
        private T10[] _a10 = [];
        private T11[] _a11 = [];
        private T12[] _a12 = [];
        private T13[] _a13 = [];
        private T14[] _a14 = [];
        private T15[] _a15 = [];

        public static Tail Rent()
        {
            var t = _spare;
            if (t is null)
            {
                return new();
            }

            _spare = null;
            return t;
        }

        public void Set(int start, int length, T1[] a1, T2[] a2, T3[] a3, T4[] a4, T5[] a5, T6[] a6, T7[] a7, T8[] a8, T9[] a9, T10[] a10, T11[] a11, T12[] a12, T13[] a13, T14[] a14, T15[] a15)
        {
            Begin(start, length);
            _a1 = a1;
            _a2 = a2;
            _a3 = a3;
            _a4 = a4;
            _a5 = a5;
            _a6 = a6;
            _a7 = a7;
            _a8 = a8;
            _a9 = a9;
            _a10 = a10;
            _a11 = a11;
            _a12 = a12;
            _a13 = a13;
            _a14 = a14;
            _a15 = a15;
        }

        public ref Vector256<TLane> Vector1() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(0))[0];
        public ref Vector256<TLane> Vector2() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(1))[0];
        public ref Vector256<TLane> Vector3() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(2))[0];
        public ref Vector256<TLane> Vector4() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(3))[0];
        public ref Vector256<TLane> Vector5() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(4))[0];
        public ref Vector256<TLane> Vector6() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(5))[0];
        public ref Vector256<TLane> Vector7() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(6))[0];
        public ref Vector256<TLane> Vector8() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(7))[0];
        public ref Vector256<TLane> Vector9() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(8))[0];
        public ref Vector256<TLane> Vector10() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(9))[0];
        public ref Vector256<TLane> Vector11() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(10))[0];
        public ref Vector256<TLane> Vector12() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(11))[0];
        public ref Vector256<TLane> Vector13() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(12))[0];
        public ref Vector256<TLane> Vector14() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(13))[0];
        public ref Vector256<TLane> Vector15() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(14))[0];

        public void Fill()
        {
            FillBlock(_a1, 0);
            FillBlock(_a2, 1);
            FillBlock(_a3, 2);
            FillBlock(_a4, 3);
            FillBlock(_a5, 4);
            FillBlock(_a6, 5);
            FillBlock(_a7, 6);
            FillBlock(_a8, 7);
            FillBlock(_a9, 8);
            FillBlock(_a10, 9);
            FillBlock(_a11, 10);
            FillBlock(_a12, 11);
            FillBlock(_a13, 12);
            FillBlock(_a14, 13);
            FillBlock(_a15, 14);
            Filled = true;
        }

        // Writes back only if the tail row was handed out.
        public void Flush()
        {
            if (Filled)
            {
                Filled = false;
                FlushBlock(_a1, 0);
                FlushBlock(_a2, 1);
                FlushBlock(_a3, 2);
                FlushBlock(_a4, 3);
                FlushBlock(_a5, 4);
                FlushBlock(_a6, 5);
                FlushBlock(_a7, 6);
                FlushBlock(_a8, 7);
                FlushBlock(_a9, 8);
                FlushBlock(_a10, 9);
                FlushBlock(_a11, 10);
                FlushBlock(_a12, 11);
                FlushBlock(_a13, 12);
                FlushBlock(_a14, 13);
                FlushBlock(_a15, 14);
            }

            _a1 = [];
            _a2 = [];
            _a3 = [];
            _a4 = [];
            _a5 = [];
            _a6 = [];
            _a7 = [];
            _a8 = [];
            _a9 = [];
            _a10 = [];
            _a11 = [];
            _a12 = [];
            _a13 = [];
            _a14 = [];
            _a15 = [];
            _spare = this;
        }
    }
}

public readonly ref struct VectorRows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TLane>
    where T1 : unmanaged, IComponent
    where T2 : unmanaged, IComponent
    where T3 : unmanaged, IComponent
    where T4 : unmanaged, IComponent
    where T5 : unmanaged, IComponent
    where T6 : unmanaged, IComponent
    where T7 : unmanaged, IComponent
    where T8 : unmanaged, IComponent
    where T9 : unmanaged, IComponent
    where T10 : unmanaged, IComponent
    where T11 : unmanaged, IComponent
    where T12 : unmanaged, IComponent
    where T13 : unmanaged, IComponent
    where T14 : unmanaged, IComponent
    where T15 : unmanaged, IComponent
    where T16 : unmanaged, IComponent
    where TLane : unmanaged
{
    private readonly int _count;
    private readonly T1[] _a1;
    private readonly T2[] _a2;
    private readonly T3[] _a3;
    private readonly T4[] _a4;
    private readonly T5[] _a5;
    private readonly T6[] _a6;
    private readonly T7[] _a7;
    private readonly T8[] _a8;
    private readonly T9[] _a9;
    private readonly T10[] _a10;
    private readonly T11[] _a11;
    private readonly T12[] _a12;
    private readonly T13[] _a13;
    private readonly T14[] _a14;
    private readonly T15[] _a15;
    private readonly T16[] _a16;

    internal VectorRows(int count, T1[] a1, T2[] a2, T3[] a3, T4[] a4, T5[] a5, T6[] a6, T7[] a7, T8[] a8, T9[] a9, T10[] a10, T11[] a11, T12[] a12, T13[] a13, T14[] a14, T15[] a15, T16[] a16)
    {
        var size = VectorSizes.Check(Unsafe.SizeOf<T1>(), typeof(T1));
        VectorSizes.Same(size, Unsafe.SizeOf<T2>(), typeof(T1), typeof(T2));
        VectorSizes.Same(size, Unsafe.SizeOf<T3>(), typeof(T1), typeof(T3));
        VectorSizes.Same(size, Unsafe.SizeOf<T4>(), typeof(T1), typeof(T4));
        VectorSizes.Same(size, Unsafe.SizeOf<T5>(), typeof(T1), typeof(T5));
        VectorSizes.Same(size, Unsafe.SizeOf<T6>(), typeof(T1), typeof(T6));
        VectorSizes.Same(size, Unsafe.SizeOf<T7>(), typeof(T1), typeof(T7));
        VectorSizes.Same(size, Unsafe.SizeOf<T8>(), typeof(T1), typeof(T8));
        VectorSizes.Same(size, Unsafe.SizeOf<T9>(), typeof(T1), typeof(T9));
        VectorSizes.Same(size, Unsafe.SizeOf<T10>(), typeof(T1), typeof(T10));
        VectorSizes.Same(size, Unsafe.SizeOf<T11>(), typeof(T1), typeof(T11));
        VectorSizes.Same(size, Unsafe.SizeOf<T12>(), typeof(T1), typeof(T12));
        VectorSizes.Same(size, Unsafe.SizeOf<T13>(), typeof(T1), typeof(T13));
        VectorSizes.Same(size, Unsafe.SizeOf<T14>(), typeof(T1), typeof(T14));
        VectorSizes.Same(size, Unsafe.SizeOf<T15>(), typeof(T1), typeof(T15));
        VectorSizes.Same(size, Unsafe.SizeOf<T16>(), typeof(T1), typeof(T16));
        _count = count;
        _a1 = a1;
        _a2 = a2;
        _a3 = a3;
        _a4 = a4;
        _a5 = a5;
        _a6 = a6;
        _a7 = a7;
        _a8 = a8;
        _a9 = a9;
        _a10 = a10;
        _a11 = a11;
        _a12 = a12;
        _a13 = a13;
        _a14 = a14;
        _a15 = a15;
        _a16 = a16;
    }

    public Enumerator GetEnumerator() => new(this);

    public readonly ref struct Row
    {
        public readonly ref Vector256<TLane> V1;
        public readonly ref Vector256<TLane> V2;
        public readonly ref Vector256<TLane> V3;
        public readonly ref Vector256<TLane> V4;
        public readonly ref Vector256<TLane> V5;
        public readonly ref Vector256<TLane> V6;
        public readonly ref Vector256<TLane> V7;
        public readonly ref Vector256<TLane> V8;
        public readonly ref Vector256<TLane> V9;
        public readonly ref Vector256<TLane> V10;
        public readonly ref Vector256<TLane> V11;
        public readonly ref Vector256<TLane> V12;
        public readonly ref Vector256<TLane> V13;
        public readonly ref Vector256<TLane> V14;
        public readonly ref Vector256<TLane> V15;
        public readonly ref Vector256<TLane> V16;

        internal Row(ref Vector256<TLane> v1, ref Vector256<TLane> v2, ref Vector256<TLane> v3, ref Vector256<TLane> v4, ref Vector256<TLane> v5, ref Vector256<TLane> v6, ref Vector256<TLane> v7, ref Vector256<TLane> v8, ref Vector256<TLane> v9, ref Vector256<TLane> v10, ref Vector256<TLane> v11, ref Vector256<TLane> v12, ref Vector256<TLane> v13, ref Vector256<TLane> v14, ref Vector256<TLane> v15, ref Vector256<TLane> v16)
        {
            V1 = ref v1;
            V2 = ref v2;
            V3 = ref v3;
            V4 = ref v4;
            V5 = ref v5;
            V6 = ref v6;
            V7 = ref v7;
            V8 = ref v8;
            V9 = ref v9;
            V10 = ref v10;
            V11 = ref v11;
            V12 = ref v12;
            V13 = ref v13;
            V14 = ref v14;
            V15 = ref v15;
            V16 = ref v16;
        }
    }

    // Few fields, and Dispose reads only the tail, so the JIT keeps spans and index in registers.
    public ref struct Enumerator
    {
        private readonly Span<Vector256<TLane>> _v1;
        private readonly Span<Vector256<TLane>> _v2;
        private readonly Span<Vector256<TLane>> _v3;
        private readonly Span<Vector256<TLane>> _v4;
        private readonly Span<Vector256<TLane>> _v5;
        private readonly Span<Vector256<TLane>> _v6;
        private readonly Span<Vector256<TLane>> _v7;
        private readonly Span<Vector256<TLane>> _v8;
        private readonly Span<Vector256<TLane>> _v9;
        private readonly Span<Vector256<TLane>> _v10;
        private readonly Span<Vector256<TLane>> _v11;
        private readonly Span<Vector256<TLane>> _v12;
        private readonly Span<Vector256<TLane>> _v13;
        private readonly Span<Vector256<TLane>> _v14;
        private readonly Span<Vector256<TLane>> _v15;
        private readonly Span<Vector256<TLane>> _v16;
        private int _i; // rows handed out so far; the tail row is one past the vectors
        private readonly Tail? _tail; // null when the count fills the vectors exactly

        internal Enumerator(VectorRows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TLane> rows)
        {
            var count = rows._count;
            _v1 = MemoryMarshal.Cast<T1, Vector256<TLane>>(rows._a1.AsSpan(0, count));
            _v2 = MemoryMarshal.Cast<T2, Vector256<TLane>>(rows._a2.AsSpan(0, count));
            _v3 = MemoryMarshal.Cast<T3, Vector256<TLane>>(rows._a3.AsSpan(0, count));
            _v4 = MemoryMarshal.Cast<T4, Vector256<TLane>>(rows._a4.AsSpan(0, count));
            _v5 = MemoryMarshal.Cast<T5, Vector256<TLane>>(rows._a5.AsSpan(0, count));
            _v6 = MemoryMarshal.Cast<T6, Vector256<TLane>>(rows._a6.AsSpan(0, count));
            _v7 = MemoryMarshal.Cast<T7, Vector256<TLane>>(rows._a7.AsSpan(0, count));
            _v8 = MemoryMarshal.Cast<T8, Vector256<TLane>>(rows._a8.AsSpan(0, count));
            _v9 = MemoryMarshal.Cast<T9, Vector256<TLane>>(rows._a9.AsSpan(0, count));
            _v10 = MemoryMarshal.Cast<T10, Vector256<TLane>>(rows._a10.AsSpan(0, count));
            _v11 = MemoryMarshal.Cast<T11, Vector256<TLane>>(rows._a11.AsSpan(0, count));
            _v12 = MemoryMarshal.Cast<T12, Vector256<TLane>>(rows._a12.AsSpan(0, count));
            _v13 = MemoryMarshal.Cast<T13, Vector256<TLane>>(rows._a13.AsSpan(0, count));
            _v14 = MemoryMarshal.Cast<T14, Vector256<TLane>>(rows._a14.AsSpan(0, count));
            _v15 = MemoryMarshal.Cast<T15, Vector256<TLane>>(rows._a15.AsSpan(0, count));
            _v16 = MemoryMarshal.Cast<T16, Vector256<TLane>>(rows._a16.AsSpan(0, count));
            var covered = (int)((long)_v1.Length * VectorSizes.Bytes / Unsafe.SizeOf<T1>());
            _i = 0;
            _tail = null;
            if (covered < count)
            {
                var tail = Tail.Rent();
                tail.Set(covered, count - covered, rows._a1, rows._a2, rows._a3, rows._a4, rows._a5, rows._a6, rows._a7, rows._a8, rows._a9, rows._a10, rows._a11, rows._a12, rows._a13, rows._a14, rows._a15, rows._a16);
                _tail = tail;
            }
        }

        // The spans share a length, so one tail test serves all.
        public readonly Row Current
        {
            get
            {
                var i = _i - 1;
                var tail = i >= _v1.Length;
                return new(ref (tail ? ref _tail!.Vector1() : ref _v1[i]), ref (tail ? ref _tail!.Vector2() : ref _v2[i]), ref (tail ? ref _tail!.Vector3() : ref _v3[i]), ref (tail ? ref _tail!.Vector4() : ref _v4[i]), ref (tail ? ref _tail!.Vector5() : ref _v5[i]), ref (tail ? ref _tail!.Vector6() : ref _v6[i]), ref (tail ? ref _tail!.Vector7() : ref _v7[i]), ref (tail ? ref _tail!.Vector8() : ref _v8[i]), ref (tail ? ref _tail!.Vector9() : ref _v9[i]), ref (tail ? ref _tail!.Vector10() : ref _v10[i]), ref (tail ? ref _tail!.Vector11() : ref _v11[i]), ref (tail ? ref _tail!.Vector12() : ref _v12[i]), ref (tail ? ref _tail!.Vector13() : ref _v13[i]), ref (tail ? ref _tail!.Vector14() : ref _v14[i]), ref (tail ? ref _tail!.Vector15() : ref _v15[i]), ref (tail ? ref _tail!.Vector16() : ref _v16[i]));
            }
        }

        public bool MoveNext()
        {
            var i = _i;
            if (i < _v1.Length)
            {
                _i = i + 1;
                return true;
            }

            var tail = _tail;
            if (tail is null || i > _v1.Length)
            {
                tail?.Flush();
                return false;
            }

            tail.Fill(); // the leftover components, spare lanes zeroed, as one more row
            _i = i + 1;
            return true;
        }

        public void Dispose() => _tail?.Flush();
    }

    // The last row's buffer, on the heap so refs can point at it; one spare per thread.
    private sealed class Tail() : VectorTail(16)
    {
        [ThreadStatic]
        private static Tail? _spare;

        private T1[] _a1 = [];
        private T2[] _a2 = [];
        private T3[] _a3 = [];
        private T4[] _a4 = [];
        private T5[] _a5 = [];
        private T6[] _a6 = [];
        private T7[] _a7 = [];
        private T8[] _a8 = [];
        private T9[] _a9 = [];
        private T10[] _a10 = [];
        private T11[] _a11 = [];
        private T12[] _a12 = [];
        private T13[] _a13 = [];
        private T14[] _a14 = [];
        private T15[] _a15 = [];
        private T16[] _a16 = [];

        public static Tail Rent()
        {
            var t = _spare;
            if (t is null)
            {
                return new();
            }

            _spare = null;
            return t;
        }

        public void Set(int start, int length, T1[] a1, T2[] a2, T3[] a3, T4[] a4, T5[] a5, T6[] a6, T7[] a7, T8[] a8, T9[] a9, T10[] a10, T11[] a11, T12[] a12, T13[] a13, T14[] a14, T15[] a15, T16[] a16)
        {
            Begin(start, length);
            _a1 = a1;
            _a2 = a2;
            _a3 = a3;
            _a4 = a4;
            _a5 = a5;
            _a6 = a6;
            _a7 = a7;
            _a8 = a8;
            _a9 = a9;
            _a10 = a10;
            _a11 = a11;
            _a12 = a12;
            _a13 = a13;
            _a14 = a14;
            _a15 = a15;
            _a16 = a16;
        }

        public ref Vector256<TLane> Vector1() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(0))[0];
        public ref Vector256<TLane> Vector2() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(1))[0];
        public ref Vector256<TLane> Vector3() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(2))[0];
        public ref Vector256<TLane> Vector4() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(3))[0];
        public ref Vector256<TLane> Vector5() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(4))[0];
        public ref Vector256<TLane> Vector6() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(5))[0];
        public ref Vector256<TLane> Vector7() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(6))[0];
        public ref Vector256<TLane> Vector8() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(7))[0];
        public ref Vector256<TLane> Vector9() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(8))[0];
        public ref Vector256<TLane> Vector10() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(9))[0];
        public ref Vector256<TLane> Vector11() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(10))[0];
        public ref Vector256<TLane> Vector12() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(11))[0];
        public ref Vector256<TLane> Vector13() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(12))[0];
        public ref Vector256<TLane> Vector14() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(13))[0];
        public ref Vector256<TLane> Vector15() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(14))[0];
        public ref Vector256<TLane> Vector16() => ref MemoryMarshal.Cast<byte, Vector256<TLane>>(Block(15))[0];

        public void Fill()
        {
            FillBlock(_a1, 0);
            FillBlock(_a2, 1);
            FillBlock(_a3, 2);
            FillBlock(_a4, 3);
            FillBlock(_a5, 4);
            FillBlock(_a6, 5);
            FillBlock(_a7, 6);
            FillBlock(_a8, 7);
            FillBlock(_a9, 8);
            FillBlock(_a10, 9);
            FillBlock(_a11, 10);
            FillBlock(_a12, 11);
            FillBlock(_a13, 12);
            FillBlock(_a14, 13);
            FillBlock(_a15, 14);
            FillBlock(_a16, 15);
            Filled = true;
        }

        // Writes back only if the tail row was handed out.
        public void Flush()
        {
            if (Filled)
            {
                Filled = false;
                FlushBlock(_a1, 0);
                FlushBlock(_a2, 1);
                FlushBlock(_a3, 2);
                FlushBlock(_a4, 3);
                FlushBlock(_a5, 4);
                FlushBlock(_a6, 5);
                FlushBlock(_a7, 6);
                FlushBlock(_a8, 7);
                FlushBlock(_a9, 8);
                FlushBlock(_a10, 9);
                FlushBlock(_a11, 10);
                FlushBlock(_a12, 11);
                FlushBlock(_a13, 12);
                FlushBlock(_a14, 13);
                FlushBlock(_a15, 14);
                FlushBlock(_a16, 15);
            }

            _a1 = [];
            _a2 = [];
            _a3 = [];
            _a4 = [];
            _a5 = [];
            _a6 = [];
            _a7 = [];
            _a8 = [];
            _a9 = [];
            _a10 = [];
            _a11 = [];
            _a12 = [];
            _a13 = [];
            _a14 = [];
            _a15 = [];
            _a16 = [];
            _spare = this;
        }
    }
}
