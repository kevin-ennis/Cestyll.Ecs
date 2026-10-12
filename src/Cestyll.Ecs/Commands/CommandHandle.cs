using Cestyll.Ecs.Attachments;
using Cestyll.Ecs.Inboxes;
using Cestyll.Ecs.Relations;
using Cestyll.Ecs.Tags;

namespace Cestyll.Ecs.Commands;

// An entity paired with a command buffer; every call records onto the buffer.
public readonly ref struct CommandHandle<TEntity>
    where TEntity : unmanaged, IEntity<TEntity>
{
    public CommandBuffer<TEntity> Buffer { get; }
    public TEntity Entity { get; }

    internal CommandHandle(CommandBuffer<TEntity> buffer, TEntity entity)
    {
        Buffer = buffer;
        Entity = entity;
    }

    public static implicit operator TEntity(CommandHandle<TEntity> h) => h.Entity;

    public CommandHandle<TEntity> Add<T>(T value)
        where T : IComponent
    {
        Buffer.Add(Entity, value);
        return this;
    }

    public CommandHandle<TEntity> AddOrReplace<T>(T value)
        where T : IComponent
    {
        Buffer.AddOrReplace(Entity, value);
        return this;
    }

    public CommandHandle<TEntity> Replace<T>(T value)
        where T : IComponent
    {
        Buffer.Replace(Entity, value);
        return this;
    }

    public CommandHandle<TEntity> Patch<T>(RefAction<T> edit)
        where T : IComponent
    {
        Buffer.Patch(Entity, edit);
        return this;
    }

    public CommandHandle<TEntity> Patch<T>()
        where T : IComponent
    {
        Buffer.Patch<T>(Entity);
        return this;
    }

    public CommandHandle<TEntity> Remove<T>()
        where T : IComponent
    {
        Buffer.Remove<T>(Entity);
        return this;
    }

    public CommandHandle<TEntity> Tag<T>()
        where T : struct, ITag
    {
        Buffer.Tag<T>(Entity);
        return this;
    }

    public CommandHandle<TEntity> Untag<T>()
        where T : struct, ITag
    {
        Buffer.Untag<T>(Entity);
        return this;
    }

    public CommandHandle<TEntity> Relate<TRelation>(TEntity target)
        where TRelation : struct, IRelation
    {
        Buffer.Relate<TRelation>(Entity, target);
        return this;
    }

    public CommandHandle<TEntity> Unrelate<TRelation>()
        where TRelation : struct, IRelation
    {
        Buffer.Unrelate<TRelation>(Entity);
        return this;
    }

    public CommandHandle<TEntity> Link<T>(T link)
        where T : struct, ILink<TEntity>
    {
        Buffer.Link(Entity, link);
        return this;
    }

    public CommandHandle<TEntity> Unlink<T>(TEntity target)
        where T : struct, ILink<TEntity>
    {
        Buffer.Unlink<T>(Entity, target);
        return this;
    }

    public CommandHandle<TEntity> Patch<T>(TEntity target, RefAction<T> edit)
        where T : struct, ILink<TEntity>
    {
        Buffer.Patch(Entity, target, edit);
        return this;
    }

    public CommandHandle<TEntity> Patch<T>(TEntity target)
        where T : struct, ILink<TEntity>
    {
        Buffer.Patch<T>(Entity, target);
        return this;
    }

    public CommandHandle<TEntity> Attach<T, TKey>(T item)
        where T : struct, IAttachment<TKey>
        where TKey : notnull
    {
        Buffer.Attach<T, TKey>(Entity, item);
        return this;
    }

    public CommandHandle<TEntity> AttachOrPatch<T, TKey>(T item, MergeAction<T> merge)
        where T : struct, IAttachment<TKey>
        where TKey : notnull
    {
        Buffer.AttachOrPatch<T, TKey>(Entity, item, merge);
        return this;
    }

    public CommandHandle<TEntity> Patch<T, TKey>(TKey key, RefAction<T> edit)
        where T : struct, IAttachment<TKey>
        where TKey : notnull
    {
        Buffer.Patch<T, TKey>(Entity, key, edit);
        return this;
    }

    public CommandHandle<TEntity> Patch<T, TKey>(TKey key)
        where T : struct, IAttachment<TKey>
        where TKey : notnull
    {
        Buffer.Patch<T, TKey>(Entity, key);
        return this;
    }

    public CommandHandle<TEntity> Detach<T, TKey>(TKey key)
        where T : struct, IAttachment<TKey>
        where TKey : notnull
    {
        Buffer.Detach<T, TKey>(Entity, key);
        return this;
    }

    public CommandHandle<TEntity> Post<T>(T message) where T : struct, IMessage
    {
        Buffer.Post(Entity, message);
        return this;
    }

    public void Destroy() => Buffer.Destroy(Entity);

    public override string ToString() => $"{Entity}";
}
