namespace Cestyll.Ecs;

// Entity id layout: index and version packed in one value. Registry validates the layout.
public interface IEntity<TSelf> : IEquatable<TSelf>, IEqualityOperators<TSelf, TSelf, bool>
    where TSelf : unmanaged, IEntity<TSelf>
{
    // Also caps one sparse set's size.
    static abstract int MaxIndex { get; }
    static abstract uint VersionMask { get; }

    int Index { get; }
    uint Version { get; }
    bool IsNull { get; }

    static abstract TSelf Make(int index, uint version);

    // Ignores indices.
    static abstract bool SameVersion(TSelf a, TSelf b);
}
