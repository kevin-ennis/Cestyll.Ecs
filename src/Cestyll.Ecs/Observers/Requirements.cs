namespace Cestyll.Ecs.Observers;

// "Gaining T adds TRequired" rules, run on Apply. Existing values win; removing TRequired later doesn't re-add it.
public sealed class Requirements<TEntity> : IDisposable
    where TEntity : unmanaged, IEntity<TEntity>
{
    private readonly Registry<TEntity> _registry;
    private readonly List<Rule> _rules = [];
    private TEntity[] _buffer = []; // shared: rules run one at a time
    private bool _applying;
    private bool _disposed;

    internal Requirements(Registry<TEntity> registry) => _registry = registry;

    public Requirements<TEntity> Require<T, TRequired>(Func<Registry<TEntity>, TEntity, bool>? when = null)
        where T : IComponent
        where TRequired : IComponent, new() =>
        Require<T, TRequired>(static (_, _) => new TRequired(), when);

    public Requirements<TEntity> Require<T, TRequired>(
        Func<Registry<TEntity>, TEntity, TRequired> make,
        Func<Registry<TEntity>, TEntity, bool>? when = null)
        where T : IComponent
        where TRequired : IComponent
    {
        ArgumentNullException.ThrowIfNull(make);
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (typeof(T) == typeof(TRequired))
        {
            throw new ArgumentException($"{typeof(T).Name} can't require itself");
        }

        _rules.Add(new Rule<T, TRequired>(_registry.Observer().OnAdd<T>(), make, when));
        return this;
    }

    // Not inside a defer scope, nor from a rule's make or when.
    public void Apply()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_registry.IsDeferring)
        {
            throw new InvalidOperationException("Apply can't run inside a defer scope");
        }

        if (_applying)
        {
            throw new InvalidOperationException("Apply is already running");
        }

        _applying = true;
        try
        {
            bool added;
            do
            {
                added = false;
                foreach (var rule in _rules)
                {
                    added |= rule.Run(this);
                }
            }
            while (added);
        }
        finally
        {
            _applying = false;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        foreach (var rule in _rules)
        {
            rule.Dispose();
        }

        _rules.Clear();
        _buffer = [];
    }

    // Adds change the sets the entities came from, so rules walk a copy.
    private Span<TEntity> Copy(ReadOnlySpan<TEntity> entities)
    {
        if (_buffer.Length < entities.Length)
        {
            _buffer = new TEntity[Math.Max(entities.Length, _buffer.Length * 2)];
        }

        entities.CopyTo(_buffer);
        return _buffer.AsSpan(0, entities.Length);
    }

    private abstract class Rule : IDisposable
    {
        public abstract bool Run(Requirements<TEntity> owner);
        public abstract void Dispose();
    }

    private sealed class Rule<T, TRequired>(
        Observer<TEntity> gained,
        Func<Registry<TEntity>, TEntity, TRequired> make,
        Func<Registry<TEntity>, TEntity, bool>? when) : Rule
        where T : IComponent
        where TRequired : IComponent
    {
        private bool _seeded;

        public override bool Run(Requirements<TEntity> owner)
        {
            var registry = owner._registry;

            // The first run covers every entity with T.
            var source = gained.Entities;
            if (!_seeded)
            {
                _seeded = true;
                source = registry.FindStorage<T>() is { } pool ? pool.Entities : [];
            }

            if (source.IsEmpty)
            {
                return false;
            }

            var entities = owner.Copy(source);
            gained.Clear();
            var added = false;
            foreach (var e in entities)
            {
                if (registry.Has<T>(e) && !registry.Has<TRequired>(e) && (when?.Invoke(registry, e) ?? true))
                {
                    registry.Add(e, make(registry, e));
                    added = true;
                }
            }

            return added;
        }

        public override void Dispose() => gained.Dispose();
    }
}
