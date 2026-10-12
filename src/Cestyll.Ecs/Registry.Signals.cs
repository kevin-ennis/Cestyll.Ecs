using Cestyll.Ecs.Attachments;
using Cestyll.Ecs.Inboxes;
using Cestyll.Ecs.Relations;
using Cestyll.Ecs.Storage;
using Cestyll.Ecs.Tags;

namespace Cestyll.Ecs;

// Shortcuts to the stores' signals. Each makes the store if needed.
public partial class Registry<TEntity>
    where TEntity : unmanaged, IEntity<TEntity>
{
    public Connection OnAdd<T>(Action<TEntity> handler) where T : IComponent =>
        ConnectAdd(Storage<T>(), handler);

    public Connection OnAdd<T>(string name, Action<TEntity> handler) where T : IComponent =>
        ConnectAdd(Storage<T>(name), handler);

    public Connection OnUpdate<T>(Action<TEntity> handler) where T : IComponent =>
        ConnectUpdate(Storage<T>(), handler);

    public Connection OnUpdate<T>(string name, Action<TEntity> handler) where T : IComponent =>
        ConnectUpdate(Storage<T>(name), handler);

    public Connection OnRemove<T>(Action<TEntity> handler) where T : IComponent =>
        ConnectRemove(Storage<T>(), handler);

    public Connection OnRemove<T>(string name, Action<TEntity> handler) where T : IComponent =>
        ConnectRemove(Storage<T>(name), handler);

    public Connection OnTag<T>(Action<TEntity> handler) where T : struct, ITag
    {
        ArgumentNullException.ThrowIfNull(handler);
        var tags = Tags<T>();
        tags.OnTag += handler;
        return new Connection(() => tags.OnTag -= handler);
    }

    public Connection OnUntag<T>(Action<TEntity> handler) where T : struct, ITag
    {
        ArgumentNullException.ThrowIfNull(handler);
        var tags = Tags<T>();
        tags.OnUntag += handler;
        return new Connection(() => tags.OnUntag -= handler);
    }

    // Named apart from the link-set signals, since a type may be both kinds.
    public Connection OnRelate<TRelation>(PairHandler<TEntity> handler) where TRelation : struct, IRelation
    {
        ArgumentNullException.ThrowIfNull(handler);
        var relation = Relations<TRelation>();
        relation.OnRelate += handler;
        return new Connection(() => relation.OnRelate -= handler);
    }

    public Connection OnUnrelate<TRelation>(PairHandler<TEntity> handler) where TRelation : struct, IRelation
    {
        ArgumentNullException.ThrowIfNull(handler);
        var relation = Relations<TRelation>();
        relation.OnUnrelate += handler;
        return new Connection(() => relation.OnUnrelate -= handler);
    }

    public Connection OnLink<T>(PairHandler<TEntity> handler) where T : struct, ILink<TEntity>
    {
        ArgumentNullException.ThrowIfNull(handler);
        var links = Links<T>();
        links.OnLink += handler;
        return new Connection(() => links.OnLink -= handler);
    }

    public Connection OnLinkUpdate<T>(PairHandler<TEntity> handler) where T : struct, ILink<TEntity>
    {
        ArgumentNullException.ThrowIfNull(handler);
        var links = Links<T>();
        links.OnUpdate += handler;
        return new Connection(() => links.OnUpdate -= handler);
    }

    public Connection OnUnlink<T>(PairHandler<TEntity> handler) where T : struct, ILink<TEntity>
    {
        ArgumentNullException.ThrowIfNull(handler);
        var links = Links<T>();
        links.OnUnlink += handler;
        return new Connection(() => links.OnUnlink -= handler);
    }

    public Connection OnAttach<T, TKey>(AttachmentHandler<TEntity, TKey> handler)
        where T : struct, IAttachment<TKey>
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(handler);
        var attachments = Attachments<T, TKey>();
        attachments.OnAttach += handler;
        return new Connection(() => attachments.OnAttach -= handler);
    }

    public Connection OnAttachmentUpdate<T, TKey>(AttachmentHandler<TEntity, TKey> handler)
        where T : struct, IAttachment<TKey>
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(handler);
        var attachments = Attachments<T, TKey>();
        attachments.OnUpdate += handler;
        return new Connection(() => attachments.OnUpdate -= handler);
    }

    public Connection OnDetach<T, TKey>(AttachmentHandler<TEntity, TKey> handler)
        where T : struct, IAttachment<TKey>
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(handler);
        var attachments = Attachments<T, TKey>();
        attachments.OnDetach += handler;
        return new Connection(() => attachments.OnDetach -= handler);
    }

    public Connection OnPost<T>(MessageHandler<TEntity, T> handler) where T : struct, IMessage
    {
        ArgumentNullException.ThrowIfNull(handler);
        var inbox = Inboxes<T>();
        inbox.OnPost += handler;
        return new Connection(() => inbox.OnPost -= handler);
    }

    private static Connection ConnectAdd<T>(Pool<TEntity, T> pool, Action<TEntity> handler) where T : IComponent
    {
        ArgumentNullException.ThrowIfNull(handler);
        pool.OnAdd += handler;
        return new Connection(() => pool.OnAdd -= handler);
    }

    private static Connection ConnectUpdate<T>(Pool<TEntity, T> pool, Action<TEntity> handler) where T : IComponent
    {
        ArgumentNullException.ThrowIfNull(handler);
        pool.OnUpdate += handler;
        return new Connection(() => pool.OnUpdate -= handler);
    }

    private static Connection ConnectRemove<T>(Pool<TEntity, T> pool, Action<TEntity> handler) where T : IComponent
    {
        ArgumentNullException.ThrowIfNull(handler);
        pool.OnRemove += handler;
        return new Connection(() => pool.OnRemove -= handler);
    }
}
