using Cestyll.Ecs.Attachments;
using Cestyll.Ecs.Collections;
using Cestyll.Ecs.Inboxes;
using Cestyll.Ecs.Relations;
using Cestyll.Ecs.Tags;

namespace Cestyll.Ecs.Snapshots;

// Merges repeated snapshots from another registry into a live one, mapping remote ids to local.
// Entity fields in values need a remap callback. A pass starts with Entities.
// Sections diff against live state: a kept value fires OnUpdate, a new one OnAdd, a dropped one OnRemove.
public sealed class ContinuousLoader<TEntity, TReader>
    where TEntity : unmanaged, IEntity<TEntity>
    where TReader : ISnapshotReader<TEntity>, allows ref struct
{
    private readonly Registry<TEntity> _registry;
    private readonly Dictionary<TEntity, TEntity> _remoteToLocal = [];
    private readonly EntityBitset _seen = new(); // remote indices live in this pass
    private readonly EntityBitset _dead = new(); // remote free list of this pass
    private readonly EntityBitset _incoming = new(); // local indices the current section lists
    private readonly HashSet<(TEntity, TEntity)> _incomingLinks = [];
    private readonly List<TEntity> _staleTargets = [];
    private List<uint> _versions = []; // remote versions of this pass, by index

    internal ContinuousLoader(Registry<TEntity> registry) => _registry = registry;

    public TEntity Map(TEntity remote) =>
        _remoteToLocal.TryGetValue(remote, out var local) ? local : default(TEntity);

    public bool Contains(TEntity remote) => _remoteToLocal.ContainsKey(remote);

    private bool Seen(TEntity remote) => _seen.Test(remote.Index) && _versions[remote.Index] == remote.Version;

    // Null unless remote is in this pass and its local is alive, so applying a section can't throw.
    private TEntity MapLive(TEntity remote) =>
        Seen(remote) && _remoteToLocal.TryGetValue(remote, out var local) && _registry.IsAlive(local)
            ? local
            : default(TEntity);

    // Starts a pass; creates locals for new or locally dead remotes.
    public ContinuousLoader<TEntity, TReader> Entities(ref TReader r)
    {
        // Read fully before clearing _seen, so a rejected section can't make Orphans destroy everything.
        var (created, versions, free) = SnapshotIo<TEntity>.ReadEntitySection(ref r);
        _seen.Clear();
        _dead.Clear();
        _versions = versions;
        foreach (var i in free)
        {
            _dead.Set(i);
        }

        for (var i = 1; i < created; i++)
        {
            if (_dead.Test(i))
            {
                continue;
            }

            _seen.Set(i);
            // One hash lookup; safe since Create never touches the map.
            var remote = TEntity.Make(i, versions[i]);
            ref var local = ref CollectionsMarshal.GetValueRefOrAddDefault(_remoteToLocal, remote, out var exists);
            if (!exists || !_registry.IsAlive(local))
            {
                local = _registry.Create();
            }
        }

        return this;
    }

    public ContinuousLoader<TEntity, TReader> Component<T>(ref TReader r, Func<T, Func<TEntity, TEntity>, T>? remap = null)
        where T : IComponent =>
        Read(ref r, null, remap);

    // A named pool, as Snapshot.Component<T>(name) wrote it; a bad name throws before reading.
    public ContinuousLoader<TEntity, TReader> Component<T>(
        ref TReader r, string name, Func<T, Func<TEntity, TEntity>, T>? remap = null)
        where T : IComponent =>
        Read(ref r, Names.Checked(name), remap);

    private ContinuousLoader<TEntity, TReader> Read<T>(
        ref TReader r, string? name, Func<T, Func<TEntity, TEntity>, T>? remap)
        where T : IComponent
    {
        r.BeginSection(SnapshotSection.Component, typeof(T), name);
        var entities = SnapshotIo<TEntity>.ReadEntities(ref r, TEntity.MaxIndex, "entity list");
        var values = SnapshotIo<TEntity>.ReadComponents<TReader, T>(ref r, entities.Length);
        r.EndSection();
        Apply<T>(name, entities, values, remap);
        return this;
    }

    // Pairs with an unmapped end are dropped. Read fully first, so a truncated archive changes nothing.
    public ContinuousLoader<TEntity, TReader> Relation<TRelation>(ref TReader r) where TRelation : struct, IRelation
    {
        r.BeginSection(SnapshotSection.Relation, typeof(TRelation), null);
        var pairs = SnapshotIo<TEntity>.ReadRelationPairs(ref r);
        r.EndSection();
        _incoming.Clear();
        foreach (var (remoteSource, remoteTarget) in pairs)
        {
            var (source, target) = (MapLive(remoteSource), MapLive(remoteTarget));
            if (!source.IsNull && !target.IsNull)
            {
                _registry.Relate<TRelation>(source, target); // the same target is a no-op, another replaces
                _incoming.Set(source.Index);
            }
        }

        foreach (var local in _remoteToLocal.Values)
        {
            if (!_incoming.Test(local.Index))
            {
                _registry.Unrelate<TRelation>(local);
            }
        }

        return this;
    }

    // remap is required: the target lives in the link data. Links with a dead end are dropped.
    public ContinuousLoader<TEntity, TReader> Link<T>(ref TReader r, Func<T, Func<TEntity, TEntity>, T> remap)
        where T : struct, ILink<TEntity>
    {
        r.BeginSection(SnapshotSection.Link, typeof(T), null);
        var sources = SnapshotIo<TEntity>.ReadEntities(ref r, Array.MaxLength, "link");
        var links = SnapshotIo<TEntity>.ReadValues<TReader, T>(ref r, sources.Length);
        r.EndSection();
        var map = MapLive;
        for (var i = 0; i < sources.Length; i++)
        {
            sources[i] = MapLive(sources[i]);
            if (!sources[i].IsNull)
            {
                links[i] = remap(links[i], map);
            }
        }

        // Nothing below can fail.
        var set = _registry.Links<T>();
        _incoming.Clear();
        _incomingLinks.Clear();
        for (var i = 0; i < sources.Length; i++)
        {
            var target = links[i].Target;
            if (!sources[i].IsNull && _registry.IsAlive(target))
            {
                set.Link(sources[i], links[i]); // replaces a link to the same target
                _incoming.Set(sources[i].Index);
                _incomingLinks.Add((sources[i], target));
            }
        }

        foreach (var local in _remoteToLocal.Values)
        {
            if (set.TargetCount(local) == 0)
            {
                continue;
            }

            if (!_incoming.Test(local.Index))
            {
                set.UnlinkTargets(local);
                continue;
            }

            _staleTargets.Clear();
            foreach (ref var link in set.GetTargets(local))
            {
                if (!_incomingLinks.Contains((local, link.Target)))
                {
                    _staleTargets.Add(link.Target);
                }
            }

            foreach (var target in _staleTargets)
            {
                set.Unlink(local, target);
            }
        }

        return this;
    }

    // Mapped entities' attachments are diffed by key.
    public ContinuousLoader<TEntity, TReader> Attachment<T, TKey>(
        ref TReader r, Func<T, Func<TEntity, TEntity>, T>? remap = null)
        where T : struct, IAttachment<TKey>
        where TKey : notnull
    {
        var (locals, counts) = ReadOwned(ref r, SnapshotSection.Attachment, remap, out var items);

        // Nothing below can fail.
        var set = _registry.Attachments<T, TKey>();
        _incoming.Clear();
        List<TKey>? stale = null;
        var next = 0;
        for (var i = 0; i < locals.Length; i++)
        {
            var local = locals[i];
            var incoming = items.Slice(next, counts[i]);
            next += counts[i];
            if (local.IsNull)
            {
                continue;
            }

            _incoming.Set(local.Index);
            foreach (var item in incoming)
            {
                set.Attach(local, item); // a repeated key replaces
            }

            stale?.Clear();
            foreach (var held in set.GetAll(local))
            {
                if (!HasKey(incoming, held.Key))
                {
                    (stale ??= []).Add(held.Key);
                }
            }

            if (stale is not null)
            {
                foreach (var key in stale)
                {
                    set.Detach(local, key);
                }
            }
        }

        foreach (var local in _remoteToLocal.Values)
        {
            if (!_incoming.Test(local.Index))
            {
                set.DetachItems(local);
            }
        }

        return this;
    }

    private static bool HasKey<T, TKey>(ReadOnlySpan<T> items, TKey key)
        where T : struct, IAttachment<TKey>
        where TKey : notnull
    {
        foreach (var item in items)
        {
            if (EqualityComparer<TKey>.Default.Equals(item.Key, key))
            {
                return true;
            }
        }

        return false;
    }

    // Listed inboxes are replaced wholesale, in saved order; other mapped ones are cleared.
    public ContinuousLoader<TEntity, TReader> Inbox<T>(
        ref TReader r, Func<T, Func<TEntity, TEntity>, T>? remap = null)
        where T : struct, IMessage
    {
        var (locals, counts) = ReadOwned(ref r, SnapshotSection.Inbox, remap, out var items);

        // Nothing below can fail.
        var inbox = _registry.Inboxes<T>();
        _incoming.Clear();
        var next = 0;
        for (var i = 0; i < locals.Length; i++)
        {
            var local = locals[i];
            var incoming = items.Slice(next, counts[i]);
            next += counts[i];
            if (local.IsNull)
            {
                continue;
            }

            _incoming.Set(local.Index);
            inbox.ClearMessages(local);
            foreach (var message in incoming)
            {
                inbox.Post(local, message);
            }
        }

        foreach (var local in _remoteToLocal.Values)
        {
            if (!_incoming.Test(local.Index) && inbox.Has(local))
            {
                inbox.ClearMessages(local);
            }
        }

        return this;
    }

    // Owners, per-owner counts, then items. Locals are null where unmapped; their items aren't remapped.
    private (TEntity[] Locals, int[] Counts) ReadOwned<T>(
        ref TReader r, SnapshotSection section, Func<T, Func<TEntity, TEntity>, T>? remap, out Span<T> items)
    {
        r.BeginSection(section, typeof(T), null);
        var owners = SnapshotIo<TEntity>.ReadEntities(ref r, TEntity.MaxIndex, "entity list");
        var counts = SnapshotIo<TEntity>.ReadAttachmentCounts(ref r, owners.Length, out var total);
        items = SnapshotIo<TEntity>.ReadValues<TReader, T>(ref r, total);
        r.EndSection();
        SnapshotIo<TEntity>.RejectDuplicates(owners);

        var map = MapLive;
        var locals = new TEntity[owners.Length];
        var next = 0;
        for (var i = 0; i < owners.Length; i++)
        {
            locals[i] = MapLive(owners[i]);
            if (!locals[i].IsNull && remap is not null)
            {
                foreach (ref var item in items.Slice(next, counts[i]))
                {
                    item = remap(item, map);
                }
            }

            next += counts[i];
        }

        return (locals, counts);
    }

    // A value saved as absent removes the local one.
    public ContinuousLoader<TEntity, TReader> Context<T>(ref TReader r, Func<T, Func<TEntity, TEntity>, T>? remap = null)
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
            _registry.AddOrReplaceContext(remap is null ? value : remap(value, MapLive));
        }
        else
        {
            _registry.RemoveContext<T>();
        }

        return this;
    }

    public ContinuousLoader<TEntity, TReader> Tag<T>(ref TReader r) where T : struct, ITag
    {
        r.BeginSection(SnapshotSection.Tag, typeof(T), null);
        var entities = SnapshotIo<TEntity>.ReadEntities(ref r, TEntity.MaxIndex, "entity list");
        r.EndSection();
        _incoming.Clear();
        foreach (var remote in entities)
        {
            var local = MapLive(remote);
            if (!local.IsNull)
            {
                _registry.Tag<T>(local); // no-op if already tagged
                _incoming.Set(local.Index);
            }
        }

        foreach (var local in _remoteToLocal.Values)
        {
            if (!_incoming.Test(local.Index))
            {
                _registry.Untag<T>(local);
            }
        }

        return this;
    }

    // Destroys locals whose remote wasn't in the last pass.
    public ContinuousLoader<TEntity, TReader> Orphans()
    {
        List<TEntity>? stale = null;
        foreach (var (remote, local) in _remoteToLocal)
        {
            if (Seen(remote))
            {
                continue;
            }

            _registry.Destroy(local);
            (stale ??= []).Add(remote);
        }

        if (stale is not null)
        {
            foreach (var remote in stale)
            {
                _remoteToLocal.Remove(remote);
            }
        }

        return this;
    }

    // Sets T on listed entities, removes it from other mapped ones. Validation and remap run first.
    private void Apply<T>(string? name, ReadOnlySpan<TEntity> entities, Span<T> values, Func<T, Func<TEntity, TEntity>, T>? remap)
        where T : IComponent
    {
        SnapshotIo<TEntity>.RejectDuplicates(entities);

        var map = MapLive;
        var locals = new TEntity[entities.Length];
        for (var i = 0; i < entities.Length; i++)
        {
            locals[i] = MapLive(entities[i]);
            if (!locals[i].IsNull && remap is not null)
            {
                values[i] = remap(values[i], map);
            }
        }

        // Nothing below can fail: locals are live and distinct.
        var pool = name is null ? _registry.Storage<T>() : _registry.Storage<T>(name);
        _incoming.Clear();
        for (var i = 0; i < locals.Length; i++)
        {
            if (!locals[i].IsNull)
            {
                pool.AddOrReplace(locals[i], values[i]);
                _incoming.Set(locals[i].Index);
            }
        }

        foreach (var local in _remoteToLocal.Values)
        {
            if (!_incoming.Test(local.Index))
            {
                pool.Remove(local);
            }
        }
    }
}
