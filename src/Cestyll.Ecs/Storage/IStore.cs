namespace Cestyll.Ecs.Storage;

internal enum StoreKind : byte { Component, Tag, Relation, Links, Attachments, Inbox }

// Anything the Registry walks per entity: pools, tag sets, relation and link sets, attachments, inboxes.
internal interface IStore<TEntity>
    where TEntity : unmanaged, IEntity<TEntity>
{
    StoreKind Kind { get; }
    Type StoredType { get; }
    string? Name { get; } // named pools only
    SparseSet<TEntity>? Owners { get; } // null for tag sets

    // Tags go last: they are index-only, and handlers of the others may still read them.
    byte DestroyOrder => Kind switch
    {
        StoreKind.Component => 0,
        StoreKind.Relation or StoreKind.Links => 1,
        StoreKind.Attachments => 2,
        StoreKind.Inbox => 3,
        _ => 4,
    };

    bool Contains(TEntity e); // tag sets test the index only; callers check liveness
    void EntityDestroyed(TEntity e);
    void Compact(double minWaste);
    void ShrinkToFit();
    void Reserve(int entities);
}
