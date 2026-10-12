using Cestyll.Ecs.Attachments;
using Cestyll.Ecs.Inboxes;
using Cestyll.Ecs.Relations;
using Cestyll.Ecs.Storage;
using Cestyll.Ecs.Tags;

namespace Cestyll.Ecs;

// An entity paired with its registry, for fluent edits.
// A ref struct so it can't be stored and pin the registry; store the entity instead.
public readonly ref struct EntityHandle<TEntity>
    where TEntity : unmanaged, IEntity<TEntity>
{
    public Registry<TEntity> Registry { get; }
    public TEntity Entity { get; }

    internal EntityHandle(Registry<TEntity> registry, TEntity entity)
    {
        Registry = registry;
        Entity = entity;
    }

    public static implicit operator TEntity(EntityHandle<TEntity> h) => h.Entity;

    public bool IsAlive => Registry.IsAlive(Entity);
    public void Destroy() => Registry.Destroy(Entity);

    public EntityHandle<TEntity> Add<T>(T value)
        where T : IComponent
    {
        Registry.Add(Entity, value);
        return this;
    }

    public EntityHandle<TEntity> Replace<T>(T value)
        where T : IComponent
    {
        Registry.Replace(Entity, value);
        return this;
    }

    public EntityHandle<TEntity> AddOrReplace<T>(T value)
        where T : IComponent
    {
        Registry.AddOrReplace(Entity, value);
        return this;
    }

    public EntityHandle<TEntity> Patch<T>(RefAction<T> edit)
        where T : IComponent
    {
        Registry.Patch(Entity, edit);
        return this;
    }

    public EntityHandle<TEntity> Patch<T>()
        where T : IComponent
    {
        Registry.Patch<T>(Entity);
        return this;
    }

    // Use the ref right away.
    public ref T Get<T>() where T : IComponent => ref Registry.Get<T>(Entity);
    public bool Has<T>() where T : IComponent => Registry.Has<T>(Entity);
    public bool Remove<T>() where T : IComponent => Registry.Remove<T>(Entity);

    public bool TryGet<T>(out T value) where T : IComponent => Registry.TryGet(Entity, out value);
    public ref T GetOrAdd<T>(T value) where T : IComponent => ref Registry.GetOrAdd(Entity, value);
    public ref T GetOrAdd<T>() where T : IComponent => ref Registry.GetOrAdd<T>(Entity);

    public bool HasAll<T1, T2>()
        where T1 : IComponent
        where T2 : IComponent =>
        Registry.HasAll<T1, T2>(Entity);
    public bool HasAll<T1, T2, T3>()
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent =>
        Registry.HasAll<T1, T2, T3>(Entity);
    public bool HasAll<T1, T2, T3, T4>()
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent =>
        Registry.HasAll<T1, T2, T3, T4>(Entity);
    public bool HasAll<T1, T2, T3, T4, T5>()
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent =>
        Registry.HasAll<T1, T2, T3, T4, T5>(Entity);
    public bool HasAll<T1, T2, T3, T4, T5, T6>()
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent =>
        Registry.HasAll<T1, T2, T3, T4, T5, T6>(Entity);
    public bool HasAll<T1, T2, T3, T4, T5, T6, T7>()
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent =>
        Registry.HasAll<T1, T2, T3, T4, T5, T6, T7>(Entity);
    public bool HasAll<T1, T2, T3, T4, T5, T6, T7, T8>()
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent =>
        Registry.HasAll<T1, T2, T3, T4, T5, T6, T7, T8>(Entity);
    public bool HasAll<T1, T2, T3, T4, T5, T6, T7, T8, T9>()
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent =>
        Registry.HasAll<T1, T2, T3, T4, T5, T6, T7, T8, T9>(Entity);
    public bool HasAll<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>()
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
        Registry.HasAll<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Entity);
    public bool HasAll<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>()
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
        Registry.HasAll<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(Entity);
    public bool HasAll<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>()
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
        Registry.HasAll<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(Entity);
    public bool HasAll<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>()
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
        Registry.HasAll<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(Entity);
    public bool HasAll<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>()
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
        Registry.HasAll<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(Entity);
    public bool HasAll<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>()
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
        Registry.HasAll<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(Entity);
    public bool HasAll<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>()
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
        Registry.HasAll<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(Entity);

    public bool HasAny<T1, T2>()
        where T1 : IComponent
        where T2 : IComponent =>
        Registry.HasAny<T1, T2>(Entity);
    public bool HasAny<T1, T2, T3>()
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent =>
        Registry.HasAny<T1, T2, T3>(Entity);
    public bool HasAny<T1, T2, T3, T4>()
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent =>
        Registry.HasAny<T1, T2, T3, T4>(Entity);
    public bool HasAny<T1, T2, T3, T4, T5>()
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent =>
        Registry.HasAny<T1, T2, T3, T4, T5>(Entity);
    public bool HasAny<T1, T2, T3, T4, T5, T6>()
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent =>
        Registry.HasAny<T1, T2, T3, T4, T5, T6>(Entity);
    public bool HasAny<T1, T2, T3, T4, T5, T6, T7>()
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent =>
        Registry.HasAny<T1, T2, T3, T4, T5, T6, T7>(Entity);
    public bool HasAny<T1, T2, T3, T4, T5, T6, T7, T8>()
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent =>
        Registry.HasAny<T1, T2, T3, T4, T5, T6, T7, T8>(Entity);
    public bool HasAny<T1, T2, T3, T4, T5, T6, T7, T8, T9>()
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent =>
        Registry.HasAny<T1, T2, T3, T4, T5, T6, T7, T8, T9>(Entity);
    public bool HasAny<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>()
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
        Registry.HasAny<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Entity);
    public bool HasAny<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>()
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
        Registry.HasAny<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(Entity);
    public bool HasAny<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>()
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
        Registry.HasAny<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(Entity);
    public bool HasAny<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>()
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
        Registry.HasAny<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(Entity);
    public bool HasAny<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>()
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
        Registry.HasAny<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(Entity);
    public bool HasAny<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>()
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
        Registry.HasAny<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(Entity);
    public bool HasAny<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>()
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
        Registry.HasAny<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(Entity);

    public bool IsOrphan => Registry.IsOrphan(Entity);
    public Registry<TEntity>.ComponentTypeEnumerator ComponentTypes => Registry.GetComponentTypes(Entity);
    public Registry<TEntity>.TagTypeEnumerator TagTypes => Registry.GetTagTypes(Entity);

    public EntityHandle<TEntity> Tag<T>() where T : struct, ITag
    {
        Registry.Tag<T>(Entity);
        return this;
    }

    public bool HasTag<T>() where T : struct, ITag => Registry.HasTag<T>(Entity);
    public bool Untag<T>() where T : struct, ITag => Registry.Untag<T>(Entity);

    public bool HasAllTags<T1, T2>()
        where T1 : struct, ITag
        where T2 : struct, ITag =>
        Registry.HasAllTags<T1, T2>(Entity);

    public bool HasAllTags<T1, T2, T3>()
        where T1 : struct, ITag
        where T2 : struct, ITag
        where T3 : struct, ITag =>
        Registry.HasAllTags<T1, T2, T3>(Entity);

    public bool HasAllTags<T1, T2, T3, T4>()
        where T1 : struct, ITag
        where T2 : struct, ITag
        where T3 : struct, ITag
        where T4 : struct, ITag =>
        Registry.HasAllTags<T1, T2, T3, T4>(Entity);

    public bool HasAllTags<T1, T2, T3, T4, T5>()
        where T1 : struct, ITag
        where T2 : struct, ITag
        where T3 : struct, ITag
        where T4 : struct, ITag
        where T5 : struct, ITag =>
        Registry.HasAllTags<T1, T2, T3, T4, T5>(Entity);

    public bool HasAllTags<T1, T2, T3, T4, T5, T6>()
        where T1 : struct, ITag
        where T2 : struct, ITag
        where T3 : struct, ITag
        where T4 : struct, ITag
        where T5 : struct, ITag
        where T6 : struct, ITag =>
        Registry.HasAllTags<T1, T2, T3, T4, T5, T6>(Entity);

    public bool HasAllTags<T1, T2, T3, T4, T5, T6, T7>()
        where T1 : struct, ITag
        where T2 : struct, ITag
        where T3 : struct, ITag
        where T4 : struct, ITag
        where T5 : struct, ITag
        where T6 : struct, ITag
        where T7 : struct, ITag =>
        Registry.HasAllTags<T1, T2, T3, T4, T5, T6, T7>(Entity);

    public bool HasAllTags<T1, T2, T3, T4, T5, T6, T7, T8>()
        where T1 : struct, ITag
        where T2 : struct, ITag
        where T3 : struct, ITag
        where T4 : struct, ITag
        where T5 : struct, ITag
        where T6 : struct, ITag
        where T7 : struct, ITag
        where T8 : struct, ITag =>
        Registry.HasAllTags<T1, T2, T3, T4, T5, T6, T7, T8>(Entity);
    public bool HasAllTags<T1, T2, T3, T4, T5, T6, T7, T8, T9>()
        where T1 : struct, ITag
        where T2 : struct, ITag
        where T3 : struct, ITag
        where T4 : struct, ITag
        where T5 : struct, ITag
        where T6 : struct, ITag
        where T7 : struct, ITag
        where T8 : struct, ITag
        where T9 : struct, ITag =>
        Registry.HasAllTags<T1, T2, T3, T4, T5, T6, T7, T8, T9>(Entity);
    public bool HasAllTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>()
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
        Registry.HasAllTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Entity);
    public bool HasAllTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>()
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
        Registry.HasAllTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(Entity);
    public bool HasAllTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>()
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
        Registry.HasAllTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(Entity);
    public bool HasAllTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>()
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
        Registry.HasAllTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(Entity);
    public bool HasAllTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>()
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
        Registry.HasAllTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(Entity);
    public bool HasAllTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>()
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
        Registry.HasAllTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(Entity);
    public bool HasAllTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>()
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
        Registry.HasAllTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(Entity);

    public bool HasAnyTags<T1, T2>()
        where T1 : struct, ITag
        where T2 : struct, ITag =>
        Registry.HasAnyTags<T1, T2>(Entity);

    public bool HasAnyTags<T1, T2, T3>()
        where T1 : struct, ITag
        where T2 : struct, ITag
        where T3 : struct, ITag =>
        Registry.HasAnyTags<T1, T2, T3>(Entity);

    public bool HasAnyTags<T1, T2, T3, T4>()
        where T1 : struct, ITag
        where T2 : struct, ITag
        where T3 : struct, ITag
        where T4 : struct, ITag =>
        Registry.HasAnyTags<T1, T2, T3, T4>(Entity);

    public bool HasAnyTags<T1, T2, T3, T4, T5>()
        where T1 : struct, ITag
        where T2 : struct, ITag
        where T3 : struct, ITag
        where T4 : struct, ITag
        where T5 : struct, ITag =>
        Registry.HasAnyTags<T1, T2, T3, T4, T5>(Entity);

    public bool HasAnyTags<T1, T2, T3, T4, T5, T6>()
        where T1 : struct, ITag
        where T2 : struct, ITag
        where T3 : struct, ITag
        where T4 : struct, ITag
        where T5 : struct, ITag
        where T6 : struct, ITag =>
        Registry.HasAnyTags<T1, T2, T3, T4, T5, T6>(Entity);

    public bool HasAnyTags<T1, T2, T3, T4, T5, T6, T7>()
        where T1 : struct, ITag
        where T2 : struct, ITag
        where T3 : struct, ITag
        where T4 : struct, ITag
        where T5 : struct, ITag
        where T6 : struct, ITag
        where T7 : struct, ITag =>
        Registry.HasAnyTags<T1, T2, T3, T4, T5, T6, T7>(Entity);

    public bool HasAnyTags<T1, T2, T3, T4, T5, T6, T7, T8>()
        where T1 : struct, ITag
        where T2 : struct, ITag
        where T3 : struct, ITag
        where T4 : struct, ITag
        where T5 : struct, ITag
        where T6 : struct, ITag
        where T7 : struct, ITag
        where T8 : struct, ITag =>
        Registry.HasAnyTags<T1, T2, T3, T4, T5, T6, T7, T8>(Entity);
    public bool HasAnyTags<T1, T2, T3, T4, T5, T6, T7, T8, T9>()
        where T1 : struct, ITag
        where T2 : struct, ITag
        where T3 : struct, ITag
        where T4 : struct, ITag
        where T5 : struct, ITag
        where T6 : struct, ITag
        where T7 : struct, ITag
        where T8 : struct, ITag
        where T9 : struct, ITag =>
        Registry.HasAnyTags<T1, T2, T3, T4, T5, T6, T7, T8, T9>(Entity);
    public bool HasAnyTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>()
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
        Registry.HasAnyTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Entity);
    public bool HasAnyTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>()
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
        Registry.HasAnyTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(Entity);
    public bool HasAnyTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>()
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
        Registry.HasAnyTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(Entity);
    public bool HasAnyTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>()
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
        Registry.HasAnyTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(Entity);
    public bool HasAnyTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>()
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
        Registry.HasAnyTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(Entity);
    public bool HasAnyTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>()
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
        Registry.HasAnyTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(Entity);
    public bool HasAnyTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>()
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
        Registry.HasAnyTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(Entity);
    public EntityHandle<TEntity> Relate<TRelation>(TEntity target) where TRelation : struct, IRelation
    {
        Registry.Relate<TRelation>(Entity, target);
        return this;
    }

    public bool Unrelate<TRelation>() where TRelation : struct, IRelation => Registry.Unrelate<TRelation>(Entity);

    public EntityHandle<TEntity> Link<T>(T link)
        where T : struct, ILink<TEntity>
    {
        Registry.Link(Entity, link);
        return this;
    }

    public bool Unlink<T>(TEntity target) where T : struct, ILink<TEntity> => Registry.Unlink<T>(Entity, target);

    public EntityHandle<TEntity> Patch<T>(TEntity target, RefAction<T> edit)
        where T : struct, ILink<TEntity>
    {
        Registry.Patch<T>(Entity, target, edit);
        return this;
    }

    public EntityHandle<TEntity> Patch<T>(TEntity target)
        where T : struct, ILink<TEntity>
    {
        Registry.Patch<T>(Entity, target);
        return this;
    }

    public TEntity GetTarget<TRelation>() where TRelation : struct, IRelation => Registry.GetTarget<TRelation>(Entity);

    public bool TryGetTarget<TRelation>(out TEntity target) where TRelation : struct, IRelation =>
        Registry.TryGetTarget<TRelation>(Entity, out target);

    public int SourceCount<TRelation>() where TRelation : struct, IRelation => Registry.SourceCount<TRelation>(Entity);

    public RelationSet<TEntity, TRelation>.SourceEnumerator GetSources<TRelation>()
        where TRelation : struct, IRelation =>
        Registry.GetSources<TRelation>(Entity);

    // Use the ref right away.
    public ref T GetLink<T>(TEntity target) where T : struct, ILink<TEntity> => ref Registry.GetLink<T>(Entity, target);

    public bool TryGetLink<T>(TEntity target, out T link) where T : struct, ILink<TEntity> =>
        Registry.TryGetLink(Entity, target, out link);

    public int TargetCount<T>() where T : struct, ILink<TEntity> => Registry.TargetCount<T>(Entity);

    public LinkSet<TEntity, T>.TargetEnumerator GetTargets<T>() where T : struct, ILink<TEntity> =>
        Registry.GetTargets<T>(Entity);

    public EntityHandle<TEntity> Attach<T, TKey>(T item)
        where T : struct, IAttachment<TKey>
        where TKey : notnull
    {
        Registry.Attach<T, TKey>(Entity, item);
        return this;
    }

    public EntityHandle<TEntity> AttachOrPatch<T, TKey>(T item, MergeAction<T> merge)
        where T : struct, IAttachment<TKey>
        where TKey : notnull
    {
        Registry.AttachOrPatch<T, TKey>(Entity, item, merge);
        return this;
    }

    public EntityHandle<TEntity> Patch<T, TKey>(TKey key, RefAction<T> edit)
        where T : struct, IAttachment<TKey>
        where TKey : notnull
    {
        Registry.Patch<T, TKey>(Entity, key, edit);
        return this;
    }

    public EntityHandle<TEntity> Patch<T, TKey>(TKey key)
        where T : struct, IAttachment<TKey>
        where TKey : notnull
    {
        Registry.Patch<T, TKey>(Entity, key);
        return this;
    }

    public bool Detach<T, TKey>(TKey key)
        where T : struct, IAttachment<TKey>
        where TKey : notnull =>
        Registry.Detach<T, TKey>(Entity, key);

    public Span<T> GetAttachments<T, TKey>()
        where T : struct, IAttachment<TKey>
        where TKey : notnull =>
        Registry.GetAttachments<T, TKey>(Entity);

    public Span<T> GetAttachments<T>() where T : struct, IAttachment => Registry.GetAttachments<T>(Entity);
    public bool HasAttachments<T>() where T : struct, IAttachment => Registry.HasAttachments<T>(Entity);

    public EntityHandle<TEntity> Post<T>(T message) where T : struct, IMessage
    {
        Registry.Post(Entity, message);
        return this;
    }

    public Span<T> GetMessages<T>() where T : struct, IMessage => Registry.GetMessages<T>(Entity);
    public bool HasMessages<T>() where T : struct, IMessage => Registry.HasMessages<T>(Entity);

    public override string ToString() => $"{Entity}";
}
