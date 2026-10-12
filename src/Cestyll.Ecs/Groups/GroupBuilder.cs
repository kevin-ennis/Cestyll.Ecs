using Cestyll.Ecs.Attachments;
using Cestyll.Ecs.Inboxes;
using Cestyll.Ecs.Relations;
using Cestyll.Ecs.Storage;
using Cestyll.Ecs.Tags;

namespace Cestyll.Ecs.Groups;

// Same rules in any order give the same group. Each step allocates: build once, keep it.
public readonly struct GroupBuilder<TEntity>
    where TEntity : unmanaged, IEntity<TEntity>
{
    private readonly Registry<TEntity> _registry;
    private readonly GroupRule[] _rules;

    internal GroupBuilder(Registry<TEntity> registry, GroupRule[] rules)
    {
        _registry = registry;
        _rules = rules;
    }

    // Packs members to the front of T's pool.
    public GroupBuilder<TEntity> Own<T>() where T : IComponent => new(_registry, [.. _rules, GroupRule.Own<T>()]);

    // Requires T without owning its pool.
    public GroupBuilder<TEntity> With<T>() where T : IComponent => new(_registry, [.. _rules, GroupRule.Get<T>()]);

    public GroupBuilder<TEntity> Without<T>() where T : IComponent => new(_registry, [.. _rules, GroupRule.Without<T>()]);

    public GroupBuilder<TEntity> WithTag<T>() where T : struct, ITag => new(_registry, [.. _rules, GroupRule.WithTag<T>()]);

    public GroupBuilder<TEntity> WithoutTag<T>() where T : struct, ITag =>
        new(_registry, [.. _rules, GroupRule.WithoutTag<T>()]);

    public GroupBuilder<TEntity> WithAttachment<T>() where T : struct, IAttachment =>
        On<T>(GroupRuleKind.WithAttachment, _registry.AttachmentOwners<T>());

    public GroupBuilder<TEntity> WithoutAttachment<T>() where T : struct, IAttachment =>
        On<T>(GroupRuleKind.WithoutAttachment, _registry.AttachmentOwners<T>());

    public GroupBuilder<TEntity> WithInbox<T>() where T : struct, IMessage =>
        On<T>(GroupRuleKind.WithInbox, _registry.Inboxes<T>().Owners);

    public GroupBuilder<TEntity> WithoutInbox<T>() where T : struct, IMessage =>
        On<T>(GroupRuleKind.WithoutInbox, _registry.Inboxes<T>().Owners);

    // WithRelationTarget: has a target (a parent). WithRelationSources: is a target (has children).
    public GroupBuilder<TEntity> WithRelationTarget<TRelation>() where TRelation : struct, IRelation =>
        On<TRelation>(GroupRuleKind.WithRelationTarget, _registry.Relations<TRelation>().HasTargets);

    public GroupBuilder<TEntity> WithoutRelationTarget<TRelation>() where TRelation : struct, IRelation =>
        On<TRelation>(GroupRuleKind.WithoutRelationTarget, _registry.Relations<TRelation>().HasTargets);

    public GroupBuilder<TEntity> WithRelationSources<TRelation>() where TRelation : struct, IRelation =>
        On<TRelation>(GroupRuleKind.WithRelationSources, _registry.Relations<TRelation>().HasSources);

    public GroupBuilder<TEntity> WithoutRelationSources<TRelation>() where TRelation : struct, IRelation =>
        On<TRelation>(GroupRuleKind.WithoutRelationSources, _registry.Relations<TRelation>().HasSources);

    public GroupBuilder<TEntity> WithLinkTargets<T>() where T : struct, ILink<TEntity> =>
        On<T>(GroupRuleKind.WithLinkTargets, _registry.Links<T>().HasTargets);

    public GroupBuilder<TEntity> WithoutLinkTargets<T>() where T : struct, ILink<TEntity> =>
        On<T>(GroupRuleKind.WithoutLinkTargets, _registry.Links<T>().HasTargets);

    public GroupBuilder<TEntity> WithLinkSources<T>() where T : struct, ILink<TEntity> =>
        On<T>(GroupRuleKind.WithLinkSources, _registry.Links<T>().HasSources);

    public GroupBuilder<TEntity> WithoutLinkSources<T>() where T : struct, ILink<TEntity> =>
        On<T>(GroupRuleKind.WithoutLinkSources, _registry.Links<T>().HasSources);

    private GroupBuilder<TEntity> On<T>(GroupRuleKind kind, SparseSet<TEntity> set) =>
        new(_registry, [.. _rules, GroupRule.OnSet<T>(kind, set)]);

    public Group<TEntity> Build() => _registry.BuildGroup(_rules);
}
