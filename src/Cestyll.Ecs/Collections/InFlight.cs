namespace Cestyll.Ecs.Collections;

// Items whose removal signal is running, so a nested removal is a no-op, not infinite recursion.
internal sealed class InFlight<T>
{
    private readonly List<T> _items = [];

    // False if item is already being removed; otherwise dispose guard to exit.
    public bool TryEnter(T item, out Guard guard)
    {
        foreach (var x in _items)
        {
            if (EqualityComparer<T>.Default.Equals(x, item))
            {
                guard = default;
                return false;
            }
        }

        _items.Add(item);
        guard = new Guard(this);
        return true;
    }

    private void Exit() => _items.RemoveAt(_items.Count - 1);

    public readonly ref struct Guard(InFlight<T> owner)
    {
        public void Dispose() => owner.Exit();
    }
}
