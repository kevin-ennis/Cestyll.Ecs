using Cestyll.Ecs.Attachments;
using Cestyll.Ecs.Relations;

namespace Cestyll.Ecs.Views;

// A per-entity test a filter carries beyond its storage.
internal abstract class KeyTest<TEntity>
    where TEntity : unmanaged, IEntity<TEntity>
{
    public abstract bool Exists { get; }
    public abstract bool Has(TEntity e);
}

// Holds a T under this key. Equal on type and key, so Join catches With and Without of one key.
internal sealed class AttachmentKey<TEntity, T, TKey> : KeyTest<TEntity>
    where TEntity : unmanaged, IEntity<TEntity>
    where T : struct, IAttachment<TKey>
    where TKey : notnull
{
    private readonly Registry<TEntity> _registry;
    private readonly TKey _key;

    // T's store, cached once made: stores are never replaced.
    private AttachmentSet<TEntity, T, TKey>? _store;

    public AttachmentKey(Registry<TEntity> registry, TKey key)
    {
        _registry = registry;
        _key = key;
    }

    private AttachmentSet<TEntity, T, TKey>? Store => _store ??= _registry.FindAttachments<T, TKey>();

    public override bool Exists => Store is not null;
    public override bool Has(TEntity e) => Store?.Has(e, _key) ?? false;

    public override bool Equals(object? obj) =>
        obj is AttachmentKey<TEntity, T, TKey> other && other._registry == _registry
        && EqualityComparer<TKey>.Default.Equals(other._key, _key);

    public override int GetHashCode() => HashCode.Combine(typeof(T), _key);
}

// Targets this entity; a view it drives walks the target's sources.
internal abstract class TargetKey<TEntity> : KeyTest<TEntity>
    where TEntity : unmanaged, IEntity<TEntity>
{
    // Live: stale after the store changes.
    public abstract ReadOnlySpan<TEntity> Sources { get; }
}

internal sealed class RelationTargetKey<TEntity, TRelation> : TargetKey<TEntity>
    where TEntity : unmanaged, IEntity<TEntity>
    where TRelation : struct, IRelation
{
    private readonly Registry<TEntity> _registry;
    private readonly TEntity _target;

    // TRelation's store, cached once made: stores are never replaced.
    private RelationSet<TEntity, TRelation>? _store;

    public RelationTargetKey(Registry<TEntity> registry, TEntity target)
    {
        _registry = registry;
        _target = target;
    }

    private RelationSet<TEntity, TRelation>? Store =>
        _store ??= (RelationSet<TEntity, TRelation>?)_registry.FindRelationStore(TypeId<TRelation>.Value);

    public override bool Exists => Store is not null;
    public override bool Has(TEntity e) => Store is { } store && store.GetTarget(e) == _target;

    public override ReadOnlySpan<TEntity> Sources => Store is { } store ? store.SourceSpan(_target) : [];

    public override bool Equals(object? obj) =>
        obj is RelationTargetKey<TEntity, TRelation> other && other._registry == _registry && other._target == _target;

    public override int GetHashCode() => HashCode.Combine(typeof(TRelation), _target);
}

internal sealed class LinkTargetKey<TEntity, T> : TargetKey<TEntity>
    where TEntity : unmanaged, IEntity<TEntity>
    where T : struct, ILink<TEntity>
{
    private readonly Registry<TEntity> _registry;
    private readonly TEntity _target;

    // T's store, cached once made: stores are never replaced.
    private LinkSet<TEntity, T>? _store;

    public LinkTargetKey(Registry<TEntity> registry, TEntity target)
    {
        _registry = registry;
        _target = target;
    }

    private LinkSet<TEntity, T>? Store =>
        _store ??= (LinkSet<TEntity, T>?)_registry.FindLinkStore(TypeId<T>.Value);

    public override bool Exists => Store is not null;
    public override bool Has(TEntity e) => Store is { } store && store.Has(e, _target);

    public override ReadOnlySpan<TEntity> Sources => Store is { } store ? store.SourceSpan(_target) : [];

    public override bool Equals(object? obj) =>
        obj is LinkTargetKey<TEntity, T> other && other._registry == _registry && other._target == _target;

    public override int GetHashCode() => HashCode.Combine(typeof(T), _target);
}
