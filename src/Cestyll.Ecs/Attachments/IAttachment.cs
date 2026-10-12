namespace Cestyll.Ecs.Attachments;

// Data an entity can hold several of, one per Key; attaching an existing key replaces it.
public interface IAttachment<TKey> : IAttachment where TKey : notnull
{
    TKey Key { get; }
}

// Key-free base for filters and reads. Implement IAttachment<TKey>; this alone can't be attached.
public interface IAttachment { }
