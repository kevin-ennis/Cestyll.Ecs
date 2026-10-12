namespace Cestyll.Ecs;

// Type ids via one static per closed generic. Shared across entity layouts.
internal static class TypeId
{
    internal static int Last = -1;
}

internal static class TypeId<T>
{
    public static readonly int Value = Interlocked.Increment(ref TypeId.Last);
}
