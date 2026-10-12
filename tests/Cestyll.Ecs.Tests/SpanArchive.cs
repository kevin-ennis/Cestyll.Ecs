using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Cestyll.Ecs;
using Cestyll.Ecs.Snapshots;

namespace Cestyll.Ecs.Tests;

// Binary ref struct archive over a span; each section starts with a checked kind byte.
// Components only, copied as bytes, so T must hold no references.
internal ref struct SpanArchive(Span<byte> buffer, bool reading) : ISnapshotWriter<Entity>, ISnapshotReader<Entity>
{
    private readonly Span<byte> _buffer = buffer;

    public int Position { get; private set; }

    private Span<byte> Take(int length)
    {
        if (length > _buffer.Length - Position)
        {
            throw new InvalidDataException("Archive ended");
        }

        var span = _buffer.Slice(Position, length);
        Position += length;
        return span;
    }

    public void BeginSection(SnapshotSection kind, Type type, string? name)
    {
        var tag = Take(1);
        if (!reading)
        {
            tag[0] = (byte)kind;
        }
        else if ((SnapshotSection)tag[0] != kind)
        {
            throw new InvalidDataException($"Expected a {kind} section, found {(SnapshotSection)tag[0]}");
        }
    }

    public void EndSection() { }

    public void WriteCount(int count) => BinaryPrimitives.WriteInt32LittleEndian(Take(4), count);

    public void WriteEntity(Entity entity)
    {
        WriteCount(entity.Index);
        BinaryPrimitives.WriteUInt32LittleEndian(Take(4), entity.Version);
    }

    public void Write<T>(in T value) => throw new NotSupportedException("SpanArchive only takes components");

    public void WriteComponents<T>(ReadOnlySpan<T> values)
    {
        var bytes = AsBytes(MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(values), values.Length));
        bytes.CopyTo(Take(bytes.Length));
    }

    public int ReadCount() => BinaryPrimitives.ReadInt32LittleEndian(Take(4));

    public Entity ReadEntity() => Entity.Make(ReadCount(), BinaryPrimitives.ReadUInt32LittleEndian(Take(4)));

    public T Read<T>() => throw new NotSupportedException("SpanArchive only takes components");

    public void ReadComponents<T>(Span<T> values)
    {
        var bytes = AsBytes(values);
        Take(bytes.Length).CopyTo(bytes);
    }

    // MemoryMarshal.AsBytes needs T : struct, which the interface doesn't promise.
    private static Span<byte> AsBytes<T>(Span<T> values)
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            throw new NotSupportedException($"SpanArchive can't copy {typeof(T).Name}: it holds references");
        }

        return MemoryMarshal.CreateSpan(
            ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(values)),
            checked(values.Length * Unsafe.SizeOf<T>()));
    }
}
