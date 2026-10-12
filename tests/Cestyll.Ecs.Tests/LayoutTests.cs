using Cestyll.Ecs;
using Cestyll.Ecs.Snapshots;

namespace Cestyll.Ecs.Tests;

// Custom and broken layouts, and snapshots across layouts. Independent of the Entity alias.
public class LayoutTests
{
    private struct Position : IComponent { public int X; }

    // 24-bit index, 8-bit version: versions wrap after 256 reuses of an index.
    private readonly record struct Entity24(uint Raw) : IEntity<Entity24>
    {
        private const uint IndexMask = (1u << 24) - 1;

        public static int MaxIndex => (int)IndexMask;
        public static uint VersionMask => 0xFF;

        public int Index => (int)(Raw & IndexMask);
        public uint Version => Raw >> 24;
        public bool IsNull => Raw == 0;

        public static Entity24 Make(int index, uint version) =>
            new(((version & VersionMask) << 24) | ((uint)index & IndexMask));

        public static bool SameVersion(Entity24 a, Entity24 b) => (a.Raw ^ b.Raw) <= IndexMask;
    }

    // MaxIndex exceeds its 24 index bits.
    private readonly record struct Overfull(uint Raw) : IEntity<Overfull>
    {
        public static int MaxIndex => int.MaxValue;
        public static uint VersionMask => 0xFF;

        public int Index => (int)(Raw & 0xFFFFFF);
        public uint Version => Raw >> 24;
        public bool IsNull => Raw == 0;

        public static Overfull Make(int index, uint version) =>
            new(((version & VersionMask) << 24) | ((uint)index & 0xFFFFFF));

        public static bool SameVersion(Overfull a, Overfull b) => (a.Raw ^ b.Raw) <= 0xFFFFFF;
    }

    private struct Health : IComponent { public int Value; }

    [Fact]
    public void CustomLayout_RecyclesWithANewVersion()
    {
        var registry = new Registry<Entity24>();
        Entity24 first = registry.Create();
        registry.Add(first, new Health { Value = 1 });
        registry.Destroy(first);

        Entity24 second = registry.Create();

        Assert.Equal(first.Index, second.Index);
        Assert.Equal(1u, second.Version);
        Assert.False(registry.Has<Health>(first)); // stale handle misses the probe
    }

    [Fact]
    public void CustomLayout_VersionWrapsAfter256Reuses()
    {
        var registry = new Registry<Entity24>();
        Entity24 first = registry.Create();
        var e = first;

        for (var i = 0; i < 256; i++)
        {
            registry.Destroy(e);
            e = registry.Create();
        }

        Assert.Equal(first, e); // 256 reuses: the version wrapped back to 0
    }

    [Fact]
    public void BrokenLayout_Rejected()
    {
        Assert.Throws<InvalidOperationException>(() => new Registry<Overfull>());
    }

    // JsonArchive stores index and version, so layouts don't have to match.
    [Fact]
    public void Snapshot_LoadsIntoWiderLayout()
    {
        var source = new Registry<Entity32>();
        var a = source.CreateAt(Entity32.Make(3, 7)).Entity;
        source.Add(a, new Position { X = 5 });
        var archive = new JsonArchive<Entity32>();
        source.Snapshot<JsonArchive<Entity32>>().Entities(ref archive).Component<Position>(ref archive);

        var loaded = new Registry<Entity64>();
        var reader = JsonArchive<Entity64>.Parse(archive.ToJson());
        loaded.SnapshotLoader<JsonArchive<Entity64>>().Entities(ref reader).Component<Position>(ref reader);

        var wide = Entity64.Make(3, 7);
        Assert.True(loaded.IsAlive(wide));
        Assert.Equal(5, loaded.Get<Position>(wide).X);
        Assert.Equal(1, loaded.AliveCount);
    }
}
