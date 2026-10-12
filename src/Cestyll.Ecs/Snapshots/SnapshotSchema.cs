using Cestyll.Ecs.Attachments;
using Cestyll.Ecs.Inboxes;
using Cestyll.Ecs.Relations;
using Cestyll.Ecs.Tags;

namespace Cestyll.Ecs.Snapshots;

// A store a registry holds, keyed as the section that saves it; Name is set for a named pool.
public readonly record struct SnapshotStore(SnapshotSection Kind, Type Type, string? Name)
{
    public override string ToString() => Name is null ? $"{Kind} {Type.Name}" : $"{Kind} {Type.Name} \"{Name}\"";
}

// Sections declared once for both Save and Load, so they can't disagree on order.
// Entities first, then declaration order. Uncovered lists stores neither saved nor skipped.
public sealed class SnapshotSchema<TEntity>
    where TEntity : unmanaged, IEntity<TEntity>
{
    private readonly List<Section> _sections = [];
    private readonly List<(Type Type, string? Name)> _skipped = [];

    public SnapshotSchema<TEntity> Component<T>() where T : IComponent =>
        Declare(new ComponentSection<T>(null));

    public SnapshotSchema<TEntity> Component<T>(string name) where T : IComponent =>
        Declare(new ComponentSection<T>(Names.Checked(name)));

    public SnapshotSchema<TEntity> Tag<T>() where T : struct, ITag => Declare(new TagSection<T>());

    public SnapshotSchema<TEntity> Relation<TRelation>() where TRelation : struct, IRelation =>
        Declare(new RelationSection<TRelation>());

    public SnapshotSchema<TEntity> Link<T>() where T : struct, ILink<TEntity> => Declare(new LinkSection<T>());

    public SnapshotSchema<TEntity> Attachment<T, TKey>()
        where T : struct, IAttachment<TKey>
        where TKey : notnull =>
        Declare(new AttachmentSection<T, TKey>());

    public SnapshotSchema<TEntity> Inbox<T>() where T : struct, IMessage => Declare(new InboxSection<T>());

    public SnapshotSchema<TEntity> Context<T>() => Declare(new ContextSection<T>());

    // Every store of T, of any kind, named pools included.
    public SnapshotSchema<TEntity> Skip<T>() => Skip(typeof(T), null);

    public SnapshotSchema<TEntity> Skip<T>(string name) where T : IComponent => Skip(typeof(T), Names.Checked(name));

    public IEnumerable<SnapshotStore> Saved
    {
        get
        {
            foreach (var section in _sections)
            {
                yield return section.Store;
            }
        }
    }

    public void Save<TWriter>(Registry<TEntity> registry, ref TWriter w)
        where TWriter : ISnapshotWriter<TEntity>, allows ref struct
    {
        ArgumentNullException.ThrowIfNull(registry);
        var snapshot = registry.Snapshot<TWriter>().Entities(ref w);
        foreach (var section in CollectionsMarshal.AsSpan(_sections))
        {
            section.Save(snapshot, ref w);
        }
    }

    // registry must be unused, as for SnapshotLoader.
    public void Load<TReader>(Registry<TEntity> registry, ref TReader r)
        where TReader : ISnapshotReader<TEntity>, allows ref struct
    {
        ArgumentNullException.ThrowIfNull(registry);
        var loader = registry.SnapshotLoader<TReader>().Entities(ref r);
        foreach (var section in CollectionsMarshal.AsSpan(_sections))
        {
            section.Load(loader, ref r);
        }
    }

    public IReadOnlyList<SnapshotStore> Uncovered(Registry<TEntity> registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        var missing = new List<SnapshotStore>();
        foreach (var store in registry.Stores())
        {
            if (!IsSaved(store) && !IsSkipped(store))
            {
                missing.Add(store);
            }
        }

        return missing;
    }

    private bool IsSaved(SnapshotStore store)
    {
        foreach (var section in _sections)
        {
            if (section.Store == store)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsSkipped(SnapshotStore store)
    {
        foreach (var (type, name) in _skipped)
        {
            if (type == store.Type && (name is null || (store.Kind == SnapshotSection.Component && name == store.Name)))
            {
                return true;
            }
        }

        return false;
    }

    private SnapshotSchema<TEntity> Declare(Section section)
    {
        if (IsSaved(section.Store))
        {
            throw new ArgumentException($"The schema already saves {section.Store}");
        }

        if (IsSkipped(section.Store))
        {
            throw new ArgumentException($"The schema skips {section.Store}, so it can't save it");
        }

        _sections.Add(section);
        return this;
    }

    private SnapshotSchema<TEntity> Skip(Type type, string? name)
    {
        foreach (var section in _sections)
        {
            var store = section.Store;
            if (store.Type == type && (name is null || (store.Kind == SnapshotSection.Component && store.Name == name)))
            {
                throw new ArgumentException($"The schema saves {store}, so it can't skip it");
            }
        }

        _skipped.Add((type, name));
        return this;
    }

    // Generic methods since the archive type is only known at Save or Load.
    private abstract class Section(SnapshotStore store)
    {
        public SnapshotStore Store { get; } = store;

        public abstract void Save<TWriter>(Snapshot<TEntity, TWriter> snapshot, ref TWriter w)
            where TWriter : ISnapshotWriter<TEntity>, allows ref struct;

        public abstract void Load<TReader>(SnapshotLoader<TEntity, TReader> loader, ref TReader r)
            where TReader : ISnapshotReader<TEntity>, allows ref struct;
    }

    private sealed class ComponentSection<T>(string? name)
        : Section(new(SnapshotSection.Component, typeof(T), name))
        where T : IComponent
    {
        public override void Save<TWriter>(Snapshot<TEntity, TWriter> snapshot, ref TWriter w)
        {
            if (name is null)
            {
                snapshot.Component<T>(ref w);
            }
            else
            {
                snapshot.Component<T>(ref w, name);
            }
        }

        public override void Load<TReader>(SnapshotLoader<TEntity, TReader> loader, ref TReader r)
        {
            if (name is null)
            {
                loader.Component<T>(ref r);
            }
            else
            {
                loader.Component<T>(ref r, name);
            }
        }
    }

    private sealed class TagSection<T>() : Section(new(SnapshotSection.Tag, typeof(T), null))
        where T : struct, ITag
    {
        public override void Save<TWriter>(Snapshot<TEntity, TWriter> snapshot, ref TWriter w) =>
            snapshot.Tag<T>(ref w);

        public override void Load<TReader>(SnapshotLoader<TEntity, TReader> loader, ref TReader r) =>
            loader.Tag<T>(ref r);
    }

    private sealed class RelationSection<TRelation>()
        : Section(new(SnapshotSection.Relation, typeof(TRelation), null))
        where TRelation : struct, IRelation
    {
        public override void Save<TWriter>(Snapshot<TEntity, TWriter> snapshot, ref TWriter w) =>
            snapshot.Relation<TRelation>(ref w);

        public override void Load<TReader>(SnapshotLoader<TEntity, TReader> loader, ref TReader r) =>
            loader.Relation<TRelation>(ref r);
    }

    private sealed class LinkSection<T>() : Section(new(SnapshotSection.Link, typeof(T), null))
        where T : struct, ILink<TEntity>
    {
        public override void Save<TWriter>(Snapshot<TEntity, TWriter> snapshot, ref TWriter w) =>
            snapshot.Link<T>(ref w);

        public override void Load<TReader>(SnapshotLoader<TEntity, TReader> loader, ref TReader r) =>
            loader.Link<T>(ref r);
    }

    private sealed class AttachmentSection<T, TKey>()
        : Section(new(SnapshotSection.Attachment, typeof(T), null))
        where T : struct, IAttachment<TKey>
        where TKey : notnull
    {
        public override void Save<TWriter>(Snapshot<TEntity, TWriter> snapshot, ref TWriter w) =>
            snapshot.Attachment<T, TKey>(ref w);

        public override void Load<TReader>(SnapshotLoader<TEntity, TReader> loader, ref TReader r) =>
            loader.Attachment<T, TKey>(ref r);
    }

    private sealed class InboxSection<T>() : Section(new(SnapshotSection.Inbox, typeof(T), null))
        where T : struct, IMessage
    {
        public override void Save<TWriter>(Snapshot<TEntity, TWriter> snapshot, ref TWriter w) =>
            snapshot.Inbox<T>(ref w);

        public override void Load<TReader>(SnapshotLoader<TEntity, TReader> loader, ref TReader r) =>
            loader.Inbox<T>(ref r);
    }

    private sealed class ContextSection<T>() : Section(new(SnapshotSection.Context, typeof(T), null))
    {
        public override void Save<TWriter>(Snapshot<TEntity, TWriter> snapshot, ref TWriter w) =>
            snapshot.Context<T>(ref w);

        public override void Load<TReader>(SnapshotLoader<TEntity, TReader> loader, ref TReader r) =>
            loader.Context<T>(ref r);
    }
}
