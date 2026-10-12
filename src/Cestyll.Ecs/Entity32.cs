namespace Cestyll.Ecs;

// 32-bit entity id: low 20 bits index, high 12 bits version.
public readonly record struct Entity32(uint Raw) : IEntity<Entity32>
{
    public const int IndexBits = 20;
    public const uint IndexMask = (1u << IndexBits) - 1;
    public const uint VersionMask = (1u << (32 - IndexBits)) - 1;

    // default, so an uninitialized field never names a live entity.
    public static readonly Entity32 Null = default;

    public int Index => (int)(Raw & IndexMask);
    public uint Version => Raw >> IndexBits;
    public bool IsNull => Raw == 0;

    public static Entity32 Make(int index, uint version) =>
        new(((version & VersionMask) << IndexBits) | ((uint)index & IndexMask));

    static int IEntity<Entity32>.MaxIndex => (int)IndexMask;
    static uint IEntity<Entity32>.VersionMask => VersionMask;
    static bool IEntity<Entity32>.SameVersion(Entity32 a, Entity32 b) => (a.Raw ^ b.Raw) <= IndexMask;
}
