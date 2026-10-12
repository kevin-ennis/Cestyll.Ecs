using Cestyll.Ecs.Groups;
using Cestyll.Ecs.Storage;
using Cestyll.Ecs.Tags;

namespace Cestyll.Ecs;

public partial class Registry<TEntity>
    where TEntity : unmanaged, IEntity<TEntity>
{
    private readonly List<NonOwningGroup<TEntity>> _nonOwningGroups = [];

    public GroupBuilder<TEntity> Group() => new(this, []);

    // Any Own rule makes an owning group.
    internal Group<TEntity> BuildGroup(ReadOnlySpan<GroupRule> rules)
    {
        foreach (var rule in rules)
        {
            if (rule.Kind == GroupRuleKind.Own)
            {
                var owning = OwningGroupFor(rules);
                return owning.Handle ??= new(this, owning);
            }
        }

        var members = NonOwningGroupFor(rules);
        return members.Handle ??= new(this, members);
    }

    // Only the first owned pool's chain can hold it. Doesn't allocate.
    private OwningGroup<TEntity> OwningGroupFor(ReadOnlySpan<GroupRule> rules)
    {
        SparseSet<TEntity>? first = null;
        foreach (var rule in rules)
        {
            if (rule.Kind == GroupRuleKind.Own)
            {
                first = rule.Storage!.In(this);
                break;
            }
        }

        if (first!.Owner is GroupChain<TEntity> chain)
        {
            foreach (var g in chain.Groups)
            {
                if (g.HasRules(rules))
                {
                    return g;
                }
            }
        }

        var owned = new List<SparseSet<TEntity>>();
        foreach (var rule in rules)
        {
            if (rule.Kind == GroupRuleKind.Own && !owned.Contains(rule.Storage!.In(this)))
            {
                owned.Add(rule.Storage!.In(this));
            }
        }

        return new OwningGroup<TEntity>([.. owned], ResolveRules(rules));
    }

    private NonOwningGroup<TEntity> NonOwningGroupFor(ReadOnlySpan<GroupRule> rules)
    {
        foreach (var g in _nonOwningGroups)
        {
            if (g.HasRules(rules))
            {
                return g;
            }
        }

        var group = new NonOwningGroup<TEntity>(this, ResolveRules(rules), g => _nonOwningGroups.Remove(g));
        _nonOwningGroups.Add(group);
        return group;
    }

    private GroupRules<TEntity> ResolveRules(ReadOnlySpan<GroupRule> rules)
    {
        var keys = GroupRules<TEntity>.SortedKeys(rules);
        var get = new List<SparseSet<TEntity>>();
        var excludePools = new List<SparseSet<TEntity>>();
        var require = new List<TagSet<TEntity>>();
        var exclude = new List<TagSet<TEntity>>();
        foreach (var rule in rules)
        {
            switch (rule.Kind)
            {
                case GroupRuleKind.Get:
                    get.Add(rule.Storage!.In(this));
                    break;
                case GroupRuleKind.Without:
                    excludePools.Add(rule.Storage!.In(this));
                    break;
                case GroupRuleKind.Own:
                    break; // the owned pools, which the caller collects
                case GroupRuleKind.WithTag:
                    require.Add(TagsAt(rule.TypeId, rule.Type));
                    break;
                case GroupRuleKind.WithoutTag:
                    exclude.Add(TagsAt(rule.TypeId, rule.Type));
                    break;
                case GroupRuleKind.WithAttachment or GroupRuleKind.WithInbox or GroupRuleKind.WithRelationTarget
                    or GroupRuleKind.WithRelationSources or GroupRuleKind.WithLinkTargets or GroupRuleKind.WithLinkSources:
                    get.Add(rule.Storage!.In(this));
                    break;
                default: // the Without forms of those
                    excludePools.Add(rule.Storage!.In(this));
                    break;

            }
        }
        foreach (var r in require)
        {
            if (exclude.Contains(r))
            {
                throw new ArgumentException("A tag can't be both required and excluded");
            }
        }

        return new GroupRules<TEntity>(
            [.. get.Distinct()], [.. excludePools.Distinct()], [.. require.Distinct()], [.. exclude.Distinct()], keys);
    }
}
