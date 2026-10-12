using Cestyll.Ecs.Storage;

namespace Cestyll.Ecs.Relations;

// Public only because Relation is; the private protected ctor keeps it sealed to this assembly.
public abstract class RelationStore<TEntity> : IPendingReads, IStore<TEntity>
    where TEntity : unmanaged, IEntity<TEntity>
{
    private protected RelationStore() { }
    // Destroy hooks: e's own pairs go first, then the pairs that point at e.
    internal abstract void SourceDestroyed(TEntity e);
    internal abstract void TargetDestroyed(TEntity e, Registry<TEntity> registry);

    // e is a source or target of any link here.
    internal abstract bool Involves(TEntity e);

    // For SnapshotSchema.
    internal abstract Type StoredType { get; }
    internal abstract bool IsLinkSet { get; }

    internal abstract SparseSet<TEntity> HasTargets { get; }
    internal abstract SparseSet<TEntity> HasSources { get; }

    // Destroy walks relation stores itself, in two phases, so EntityDestroyed does nothing.
    StoreKind IStore<TEntity>.Kind => IsLinkSet ? StoreKind.Links : StoreKind.Relation;
    Type IStore<TEntity>.StoredType => StoredType;
    string? IStore<TEntity>.Name => null;
    SparseSet<TEntity>? IStore<TEntity>.Owners => HasTargets;
    bool IStore<TEntity>.Contains(TEntity e) => Involves(e);
    void IStore<TEntity>.EntityDestroyed(TEntity e) { }
    void IStore<TEntity>.Reserve(int entities) { }

    // Set while an open defer scope has a link-set Link or Unlink of this type pending.
    internal bool PendingReads;
    bool IPendingReads.PendingReads { get => PendingReads; set => PendingReads = value; }

    // Unlinks never free memory. Compact and ShrinkToFit move items: refs and spans go stale.
    public abstract void Compact(double minWaste = 0);
    public abstract void ShrinkToFit();
    public abstract int SlabUsed { get; }
    public abstract int SlabGaps { get; }
    public abstract int SlabCapacity { get; }
}
