using Cestyll.Ecs.Collections;

namespace Cestyll.Ecs;

// Per-registry singletons keyed by type. Apart from _pools, so a type can be both.
public partial class Registry<TEntity>
    where TEntity : unmanaged, IEntity<TEntity>
{
    private TypeSlots<ContextBox> _context = new(); // indexed by the same TypeId as _pools

    // A box per value keeps refs valid as _context grows; AddOrReplaceContext reuses it, RemoveContext detaches it.
    private abstract class ContextBox
    {
        public abstract Type ValueType { get; }
    }

    private sealed class ContextBox<T>(T value) : ContextBox
    {
        public T Value = value;
        public override Type ValueType => typeof(T);
    }

    public ref T AddOrReplaceContext<T>(T value)
    {
        ref var slot = ref _context.Slot(TypeId<T>.Value);
        if (slot is ContextBox<T> box)
        {
            box.Value = value;
        }
        else
        {
            box = new ContextBox<T>(value);
            slot = box;
        }
        return ref box.Value;
    }

    // Throws rather than creating a silent default.
    public ref T GetContext<T>() =>
        ref (FindContext<T>() ?? throw new KeyNotFoundException($"No context value of type {typeof(T).Name}")).Value;

    public bool HasContext<T>() => FindContext<T>() is not null;

    public bool RemoveContext<T>()
    {
        var id = TypeId<T>.Value;
        if (_context.Find(id) is null)
        {
            return false;
        }

        _context.Slot(id) = null;
        return true;
    }

    // For SnapshotSchema.Uncovered.
    internal IEnumerable<Type> ContextTypes()
    {
        for (var i = 0; i < _context.Length; i++)
        {
            if (_context[i] is { } box)
            {
                yield return box.ValueType;
            }
        }
    }

    private ContextBox<T>? FindContext<T>() => (ContextBox<T>?)_context.Find(TypeId<T>.Value);
}
