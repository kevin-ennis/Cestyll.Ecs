namespace Cestyll.Ecs;

// A handler's link to a signal or queue. Dispose disconnects; extra disposes are no-ops.
public readonly struct Connection : IDisposable
{
    private readonly Action? _disconnect;

    internal Connection(Action disconnect) => _disconnect = disconnect;

    public void Dispose() => _disconnect?.Invoke();
}
