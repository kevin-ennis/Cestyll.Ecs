using Cestyll.Ecs.Attachments;
using Cestyll.Ecs.Groups;
using Cestyll.Ecs.Inboxes;
using Cestyll.Ecs.Relations;
using Cestyll.Ecs.Storage;
using Cestyll.Ecs.Tags;

namespace Cestyll.Ecs.Views;

// Entities passing the filters, with the callback's components. Build once: each step allocates.
public readonly struct View<TEntity>
    where TEntity : unmanaged, IEntity<TEntity>
{
    private readonly ViewQuery<TEntity> _query;
    private readonly SparseSet<TEntity>? _pin;

    // Set by Join: Each checks the halves against the pools it fetches.
    private readonly bool _joined;

    internal View(ViewQuery<TEntity> query, SparseSet<TEntity>? pin = null, bool joined = false)
    {
        _query = query;
        _pin = pin;
        _joined = joined;
    }

    private Registry<TEntity> Registry => _query.Registry;

    public View<TEntity> With<T>() where T : IComponent => new(_query.With<T>(ViewFilterKind.Component), _pin, _joined);

    public View<TEntity> Without<T>() where T : IComponent => new(_query.Without<T>(ViewFilterKind.Component), _pin, _joined);

    public View<TEntity> WithTag<T>() where T : struct, ITag => new(_query.WithTag<T>(live: false), _pin, _joined);

    public View<TEntity> WithoutTag<T>() where T : struct, ITag => new(_query.WithoutTag<T>(live: false), _pin, _joined);

    // Tested at each entity as the walk reaches it, for walks that change the tag.
    public View<TEntity> WithLiveTag<T>() where T : struct, ITag => new(_query.WithTag<T>(live: true), _pin, _joined);

    public View<TEntity> WithoutLiveTag<T>() where T : struct, ITag => new(_query.WithoutTag<T>(live: true), _pin, _joined);

    public View<TEntity> WithAttachment<T>() where T : struct, IAttachment =>
        new(_query.With<T>(ViewFilterKind.Attachment), _pin, _joined);

    public View<TEntity> WithoutAttachment<T>() where T : struct, IAttachment =>
        new(_query.Without<T>(ViewFilterKind.Attachment), _pin, _joined);

    // Holds a T under this key.
    public View<TEntity> WithAttachment<T, TKey>(TKey key)
        where T : struct, IAttachment<TKey>
        where TKey : notnull =>
        new(_query.With<T>(ViewFilterKind.AttachmentKey, new AttachmentKey<TEntity, T, TKey>(Registry, key)), _pin, _joined);

    public View<TEntity> WithoutAttachment<T, TKey>(TKey key)
        where T : struct, IAttachment<TKey>
        where TKey : notnull =>
        new(_query.Without<T>(ViewFilterKind.AttachmentKey, new AttachmentKey<TEntity, T, TKey>(Registry, key)), _pin, _joined);

    public View<TEntity> WithInbox<T>() where T : struct, IMessage => new(_query.With<T>(ViewFilterKind.Inbox), _pin, _joined);

    public View<TEntity> WithoutInbox<T>() where T : struct, IMessage => new(_query.Without<T>(ViewFilterKind.Inbox), _pin, _joined);

    // WithRelationTarget: has a target (a parent). WithRelationSources: is a target (has children).
    public View<TEntity> WithRelationTarget<TRelation>() where TRelation : struct, IRelation =>
        new(_query.With<TRelation>(ViewFilterKind.RelationTargets), _pin, _joined);

    public View<TEntity> WithoutRelationTarget<TRelation>() where TRelation : struct, IRelation =>
        new(_query.Without<TRelation>(ViewFilterKind.RelationTargets), _pin, _joined);

    // Targets this entity. The first one walks the target's sources unless a pin or group drives.
    public View<TEntity> WithRelationTarget<TRelation>(TEntity target) where TRelation : struct, IRelation =>
        new(_query.WithTargetKey<TRelation>(ViewFilterKind.RelationTarget, new RelationTargetKey<TEntity, TRelation>(Registry, target)),
            _pin, _joined);

    public View<TEntity> WithoutRelationTarget<TRelation>(TEntity target) where TRelation : struct, IRelation =>
        new(_query.Without<TRelation>(ViewFilterKind.RelationTarget, new RelationTargetKey<TEntity, TRelation>(Registry, target)),
            _pin, _joined);

    public View<TEntity> WithRelationSources<TRelation>() where TRelation : struct, IRelation =>
        new(_query.With<TRelation>(ViewFilterKind.RelationSources), _pin, _joined);

    public View<TEntity> WithoutRelationSources<TRelation>() where TRelation : struct, IRelation =>
        new(_query.Without<TRelation>(ViewFilterKind.RelationSources), _pin, _joined);

    public View<TEntity> WithLinkTargets<T>() where T : struct, ILink<TEntity> =>
        new(_query.With<T>(ViewFilterKind.LinkTargets), _pin, _joined);

    public View<TEntity> WithoutLinkTargets<T>() where T : struct, ILink<TEntity> =>
        new(_query.Without<T>(ViewFilterKind.LinkTargets), _pin, _joined);

    // Links to this entity. Drives like WithRelationTarget(target), from a copy of the target's sources.
    public View<TEntity> WithLinkTarget<T>(TEntity target) where T : struct, ILink<TEntity> =>
        new(_query.WithTargetKey<T>(ViewFilterKind.LinkTarget, new LinkTargetKey<TEntity, T>(Registry, target)), _pin, _joined);

    public View<TEntity> WithoutLinkTarget<T>(TEntity target) where T : struct, ILink<TEntity> =>
        new(_query.Without<T>(ViewFilterKind.LinkTarget, new LinkTargetKey<TEntity, T>(Registry, target)), _pin, _joined);

    public View<TEntity> WithLinkSources<T>() where T : struct, ILink<TEntity> =>
        new(_query.With<T>(ViewFilterKind.LinkSources), _pin, _joined);

    public View<TEntity> WithoutLinkSources<T>() where T : struct, ILink<TEntity> =>
        new(_query.Without<T>(ViewFilterKind.LinkSources), _pin, _joined);

    // Naming a pool creates it.
    public View<TEntity> With<T>(string name) where T : IComponent =>
        new(_query.With(Registry.Storage<T>(Names.Checked(name))), _pin, _joined);

    public View<TEntity> Without<T>(string name) where T : IComponent =>
        new(_query.Without(Registry.Storage<T>(Names.Checked(name))), _pin, _joined);

    // Drive from T's default pool; the callback must fetch it.
    public View<TEntity> Use<T>() where T : IComponent => new(_query.Unpinned(), Registry.Storage<T>(), _joined);

    // Drive from this named pool; the callback must fetch it.
    public View<TEntity> Use<T>(string name) where T : IComponent =>
        new(_query.Unpinned(), Registry.Storage<T>(Names.Checked(name)), _joined);

    public View<TEntity> UseTag<T>() where T : struct, ITag => new(_query.UseTag<T>(), null, _joined);

    // Drive from a non-owning group's members.
    public View<TEntity> Use(Group<TEntity> group) =>
        new(_query.Use(group.Members
            ?? throw new InvalidOperationException("Use takes a group that owns nothing: walk an owning group with its Each")),
            null, _joined);

    // Both views' filters, no pin.
    public View<TEntity> Join(View<TEntity> other) => new(_query.Join(other._query), null, joined: true);

    private ViewQuery<TEntity> Pinned(scoped ReadOnlySpan<SparseSet<TEntity>> pools)
    {
        if (_joined)
        {
            _query.CheckJoin(pools);
        }

        return _pin is null ? _query : _query.Use(_pin, pools);
    }

    // The struct names its types, so no type arguments.
    public void Each<TFn>(ref TFn fn) where TFn : struct, IEach<TFn, TEntity> => TFn.Run(in this, ref fn);

    // Entities alone, driven by the pin, the group, the smallest required store, else the tag mask.
    public ViewRows<TEntity> Rows()
    {
        if (_pin is null && !_query.HasDriver)
        {
            throw new InvalidOperationException(
                "Rows() needs something to walk: a With filter other than a live tag or attachment key, a pin, or Use(group)");
        }

        return new(_pin is null ? _query : _query.Use(_pin, [_pin]), _pin, _joined);
    }

    public ViewRows<TEntity, T1> Rows<T1>()
        where T1 : IComponent
        => Rows(Registry.Storage<T1>());

    // Over these pools, e.g. named ones.
    public ViewRows<TEntity, T1> Rows<T1>(Pool<TEntity, T1> p1)
        where T1 : IComponent
    {
        Registry.CheckOwns(p1);
        return new(Pinned([p1]), p1);
    }

    public void Each<T1>(RefAction<TEntity, T1> action)
        where T1 : IComponent =>
        Rows<T1>().Each(action);

    public void Each<TFn, T1>(ref TFn fn)
        where TFn : struct, IEach<TFn, TEntity, T1>
        where T1 : IComponent =>
        Rows<T1>().Each(ref fn);

    public ViewRows<TEntity, T1, T2> Rows<T1, T2>()
        where T1 : IComponent
        where T2 : IComponent
        => Rows(Registry.Storage<T1>(), Registry.Storage<T2>());

    // Over these pools, e.g. named ones.
    public ViewRows<TEntity, T1, T2> Rows<T1, T2>(Pool<TEntity, T1> p1, Pool<TEntity, T2> p2)
        where T1 : IComponent
        where T2 : IComponent
    {
        Registry.CheckOwns(p1);
        Registry.CheckOwns(p2);
        return new(Pinned([p1, p2]), p1, p2);
    }

    public void Each<T1, T2>(RefAction<TEntity, T1, T2> action)
        where T1 : IComponent
        where T2 : IComponent =>
        Rows<T1, T2>().Each(action);

    public void Each<TFn, T1, T2>(ref TFn fn)
        where TFn : struct, IEach<TFn, TEntity, T1, T2>
        where T1 : IComponent
        where T2 : IComponent =>
        Rows<T1, T2>().Each(ref fn);

    public ViewRows<TEntity, T1, T2, T3> Rows<T1, T2, T3>()
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        => Rows(Registry.Storage<T1>(), Registry.Storage<T2>(), Registry.Storage<T3>());

    // Over these pools, e.g. named ones.
    public ViewRows<TEntity, T1, T2, T3> Rows<T1, T2, T3>(Pool<TEntity, T1> p1, Pool<TEntity, T2> p2, Pool<TEntity, T3> p3)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
    {
        Registry.CheckOwns(p1);
        Registry.CheckOwns(p2);
        Registry.CheckOwns(p3);
        return new(Pinned([p1, p2, p3]), p1, p2, p3);
    }

    public void Each<T1, T2, T3>(RefAction<TEntity, T1, T2, T3> action)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent =>
        Rows<T1, T2, T3>().Each(action);

    public void Each<TFn, T1, T2, T3>(ref TFn fn)
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3>
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent =>
        Rows<T1, T2, T3>().Each(ref fn);

    public ViewRows<TEntity, T1, T2, T3, T4> Rows<T1, T2, T3, T4>()
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        => Rows(Registry.Storage<T1>(), Registry.Storage<T2>(), Registry.Storage<T3>(), Registry.Storage<T4>());

    // Over these pools, e.g. named ones.
    public ViewRows<TEntity, T1, T2, T3, T4> Rows<T1, T2, T3, T4>(Pool<TEntity, T1> p1, Pool<TEntity, T2> p2, Pool<TEntity, T3> p3, Pool<TEntity, T4> p4)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
    {
        Registry.CheckOwns(p1);
        Registry.CheckOwns(p2);
        Registry.CheckOwns(p3);
        Registry.CheckOwns(p4);
        return new(Pinned([p1, p2, p3, p4]), p1, p2, p3, p4);
    }

    public void Each<T1, T2, T3, T4>(RefAction<TEntity, T1, T2, T3, T4> action)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent =>
        Rows<T1, T2, T3, T4>().Each(action);

    public void Each<TFn, T1, T2, T3, T4>(ref TFn fn)
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4>
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent =>
        Rows<T1, T2, T3, T4>().Each(ref fn);

    public ViewRows<TEntity, T1, T2, T3, T4, T5> Rows<T1, T2, T3, T4, T5>()
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        => Rows(Registry.Storage<T1>(), Registry.Storage<T2>(), Registry.Storage<T3>(), Registry.Storage<T4>(), Registry.Storage<T5>());

    // Over these pools, e.g. named ones.
    public ViewRows<TEntity, T1, T2, T3, T4, T5> Rows<T1, T2, T3, T4, T5>(Pool<TEntity, T1> p1, Pool<TEntity, T2> p2, Pool<TEntity, T3> p3, Pool<TEntity, T4> p4, Pool<TEntity, T5> p5)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
    {
        Registry.CheckOwns(p1);
        Registry.CheckOwns(p2);
        Registry.CheckOwns(p3);
        Registry.CheckOwns(p4);
        Registry.CheckOwns(p5);
        return new(Pinned([p1, p2, p3, p4, p5]), p1, p2, p3, p4, p5);
    }

    public void Each<T1, T2, T3, T4, T5>(RefAction<TEntity, T1, T2, T3, T4, T5> action)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent =>
        Rows<T1, T2, T3, T4, T5>().Each(action);

    public void Each<TFn, T1, T2, T3, T4, T5>(ref TFn fn)
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5>
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent =>
        Rows<T1, T2, T3, T4, T5>().Each(ref fn);

    public ViewRows<TEntity, T1, T2, T3, T4, T5, T6> Rows<T1, T2, T3, T4, T5, T6>()
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        => Rows(Registry.Storage<T1>(), Registry.Storage<T2>(), Registry.Storage<T3>(), Registry.Storage<T4>(), Registry.Storage<T5>(), Registry.Storage<T6>());

    // Over these pools, e.g. named ones.
    public ViewRows<TEntity, T1, T2, T3, T4, T5, T6> Rows<T1, T2, T3, T4, T5, T6>(Pool<TEntity, T1> p1, Pool<TEntity, T2> p2, Pool<TEntity, T3> p3, Pool<TEntity, T4> p4, Pool<TEntity, T5> p5, Pool<TEntity, T6> p6)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
    {
        Registry.CheckOwns(p1);
        Registry.CheckOwns(p2);
        Registry.CheckOwns(p3);
        Registry.CheckOwns(p4);
        Registry.CheckOwns(p5);
        Registry.CheckOwns(p6);
        return new(Pinned([p1, p2, p3, p4, p5, p6]), p1, p2, p3, p4, p5, p6);
    }

    public void Each<T1, T2, T3, T4, T5, T6>(RefAction<TEntity, T1, T2, T3, T4, T5, T6> action)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent =>
        Rows<T1, T2, T3, T4, T5, T6>().Each(action);

    public void Each<TFn, T1, T2, T3, T4, T5, T6>(ref TFn fn)
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6>
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent =>
        Rows<T1, T2, T3, T4, T5, T6>().Each(ref fn);

    public ViewRows<TEntity, T1, T2, T3, T4, T5, T6, T7> Rows<T1, T2, T3, T4, T5, T6, T7>()
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        => Rows(Registry.Storage<T1>(), Registry.Storage<T2>(), Registry.Storage<T3>(), Registry.Storage<T4>(), Registry.Storage<T5>(), Registry.Storage<T6>(), Registry.Storage<T7>());

    // Over these pools, e.g. named ones.
    public ViewRows<TEntity, T1, T2, T3, T4, T5, T6, T7> Rows<T1, T2, T3, T4, T5, T6, T7>(Pool<TEntity, T1> p1, Pool<TEntity, T2> p2, Pool<TEntity, T3> p3, Pool<TEntity, T4> p4, Pool<TEntity, T5> p5, Pool<TEntity, T6> p6, Pool<TEntity, T7> p7)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
    {
        Registry.CheckOwns(p1);
        Registry.CheckOwns(p2);
        Registry.CheckOwns(p3);
        Registry.CheckOwns(p4);
        Registry.CheckOwns(p5);
        Registry.CheckOwns(p6);
        Registry.CheckOwns(p7);
        return new(Pinned([p1, p2, p3, p4, p5, p6, p7]), p1, p2, p3, p4, p5, p6, p7);
    }

    public void Each<T1, T2, T3, T4, T5, T6, T7>(RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7> action)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent =>
        Rows<T1, T2, T3, T4, T5, T6, T7>().Each(action);

    public void Each<TFn, T1, T2, T3, T4, T5, T6, T7>(ref TFn fn)
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7>
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent =>
        Rows<T1, T2, T3, T4, T5, T6, T7>().Each(ref fn);

    public ViewRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8> Rows<T1, T2, T3, T4, T5, T6, T7, T8>()
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        => Rows(Registry.Storage<T1>(), Registry.Storage<T2>(), Registry.Storage<T3>(), Registry.Storage<T4>(), Registry.Storage<T5>(), Registry.Storage<T6>(), Registry.Storage<T7>(), Registry.Storage<T8>());

    // Over these pools, e.g. named ones.
    public ViewRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8> Rows<T1, T2, T3, T4, T5, T6, T7, T8>(Pool<TEntity, T1> p1, Pool<TEntity, T2> p2, Pool<TEntity, T3> p3, Pool<TEntity, T4> p4, Pool<TEntity, T5> p5, Pool<TEntity, T6> p6, Pool<TEntity, T7> p7, Pool<TEntity, T8> p8)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
    {
        Registry.CheckOwns(p1);
        Registry.CheckOwns(p2);
        Registry.CheckOwns(p3);
        Registry.CheckOwns(p4);
        Registry.CheckOwns(p5);
        Registry.CheckOwns(p6);
        Registry.CheckOwns(p7);
        Registry.CheckOwns(p8);
        return new(Pinned([p1, p2, p3, p4, p5, p6, p7, p8]), p1, p2, p3, p4, p5, p6, p7, p8);
    }

    public void Each<T1, T2, T3, T4, T5, T6, T7, T8>(RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7, T8> action)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent =>
        Rows<T1, T2, T3, T4, T5, T6, T7, T8>().Each(action);

    public void Each<TFn, T1, T2, T3, T4, T5, T6, T7, T8>(ref TFn fn)
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7, T8>
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent =>
        Rows<T1, T2, T3, T4, T5, T6, T7, T8>().Each(ref fn);

    public ViewRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9> Rows<T1, T2, T3, T4, T5, T6, T7, T8, T9>()
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        => Rows(Registry.Storage<T1>(), Registry.Storage<T2>(), Registry.Storage<T3>(), Registry.Storage<T4>(), Registry.Storage<T5>(), Registry.Storage<T6>(), Registry.Storage<T7>(), Registry.Storage<T8>(), Registry.Storage<T9>());

    // Over these pools, e.g. named ones.
    public ViewRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9> Rows<T1, T2, T3, T4, T5, T6, T7, T8, T9>(Pool<TEntity, T1> p1, Pool<TEntity, T2> p2, Pool<TEntity, T3> p3, Pool<TEntity, T4> p4, Pool<TEntity, T5> p5, Pool<TEntity, T6> p6, Pool<TEntity, T7> p7, Pool<TEntity, T8> p8, Pool<TEntity, T9> p9)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
    {
        Registry.CheckOwns(p1);
        Registry.CheckOwns(p2);
        Registry.CheckOwns(p3);
        Registry.CheckOwns(p4);
        Registry.CheckOwns(p5);
        Registry.CheckOwns(p6);
        Registry.CheckOwns(p7);
        Registry.CheckOwns(p8);
        Registry.CheckOwns(p9);
        return new(Pinned([p1, p2, p3, p4, p5, p6, p7, p8, p9]), p1, p2, p3, p4, p5, p6, p7, p8, p9);
    }

    public void Each<T1, T2, T3, T4, T5, T6, T7, T8, T9>(RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9> action)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent =>
        Rows<T1, T2, T3, T4, T5, T6, T7, T8, T9>().Each(action);

    public void Each<TFn, T1, T2, T3, T4, T5, T6, T7, T8, T9>(ref TFn fn)
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9>
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent =>
        Rows<T1, T2, T3, T4, T5, T6, T7, T8, T9>().Each(ref fn);

    public ViewRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Rows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>()
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        => Rows(Registry.Storage<T1>(), Registry.Storage<T2>(), Registry.Storage<T3>(), Registry.Storage<T4>(), Registry.Storage<T5>(), Registry.Storage<T6>(), Registry.Storage<T7>(), Registry.Storage<T8>(), Registry.Storage<T9>(), Registry.Storage<T10>());

    // Over these pools, e.g. named ones.
    public ViewRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Rows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Pool<TEntity, T1> p1, Pool<TEntity, T2> p2, Pool<TEntity, T3> p3, Pool<TEntity, T4> p4, Pool<TEntity, T5> p5, Pool<TEntity, T6> p6, Pool<TEntity, T7> p7, Pool<TEntity, T8> p8, Pool<TEntity, T9> p9, Pool<TEntity, T10> p10)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
    {
        Registry.CheckOwns(p1);
        Registry.CheckOwns(p2);
        Registry.CheckOwns(p3);
        Registry.CheckOwns(p4);
        Registry.CheckOwns(p5);
        Registry.CheckOwns(p6);
        Registry.CheckOwns(p7);
        Registry.CheckOwns(p8);
        Registry.CheckOwns(p9);
        Registry.CheckOwns(p10);
        return new(Pinned([p1, p2, p3, p4, p5, p6, p7, p8, p9, p10]), p1, p2, p3, p4, p5, p6, p7, p8, p9, p10);
    }

    public void Each<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> action)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent =>
        Rows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>().Each(action);

    public void Each<TFn, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(ref TFn fn)
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent =>
        Rows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>().Each(ref fn);

    public ViewRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> Rows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>()
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        => Rows(Registry.Storage<T1>(), Registry.Storage<T2>(), Registry.Storage<T3>(), Registry.Storage<T4>(), Registry.Storage<T5>(), Registry.Storage<T6>(), Registry.Storage<T7>(), Registry.Storage<T8>(), Registry.Storage<T9>(), Registry.Storage<T10>(), Registry.Storage<T11>());

    // Over these pools, e.g. named ones.
    public ViewRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> Rows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(Pool<TEntity, T1> p1, Pool<TEntity, T2> p2, Pool<TEntity, T3> p3, Pool<TEntity, T4> p4, Pool<TEntity, T5> p5, Pool<TEntity, T6> p6, Pool<TEntity, T7> p7, Pool<TEntity, T8> p8, Pool<TEntity, T9> p9, Pool<TEntity, T10> p10, Pool<TEntity, T11> p11)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
    {
        Registry.CheckOwns(p1);
        Registry.CheckOwns(p2);
        Registry.CheckOwns(p3);
        Registry.CheckOwns(p4);
        Registry.CheckOwns(p5);
        Registry.CheckOwns(p6);
        Registry.CheckOwns(p7);
        Registry.CheckOwns(p8);
        Registry.CheckOwns(p9);
        Registry.CheckOwns(p10);
        Registry.CheckOwns(p11);
        return new(Pinned([p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11]), p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11);
    }

    public void Each<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> action)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent =>
        Rows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>().Each(action);

    public void Each<TFn, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(ref TFn fn)
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent =>
        Rows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>().Each(ref fn);

    public ViewRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> Rows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>()
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent
        => Rows(Registry.Storage<T1>(), Registry.Storage<T2>(), Registry.Storage<T3>(), Registry.Storage<T4>(), Registry.Storage<T5>(), Registry.Storage<T6>(), Registry.Storage<T7>(), Registry.Storage<T8>(), Registry.Storage<T9>(), Registry.Storage<T10>(), Registry.Storage<T11>(), Registry.Storage<T12>());

    // Over these pools, e.g. named ones.
    public ViewRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> Rows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(Pool<TEntity, T1> p1, Pool<TEntity, T2> p2, Pool<TEntity, T3> p3, Pool<TEntity, T4> p4, Pool<TEntity, T5> p5, Pool<TEntity, T6> p6, Pool<TEntity, T7> p7, Pool<TEntity, T8> p8, Pool<TEntity, T9> p9, Pool<TEntity, T10> p10, Pool<TEntity, T11> p11, Pool<TEntity, T12> p12)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent
    {
        Registry.CheckOwns(p1);
        Registry.CheckOwns(p2);
        Registry.CheckOwns(p3);
        Registry.CheckOwns(p4);
        Registry.CheckOwns(p5);
        Registry.CheckOwns(p6);
        Registry.CheckOwns(p7);
        Registry.CheckOwns(p8);
        Registry.CheckOwns(p9);
        Registry.CheckOwns(p10);
        Registry.CheckOwns(p11);
        Registry.CheckOwns(p12);
        return new(Pinned([p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12]), p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12);
    }

    public void Each<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> action)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent =>
        Rows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>().Each(action);

    public void Each<TFn, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(ref TFn fn)
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent =>
        Rows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>().Each(ref fn);

    public ViewRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> Rows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>()
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent
        where T13 : IComponent
        => Rows(Registry.Storage<T1>(), Registry.Storage<T2>(), Registry.Storage<T3>(), Registry.Storage<T4>(), Registry.Storage<T5>(), Registry.Storage<T6>(), Registry.Storage<T7>(), Registry.Storage<T8>(), Registry.Storage<T9>(), Registry.Storage<T10>(), Registry.Storage<T11>(), Registry.Storage<T12>(), Registry.Storage<T13>());

    // Over these pools, e.g. named ones.
    public ViewRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> Rows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(Pool<TEntity, T1> p1, Pool<TEntity, T2> p2, Pool<TEntity, T3> p3, Pool<TEntity, T4> p4, Pool<TEntity, T5> p5, Pool<TEntity, T6> p6, Pool<TEntity, T7> p7, Pool<TEntity, T8> p8, Pool<TEntity, T9> p9, Pool<TEntity, T10> p10, Pool<TEntity, T11> p11, Pool<TEntity, T12> p12, Pool<TEntity, T13> p13)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent
        where T13 : IComponent
    {
        Registry.CheckOwns(p1);
        Registry.CheckOwns(p2);
        Registry.CheckOwns(p3);
        Registry.CheckOwns(p4);
        Registry.CheckOwns(p5);
        Registry.CheckOwns(p6);
        Registry.CheckOwns(p7);
        Registry.CheckOwns(p8);
        Registry.CheckOwns(p9);
        Registry.CheckOwns(p10);
        Registry.CheckOwns(p11);
        Registry.CheckOwns(p12);
        Registry.CheckOwns(p13);
        return new(Pinned([p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13]), p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13);
    }

    public void Each<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> action)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent
        where T13 : IComponent =>
        Rows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>().Each(action);

    public void Each<TFn, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(ref TFn fn)
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent
        where T13 : IComponent =>
        Rows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>().Each(ref fn);

    public ViewRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> Rows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>()
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent
        where T13 : IComponent
        where T14 : IComponent
        => Rows(Registry.Storage<T1>(), Registry.Storage<T2>(), Registry.Storage<T3>(), Registry.Storage<T4>(), Registry.Storage<T5>(), Registry.Storage<T6>(), Registry.Storage<T7>(), Registry.Storage<T8>(), Registry.Storage<T9>(), Registry.Storage<T10>(), Registry.Storage<T11>(), Registry.Storage<T12>(), Registry.Storage<T13>(), Registry.Storage<T14>());

    // Over these pools, e.g. named ones.
    public ViewRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> Rows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(Pool<TEntity, T1> p1, Pool<TEntity, T2> p2, Pool<TEntity, T3> p3, Pool<TEntity, T4> p4, Pool<TEntity, T5> p5, Pool<TEntity, T6> p6, Pool<TEntity, T7> p7, Pool<TEntity, T8> p8, Pool<TEntity, T9> p9, Pool<TEntity, T10> p10, Pool<TEntity, T11> p11, Pool<TEntity, T12> p12, Pool<TEntity, T13> p13, Pool<TEntity, T14> p14)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent
        where T13 : IComponent
        where T14 : IComponent
    {
        Registry.CheckOwns(p1);
        Registry.CheckOwns(p2);
        Registry.CheckOwns(p3);
        Registry.CheckOwns(p4);
        Registry.CheckOwns(p5);
        Registry.CheckOwns(p6);
        Registry.CheckOwns(p7);
        Registry.CheckOwns(p8);
        Registry.CheckOwns(p9);
        Registry.CheckOwns(p10);
        Registry.CheckOwns(p11);
        Registry.CheckOwns(p12);
        Registry.CheckOwns(p13);
        Registry.CheckOwns(p14);
        return new(Pinned([p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14]), p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14);
    }

    public void Each<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> action)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent
        where T13 : IComponent
        where T14 : IComponent =>
        Rows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>().Each(action);

    public void Each<TFn, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(ref TFn fn)
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent
        where T13 : IComponent
        where T14 : IComponent =>
        Rows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>().Each(ref fn);

    public ViewRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> Rows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>()
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent
        where T13 : IComponent
        where T14 : IComponent
        where T15 : IComponent
        => Rows(Registry.Storage<T1>(), Registry.Storage<T2>(), Registry.Storage<T3>(), Registry.Storage<T4>(), Registry.Storage<T5>(), Registry.Storage<T6>(), Registry.Storage<T7>(), Registry.Storage<T8>(), Registry.Storage<T9>(), Registry.Storage<T10>(), Registry.Storage<T11>(), Registry.Storage<T12>(), Registry.Storage<T13>(), Registry.Storage<T14>(), Registry.Storage<T15>());

    // Over these pools, e.g. named ones.
    public ViewRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> Rows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(Pool<TEntity, T1> p1, Pool<TEntity, T2> p2, Pool<TEntity, T3> p3, Pool<TEntity, T4> p4, Pool<TEntity, T5> p5, Pool<TEntity, T6> p6, Pool<TEntity, T7> p7, Pool<TEntity, T8> p8, Pool<TEntity, T9> p9, Pool<TEntity, T10> p10, Pool<TEntity, T11> p11, Pool<TEntity, T12> p12, Pool<TEntity, T13> p13, Pool<TEntity, T14> p14, Pool<TEntity, T15> p15)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent
        where T13 : IComponent
        where T14 : IComponent
        where T15 : IComponent
    {
        Registry.CheckOwns(p1);
        Registry.CheckOwns(p2);
        Registry.CheckOwns(p3);
        Registry.CheckOwns(p4);
        Registry.CheckOwns(p5);
        Registry.CheckOwns(p6);
        Registry.CheckOwns(p7);
        Registry.CheckOwns(p8);
        Registry.CheckOwns(p9);
        Registry.CheckOwns(p10);
        Registry.CheckOwns(p11);
        Registry.CheckOwns(p12);
        Registry.CheckOwns(p13);
        Registry.CheckOwns(p14);
        Registry.CheckOwns(p15);
        return new(Pinned([p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14, p15]), p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14, p15);
    }

    public void Each<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> action)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent
        where T13 : IComponent
        where T14 : IComponent
        where T15 : IComponent =>
        Rows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>().Each(action);

    public void Each<TFn, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(ref TFn fn)
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent
        where T13 : IComponent
        where T14 : IComponent
        where T15 : IComponent =>
        Rows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>().Each(ref fn);

    public ViewRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> Rows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>()
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent
        where T13 : IComponent
        where T14 : IComponent
        where T15 : IComponent
        where T16 : IComponent
        => Rows(Registry.Storage<T1>(), Registry.Storage<T2>(), Registry.Storage<T3>(), Registry.Storage<T4>(), Registry.Storage<T5>(), Registry.Storage<T6>(), Registry.Storage<T7>(), Registry.Storage<T8>(), Registry.Storage<T9>(), Registry.Storage<T10>(), Registry.Storage<T11>(), Registry.Storage<T12>(), Registry.Storage<T13>(), Registry.Storage<T14>(), Registry.Storage<T15>(), Registry.Storage<T16>());

    // Over these pools, e.g. named ones.
    public ViewRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> Rows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(Pool<TEntity, T1> p1, Pool<TEntity, T2> p2, Pool<TEntity, T3> p3, Pool<TEntity, T4> p4, Pool<TEntity, T5> p5, Pool<TEntity, T6> p6, Pool<TEntity, T7> p7, Pool<TEntity, T8> p8, Pool<TEntity, T9> p9, Pool<TEntity, T10> p10, Pool<TEntity, T11> p11, Pool<TEntity, T12> p12, Pool<TEntity, T13> p13, Pool<TEntity, T14> p14, Pool<TEntity, T15> p15, Pool<TEntity, T16> p16)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent
        where T13 : IComponent
        where T14 : IComponent
        where T15 : IComponent
        where T16 : IComponent
    {
        Registry.CheckOwns(p1);
        Registry.CheckOwns(p2);
        Registry.CheckOwns(p3);
        Registry.CheckOwns(p4);
        Registry.CheckOwns(p5);
        Registry.CheckOwns(p6);
        Registry.CheckOwns(p7);
        Registry.CheckOwns(p8);
        Registry.CheckOwns(p9);
        Registry.CheckOwns(p10);
        Registry.CheckOwns(p11);
        Registry.CheckOwns(p12);
        Registry.CheckOwns(p13);
        Registry.CheckOwns(p14);
        Registry.CheckOwns(p15);
        Registry.CheckOwns(p16);
        return new(Pinned([p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14, p15, p16]), p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14, p15, p16);
    }

    public void Each<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> action)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent
        where T13 : IComponent
        where T14 : IComponent
        where T15 : IComponent
        where T16 : IComponent =>
        Rows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>().Each(action);

    public void Each<TFn, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(ref TFn fn)
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent
        where T13 : IComponent
        where T14 : IComponent
        where T15 : IComponent
        where T16 : IComponent =>
        Rows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>().Each(ref fn);
}
