using Cestyll.Ecs.Attachments;
using Cestyll.Ecs.Inboxes;
using Cestyll.Ecs.Relations;
using Cestyll.Ecs.Storage;
using Cestyll.Ecs.Tags;

namespace Cestyll.Ecs.Observers;

// Collects each entity once when a watched signal fires, until Clear or destroy.
// Membership means a signal fired, not that the component is still there. Filters apply at collect time.
public sealed class Observer<TEntity> : IDisposable
    where TEntity : unmanaged, IEntity<TEntity>
{
    private readonly Registry<TEntity> _registry;
    private readonly EntitySet<TEntity> _set = new();
    private readonly Action<TEntity> _collect;
    private readonly PairHandler<TEntity> _collectSource;
    private readonly List<Action> _disconnect = [];
    // Arrays: Collect walks them on every signal; append by copy is fine.
    private SparseSet<TEntity>[] _with = [];
    private SparseSet<TEntity>[] _without = [];
    private TagSet<TEntity>[] _withTags = [];
    private TagSet<TEntity>[] _withoutTags = [];
    private readonly List<SparseSet<TEntity>> _watched = [];
    private readonly List<TagSet<TEntity>> _watchedTags = [];

    internal Observer(Registry<TEntity> registry)
    {
        _registry = registry;
        _collect = Collect;
        _collectSource = (source, _) => Collect(source);
    }

    public int Count => _set.Count;
    public ReadOnlySpan<TEntity> Entities => _set.Entities;
    public bool Contains(TEntity e) => _set.Has(e);
    public void Clear() => _set.Clear();

    // Clear keeps the sparse array's size; this trims it.
    public void ShrinkToFit() => _set.ShrinkToFit();

    internal int SparseCapacity => _set.SparseCapacity;

    // Naming a pool creates it, so a misspelled name silently never collects.
    public Observer<TEntity> OnAdd<T>() where T : IComponent => OnAddIn<T>(null);
    public Observer<TEntity> OnAdd<T>(string name) where T : IComponent => OnAddIn<T>(Names.Checked(name));

    private Observer<TEntity> OnAddIn<T>(string? name)
        where T : IComponent
    {
        var pool = Watch<T>(name);
        pool.OnAdd += _collect;
        _disconnect.Add(() => pool.OnAdd -= _collect);
        return this;
    }

    public Observer<TEntity> OnUpdate<T>() where T : IComponent => OnUpdateIn<T>(null);
    public Observer<TEntity> OnUpdate<T>(string name) where T : IComponent => OnUpdateIn<T>(Names.Checked(name));

    private Observer<TEntity> OnUpdateIn<T>(string? name)
        where T : IComponent
    {
        var pool = Watch<T>(name);
        pool.OnUpdate += _collect;
        _disconnect.Add(() => pool.OnUpdate -= _collect);
        return this;
    }

    // Destroyed entities are dropped afterwards, so only removals from live ones show.
    public Observer<TEntity> OnRemove<T>() where T : IComponent => OnRemoveIn<T>(null);
    public Observer<TEntity> OnRemove<T>(string name) where T : IComponent => OnRemoveIn<T>(Names.Checked(name));

    private Observer<TEntity> OnRemoveIn<T>(string? name)
        where T : IComponent
    {
        var pool = Watch<T>(name);
        pool.OnRemove += _collect;
        _disconnect.Add(() => pool.OnRemove -= _collect);
        return this;
    }

    public Observer<TEntity> OnTag<T>() where T : struct, ITag
    {
        var tags = WatchTags<T>();
        tags.OnTag += _collect;
        _disconnect.Add(() => tags.OnTag -= _collect);
        return this;
    }

    public Observer<TEntity> OnUntag<T>() where T : struct, ITag
    {
        var tags = WatchTags<T>();
        tags.OnUntag += _collect;
        _disconnect.Add(() => tags.OnUntag -= _collect);
        return this;
    }

    public Observer<TEntity> OnRelate<TRelation>() where TRelation : struct, IRelation
    {
        var relation = Live.Relations<TRelation>();
        relation.OnRelate += _collectSource;
        _disconnect.Add(() => relation.OnRelate -= _collectSource);
        return this;
    }

    public Observer<TEntity> OnUnrelate<TRelation>() where TRelation : struct, IRelation
    {
        var relation = Live.Relations<TRelation>();
        relation.OnUnrelate += _collectSource;
        _disconnect.Add(() => relation.OnUnrelate -= _collectSource);
        return this;
    }

    public Observer<TEntity> OnLink<T>() where T : struct, ILink<TEntity>
    {
        var links = Live.Links<T>();
        links.OnLink += _collectSource;
        _disconnect.Add(() => links.OnLink -= _collectSource);
        return this;
    }

    // Not OnUpdate<T>: constraints don't tell overloads apart.
    public Observer<TEntity> OnLinkUpdate<T>() where T : struct, ILink<TEntity>
    {
        var links = Live.Links<T>();
        links.OnUpdate += _collectSource;
        _disconnect.Add(() => links.OnUpdate -= _collectSource);
        return this;
    }

    public Observer<TEntity> OnUnlink<T>() where T : struct, ILink<TEntity>
    {
        var links = Live.Links<T>();
        links.OnUnlink += _collectSource;
        _disconnect.Add(() => links.OnUnlink -= _collectSource);
        return this;
    }

    public Observer<TEntity> OnAttach<T, TKey>()
        where T : struct, IAttachment<TKey>
        where TKey : notnull
    {
        var attachments = Live.Attachments<T, TKey>();
        AttachmentHandler<TEntity, TKey> collect = (e, _) => Collect(e);
        attachments.OnAttach += collect;
        _disconnect.Add(() => attachments.OnAttach -= collect);
        return this;
    }

    public Observer<TEntity> OnAttachmentUpdate<T, TKey>()
        where T : struct, IAttachment<TKey>
        where TKey : notnull
    {
        var attachments = Live.Attachments<T, TKey>();
        AttachmentHandler<TEntity, TKey> collect = (e, _) => Collect(e);
        attachments.OnUpdate += collect;
        _disconnect.Add(() => attachments.OnUpdate -= collect);
        return this;
    }

    public Observer<TEntity> OnDetach<T, TKey>()
        where T : struct, IAttachment<TKey>
        where TKey : notnull
    {
        var attachments = Live.Attachments<T, TKey>();
        AttachmentHandler<TEntity, TKey> collect = (e, _) => Collect(e);
        attachments.OnDetach += collect;
        _disconnect.Add(() => attachments.OnDetach -= collect);
        return this;
    }

    public Observer<TEntity> OnPost<T>() where T : struct, IMessage
    {
        var inbox = Live.Inboxes<T>();
        MessageHandler<TEntity, T> collect = (e, in _) => Collect(e);
        inbox.OnPost += collect;
        _disconnect.Add(() => inbox.OnPost -= collect);
        return this;
    }

    public Observer<TEntity> With<T>() where T : IComponent => WithIn<T>(null);
    public Observer<TEntity> With<T>(string name) where T : IComponent => WithIn<T>(Names.Checked(name));

    private Observer<TEntity> WithIn<T>(string? name)
        where T : IComponent
    {
        var pool = Connect<T>(name);
        if (Array.IndexOf(_without, pool) >= 0)
        {
            throw Contradicts(pool);
        }

        _with = [.. _with, pool];
        return this;
    }

    public Observer<TEntity> Without<T>() where T : IComponent => WithoutIn<T>(null);
    public Observer<TEntity> Without<T>(string name) where T : IComponent => WithoutIn<T>(Names.Checked(name));

    private Observer<TEntity> WithoutIn<T>(string? name)
        where T : IComponent
    {
        var pool = Connect<T>(name);
        if (_watched.Contains(pool))
        {
            throw NeverCollects(pool);
        }

        if (Array.IndexOf(_with, pool) >= 0)
        {
            throw Contradicts(pool);
        }

        _without = [.. _without, pool];
        return this;
    }

    public Observer<TEntity> WithTag<T>() where T : struct, ITag
    {
        var tags = ConnectTags<T>();
        if (Array.IndexOf(_withoutTags, tags) >= 0)
        {
            throw Contradicts<T>();
        }

        _withTags = [.. _withTags, tags];
        return this;
    }

    public Observer<TEntity> WithoutTag<T>() where T : struct, ITag
    {
        var tags = ConnectTags<T>();
        if (_watchedTags.Contains(tags))
        {
            throw NeverCollects<T>();
        }

        if (Array.IndexOf(_withTags, tags) >= 0)
        {
            throw Contradicts<T>();
        }

        _withoutTags = [.. _withoutTags, tags];
        return this;
    }

    public Observer<TEntity> WithAttachment<T>() where T : struct, IAttachment =>
        WithOwners<T>(Live.AttachmentOwners<T>());

    public Observer<TEntity> WithoutAttachment<T>() where T : struct, IAttachment =>
        WithoutOwners<T>(Live.AttachmentOwners<T>());

    public Observer<TEntity> WithInbox<T>() where T : struct, IMessage => WithOwners<T>(Live.Inboxes<T>().Owners);
    public Observer<TEntity> WithoutInbox<T>() where T : struct, IMessage => WithoutOwners<T>(Live.Inboxes<T>().Owners);

    // WithRelationTarget: has a target (a parent). WithRelationSources: is a target (has children).
    public Observer<TEntity> WithRelationTarget<TRelation>() where TRelation : struct, IRelation =>
        WithOwners<TRelation>(Live.Relations<TRelation>().HasTargets);

    public Observer<TEntity> WithoutRelationTarget<TRelation>() where TRelation : struct, IRelation =>
        WithoutOwners<TRelation>(Live.Relations<TRelation>().HasTargets);

    public Observer<TEntity> WithRelationSources<TRelation>() where TRelation : struct, IRelation =>
        WithOwners<TRelation>(Live.Relations<TRelation>().HasSources);

    public Observer<TEntity> WithoutRelationSources<TRelation>() where TRelation : struct, IRelation =>
        WithoutOwners<TRelation>(Live.Relations<TRelation>().HasSources);

    public Observer<TEntity> WithLinkTargets<T>() where T : struct, ILink<TEntity> =>
        WithOwners<T>(Live.Links<T>().HasTargets);

    public Observer<TEntity> WithoutLinkTargets<T>() where T : struct, ILink<TEntity> =>
        WithoutOwners<T>(Live.Links<T>().HasTargets);

    public Observer<TEntity> WithLinkSources<T>() where T : struct, ILink<TEntity> =>
        WithOwners<T>(Live.Links<T>().HasSources);

    public Observer<TEntity> WithoutLinkSources<T>() where T : struct, ILink<TEntity> =>
        WithoutOwners<T>(Live.Links<T>().HasSources);

    // Owner sets join the component filters; T names them in errors.
    private Observer<TEntity> WithOwners<T>(SparseSet<TEntity> owners)
    {
        if (Array.IndexOf(_without, owners) >= 0)
        {
            throw Contradicts<T>();
        }

        _with = [.. _with, owners];
        return this;
    }

    private Observer<TEntity> WithoutOwners<T>(SparseSet<TEntity> owners)
    {
        if (Array.IndexOf(_with, owners) >= 0)
        {
            throw Contradicts<T>();
        }

        _without = [.. _without, owners];
        return this;
    }

    // Signals only fire for live entities, so the tag bits can be read by index.
    private void Collect(TEntity e)
    {
        foreach (var pool in _with)
        {
            if (!pool.Has(e))
            {
                return;
            }
        }

        foreach (var pool in _without)
        {
            if (pool.Has(e))
            {
                return;
            }
        }

        foreach (var tags in _withTags)
        {
            if (!tags.Test(e.Index))
            {
                return;
            }
        }

        foreach (var tags in _withoutTags)
        {
            if (tags.Test(e.Index))
            {
                return;
            }
        }

        _set.Collect(e);
    }

    private Pool<TEntity, T> Watch<T>(string? name)
        where T : IComponent
    {
        var pool = Connect<T>(name);
        if (Array.IndexOf(_without, pool) >= 0)
        {
            throw NeverCollects(pool);
        }

        _watched.Add(pool);
        return pool;
    }

    private TagSet<TEntity> WatchTags<T>() where T : struct, ITag
    {
        var tags = ConnectTags<T>();
        if (Array.IndexOf(_withoutTags, tags) >= 0)
        {
            throw NeverCollects<T>();
        }

        _watchedTags.Add(tags);
        return tags;
    }

    private static InvalidOperationException NeverCollects<T>() => NeverCollects(typeof(T).Name);
    private static InvalidOperationException NeverCollects(SparseSet<TEntity> pool) => NeverCollects(Describe(pool));

    private static InvalidOperationException NeverCollects(string what) =>
        new($"Observer both watches and excludes {what}: its signals fire while the entity has it, so they would never collect.");

    private static InvalidOperationException Contradicts<T>() => Contradicts(typeof(T).Name);
    private static InvalidOperationException Contradicts(SparseSet<TEntity> pool) => Contradicts(Describe(pool));

    private static InvalidOperationException Contradicts(string what) =>
        new($"Observer both requires and excludes {what}: no entity can pass, so it would never collect.");

    private static string Describe(SparseSet<TEntity> pool) =>
        pool.Name is { } name ? $"{pool.ComponentType!.Name} \"{name}\"" : pool.ComponentType!.Name;

    private Pool<TEntity, T> Connect<T>(string? name) where T : IComponent =>
        name is null ? Live.Storage<T>() : Live.Storage<T>(name);

    private TagSet<TEntity> ConnectTags<T>() where T : struct, ITag => Live.Tags<T>();

    // Throws once disposed.
    private Registry<TEntity> Live =>
        _registry.Observing(this) ? _registry : throw new ObjectDisposedException(nameof(Observer<>));

    public void Dispose()
    {
        foreach (var disconnect in _disconnect)
        {
            disconnect();
        }

        _disconnect.Clear();
        _with = [];
        _without = [];
        _withTags = [];
        _withoutTags = [];
        _watched.Clear();
        _watchedTags.Clear();
        _set.Clear();
        _registry.Unobserve(this);
    }

    internal void EntityDestroyed(TEntity e) => _set.Remove(e);
}
