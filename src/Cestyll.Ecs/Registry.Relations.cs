using Cestyll.Ecs.Collections;
using Cestyll.Ecs.Relations;

namespace Cestyll.Ecs;

public partial class Registry<TEntity>
    where TEntity : unmanaged, IEntity<TEntity>
{
    // By TypeId; the list (link sets too) is what Destroy's two relation phases walk.
    private TypeSlots<RelationStore<TEntity>> _relations = new();
    private readonly List<RelationStore<TEntity>> _relationList = [];

    public RelationSet<TEntity, TRelation> Relations<TRelation>() where TRelation : struct, IRelation
    {
        ref var slot = ref _relations.Slot(TypeId<TRelation>.Value);
        if (slot is null)
        {
            var store = new RelationSet<TEntity, TRelation>(this);
            slot = store;
            _relationList.Add(store);
            AddStore(store);
        }
        return (RelationSet<TEntity, TRelation>)slot;
    }

    // Never makes the store.
    internal RelationSet<TEntity, TRelation>? FindRelations<TRelation>() where TRelation : struct, IRelation =>
        (RelationSet<TEntity, TRelation>?)_relations.Find(TypeId<TRelation>.Value);

    // Apart from _relations so a type may be both kinds.
    private TypeSlots<RelationStore<TEntity>> _linkSets = new();

    public LinkSet<TEntity, T> Links<T>() where T : struct, ILink<TEntity>
    {
        ref var slot = ref _linkSets.Slot(TypeId<T>.Value);
        if (slot is null)
        {
            var store = new LinkSet<TEntity, T>(this);
            slot = store;
            _relationList.Add(store);
            AddStore(store);
        }
        return (LinkSet<TEntity, T>)slot;
    }

    // Never makes the store.
    internal RelationStore<TEntity>? FindRelationStore(int id) => _relations.Find(id);
    internal RelationStore<TEntity>? FindLinkStore(int id) => _linkSets.Find(id);

    internal LinkSet<TEntity, T>? FindLinks<T>() where T : struct, ILink<TEntity> =>
        (LinkSet<TEntity, T>?)_linkSets.Find(TypeId<T>.Value);

    // Unlike calls on the LinkSet, recorded inside a defer scope.
    public ref T Link<T>(TEntity source, T link)
        where T : struct, ILink<TEntity> =>
        ref _deferDepth > 0 ? ref RecordLink(source, link) : ref Links<T>().Link(source, link);

    public bool Unlink<T>(TEntity source, TEntity target)
        where T : struct, ILink<TEntity> =>
        _deferDepth > 0 ? RecordUnlink<T>(source, target) : Links<T>().Unlink(source, target);

    // Fires OnLinkUpdate; not recorded in a defer scope.
    public void Patch<T>(TEntity source, TEntity target, RefAction<T> edit)
        where T : struct, ILink<TEntity> =>
        RequireLinks<T>(source, target).Patch(source, target, edit);

    public void Patch<T>(TEntity source, TEntity target)
        where T : struct, ILink<TEntity> =>
        RequireLinks<T>(source, target).Patch(source, target);

    // In a defer scope, also sees records for a source born in it.
    public ref T GetLink<T>(TEntity source, TEntity target)
        where T : struct, ILink<TEntity> =>
        ref RequireLinks<T>(source, target).Get(source, target);

    public bool TryGetLink<T>(TEntity source, TEntity target, out T link)
        where T : struct, ILink<TEntity> =>
        Links<T>().TryGet(source, target, out link);

    // Applied links only, even in a defer scope.
    public int TargetCount<T>(TEntity source) where T : struct, ILink<TEntity> => Links<T>().TargetCount(source);

    public LinkSet<TEntity, T>.TargetEnumerator GetTargets<T>(TEntity source)
        where T : struct, ILink<TEntity> =>
        Links<T>().GetTargets(source);

    // Throws as Get would, without making the store.
    private LinkSet<TEntity, T> RequireLinks<T>(TEntity source, TEntity target)
        where T : struct, ILink<TEntity>
    {
        var set = FindLinks<T>();
        if (set is null)
        {
            Throw.NoLink<TEntity, T>(source, target);
        }

        return set;
    }

    // Relating again replaces the source's target.
    public void Relate<TRelation>(TEntity source, TEntity target) where TRelation : struct, IRelation
    {
        if (!CanAttachTo(source))
        {
            Throw.NotAlive(source);
        }

        if (!CanAttachTo(target))
        {
            Throw.NotAlive(target);
        }

        if (_deferDepth > 0)
        {
            Deferred.Relate<TRelation>(source, target);
            return;
        }

        Relations<TRelation>().Relate(source, target);
    }

    // No liveness check: pools are version-checked and destroyed entities are unlinked.
    public bool Unrelate<TRelation>(TEntity source)
        where TRelation : struct, IRelation
    {
        if (_deferDepth > 0)
        {
            var had = !GetTarget<TRelation>(source).IsNull; // before recording, which GetTarget would then see
            Deferred.Unrelate<TRelation>(source);
            return had;
        }

        return Relations<TRelation>().Unrelate(source);
    }

    // In a defer scope, also sees records for a source born in it.
    public TEntity GetTarget<TRelation>(TEntity source)
        where TRelation : struct, IRelation =>
        _bornCount > 0 ? BornTarget<TRelation>(source) : Relations<TRelation>().GetTarget(source);

    // The latest record decides; the relation answers only when nothing is recorded.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private TEntity BornTarget<TRelation>(TEntity source)
        where TRelation : struct, IRelation =>
        _born!.TryGetValue(source, out var since) && Deferred.TryFindPendingTarget<TRelation>(source, since, out var target)
            ? target
            : Relations<TRelation>().GetTarget(source);

    // No liveness check: a link never outlives its target.
    public bool TryGetTarget<TRelation>(TEntity source, out TEntity target)
        where TRelation : struct, IRelation
    {
        target = GetTarget<TRelation>(source);
        return !target.IsNull;
    }

    // Applied links only, even in a defer scope.
    public int SourceCount<TRelation>(TEntity target) where TRelation : struct, IRelation =>
        Relations<TRelation>().SourceCount(target);

    public RelationSet<TEntity, TRelation>.SourceEnumerator GetSources<TRelation>(TEntity target)
        where TRelation : struct, IRelation =>
        Relations<TRelation>().GetSources(target);
}
