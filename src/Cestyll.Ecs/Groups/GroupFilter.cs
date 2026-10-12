using Cestyll.Ecs.Storage;
using Cestyll.Ecs.Tags;

namespace Cestyll.Ecs.Groups;

// Membership checks shared by owning and non-owning groups.
internal static class GroupFilter<TEntity>
    where TEntity : unmanaged, IEntity<TEntity>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool Passes(TEntity e, SparseSet<TEntity>[] must, SparseSet<TEntity>[] mustNot,
        TagSet<TEntity>[] require, TagSet<TEntity>[] exclude)
    {
        foreach (var p in must)
        {
            if (!p.Has(e))
            {
                return false;
            }
        }

        foreach (var p in mustNot)
        {
            if (p.Has(e))
            {
                return false;
            }
        }

        var index = e.Index;
        foreach (var t in require)
        {
            if (!t.Test(index))
            {
                return false;
            }
        }

        foreach (var t in exclude)
        {
            if (t.Test(index))
            {
                return false;
            }
        }

        return true;
    }

    // Each set is at most one of owned, Get or excluded.
    internal static void Validate(SparseSet<TEntity>[] owned, GroupRules<TEntity> rules)
    {
        if (new HashSet<SparseSet<TEntity>>(owned).Count != owned.Length)
        {
            throw new InvalidOperationException("A group can't own the same pool twice");
        }

        foreach (var g in rules.Get)
        {
            if (Array.IndexOf(owned, g) >= 0)
            {
                throw new InvalidOperationException("A group can't both own and Get the same component");
            }
        }

        foreach (var x in rules.ExcludePools)
        {
            if (Array.IndexOf(owned, x) >= 0 || Array.IndexOf(rules.Get, x) >= 0)
            {
                throw new InvalidOperationException("A group can't both require and exclude the same component");
            }
        }
    }

    // T's pool among the owned and Get sets.
    internal static Pool<TEntity, T> Find<T>(SparseSet<TEntity>[] owned, SparseSet<TEntity>[] gets)
        where T : IComponent
    {
        foreach (var p in owned)
        {
            if (p is Pool<TEntity, T> pool)
            {
                return pool;
            }
        }

        foreach (var p in gets)
        {
            if (p is Pool<TEntity, T> pool)
            {
                return pool;
            }
        }

        throw new InvalidOperationException($"This group neither owns nor Gets {typeof(T).Name}");
    }
}
