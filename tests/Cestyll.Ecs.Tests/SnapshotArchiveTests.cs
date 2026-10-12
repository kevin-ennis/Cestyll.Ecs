using Cestyll.Ecs;
using Cestyll.Ecs.Attachments;
using Cestyll.Ecs.Relations;
using Cestyll.Ecs.Snapshots;
using Cestyll.Ecs.Tags;

namespace Cestyll.Ecs.Tests;

// What an archive sees of a snapshot: sections, caller-owned position, block path.
public class SnapshotArchiveTests
{
    private struct Position : IComponent { public int X; }
    private struct Velocity : IComponent { public int X; }
    private struct Marked : ITag;

    private struct Owns : IRelation
    {
        public static OnTargetDestroyed Policy => OnTargetDestroyed.Unrelate;
    }

    private struct Hits : ILink<Entity>
    {
        public Entity Target { get; init; }
    }

    private struct Slot : IAttachment<int>
    {
        public readonly int Key => 0;
    }

    private static (Registry Registry, Entity A, Entity B) Populated()
    {
        var registry = new Registry();
        Entity a = registry.Create();
        Entity b = registry.Create();
        registry.Add(a, new Position { X = 1 });
        registry.Add(b, new Position { X = 2 });
        registry.Storage<Position>("previous").Add(a, new Position { X = 3 });
        registry.Add(b, new Velocity { X = 4 });
        registry.Tag<Marked>(b);
        return (registry, a, b);
    }

    [Fact]
    public void Json_KeysSectionsByKindTypeAndName()
    {
        var (source, _, _) = Populated();
        var archive = new Archive();
        source.Snapshot<Archive>()
            .Entities(ref archive)
            .Component<Position>(ref archive)
            .Component<Position>(ref archive, "previous")
            .Tag<Marked>(ref archive);

        var json = archive.ToJson();
        Assert.Contains("\"entities\":", json);
        Assert.Contains("\"Component:Position\":", json);
        Assert.Contains("\"Component:Position/previous\":", json);
        Assert.Contains("\"Tag:Marked\":", json);
    }

    [Fact]
    public void Json_LoadsSectionsInAnyOrder()
    {
        var (source, a, b) = Populated();
        var archive = new Archive();
        source.Snapshot<Archive>()
            .Entities(ref archive)
            .Component<Position>(ref archive)
            .Component<Velocity>(ref archive)
            .Tag<Marked>(ref archive);

        var loaded = new Registry();
        var reader = archive.Reopen();
        loaded.SnapshotLoader<Archive>()
            .Entities(ref reader)
            .Tag<Marked>(ref reader)
            .Component<Velocity>(ref reader)
            .Component<Position>(ref reader);

        Assert.Equal(1, loaded.Get<Position>(a).X);
        Assert.Equal(4, loaded.Get<Velocity>(b).X);
        Assert.True(loaded.HasTag<Marked>(b));
        Assert.True(reader.AllRead);
    }

    // The pool name reaches the archive, so a mismatch is caught before anything is added.
    [Fact]
    public void Json_NamedPoolMismatch_Rejected()
    {
        var (source, a, _) = Populated();
        var archive = new Archive();
        source.Snapshot<Archive>().Entities(ref archive).Component<Position>(ref archive, "previous");

        var loaded = new Registry();
        var reader = archive.Reopen();
        var loader = loaded.SnapshotLoader<Archive>().Entities(ref reader);

        Assert.Throws<InvalidDataException>(() => loader.Component<Position>(ref reader, "spawn"));
        Assert.Throws<InvalidDataException>(() => loader.Component<Position>(ref reader));
        Assert.False(loaded.Has<Position>(a));
    }

    [Fact]
    public void Json_SectionWithDataLeftOver_Rejected()
    {
        var (source, _, b) = Populated();
        var archive = new Archive();
        source.Snapshot<Archive>().Entities(ref archive);
        archive.BeginSection(SnapshotSection.Tag, typeof(Marked), null);
        archive.WriteCount(1);
        archive.WriteEntity(b);
        archive.WriteEntity(b); // one more than the count
        archive.EndSection();

        var loaded = new Registry();
        var reader = archive.Reopen();
        var loader = loaded.SnapshotLoader<Archive>().Entities(ref reader);

        Assert.Throws<InvalidDataException>(() => loader.Tag<Marked>(ref reader));
        Assert.False(loaded.HasTag<Marked>(b));
    }

    // The caller's archive is the one that advances.
    [Fact]
    public void RefStructArchive_RoundTrip()
    {
        var (source, a, b) = Populated();
        Span<byte> buffer = stackalloc byte[1024];
        var writer = new SpanArchive(buffer, reading: false);
        source.Snapshot<SpanArchive>()
            .Entities(ref writer)
            .Component<Position>(ref writer)
            .Component<Position>(ref writer, "previous")
            .Tag<Marked>(ref writer);

        var loaded = new Registry();
        var reader = new SpanArchive(buffer[..writer.Position], reading: true);
        loaded.SnapshotLoader<SpanArchive>()
            .Entities(ref reader)
            .Component<Position>(ref reader)
            .Component<Position>(ref reader, "previous")
            .Tag<Marked>(ref reader);

        Assert.Equal(writer.Position, reader.Position);
        Assert.Equal(1, loaded.Get<Position>(a).X);
        Assert.Equal(2, loaded.Get<Position>(b).X);
        Assert.Equal(3, loaded.Storage<Position>("previous").Get(a).X);
        Assert.True(loaded.HasTag<Marked>(b));
    }

    [Fact]
    public void RefStructArchive_ContinuousLoaderReadsComponents()
    {
        var (remote, a, _) = Populated();
        var buffer = new byte[1024];
        var writer = new SpanArchive(buffer, reading: false);
        remote.Snapshot<SpanArchive>().Entities(ref writer).Component<Position>(ref writer);

        var local = new Registry();
        var loader = local.ContinuousLoader<SpanArchive>();
        var reader = new SpanArchive(buffer, reading: true);
        loader.Entities(ref reader).Component<Position>(ref reader);

        Assert.Equal(writer.Position, reader.Position);
        Assert.Equal(1, local.Get<Position>(loader.Map(a)).X);
    }

    // A binary archive that tags each section catches a loader asking for the wrong one.
    [Fact]
    public void RefStructArchive_SectionOrderMismatch_Rejected()
    {
        var (source, a, _) = Populated();
        var buffer = new byte[1024];
        var writer = new SpanArchive(buffer, reading: false);
        source.Snapshot<SpanArchive>().Entities(ref writer).Tag<Marked>(ref writer);
        var loaded = new Registry();

        Assert.Throws<InvalidDataException>(() =>
        {
            var reader = new SpanArchive(buffer, reading: true);
            loaded.SnapshotLoader<SpanArchive>().Entities(ref reader).Component<Position>(ref reader);
        });
        Assert.False(loaded.Has<Position>(a));
    }

    [Fact]
    public void Writing_NeverMakesStores()
    {
        var (source, _, _) = Populated();
        var archive = new Archive();

        source.Snapshot<Archive>()
            .Entities(ref archive)
            .Relation<Owns>(ref archive)
            .Link<Hits>(ref archive)
            .Attachment<Slot, int>(ref archive);

        Assert.Null(source.FindRelations<Owns>());
        Assert.Null(source.FindLinks<Hits>());
        Assert.Null(source.FindAttachments<Slot, int>());
    }

    [Fact]
    public void EmptySections_LoadAndConsumeArchive()
    {
        var (source, _, _) = Populated();
        var archive = new Archive();
        source.Snapshot<Archive>()
            .Entities(ref archive)
            .Relation<Owns>(ref archive)
            .Link<Hits>(ref archive)
            .Attachment<Slot, int>(ref archive);
        var loaded = new Registry();
        var reader = archive.Reopen();

        loaded.SnapshotLoader<Archive>()
            .Entities(ref reader)
            .Relation<Owns>(ref reader)
            .Link<Hits>(ref reader)
            .Attachment<Slot, int>(ref reader);

        Assert.True(reader.AllRead);
    }
}
