using Cestyll.Ecs.Storage;

namespace Cestyll.Ecs.Inboxes;

// Per-entity messages in posting order, in a shared slab; typically emptied each frame.
// GetAll spans go stale on any Post or Clear. ClearMessages swap-removes from Entities, so not mid-walk.
public sealed class Inbox<TEntity, T> : IStore<TEntity>
    where TEntity : unmanaged, IEntity<TEntity>
    where T : struct, IMessage
{
    private readonly SlabLists<TEntity, T> _lists = new();
    private readonly Registry<TEntity> _registry;

    internal Inbox(Registry<TEntity> registry) => _registry = registry;

    // ClearMessages, Clear and Destroy fire nothing.
    public event MessageHandler<TEntity, T>? OnPost;

    // Every entity with at least one message, in packed order.
    public ReadOnlySpan<TEntity> Entities => _lists.Owners.Entities;

    public int MessageCount(TEntity e) => _lists.Count(e);

    public bool Has(TEntity e) => _lists.IndexOf(e) >= 0;

    public Span<T> GetAll(TEntity e) => _lists.Span(e);

    public void Post(TEntity e, T message)
    {
        if (!_registry.CanAttachTo(e))
        {
            Throw.NotAlive(e);
        }

        _lists.Append(e, message, out _);
        OnPost?.Invoke(e, in message);
    }

    // Returns how many there were.
    public int ClearMessages(TEntity e) => _lists.RemoveAll(e);

    // Keeps the memory for the next frame.
    public void Clear() => _lists.Clear();

    // Clear() leaves no gaps, so a per-frame inbox rarely needs this.
    public void Compact(double minWaste = 0) => _lists.Compact(minWaste);

    public void ShrinkToFit() => _lists.ShrinkToFit();

    internal SparseSet<TEntity> Owners => _lists.Owners;

    StoreKind IStore<TEntity>.Kind => StoreKind.Inbox;
    Type IStore<TEntity>.StoredType => typeof(T);
    string? IStore<TEntity>.Name => null;
    SparseSet<TEntity>? IStore<TEntity>.Owners => Owners;
    bool IStore<TEntity>.Contains(TEntity e) => Has(e);
    void IStore<TEntity>.EntityDestroyed(TEntity e) => _lists.RemoveAll(e);
    void IStore<TEntity>.Reserve(int entities) { }
}
