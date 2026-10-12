using Cestyll.Ecs.Collections;
using Cestyll.Ecs.Inboxes;
using Cestyll.Ecs.Storage;

namespace Cestyll.Ecs;

public partial class Registry<TEntity>
    where TEntity : unmanaged, IEntity<TEntity>
{
    private TypeSlots<IStore<TEntity>> _inboxes = new(); // by TypeId

    public Inbox<TEntity, T> Inboxes<T>() where T : struct, IMessage
    {
        ref var slot = ref _inboxes.Slot(TypeId<T>.Value);
        if (slot is not { } inbox)
        {
            Markers.RequireOne(typeof(T));
            inbox = new Inbox<TEntity, T>(this);
            slot = inbox;
            AddStore(inbox);
        }

        return (Inbox<TEntity, T>)inbox;
    }

    // Never makes the inbox.
    internal Inbox<TEntity, T>? FindInboxes<T>() where T : struct, IMessage =>
        FindInboxes(TypeId<T>.Value) as Inbox<TEntity, T>;

    private IStore<TEntity>? FindInboxes(int id) => _inboxes.Find(id);

    // Recorded in a defer scope; reads don't see it until the scope ends.
    public void Post<T>(TEntity e, T message) where T : struct, IMessage
    {
        if (_deferDepth > 0)
        {
            Deferred.Post(e, message);
            return;
        }

        Inboxes<T>().Post(e, message);
    }

    // Reads; neither makes the inbox.
    public Span<T> GetMessages<T>(TEntity e) where T : struct, IMessage =>
        FindInboxes<T>() is { } inbox ? inbox.GetAll(e) : [];

    public bool HasMessages<T>(TEntity e) where T : struct, IMessage => FindInboxes<T>()?.Has(e) ?? false;
}
