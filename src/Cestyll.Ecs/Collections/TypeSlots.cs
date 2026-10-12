namespace Cestyll.Ecs.Collections;

// Type-id indexed slots that grow on write. A mutable struct: hold in a non-readonly field, never copy.
internal struct TypeSlots<T> where T : class
{
    private T?[] _items;

    public TypeSlots() => _items = new T?[16];

    public readonly int Length => _items.Length;

    public readonly T? this[int id] => _items[id];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly T? Find(int id)
    {
        var items = _items;
        return id < items.Length ? items[id] : null;
    }

    public ref T? Slot(int id)
    {
        if (id >= _items.Length)
        {
            Array.Resize(ref _items, Math.Max(id + 1, _items.Length * 2));
        }

        return ref _items[id];
    }
}
