namespace Cestyll.Ecs;

// Name overloads reject null, which means the default pool. "name" shows in the exception.
internal static class Names
{
    public static string Checked(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return name;
    }
}
