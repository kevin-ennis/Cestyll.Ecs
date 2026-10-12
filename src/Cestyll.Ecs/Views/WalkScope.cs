using Cestyll.Ecs.Storage;

namespace Cestyll.Ecs.Views;

// A walk's defer scope; DEBUG also marks the pools walked so reordering them mid-walk throws.
internal readonly ref struct WalkScope<TEntity>
    where TEntity : unmanaged, IEntity<TEntity>
{
    private readonly Registry<TEntity> _registry;
    private readonly bool _deferred;
#if DEBUG
    private readonly ReadOnlySpan<SparseSet<TEntity>> _pools;
    private readonly SparseSet<TEntity>? _driveSet;
#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private WalkScope(Registry<TEntity> registry, ReadOnlySpan<SparseSet<TEntity>> pools, SparseSet<TEntity>? driveSet)
    {
        _registry = registry;
        _deferred = registry.EnterWalk();
#if DEBUG
        _pools = pools;
        _driveSet = driveSet;
        foreach (var pool in pools)
        {
            pool.EnterWalk();
        }

        // So pulling an entity into the group mid-walk throws.
        driveSet?.EnterWalk();
#endif
    }

    // pools isn't scoped: DEBUG keeps it for Dispose.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WalkScope<TEntity> Enter(
        Registry<TEntity> registry, ReadOnlySpan<SparseSet<TEntity>> pools, SparseSet<TEntity>? driveSet = null)
        => new(registry, pools, driveSet);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
#if DEBUG
        _driveSet?.ExitWalk();
        foreach (var pool in _pools)
        {
            pool.ExitWalk();
        }
#endif
        _registry.ExitWalk(_deferred);
    }
}
