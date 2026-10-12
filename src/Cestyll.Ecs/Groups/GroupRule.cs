using Cestyll.Ecs.Storage;
using Cestyll.Ecs.Tags;

namespace Cestyll.Ecs.Groups;

// One GroupBuilder rule. Get is GroupBuilder.With.
internal readonly struct GroupRule
{
    // id + 1 so default(GroupRule) is invalid rather than type 0.
    private readonly int _idPlusOne;
    internal readonly GroupRuleKind Kind;
    internal readonly Type Type; // names the tag set if the group creates it
    internal readonly StorageRef? Storage; // Get and Exclude: resolves T's pool in a registry

    private GroupRule(int typeId, Type type, GroupRuleKind kind, StorageRef? storage)
    {
        _idPlusOne = typeId + 1;
        Kind = kind;
        Type = type;
        Storage = storage;
    }

    internal bool IsValid => _idPlusOne != 0;
    internal int TypeId => _idPlusOne - 1;
    internal int Key => TypeId * 32 + (int)Kind;

    public static GroupRule WithTag<T>() where T : struct, ITag =>
        new(TypeId<T>.Value, typeof(T), GroupRuleKind.WithTag, null);

    public static GroupRule WithoutTag<T>() where T : struct, ITag =>
        new(TypeId<T>.Value, typeof(T), GroupRuleKind.WithoutTag, null);

    public static GroupRule Without<T>() where T : IComponent =>
        new(TypeId<T>.Value, typeof(T), GroupRuleKind.Without, StorageRef<T>.Instance);

    public static GroupRule Get<T>() where T : IComponent =>
        new(TypeId<T>.Value, typeof(T), GroupRuleKind.Get, StorageRef<T>.Instance);

    internal static GroupRule OnSet<T>(GroupRuleKind kind, object set) => new(TypeId<T>.Value, typeof(T), kind, new SetRef(set));

    internal static GroupRule Own<T>() where T : IComponent =>
        new(TypeId<T>.Value, typeof(T), GroupRuleKind.Own, StorageRef<T>.Instance);
}

internal enum GroupRuleKind : byte
{
    WithTag, WithoutTag, Get, Without, Own,
    WithAttachment, WithoutAttachment, WithInbox, WithoutInbox,
    WithRelationTarget, WithoutRelationTarget, WithRelationSources, WithoutRelationSources,
    WithLinkTargets, WithoutLinkTargets, WithLinkSources, WithoutLinkSources,
}


// Attachment or inbox owners, or one end of a relation.
internal sealed class SetRef(object set) : StorageRef
{
    internal override SparseSet<TEntity> In<TEntity>(Registry<TEntity> registry) => (SparseSet<TEntity>)set;
}

// Resolves a rule's pool once TEntity is known, without reflection.
internal abstract class StorageRef
{
    internal abstract SparseSet<TEntity> In<TEntity>(Registry<TEntity> registry)
        where TEntity : unmanaged, IEntity<TEntity>;
}

internal sealed class StorageRef<T> : StorageRef
    where T : IComponent
{
    internal static readonly StorageRef<T> Instance = new();

    internal override SparseSet<TEntity> In<TEntity>(Registry<TEntity> registry) => registry.Storage<T>();
}

internal readonly record struct GroupRules<TEntity>(
    SparseSet<TEntity>[] Get, SparseSet<TEntity>[] ExcludePools, TagSet<TEntity>[] Require, TagSet<TEntity>[] Exclude,
    int[] Keys)
    where TEntity : unmanaged, IEntity<TEntity>
{
    // Sorted, distinct keys, so rule order doesn't matter.
    public static int[] SortedKeys(ReadOnlySpan<GroupRule> rules)
    {
        var keys = new SortedSet<int>();
        foreach (var r in rules)
        {
            keys.Add(r.Key);
        }

        return [.. keys];
    }

    // Doesn't allocate: lookups run every frame.
    public static bool Match(ReadOnlySpan<GroupRule> rules, int[] keys)
    {
        var distinct = 0;
        for (var i = 0; i < rules.Length; i++)
        {
            var key = rules[i].Key;
            if (Array.BinarySearch(keys, key) < 0)
            {
                return false;
            }

            var repeat = false;
            for (var j = 0; j < i && !repeat; j++)
            {
                repeat = rules[j].Key == key;
            }

            if (!repeat)
            {
                distinct++;
            }
        }

        return distinct == keys.Length;
    }
}
