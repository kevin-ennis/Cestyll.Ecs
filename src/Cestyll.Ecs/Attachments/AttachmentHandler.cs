namespace Cestyll.Ecs.Attachments;

// Attachment signal: the entity and the attached, updated or detached key.
public delegate void AttachmentHandler<TEntity, in TKey>(TEntity entity, TKey key);
