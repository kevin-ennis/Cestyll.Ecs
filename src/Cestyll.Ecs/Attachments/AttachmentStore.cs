using Cestyll.Ecs.Storage;

namespace Cestyll.Ecs.Attachments;

// Public only because AttachmentSet is; the private protected ctor keeps it sealed to this assembly.
public abstract class AttachmentStore<TEntity> : IPendingReads, IStore<TEntity>
    where TEntity : unmanaged, IEntity<TEntity>
{
    private protected AttachmentStore() { }

    // Entities holding at least one attachment.
    internal abstract SparseSet<TEntity> Owners { get; }
    internal abstract void EntityDestroyed(TEntity e);
    internal abstract Type StoredType { get; }

    StoreKind IStore<TEntity>.Kind => StoreKind.Attachments;
    Type IStore<TEntity>.StoredType => StoredType;
    string? IStore<TEntity>.Name => null;
    SparseSet<TEntity>? IStore<TEntity>.Owners => Owners;
    bool IStore<TEntity>.Contains(TEntity e) => Owners.Has(e);
    void IStore<TEntity>.EntityDestroyed(TEntity e) => EntityDestroyed(e);
    void IStore<TEntity>.Reserve(int entities) { }

    // Set while an open defer scope has an attach or detach of this type pending.
    internal bool PendingReads;
    bool IPendingReads.PendingReads { get => PendingReads; set => PendingReads = value; }

    // Detaches never free memory. Compact and ShrinkToFit move items: refs and spans go stale.
    public abstract void Compact(double minWaste = 0);
    public abstract void ShrinkToFit();
    public abstract int SlabUsed { get; }
    public abstract int SlabGaps { get; }
    public abstract int SlabCapacity { get; }
}

// An attachment store known by item type alone, for key-free reads.
public abstract class AttachmentStore<TEntity, T> : AttachmentStore<TEntity>
    where TEntity : unmanaged, IEntity<TEntity>
    where T : struct, IAttachment
{
    private protected AttachmentStore() { }

    // Empty if e has none or is dead.
    public abstract Span<T> GetAll(TEntity e);
}
