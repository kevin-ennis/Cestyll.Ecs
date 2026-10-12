namespace Cestyll.Ecs.Inboxes;

// Inbox signal: the recipient and the posted message.
public delegate void MessageHandler<TEntity, T>(TEntity entity, in T message);
