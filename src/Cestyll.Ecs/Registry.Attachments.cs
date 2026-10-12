using Cestyll.Ecs.Attachments;
using Cestyll.Ecs.Collections;
using Cestyll.Ecs.Storage;

namespace Cestyll.Ecs;

public partial class Registry<TEntity>
    where TEntity : unmanaged, IEntity<TEntity>
{
    private TypeSlots<AttachmentStore<TEntity>> _attachments = new(); // by TypeId

    public AttachmentSet<TEntity, T, TKey> Attachments<T, TKey>()
        where T : struct, IAttachment<TKey>
        where TKey : notnull
    {
        ref var slot = ref _attachments.Slot(TypeId<T>.Value);
        if (slot is null)
        {
            Markers.RequireOne(typeof(T));
            var store = new AttachmentSet<TEntity, T, TKey>(this);
            slot = store;
            AddStore(store);
        }

        // Only reachable when T implements IAttachment for two key types.
        return slot as AttachmentSet<TEntity, T, TKey>
            ?? throw new InvalidOperationException(
                $"{typeof(T).Name} is already stored with a key type other than {typeof(TKey).Name}");
    }

    // Never makes the store; one under another key type still throws.
    internal AttachmentSet<TEntity, T, TKey>? FindAttachments<T, TKey>()
        where T : struct, IAttachment<TKey>
        where TKey : notnull =>
        _attachments.Find(TypeId<T>.Value) is not null ? Attachments<T, TKey>() : null;

    public ref T Attach<T, TKey>(TEntity e, T item)
        where T : struct, IAttachment<TKey>
        where TKey : notnull =>
        ref _deferDepth > 0
            ? ref RecordAttach<T, TKey>(e, item)
            : ref Attachments<T, TKey>().Attach(e, item);

    public bool Detach<T, TKey>(TEntity e, TKey key)
        where T : struct, IAttachment<TKey>
        where TKey : notnull
    {
        if (_deferDepth > 0)
        {
            return RecordDetach<T, TKey>(e, key);
        }

        return Attachments<T, TKey>().Detach(e, key);
    }

    // Fires OnAttachmentUpdate; not recorded in a defer scope.
    public void Patch<T, TKey>(TEntity e, TKey key, RefAction<T> edit)
        where T : struct, IAttachment<TKey>
        where TKey : notnull =>
        RequireAttachments<T, TKey>(e, key).Patch(e, key, edit);

    public void Patch<T, TKey>(TEntity e, TKey key)
        where T : struct, IAttachment<TKey>
        where TKey : notnull =>
        RequireAttachments<T, TKey>(e, key).Patch(e, key);

    // Throws as Get would, without making the store.
    private AttachmentSet<TEntity, T, TKey> RequireAttachments<T, TKey>(TEntity e, TKey key)
        where T : struct, IAttachment<TKey>
        where TKey : notnull
    {
        var set = FindAttachments<T, TKey>();
        if (set is null)
        {
            Throw.NoAttachment<TEntity, T, TKey>(e, key);
        }

        return set;
    }

    // Deferred: a merge into an attached item runs at once (no layout change); a missing item's attach is recorded.
    public ref T AttachOrPatch<T, TKey>(TEntity e, T item, MergeAction<T> merge)
        where T : struct, IAttachment<TKey>
        where TKey : notnull
    {
        if (_deferDepth > 0)
        {
            // Born in the scope: the latest record takes the merge, or a recorded Detach forces an attach.
            var pending = _bornCount > 0 ? PendingAttachmentSlot<T, TKey>(e, item.Key) : NoPending;
            if (pending >= 0)
            {
                ref var current = ref Deferred.PendingAttachment<T, TKey>(pending);
                merge(ref current, in item);
                return ref current;
            }

            if (pending == PendingRemoved || !(FindAttachments<T, TKey>()?.HasApplied(e, item.Key) ?? false))
            {
                return ref RecordAttach<T, TKey>(e, item);
            }
        }

        return ref Attachments<T, TKey>().AttachOrPatch(e, item, merge);
    }

    public Span<T> GetAttachments<T, TKey>(TEntity e)
        where T : struct, IAttachment<TKey>
        where TKey : notnull =>
        Attachments<T, TKey>().GetAll(e);

    // Key-free reads; neither makes the store.
    public Span<T> GetAttachments<T>(TEntity e)
        where T : struct, IAttachment =>
        FindAttachments(TypeId<T>.Value) is AttachmentStore<TEntity, T> store ? store.GetAll(e) : [];

    public bool HasAttachments<T>(TEntity e)
        where T : struct, IAttachment =>
        FindAttachments(TypeId<T>.Value)?.Owners.Has(e) ?? false;

    private AttachmentStore<TEntity>? FindAttachments(int id) => _attachments.Find(id);

    // Owner sets made ahead of their store for groups; the store adopts its set.
    private TypeSlots<SparseSet<TEntity>> _attachmentOwners = new();

    internal SparseSet<TEntity> AttachmentOwners<T>() where T : struct, IAttachment
    {
        var id = TypeId<T>.Value;
        if (FindAttachments(id) is { } store)
        {
            return store.Owners;
        }

        return _attachmentOwners.Slot(id) ??= new Pool<TEntity, SlabLists<TEntity, T>.Head>();
    }

    internal Pool<TEntity, SlabLists<TEntity, T>.Head>? TakeAttachmentOwners<T>() where T : struct, IAttachment
    {
        var id = TypeId<T>.Value;
        if (_attachmentOwners.Find(id) is not Pool<TEntity, SlabLists<TEntity, T>.Head> owners)
        {
            return null;
        }

        _attachmentOwners.Slot(id) = null;
        return owners;
    }
}
