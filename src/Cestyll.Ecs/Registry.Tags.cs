using Cestyll.Ecs.Collections;
using Cestyll.Ecs.Tags;

namespace Cestyll.Ecs;

public partial class Registry<TEntity>
    where TEntity : unmanaged, IEntity<TEntity>
{
    private TypeSlots<TagSet<TEntity>> _tags = new(); // by TypeId

    public TagSet<TEntity> Tags<T>() where T : struct, ITag => TagsAt(TypeId<T>.Value, typeof(T));

    private TagSet<TEntity> TagsAt(int id, Type type)
    {
        ref var slot = ref _tags.Slot(id);
        if (slot is not { } tags)
        {
            Markers.RequireOne(type);
            tags = new TagSet<TEntity>(this, type);
            slot = tags;
            AddStore(tags);
        }
        return tags;
    }

    internal TagSet<TEntity>? FindTags<T>() => _tags.Find(TypeId<T>.Value);

    // The bitset is index-only, so check liveness here.
    public bool Tag<T>(TEntity e) where T : struct, ITag
    {
        if (!IsAlive(e))
        {
            Throw.NotAlive(e);
        }

        return _deferDepth > 0 ? RecordTag<T>(e) : Tags<T>().Add(e);
    }

    // Out of line so Tag and Untag stay small.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool RecordTag<T>(TEntity e)
        where T : struct, ITag
    {
        var had = HasTag<T>(e); // before recording, which HasTag would then see
        Deferred.Tag<T>(e);
        return !had;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool RecordUntag<T>(TEntity e)
        where T : struct, ITag
    {
        if (!IsAlive(e))
        {
            return false;
        }

        var had = HasTag<T>(e);
        Deferred.Untag<T>(e);
        return had;
    }

    // In a defer scope, also sees records for an entity born in it.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasTag<T>(TEntity e) where T : struct, ITag =>
        IsAlive(e) && TagBit<T>(e);

    // The latest record wins over the tag set. e is known alive.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool TagBitBorn<T>(TEntity e)
        where T : struct, ITag =>
        _born!.TryGetValue(e, out var since) && Deferred.TryFindPendingTag<T>(e, since, out var tagged)
            ? tagged
            : FindTags<T>()?.Test(e.Index) ?? false;

    public bool Untag<T>(TEntity e)
        where T : struct, ITag
    {
        if (_deferDepth > 0)
        {
            return RecordUntag<T>(e);
        }

        return IsAlive(e) && (FindTags<T>()?.Remove(e) ?? false);
    }

    public bool HasAllTags<T1, T2>(TEntity e)
        where T1 : struct, ITag
        where T2 : struct, ITag =>
        IsAlive(e) && TagBit<T1>(e) && TagBit<T2>(e);

    public bool HasAllTags<T1, T2, T3>(TEntity e)
        where T1 : struct, ITag
        where T2 : struct, ITag
        where T3 : struct, ITag =>
        IsAlive(e) && TagBit<T1>(e) && TagBit<T2>(e) && TagBit<T3>(e);

    public bool HasAllTags<T1, T2, T3, T4>(TEntity e)
        where T1 : struct, ITag
        where T2 : struct, ITag
        where T3 : struct, ITag
        where T4 : struct, ITag =>
        IsAlive(e) && TagBit<T1>(e) && TagBit<T2>(e) && TagBit<T3>(e) && TagBit<T4>(e);

    public bool HasAllTags<T1, T2, T3, T4, T5>(TEntity e)
        where T1 : struct, ITag
        where T2 : struct, ITag
        where T3 : struct, ITag
        where T4 : struct, ITag
        where T5 : struct, ITag =>
        IsAlive(e) && TagBit<T1>(e) && TagBit<T2>(e) && TagBit<T3>(e) && TagBit<T4>(e) && TagBit<T5>(e);

    public bool HasAllTags<T1, T2, T3, T4, T5, T6>(TEntity e)
        where T1 : struct, ITag
        where T2 : struct, ITag
        where T3 : struct, ITag
        where T4 : struct, ITag
        where T5 : struct, ITag
        where T6 : struct, ITag =>
        IsAlive(e) && TagBit<T1>(e) && TagBit<T2>(e) && TagBit<T3>(e) && TagBit<T4>(e) && TagBit<T5>(e) && TagBit<T6>(e);

    public bool HasAllTags<T1, T2, T3, T4, T5, T6, T7>(TEntity e)
        where T1 : struct, ITag
        where T2 : struct, ITag
        where T3 : struct, ITag
        where T4 : struct, ITag
        where T5 : struct, ITag
        where T6 : struct, ITag
        where T7 : struct, ITag =>
        IsAlive(e) && TagBit<T1>(e) && TagBit<T2>(e) && TagBit<T3>(e) && TagBit<T4>(e) && TagBit<T5>(e) && TagBit<T6>(e) && TagBit<T7>(e);

    public bool HasAllTags<T1, T2, T3, T4, T5, T6, T7, T8>(TEntity e)
        where T1 : struct, ITag
        where T2 : struct, ITag
        where T3 : struct, ITag
        where T4 : struct, ITag
        where T5 : struct, ITag
        where T6 : struct, ITag
        where T7 : struct, ITag
        where T8 : struct, ITag =>
        IsAlive(e) && TagBit<T1>(e) && TagBit<T2>(e) && TagBit<T3>(e) && TagBit<T4>(e) && TagBit<T5>(e) && TagBit<T6>(e) && TagBit<T7>(e) && TagBit<T8>(e);

    public bool HasAllTags<T1, T2, T3, T4, T5, T6, T7, T8, T9>(TEntity e)
        where T1 : struct, ITag
        where T2 : struct, ITag
        where T3 : struct, ITag
        where T4 : struct, ITag
        where T5 : struct, ITag
        where T6 : struct, ITag
        where T7 : struct, ITag
        where T8 : struct, ITag
        where T9 : struct, ITag =>
        IsAlive(e) && TagBit<T1>(e) && TagBit<T2>(e) && TagBit<T3>(e) && TagBit<T4>(e) && TagBit<T5>(e) && TagBit<T6>(e) && TagBit<T7>(e) && TagBit<T8>(e) && TagBit<T9>(e);

    public bool HasAllTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(TEntity e)
        where T1 : struct, ITag
        where T2 : struct, ITag
        where T3 : struct, ITag
        where T4 : struct, ITag
        where T5 : struct, ITag
        where T6 : struct, ITag
        where T7 : struct, ITag
        where T8 : struct, ITag
        where T9 : struct, ITag
        where T10 : struct, ITag =>
        IsAlive(e) && TagBit<T1>(e) && TagBit<T2>(e) && TagBit<T3>(e) && TagBit<T4>(e) && TagBit<T5>(e) && TagBit<T6>(e) && TagBit<T7>(e) && TagBit<T8>(e) && TagBit<T9>(e) && TagBit<T10>(e);

    public bool HasAllTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(TEntity e)
        where T1 : struct, ITag
        where T2 : struct, ITag
        where T3 : struct, ITag
        where T4 : struct, ITag
        where T5 : struct, ITag
        where T6 : struct, ITag
        where T7 : struct, ITag
        where T8 : struct, ITag
        where T9 : struct, ITag
        where T10 : struct, ITag
        where T11 : struct, ITag =>
        IsAlive(e) && TagBit<T1>(e) && TagBit<T2>(e) && TagBit<T3>(e) && TagBit<T4>(e) && TagBit<T5>(e) && TagBit<T6>(e) && TagBit<T7>(e) && TagBit<T8>(e) && TagBit<T9>(e) && TagBit<T10>(e) && TagBit<T11>(e);

    public bool HasAllTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(TEntity e)
        where T1 : struct, ITag
        where T2 : struct, ITag
        where T3 : struct, ITag
        where T4 : struct, ITag
        where T5 : struct, ITag
        where T6 : struct, ITag
        where T7 : struct, ITag
        where T8 : struct, ITag
        where T9 : struct, ITag
        where T10 : struct, ITag
        where T11 : struct, ITag
        where T12 : struct, ITag =>
        IsAlive(e) && TagBit<T1>(e) && TagBit<T2>(e) && TagBit<T3>(e) && TagBit<T4>(e) && TagBit<T5>(e) && TagBit<T6>(e) && TagBit<T7>(e) && TagBit<T8>(e) && TagBit<T9>(e) && TagBit<T10>(e) && TagBit<T11>(e) && TagBit<T12>(e);

    public bool HasAllTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(TEntity e)
        where T1 : struct, ITag
        where T2 : struct, ITag
        where T3 : struct, ITag
        where T4 : struct, ITag
        where T5 : struct, ITag
        where T6 : struct, ITag
        where T7 : struct, ITag
        where T8 : struct, ITag
        where T9 : struct, ITag
        where T10 : struct, ITag
        where T11 : struct, ITag
        where T12 : struct, ITag
        where T13 : struct, ITag =>
        IsAlive(e) && TagBit<T1>(e) && TagBit<T2>(e) && TagBit<T3>(e) && TagBit<T4>(e) && TagBit<T5>(e) && TagBit<T6>(e) && TagBit<T7>(e) && TagBit<T8>(e) && TagBit<T9>(e) && TagBit<T10>(e) && TagBit<T11>(e) && TagBit<T12>(e) && TagBit<T13>(e);

    public bool HasAllTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(TEntity e)
        where T1 : struct, ITag
        where T2 : struct, ITag
        where T3 : struct, ITag
        where T4 : struct, ITag
        where T5 : struct, ITag
        where T6 : struct, ITag
        where T7 : struct, ITag
        where T8 : struct, ITag
        where T9 : struct, ITag
        where T10 : struct, ITag
        where T11 : struct, ITag
        where T12 : struct, ITag
        where T13 : struct, ITag
        where T14 : struct, ITag =>
        IsAlive(e) && TagBit<T1>(e) && TagBit<T2>(e) && TagBit<T3>(e) && TagBit<T4>(e) && TagBit<T5>(e) && TagBit<T6>(e) && TagBit<T7>(e) && TagBit<T8>(e) && TagBit<T9>(e) && TagBit<T10>(e) && TagBit<T11>(e) && TagBit<T12>(e) && TagBit<T13>(e) && TagBit<T14>(e);

    public bool HasAllTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(TEntity e)
        where T1 : struct, ITag
        where T2 : struct, ITag
        where T3 : struct, ITag
        where T4 : struct, ITag
        where T5 : struct, ITag
        where T6 : struct, ITag
        where T7 : struct, ITag
        where T8 : struct, ITag
        where T9 : struct, ITag
        where T10 : struct, ITag
        where T11 : struct, ITag
        where T12 : struct, ITag
        where T13 : struct, ITag
        where T14 : struct, ITag
        where T15 : struct, ITag =>
        IsAlive(e) && TagBit<T1>(e) && TagBit<T2>(e) && TagBit<T3>(e) && TagBit<T4>(e) && TagBit<T5>(e) && TagBit<T6>(e) && TagBit<T7>(e) && TagBit<T8>(e) && TagBit<T9>(e) && TagBit<T10>(e) && TagBit<T11>(e) && TagBit<T12>(e) && TagBit<T13>(e) && TagBit<T14>(e) && TagBit<T15>(e);

    public bool HasAllTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(TEntity e)
        where T1 : struct, ITag
        where T2 : struct, ITag
        where T3 : struct, ITag
        where T4 : struct, ITag
        where T5 : struct, ITag
        where T6 : struct, ITag
        where T7 : struct, ITag
        where T8 : struct, ITag
        where T9 : struct, ITag
        where T10 : struct, ITag
        where T11 : struct, ITag
        where T12 : struct, ITag
        where T13 : struct, ITag
        where T14 : struct, ITag
        where T15 : struct, ITag
        where T16 : struct, ITag =>
        IsAlive(e) && TagBit<T1>(e) && TagBit<T2>(e) && TagBit<T3>(e) && TagBit<T4>(e) && TagBit<T5>(e) && TagBit<T6>(e) && TagBit<T7>(e) && TagBit<T8>(e) && TagBit<T9>(e) && TagBit<T10>(e) && TagBit<T11>(e) && TagBit<T12>(e) && TagBit<T13>(e) && TagBit<T14>(e) && TagBit<T15>(e) && TagBit<T16>(e);

    public bool HasAnyTags<T1, T2>(TEntity e)
        where T1 : struct, ITag
        where T2 : struct, ITag =>
        IsAlive(e) && (TagBit<T1>(e) || TagBit<T2>(e));

    public bool HasAnyTags<T1, T2, T3>(TEntity e)
        where T1 : struct, ITag
        where T2 : struct, ITag
        where T3 : struct, ITag =>
        IsAlive(e) && (TagBit<T1>(e) || TagBit<T2>(e) || TagBit<T3>(e));

    public bool HasAnyTags<T1, T2, T3, T4>(TEntity e)
        where T1 : struct, ITag
        where T2 : struct, ITag
        where T3 : struct, ITag
        where T4 : struct, ITag =>
        IsAlive(e) && (TagBit<T1>(e) || TagBit<T2>(e) || TagBit<T3>(e) || TagBit<T4>(e));

    public bool HasAnyTags<T1, T2, T3, T4, T5>(TEntity e)
        where T1 : struct, ITag
        where T2 : struct, ITag
        where T3 : struct, ITag
        where T4 : struct, ITag
        where T5 : struct, ITag =>
        IsAlive(e) && (TagBit<T1>(e) || TagBit<T2>(e) || TagBit<T3>(e) || TagBit<T4>(e) || TagBit<T5>(e));

    public bool HasAnyTags<T1, T2, T3, T4, T5, T6>(TEntity e)
        where T1 : struct, ITag
        where T2 : struct, ITag
        where T3 : struct, ITag
        where T4 : struct, ITag
        where T5 : struct, ITag
        where T6 : struct, ITag =>
        IsAlive(e) && (TagBit<T1>(e) || TagBit<T2>(e) || TagBit<T3>(e) || TagBit<T4>(e) || TagBit<T5>(e) || TagBit<T6>(e));

    public bool HasAnyTags<T1, T2, T3, T4, T5, T6, T7>(TEntity e)
        where T1 : struct, ITag
        where T2 : struct, ITag
        where T3 : struct, ITag
        where T4 : struct, ITag
        where T5 : struct, ITag
        where T6 : struct, ITag
        where T7 : struct, ITag =>
        IsAlive(e) && (TagBit<T1>(e) || TagBit<T2>(e) || TagBit<T3>(e) || TagBit<T4>(e) || TagBit<T5>(e) || TagBit<T6>(e) || TagBit<T7>(e));

    public bool HasAnyTags<T1, T2, T3, T4, T5, T6, T7, T8>(TEntity e)
        where T1 : struct, ITag
        where T2 : struct, ITag
        where T3 : struct, ITag
        where T4 : struct, ITag
        where T5 : struct, ITag
        where T6 : struct, ITag
        where T7 : struct, ITag
        where T8 : struct, ITag =>
        IsAlive(e) && (TagBit<T1>(e) || TagBit<T2>(e) || TagBit<T3>(e) || TagBit<T4>(e) || TagBit<T5>(e) || TagBit<T6>(e) || TagBit<T7>(e) || TagBit<T8>(e));

    public bool HasAnyTags<T1, T2, T3, T4, T5, T6, T7, T8, T9>(TEntity e)
        where T1 : struct, ITag
        where T2 : struct, ITag
        where T3 : struct, ITag
        where T4 : struct, ITag
        where T5 : struct, ITag
        where T6 : struct, ITag
        where T7 : struct, ITag
        where T8 : struct, ITag
        where T9 : struct, ITag =>
        IsAlive(e) && (TagBit<T1>(e) || TagBit<T2>(e) || TagBit<T3>(e) || TagBit<T4>(e) || TagBit<T5>(e) || TagBit<T6>(e) || TagBit<T7>(e) || TagBit<T8>(e) || TagBit<T9>(e));

    public bool HasAnyTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(TEntity e)
        where T1 : struct, ITag
        where T2 : struct, ITag
        where T3 : struct, ITag
        where T4 : struct, ITag
        where T5 : struct, ITag
        where T6 : struct, ITag
        where T7 : struct, ITag
        where T8 : struct, ITag
        where T9 : struct, ITag
        where T10 : struct, ITag =>
        IsAlive(e) && (TagBit<T1>(e) || TagBit<T2>(e) || TagBit<T3>(e) || TagBit<T4>(e) || TagBit<T5>(e) || TagBit<T6>(e) || TagBit<T7>(e) || TagBit<T8>(e) || TagBit<T9>(e) || TagBit<T10>(e));

    public bool HasAnyTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(TEntity e)
        where T1 : struct, ITag
        where T2 : struct, ITag
        where T3 : struct, ITag
        where T4 : struct, ITag
        where T5 : struct, ITag
        where T6 : struct, ITag
        where T7 : struct, ITag
        where T8 : struct, ITag
        where T9 : struct, ITag
        where T10 : struct, ITag
        where T11 : struct, ITag =>
        IsAlive(e) && (TagBit<T1>(e) || TagBit<T2>(e) || TagBit<T3>(e) || TagBit<T4>(e) || TagBit<T5>(e) || TagBit<T6>(e) || TagBit<T7>(e) || TagBit<T8>(e) || TagBit<T9>(e) || TagBit<T10>(e) || TagBit<T11>(e));

    public bool HasAnyTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(TEntity e)
        where T1 : struct, ITag
        where T2 : struct, ITag
        where T3 : struct, ITag
        where T4 : struct, ITag
        where T5 : struct, ITag
        where T6 : struct, ITag
        where T7 : struct, ITag
        where T8 : struct, ITag
        where T9 : struct, ITag
        where T10 : struct, ITag
        where T11 : struct, ITag
        where T12 : struct, ITag =>
        IsAlive(e) && (TagBit<T1>(e) || TagBit<T2>(e) || TagBit<T3>(e) || TagBit<T4>(e) || TagBit<T5>(e) || TagBit<T6>(e) || TagBit<T7>(e) || TagBit<T8>(e) || TagBit<T9>(e) || TagBit<T10>(e) || TagBit<T11>(e) || TagBit<T12>(e));

    public bool HasAnyTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(TEntity e)
        where T1 : struct, ITag
        where T2 : struct, ITag
        where T3 : struct, ITag
        where T4 : struct, ITag
        where T5 : struct, ITag
        where T6 : struct, ITag
        where T7 : struct, ITag
        where T8 : struct, ITag
        where T9 : struct, ITag
        where T10 : struct, ITag
        where T11 : struct, ITag
        where T12 : struct, ITag
        where T13 : struct, ITag =>
        IsAlive(e) && (TagBit<T1>(e) || TagBit<T2>(e) || TagBit<T3>(e) || TagBit<T4>(e) || TagBit<T5>(e) || TagBit<T6>(e) || TagBit<T7>(e) || TagBit<T8>(e) || TagBit<T9>(e) || TagBit<T10>(e) || TagBit<T11>(e) || TagBit<T12>(e) || TagBit<T13>(e));

    public bool HasAnyTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(TEntity e)
        where T1 : struct, ITag
        where T2 : struct, ITag
        where T3 : struct, ITag
        where T4 : struct, ITag
        where T5 : struct, ITag
        where T6 : struct, ITag
        where T7 : struct, ITag
        where T8 : struct, ITag
        where T9 : struct, ITag
        where T10 : struct, ITag
        where T11 : struct, ITag
        where T12 : struct, ITag
        where T13 : struct, ITag
        where T14 : struct, ITag =>
        IsAlive(e) && (TagBit<T1>(e) || TagBit<T2>(e) || TagBit<T3>(e) || TagBit<T4>(e) || TagBit<T5>(e) || TagBit<T6>(e) || TagBit<T7>(e) || TagBit<T8>(e) || TagBit<T9>(e) || TagBit<T10>(e) || TagBit<T11>(e) || TagBit<T12>(e) || TagBit<T13>(e) || TagBit<T14>(e));

    public bool HasAnyTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(TEntity e)
        where T1 : struct, ITag
        where T2 : struct, ITag
        where T3 : struct, ITag
        where T4 : struct, ITag
        where T5 : struct, ITag
        where T6 : struct, ITag
        where T7 : struct, ITag
        where T8 : struct, ITag
        where T9 : struct, ITag
        where T10 : struct, ITag
        where T11 : struct, ITag
        where T12 : struct, ITag
        where T13 : struct, ITag
        where T14 : struct, ITag
        where T15 : struct, ITag =>
        IsAlive(e) && (TagBit<T1>(e) || TagBit<T2>(e) || TagBit<T3>(e) || TagBit<T4>(e) || TagBit<T5>(e) || TagBit<T6>(e) || TagBit<T7>(e) || TagBit<T8>(e) || TagBit<T9>(e) || TagBit<T10>(e) || TagBit<T11>(e) || TagBit<T12>(e) || TagBit<T13>(e) || TagBit<T14>(e) || TagBit<T15>(e));

    public bool HasAnyTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(TEntity e)
        where T1 : struct, ITag
        where T2 : struct, ITag
        where T3 : struct, ITag
        where T4 : struct, ITag
        where T5 : struct, ITag
        where T6 : struct, ITag
        where T7 : struct, ITag
        where T8 : struct, ITag
        where T9 : struct, ITag
        where T10 : struct, ITag
        where T11 : struct, ITag
        where T12 : struct, ITag
        where T13 : struct, ITag
        where T14 : struct, ITag
        where T15 : struct, ITag
        where T16 : struct, ITag =>
        IsAlive(e) && (TagBit<T1>(e) || TagBit<T2>(e) || TagBit<T3>(e) || TagBit<T4>(e) || TagBit<T5>(e) || TagBit<T6>(e) || TagBit<T7>(e) || TagBit<T8>(e) || TagBit<T9>(e) || TagBit<T10>(e) || TagBit<T11>(e) || TagBit<T12>(e) || TagBit<T13>(e) || TagBit<T14>(e) || TagBit<T15>(e) || TagBit<T16>(e));

    // Callers check liveness first.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TagBit<T>(TEntity e)
        where T : struct, ITag =>
        _bornCount > 0 ? TagBitBorn<T>(e) : FindTags<T>()?.Test(e.Index) ?? false;

    // Never creates the tag set, which Destroy would then walk forever.
    public int TagCount<T>() where T : struct, ITag => FindTags<T>()?.Count ?? 0;

    // In tag set creation order. The walk may tag or untag.
    public TagTypeEnumerator GetTagTypes(TEntity e) => new(this, e);

    public ref struct TagTypeEnumerator
    {
        private readonly Registry<TEntity> _registry;
        private readonly TEntity _entity;
        private int _i;
        private TagSet<TEntity>? _set;

        internal TagTypeEnumerator(Registry<TEntity> registry, TEntity entity)
        {
            _registry = registry;
            _entity = entity;
            _i = -1;
            Current = typeof(void);
        }

        public Type Current { get; private set; }
        public readonly TagTypeEnumerator GetEnumerator() => this;

        public bool MoveNext()
        {
            if (!_registry.IsAlive(_entity))
            {
                return false; // tag sets are index-only
            }

            // Tag sets sit last, so a store made mid-walk shifts them: find the current one again.
            var stores = _registry._stores;
            if (_set is not null && ((uint)_i >= (uint)stores.Count || stores[_i] != _set))
            {
                _i = stores.IndexOf(_set);
            }

            while (++_i < stores.Count)
            {
                if (stores[_i] is TagSet<TEntity> set && set.Test(_entity.Index))
                {
                    _set = set;
                    Current = set.TagType;
                    return true;
                }
            }
            return false;
        }
    }
}
