namespace Cestyll.Ecs.Storage;

// A sparse set of bare entities, no component storage. Backs observers and non-owning groups.
internal sealed class EntitySet<TEntity> : SparseSet<TEntity>
    where TEntity : unmanaged, IEntity<TEntity>
{
    public void Collect(TEntity e)
    {
        if (!Has(e))
        {
            Emplace(e);
        }
    }

    public void Add(TEntity e) => Emplace(e);

    public void Clear() => ClearSet();

    private protected override void ResizeStorage(int capacity) { }
    private protected override void MoveStorage(int from, int to) { }
    private protected override void SwapStorage(int i, int j) { }
    private protected override void ParkStorage(int slot) { }
    private protected override void UnparkStorage(int slot) { }
}
