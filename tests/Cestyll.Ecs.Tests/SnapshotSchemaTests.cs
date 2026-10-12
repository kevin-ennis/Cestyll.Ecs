using Cestyll.Ecs;
using Cestyll.Ecs.Attachments;
using Cestyll.Ecs.Inboxes;
using Cestyll.Ecs.Relations;
using Cestyll.Ecs.Snapshots;
using Cestyll.Ecs.Tags;

namespace Cestyll.Ecs.Tests;

public class SnapshotSchemaTests
{
    private struct Position : IComponent { public int X; }
    private struct Velocity : IComponent { public int X; }
    private struct Recording : IComponent { public int Frames; }
    private struct Downed : ITag;

    private struct Orbits : IRelation
    {
        public static OnTargetDestroyed Policy => OnTargetDestroyed.Unrelate;
    }

    private struct Struck : ILink<Entity>
    {
        public Entity Target { get; init; }
        public int Cooldown;
    }

    private enum UpgradeKind { Blades, Dash }

    private struct Upgrade : IAttachment<UpgradeKind>
    {
        public UpgradeKind Kind;
        public int Level;
        public readonly UpgradeKind Key => Kind;
    }

    private struct Clock { public int Tick; }
    private struct Ping : IMessage;
    private struct Seed { public int Value; }

    private static readonly SnapshotSchema<Entity> _schema = new SnapshotSchema<Entity>()
        .Component<Position>()
        .Component<Position>("previous")
        .Component<Velocity>()
        .Tag<Downed>()
        .Relation<Orbits>()
        .Link<Struck>()
        .Attachment<Upgrade, UpgradeKind>()
        .Context<Clock>()
        .Skip<Recording>()
        .Skip<Seed>();

    private static (Registry Registry, Entity Pilot, Entity Blade) World()
    {
        var registry = new Registry();
        Entity pilot = registry.Create(new Position { X = 1 }, new Velocity { X = 2 }, new Recording { Frames = 8 });
        Entity blade = registry.Create(new Position { X = 3 });
        registry.Storage<Position>("previous").Add(pilot, new Position { X = 9 });
        registry.Tag<Downed>(pilot);
        registry.Relate<Orbits>(blade, pilot);
        registry.Links<Struck>().Link(blade, new Struck { Target = pilot, Cooldown = 4 });
        registry.Attach<Upgrade, UpgradeKind>(pilot, new Upgrade { Kind = UpgradeKind.Dash, Level = 2 });
        registry.AddOrReplaceContext(new Clock { Tick = 77 });
        registry.AddOrReplaceContext(new Seed { Value = 5 });
        return (registry, pilot, blade);
    }

    [Fact]
    public void SaveAndLoad_RoundTripEveryDeclaredStore()
    {
        var (source, pilot, blade) = World();
        var archive = new Archive();
        _schema.Save(source, ref archive);

        var loaded = new Registry();
        var reader = archive.Reopen();
        _schema.Load(loaded, ref reader);

        Assert.True(reader.AllRead);
        Assert.Equal(1, loaded.Get<Position>(pilot).X);
        Assert.Equal(9, loaded.Storage<Position>("previous").Get(pilot).X);
        Assert.Equal(2, loaded.Get<Velocity>(pilot).X);
        Assert.True(loaded.HasTag<Downed>(pilot));
        Assert.Equal(pilot, loaded.GetTarget<Orbits>(blade));
        Assert.Equal(4, loaded.Links<Struck>().Get(blade, pilot).Cooldown);
        Assert.Equal(2, loaded.Attachments<Upgrade, UpgradeKind>().Get(pilot, UpgradeKind.Dash).Level);
        Assert.Equal(77, loaded.GetContext<Clock>().Tick);
        Assert.False(loaded.Has<Recording>(pilot));
        Assert.False(loaded.HasContext<Seed>());
    }

    [Fact]
    public void Save_WritesWhatTheHandCallsWrite()
    {
        var (source, _, _) = World();
        var bySchema = new Archive();
        new SnapshotSchema<Entity>().Component<Position>().Tag<Downed>().Context<Clock>().Save(source, ref bySchema);

        var byHand = new Archive();
        source.Snapshot<Archive>().Entities(ref byHand).Component<Position>(ref byHand).Tag<Downed>(ref byHand)
            .Context<Clock>(ref byHand);

        Assert.Equal(byHand.ToJson(), bySchema.ToJson());
    }

    [Fact]
    public void Uncovered_IsEmptyWhenEveryStoreIsSavedOrSkipped()
    {
        var (source, _, _) = World();

        Assert.Empty(_schema.Uncovered(source));
    }

    [Fact]
    public void Uncovered_NamesEachForgottenStore()
    {
        var (source, _, _) = World();
        var schema = new SnapshotSchema<Entity>().Component<Position>().Tag<Downed>();

        var missing = schema.Uncovered(source);

        Assert.Equal(
            [
                new(SnapshotSection.Component, typeof(Velocity), null),
                new(SnapshotSection.Component, typeof(Recording), null),
                new(SnapshotSection.Component, typeof(Position), "previous"),
                new(SnapshotSection.Relation, typeof(Orbits), null),
                new(SnapshotSection.Link, typeof(Struck), null),
                new(SnapshotSection.Attachment, typeof(Upgrade), null),
                new(SnapshotSection.Context, typeof(Clock), null),
                new(SnapshotSection.Context, typeof(Seed), null),
            ],
            missing);
    }

    [Fact]
    public void Uncovered_ListsEveryKindOfStoreOnce()
    {
        var registry = new Registry();
        registry.Tags<Downed>();
        registry.Inboxes<Ping>();
        registry.Attachments<Upgrade, UpgradeKind>();
        registry.Links<Struck>();
        registry.Relations<Orbits>();
        registry.Storage<Position>("previous");
        registry.Storage<Velocity>();
        registry.AddOrReplaceContext(new Clock());

        var missing = new SnapshotSchema<Entity>().Uncovered(registry);

        Assert.Equal(
            [
                new(SnapshotSection.Component, typeof(Position), "previous"),
                new(SnapshotSection.Component, typeof(Velocity), null),
                new(SnapshotSection.Link, typeof(Struck), null),
                new(SnapshotSection.Relation, typeof(Orbits), null),
                new(SnapshotSection.Attachment, typeof(Upgrade), null),
                new(SnapshotSection.Inbox, typeof(Ping), null),
                new(SnapshotSection.Tag, typeof(Downed), null),
                new(SnapshotSection.Context, typeof(Clock), null),
            ],
            missing);
    }

    [Fact]
    public void SkipByName_LeavesTheDefaultPoolUncovered()
    {
        var (source, _, _) = World();
        var schema = new SnapshotSchema<Entity>().Skip<Position>("previous");

        var missing = schema.Uncovered(source);

        Assert.Contains(new(SnapshotSection.Component, typeof(Position), null), missing);
        Assert.DoesNotContain(new(SnapshotSection.Component, typeof(Position), "previous"), missing);
    }

    [Fact]
    public void DeclaringTwice_Throws()
    {
        Assert.Throws<ArgumentException>(() => new SnapshotSchema<Entity>().Component<Position>().Component<Position>());
    }

    [Fact]
    public void SavingWhatIsSkipped_Throws()
    {
        Assert.Throws<ArgumentException>(() => new SnapshotSchema<Entity>().Skip<Position>().Component<Position>());
        Assert.Throws<ArgumentException>(() => new SnapshotSchema<Entity>().Component<Position>("previous").Skip<Position>());
    }

    // Different names, or a type saved as one kind of store and skipped as none, are fine.
    [Fact]
    public void DifferentNames_AreAccepted()
    {
        new SnapshotSchema<Entity>().Component<Position>().Component<Position>("previous");
        new SnapshotSchema<Entity>().Component<Position>().Skip<Position>("previous");
    }

    [Fact]
    public void Context_SavedAsAbsent_RemovesALoadedDefault()
    {
        var source = new Registry();
        var archive = new Archive();
        new SnapshotSchema<Entity>().Context<Clock>().Save(source, ref archive);

        var loaded = new Registry();
        loaded.AddOrReplaceContext(new Clock { Tick = 1 });
        var reader = archive.Reopen();
        new SnapshotSchema<Entity>().Context<Clock>().Load(loaded, ref reader);

        Assert.False(loaded.HasContext<Clock>());
    }

    [Fact]
    public void RefStructArchive_WorksThroughTheSchema()
    {
        var (source, pilot, blade) = World();
        var schema = new SnapshotSchema<Entity>().Component<Position>().Tag<Downed>();
        Span<byte> buffer = stackalloc byte[1024];
        var writer = new SpanArchive(buffer, reading: false);
        schema.Save(source, ref writer);

        var loaded = new Registry();
        var reader = new SpanArchive(buffer[..writer.Position], reading: true);
        schema.Load(loaded, ref reader);

        Assert.Equal(writer.Position, reader.Position);
        Assert.Equal(1, loaded.Get<Position>(pilot).X);
        Assert.Equal(3, loaded.Get<Position>(blade).X);
        Assert.True(loaded.HasTag<Downed>(pilot));
    }
}
