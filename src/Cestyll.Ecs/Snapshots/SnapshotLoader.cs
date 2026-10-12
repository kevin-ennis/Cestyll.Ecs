using Cestyll.Ecs.Attachments;
using Cestyll.Ecs.Inboxes;
using Cestyll.Ecs.Relations;
using Cestyll.Ecs.Storage;
using Cestyll.Ecs.Tags;

namespace Cestyll.Ecs.Snapshots;

// Restores into a never-used registry (Clear isn't enough) with identical entity ids.
// Each section is read and validated before anything is applied. Groups, handlers etc. may pre-exist.
public sealed class SnapshotLoader<TEntity, TReader>
    where TEntity : unmanaged, IEntity<TEntity>
    where TReader : ISnapshotReader<TEntity>, allows ref struct
{
    private readonly Registry<TEntity> _registry;

    internal SnapshotLoader(Registry<TEntity> registry) => _registry = registry;

    public SnapshotLoader<TEntity, TReader> Entities(ref TReader r)
    {
        _registry.ReadEntities(ref r);
        return this;
    }

    public SnapshotLoader<TEntity, TReader> Component<T>(ref TReader r) where T : IComponent =>
        Component(ref r, _registry.Storage<T>(), null);

    public SnapshotLoader<TEntity, TReader> Component<T>(ref TReader r, string name) where T : IComponent =>
        Component(ref r, _registry.Storage<T>(name), name);

    private SnapshotLoader<TEntity, TReader> Component<T>(ref TReader r, Pool<TEntity, T> pool, string? name)
        where T : IComponent
    {
        r.BeginSection(SnapshotSection.Component, typeof(T), name);
        var entities = SnapshotIo<TEntity>.ReadEntities(ref r, TEntity.MaxIndex, "entity list");
        var values = SnapshotIo<TEntity>.ReadComponents<TReader, T>(ref r, entities.Length);
        r.EndSection();
        Add<T>(pool, entities, values);
        return this;
    }

    private void Add<T>(Pool<TEntity, T> pool, ReadOnlySpan<TEntity> entities, ReadOnlySpan<T> values)
        where T : IComponent
    {
        ValidateAlive(entities);
        pool.Reserve(pool.Count + entities.Length);
        for (var i = 0; i < entities.Length; i++)
        {
            pool.Add(entities[i], values[i]);
        }
    }

    // Pool.Add's cheap IsCurrent check can miss corrupt data; loads aren't hot, so check fully.
    private void ValidateAlive(ReadOnlySpan<TEntity> entities)
    {
        SnapshotIo<TEntity>.RejectDuplicates(entities);
        foreach (var e in entities)
        {
            if (!_registry.IsAlive(e))
            {
                throw new InvalidDataException(
                    $"Snapshot references {e}, which is not alive in the loaded entity section");
            }
        }
    }

    // Validates all pairs first, so a bad file can't half-load.
    public SnapshotLoader<TEntity, TReader> Relation<TRelation>(ref TReader r) where TRelation : struct, IRelation
    {
        r.BeginSection(SnapshotSection.Relation, typeof(TRelation), null);
        var pairs = SnapshotIo<TEntity>.ReadRelationPairs(ref r);
        r.EndSection();
        foreach (var (source, target) in pairs)
        {
            if (!_registry.IsAlive(source) || !_registry.IsAlive(target))
            {
                throw new InvalidDataException($"Snapshot links {source} -> {target}, which is not alive");
            }
        }

        foreach (var (source, target) in pairs)
        {
            _registry.Relate<TRelation>(source, target);
        }

        return this;
    }

    public SnapshotLoader<TEntity, TReader> Link<T>(ref TReader r) where T : struct, ILink<TEntity>
    {
        r.BeginSection(SnapshotSection.Link, typeof(T), null);
        var sources = SnapshotIo<TEntity>.ReadEntities(ref r, Array.MaxLength, "link");
        var links = SnapshotIo<TEntity>.ReadValues<TReader, T>(ref r, sources.Length);
        r.EndSection();
        for (var i = 0; i < sources.Length; i++)
        {
            if (!_registry.IsAlive(sources[i]) || !_registry.IsAlive(links[i].Target))
            {
                throw new InvalidDataException(
                    $"Snapshot links {sources[i]} -> {links[i].Target}, which is not alive");
            }
        }

        var set = _registry.Links<T>();
        for (var i = 0; i < sources.Length; i++)
        {
            set.Link(sources[i], links[i]); // a repeated pair replaces, so this can't throw
        }

        return this;
    }

    public SnapshotLoader<TEntity, TReader> Attachment<T, TKey>(ref TReader r)
        where T : struct, IAttachment<TKey>
        where TKey : notnull =>
        Owned<T>(ref r, SnapshotSection.Attachment, () =>
        {
            var set = _registry.Attachments<T, TKey>();
            return (e, item) => set.Attach(e, item); // a repeated key replaces, so this can't throw
        });

    public SnapshotLoader<TEntity, TReader> Inbox<T>(ref TReader r)
        where T : struct, IMessage =>
        Owned<T>(ref r, SnapshotSection.Inbox, () => _registry.Inboxes<T>().Post);

    // Owners, per-owner counts, then items. target opens the store once the section is validated.
    private SnapshotLoader<TEntity, TReader> Owned<T>(ref TReader r, SnapshotSection section, Func<Action<TEntity, T>> target)
    {
        r.BeginSection(section, typeof(T), null);
        var owners = SnapshotIo<TEntity>.ReadEntities(ref r, TEntity.MaxIndex, "entity list");
        var counts = SnapshotIo<TEntity>.ReadAttachmentCounts(ref r, owners.Length, out var total);
        var items = SnapshotIo<TEntity>.ReadValues<TReader, T>(ref r, total);
        r.EndSection();
        ValidateAlive(owners);
        var add = target();
        var next = 0;
        for (var i = 0; i < owners.Length; i++)
        {
            foreach (var item in items.Slice(next, counts[i]))
            {
                add(owners[i], item);
            }

            next += counts[i];
        }

        return this;
    }

    // A value saved as absent removes any preset one.
    public SnapshotLoader<TEntity, TReader> Context<T>(ref TReader r)
    {
        r.BeginSection(SnapshotSection.Context, typeof(T), null);
        var count = r.ReadCount();
        if (count is not (0 or 1))
        {
            throw new InvalidDataException($"Context section for {typeof(T).Name} holds {count} values");
        }

        var value = count == 1 ? r.Read<T>() : default!;
        r.EndSection();
        if (count == 1)
        {
            _registry.AddOrReplaceContext(value);
        }
        else
        {
            _registry.RemoveContext<T>();
        }

        return this;
    }

    public SnapshotLoader<TEntity, TReader> Tag<T>(ref TReader r) where T : struct, ITag
    {
        r.BeginSection(SnapshotSection.Tag, typeof(T), null);
        var entities = SnapshotIo<TEntity>.ReadEntities(ref r, TEntity.MaxIndex, "entity list");
        r.EndSection();
        ValidateAlive(entities); // InvalidDataException, not Tag's InvalidOperationException
        foreach (var e in entities)
        {
            _registry.Tag<T>(e);
        }

        return this;
    }
}
