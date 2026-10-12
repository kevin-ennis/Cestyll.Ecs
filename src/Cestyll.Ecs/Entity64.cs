namespace Cestyll.Ecs;

// 64-bit entity id: low 32 bits index, high 32 bits version. Twice Entity32's memory, so slower.
public readonly record struct Entity64(ulong Raw) : IEntity<Entity64>
{
    public const int IndexBits = 32;
    public const ulong IndexMask = (1UL << IndexBits) - 1;
    public const uint VersionMask = uint.MaxValue;

    public static readonly Entity64 Null = default;

    public int Index => (int)(Raw & IndexMask);
    public uint Version => (uint)(Raw >> IndexBits);
    public bool IsNull => Raw == 0;

    public static Entity64 Make(int index, uint version) => new(((ulong)version << IndexBits) | (uint)index);

    static int IEntity<Entity64>.MaxIndex => int.MaxValue;
    static uint IEntity<Entity64>.VersionMask => VersionMask;
    static bool IEntity<Entity64>.SameVersion(Entity64 a, Entity64 b) => (a.Raw ^ b.Raw) <= IndexMask;
}
