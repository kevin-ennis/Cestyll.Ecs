using Cestyll.Ecs.Attachments;
using Cestyll.Ecs.Inboxes;
using Cestyll.Ecs.Relations;
using Cestyll.Ecs.Storage;
using Cestyll.Ecs.Tags;

namespace Cestyll.Ecs.Snapshots;

// Each call writes one section to the by-ref archive; loaders read them in the same order.
// A missing pool or store writes an empty section rather than being created.
public sealed class Snapshot<TEntity, TWriter>
    where TEntity : unmanaged, IEntity<TEntity>
    where TWriter : ISnapshotWriter<TEntity>, allows ref struct
{
    private readonly Registry<TEntity> _registry;

    internal Snapshot(Registry<TEntity> registry) => _registry = registry;

    public Snapshot<TEntity, TWriter> Entities(ref TWriter w)
    {
        _registry.WriteEntities(ref w);
        return this;
    }

    // Entity list, then values as one span. Byte copies include padding: check determinism with Fingerprint.
    public Snapshot<TEntity, TWriter> Component<T>(ref TWriter w) where T : IComponent =>
        Component(ref w, _registry.FindStorage<T>(), null);

    public Snapshot<TEntity, TWriter> Component<T>(ref TWriter w, string name) where T : IComponent =>
        Component(ref w, _registry.FindStorage<T>(Names.Checked(name)), name);

    private Snapshot<TEntity, TWriter> Component<T>(ref TWriter w, Pool<TEntity, T>? pool, string? name)
        where T : IComponent
    {
        w.BeginSection(SnapshotSection.Component, typeof(T), name);
        SnapshotIo<TEntity>.WriteEntityList(ref w, pool is null ? [] : pool.Entities);
        w.WriteComponents<T>(pool is null ? [] : pool.Components);
        w.EndSection();
        return this;
    }

    // Pairs of (source, target). Sibling order isn't preserved on load.
    public Snapshot<TEntity, TWriter> Relation<TRelation>(ref TWriter w) where TRelation : struct, IRelation
    {
        w.BeginSection(SnapshotSection.Relation, typeof(TRelation), null);
        if (_registry.FindRelations<TRelation>() is { } relation)
        {
            relation.Write(ref w);
        }
        else
        {
            w.WriteCount(0);
        }

        w.EndSection();
        return this;
    }

    // Count, sources, then link data (holding the targets).
    public Snapshot<TEntity, TWriter> Link<T>(ref TWriter w) where T : struct, ILink<TEntity>
    {
        w.BeginSection(SnapshotSection.Link, typeof(T), null);
        if (_registry.FindLinks<T>() is { } links)
        {
            links.Write(ref w);
        }
        else
        {
            w.WriteCount(0);
        }

        w.EndSection();
        return this;
    }

    // Owners, per-owner counts, then items. Order within an owner isn't preserved.
    public Snapshot<TEntity, TWriter> Attachment<T, TKey>(ref TWriter w)
        where T : struct, IAttachment<TKey>
        where TKey : notnull
    {
        var set = _registry.FindAttachments<T, TKey>();
        return Owned(ref w, SnapshotSection.Attachment, set is null ? [] : set.Entities, e => set!.GetAll(e));
    }

    // Owners, per-owner counts, then messages in posting order.
    public Snapshot<TEntity, TWriter> Inbox<T>(ref TWriter w)
        where T : struct, IMessage
    {
        var inbox = _registry.FindInboxes<T>();
        return Owned(ref w, SnapshotSection.Inbox, inbox is null ? [] : inbox.Entities, e => inbox!.GetAll(e));
    }

    private Snapshot<TEntity, TWriter> Owned<T>(
        ref TWriter w, SnapshotSection section, ReadOnlySpan<TEntity> owners, Func<TEntity, Span<T>> items)
    {
        w.BeginSection(section, typeof(T), null);
        SnapshotIo<TEntity>.WriteEntityList(ref w, owners);
        foreach (var e in owners)
        {
            w.WriteCount(items(e).Length);
        }

        foreach (var e in owners)
        {
            SnapshotIo<TEntity>.WriteValues<TWriter, T>(ref w, items(e));
        }

        w.EndSection();
        return this;
    }

    // Count 1 and the value via Write<T>, or 0 if unset.
    public Snapshot<TEntity, TWriter> Context<T>(ref TWriter w)
    {
        w.BeginSection(SnapshotSection.Context, typeof(T), null);
        if (_registry.HasContext<T>())
        {
            w.WriteCount(1);
            w.Write(in _registry.GetContext<T>());
        }
        else
        {
            w.WriteCount(0);
        }

        w.EndSection();
        return this;
    }

    public Snapshot<TEntity, TWriter> Tag<T>(ref TWriter w) where T : struct, ITag
    {
        w.BeginSection(SnapshotSection.Tag, typeof(T), null);
        _registry.WriteTag<T, TWriter>(ref w);
        w.EndSection();
        return this;
    }
}
