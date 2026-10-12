using System.Diagnostics.CodeAnalysis;

namespace Cestyll.Ecs;

// Out-of-line throws, so hot callers skip the string formatting.
internal static class Throw
{
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void NotAlive<TEntity>(TEntity e) => throw new InvalidOperationException($"{e} is not alive");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void NoComponent<TEntity, T>(TEntity e) =>
        throw new KeyNotFoundException($"{e} has no {typeof(T).Name}");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void NoLink<TEntity, T>(TEntity source, TEntity target) =>
        throw new KeyNotFoundException($"{source} has no {typeof(T).Name} link to {target}");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void NoAttachment<TEntity, T, TKey>(TEntity e, TKey key) =>
        throw new KeyNotFoundException($"{e} has no {typeof(T).Name} with key {key}");
}
