using Cestyll.Ecs.Attachments;
using Cestyll.Ecs.Inboxes;
using Cestyll.Ecs.Relations;
using Cestyll.Ecs.Tags;

namespace Cestyll.Ecs.Commands;

// Structural changes recorded now, applied in order at Flush through the plain registry calls.
// Create makes the entity at once (it touches no pool); commands for entities dead by Flush are skipped.
// A throwing command is dropped; earlier ones stay applied, later ones stay recorded.
public sealed partial class CommandBuffer<TEntity>
    where TEntity : unmanaged, IEntity<TEntity>
{
    private readonly Registry<TEntity> _registry;
    private readonly List<Command> _commands = [];

    // One column per store kind and type, by TypeId; commands hold a value index, so nothing boxes.
    private CommandColumn?[] _components = [];
    private CommandColumn?[] _tags = [];
    private CommandColumn?[] _relations = [];
    private CommandColumn?[] _linkSets = [];
    private CommandColumn?[] _attachments = [];
    private CommandColumn?[] _inboxes = [];
    private readonly List<CommandColumn> _columns = [];

    // Created since the last Flush, for Clear to destroy.
    private readonly List<TEntity> _created = [];
    private bool _flushing;

    internal CommandBuffer(Registry<TEntity> registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        _registry = registry;
    }

    public Registry<TEntity> Registry => _registry;

    public int Count => _commands.Count;
    public bool IsEmpty => _commands.Count == 0;

    // Makes the entity now; components arrive at Flush.
    public CommandHandle<TEntity> Create() => new(this, Created(_registry.CreateEntityNow()));

    private TEntity Created(TEntity e)
    {
        _created.Add(e);
        return e;
    }

    public void Add<T>(TEntity e, T value) where T : IComponent => Pending(CommandOp.Add, e, value);

    public void AddOrReplace<T>(TEntity e, T value) where T : IComponent => Pending(CommandOp.AddOrReplace, e, value);

    public void Replace<T>(TEntity e, T value) where T : IComponent => Pending(CommandOp.Replace, e, value);

    // The edit runs at Flush, on the value then current.
    public void Patch<T>(TEntity e, RefAction<T> edit)
        where T : IComponent
    {
        ArgumentNullException.ThrowIfNull(edit);
        RequireAlive(e);
        var column = Column<ComponentColumn<T>>(ref _components, TypeId<T>.Value);
        column.Edits.Add(edit);
        Record(CommandOp.Patch, column, e, column.Edits.Count - 1);
    }

    // Fires OnUpdate at Flush.
    public void Patch<T>(TEntity e)
        where T : IComponent
    {
        RequireAlive(e);
        Record(CommandOp.Patch, Column<ComponentColumn<T>>(ref _components, TypeId<T>.Value), e, -1);
    }

    public void Remove<T>(TEntity e)
        where T : IComponent
    {
        var column = Column<ComponentColumn<T>>(ref _components, TypeId<T>.Value);
        column.Log.Add(new(e, -1, _commands.Count));
        Record(CommandOp.Remove, column, e, 0);
    }

    public void Destroy(TEntity e) => Record(CommandOp.Destroy, null, e, 0);

    public void Tag<T>(TEntity e)
        where T : struct, ITag
    {
        RequireAlive(e);
        var column = Column<TagColumn<T>>(ref _tags, TypeId<T>.Value);
        column.Log.Add(new(e, true, _commands.Count));
        Record(CommandOp.Tag, column, e, 0);
    }

    public void Untag<T>(TEntity e)
        where T : struct, ITag
    {
        var column = Column<TagColumn<T>>(ref _tags, TypeId<T>.Value);
        column.Log.Add(new(e, false, _commands.Count));
        Record(CommandOp.Untag, column, e, 0);
    }

    // As TryFindPending, for tag T.
    internal bool TryFindPendingTag<T>(TEntity e, int since, out bool tagged)
        where T : struct, ITag
    {
        var id = TypeId<T>.Value;
        if (id < _tags.Length && _tags[id] is TagColumn<T> column)
        {
            var log = CollectionsMarshal.AsSpan(column.Log);
            for (var i = log.Length - 1; i >= 0 && log[i].Seq >= since; i--)
            {
                if (log[i].Entity == e)
                {
                    tagged = log[i].Tagged;
                    return true;
                }
            }
        }

        tagged = false;
        return false;
    }

    // Skipped at Flush if either end is dead by then.
    public void Relate<TRelation>(TEntity source, TEntity target)
        where TRelation : struct, IRelation
    {
        RequireAlive(source);
        RequireAlive(target);
        var column = Column<RelationColumn<TRelation>>(ref _relations, TypeId<TRelation>.Value);
        column.Targets.Add(target);
        column.Log.Add(new(source, target, _commands.Count));
        Record(CommandOp.Link, column, source, column.Targets.Count - 1);
    }

    public void Unrelate<TRelation>(TEntity source)
        where TRelation : struct, IRelation
    {
        var column = Column<RelationColumn<TRelation>>(ref _relations, TypeId<TRelation>.Value);
        column.Log.Add(new(source, default, _commands.Count));
        Record(CommandOp.Unlink, column, source, 0);
    }

    // As TryFindPending, for TRelation; Null target for an Unlink.
    internal bool TryFindPendingTarget<TRelation>(TEntity source, int since, out TEntity target)
        where TRelation : struct, IRelation
    {
        var id = TypeId<TRelation>.Value;
        if (id < _relations.Length && _relations[id] is RelationColumn<TRelation> column)
        {
            var log = CollectionsMarshal.AsSpan(column.Log);
            for (var i = log.Length - 1; i >= 0 && log[i].Seq >= since; i--)
            {
                if (log[i].Source == source)
                {
                    target = log[i].Target;
                    return true;
                }
            }
        }

        target = default;
        return false;
    }

    // Adds or replaces source's link; skipped at Flush if either end is dead by then.
    public void Link<T>(TEntity source, T link)
        where T : struct, ILink<TEntity> =>
        PendingLink(source, link);

    public void Unlink<T>(TEntity source, TEntity target)
        where T : struct, ILink<TEntity>
    {
        var column = Column<LinkColumn<T>>(ref _linkSets, TypeId<T>.Value);
        column.Targets.Add(target);
        column.Log.Add(new(source, target, -1, _commands.Count));
        Record(CommandOp.Unlink, column, source, column.Targets.Count - 1);
    }

    // The edit runs at Flush, on the link then current; skipped if the target is dead by then.
    public void Patch<T>(TEntity source, TEntity target, RefAction<T> edit)
        where T : struct, ILink<TEntity>
    {
        ArgumentNullException.ThrowIfNull(edit);
        PendingLinkPatch(source, target, edit);
    }

    // Fires OnLinkUpdate at Flush.
    public void Patch<T>(TEntity source, TEntity target)
        where T : struct, ILink<TEntity> =>
        PendingLinkPatch<T>(source, target, null);

    private void PendingLinkPatch<T>(TEntity source, TEntity target, RefAction<T>? edit)
        where T : struct, ILink<TEntity>
    {
        RequireAlive(source);
        RequireAlive(target);
        var column = Column<LinkColumn<T>>(ref _linkSets, TypeId<T>.Value);
        column.Patches.Add(new(target, edit));
        Record(CommandOp.Patch, column, source, column.Patches.Count - 1);
    }

    internal ref T PendingLink<T>(TEntity source, T link)
        where T : struct, ILink<TEntity>
    {
        RequireAttachable(source);
        RequireAttachable(link.Target);

        var column = Column<LinkColumn<T>>(ref _linkSets, TypeId<T>.Value);
        column.Links.Add(link);
        column.Log.Add(new(source, link.Target, column.Links.Count - 1, _commands.Count));
        Record(CommandOp.Link, column, source, column.Links.Count - 1);
        return ref CollectionsMarshal.AsSpan(column.Links)[^1];
    }

    // As TryFindPending, for source's T link to target.
    internal bool TryFindPendingLink<T>(TEntity source, TEntity target, int since, out int link)
        where T : struct, ILink<TEntity>
    {
        var id = TypeId<T>.Value;
        if (id < _linkSets.Length && _linkSets[id] is LinkColumn<T> column)
        {
            var log = CollectionsMarshal.AsSpan(column.Log);
            for (var i = log.Length - 1; i >= 0 && log[i].Seq >= since; i--)
            {
                if (log[i].Source == source && log[i].Target == target)
                {
                    link = log[i].Link;
                    return true;
                }
            }
        }

        link = -1;
        return false;
    }

    internal ref T PendingLinkValue<T>(int link)
        where T : struct, ILink<TEntity> =>
        ref CollectionsMarshal.AsSpan(((LinkColumn<T>)_linkSets[TypeId<T>.Value]!).Links)[link];

    public void Attach<T, TKey>(TEntity e, T item)
        where T : struct, IAttachment<TKey>
        where TKey : notnull =>
        PendingAttach<T, TKey>(e, item);

    public void Detach<T, TKey>(TEntity e, TKey key)
        where T : struct, IAttachment<TKey>
        where TKey : notnull
    {
        var column = Column<AttachmentColumn<T, TKey>>(ref _attachments, TypeId<T>.Value);
        column.Keys.Add(key);
        column.Log.Add(new(e, key, -1, _commands.Count));
        Record(CommandOp.Detach, column, e, column.Keys.Count - 1);
    }

    // At Flush: merges into the item under item.Key, or attaches item if there is none.
    public void AttachOrPatch<T, TKey>(TEntity e, T item, MergeAction<T> merge)
        where T : struct, IAttachment<TKey>
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(merge);
        RequireAttachable(e);
        var column = Column<AttachmentColumn<T, TKey>>(ref _attachments, TypeId<T>.Value);
        column.Merges.Add(new(item, merge));
        Record(CommandOp.AttachOrPatch, column, e, column.Merges.Count - 1);
    }

    // The edit runs at Flush, on the item then current.
    public void Patch<T, TKey>(TEntity e, TKey key, RefAction<T> edit)
        where T : struct, IAttachment<TKey>
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(edit);
        PendingAttachmentPatch(e, key, edit);
    }

    // Fires OnAttachmentUpdate at Flush.
    public void Patch<T, TKey>(TEntity e, TKey key)
        where T : struct, IAttachment<TKey>
        where TKey : notnull =>
        PendingAttachmentPatch<T, TKey>(e, key, null);

    private void PendingAttachmentPatch<T, TKey>(TEntity e, TKey key, RefAction<T>? edit)
        where T : struct, IAttachment<TKey>
        where TKey : notnull
    {
        RequireAlive(e);
        var column = Column<AttachmentColumn<T, TKey>>(ref _attachments, TypeId<T>.Value);
        column.Patches.Add(new(key, edit));
        Record(CommandOp.Patch, column, e, column.Patches.Count - 1);
    }

    public void Post<T>(TEntity e, T message) where T : struct, IMessage
    {
        RequireAttachable(e);

        var column = Column<InboxColumn<T>>(ref _inboxes, TypeId<T>.Value);
        column.Messages.Add(message);
        Record(CommandOp.Post, column, e, column.Messages.Count - 1);
    }

    // Inside a defer scope, what this applies is itself deferred to the scope's end.
    public void Flush()
    {
        if (_flushing)
        {
            throw new InvalidOperationException("This command buffer is already flushing");
        }

        FlushPending();
    }

    // Drops every command and destroys the entities Create made.
    public void Clear()
    {
        if (_flushing)
        {
            throw new InvalidOperationException("Can't clear a command buffer while it flushes");
        }

        foreach (var e in CollectionsMarshal.AsSpan(_created))
        {
            _registry.Destroy(e);
        }

        Reset();
    }

    // No-op while flushing: the running loop reaches commands a nested scope adds.
    internal void FlushPending()
    {
        if (_flushing)
        {
            return;
        }

        _flushing = true;
        var i = 0;
        try
        {
            // Count is read each time round: handlers may record more.
            for (; i < _commands.Count; i++)
            {
                var c = _commands[i];
                if (!_registry.IsAlive(c.Entity))
                {
                    continue;
                }

                if (c.Column is null)
                {
                    _registry.Destroy(c.Entity);
                }
                else
                {
                    c.Column.Apply(_registry, c.Op, c.Entity, c.Index);
                }
            }
        }
        finally
        {
            _flushing = false;
            if (i >= _commands.Count)
            {
                Reset();
            }
            else
            {
                _commands.RemoveRange(0, i + 1); // the rest keep their column indices
            }
        }
    }

    private void Reset()
    {
        _commands.Clear();
        _created.Clear();
        foreach (var column in CollectionsMarshal.AsSpan(_columns))
        {
            column.Clear();
        }
    }

    // Returned ref is good until the next command of its type.
    internal ref T Pending<T>(CommandOp op, TEntity e, T value)
        where T : IComponent
    {
        RequireAlive(e);
        var column = Column<ComponentColumn<T>>(ref _components, TypeId<T>.Value);
        column.Values.Add(value);
        column.Log.Add(new(e, column.Values.Count - 1, _commands.Count));
        Record(op, column, e, column.Values.Count - 1);
        return ref CollectionsMarshal.AsSpan(column.Values)[^1];
    }

    // e's latest T value index since command since, -1 for a Remove. Walks newest-first.
    internal bool TryFindPending<T>(TEntity e, int since, out int value)
        where T : IComponent
    {
        var id = TypeId<T>.Value;
        if (id < _components.Length && _components[id] is ComponentColumn<T> column)
        {
            var log = CollectionsMarshal.AsSpan(column.Log);
            for (var i = log.Length - 1; i >= 0 && log[i].Seq >= since; i--)
            {
                if (log[i].Entity == e)
                {
                    value = log[i].Value;
                    return true;
                }
            }
        }

        value = -1;
        return false;
    }

    internal ref T PendingValue<T>(int value)
        where T : IComponent =>
        ref CollectionsMarshal.AsSpan(((ComponentColumn<T>)_components[TypeId<T>.Value]!).Values)[value];

    internal ref T PendingAttach<T, TKey>(TEntity e, T item)
        where T : struct, IAttachment<TKey>
        where TKey : notnull
    {
        RequireAttachable(e);

        var column = Column<AttachmentColumn<T, TKey>>(ref _attachments, TypeId<T>.Value);
        column.Items.Add(item);
        column.Log.Add(new(e, item.Key, column.Items.Count - 1, _commands.Count));
        Record(CommandOp.Attach, column, e, column.Items.Count - 1);
        return ref CollectionsMarshal.AsSpan(column.Items)[^1];
    }

    // As TryFindPending, for the item under key.
    internal bool TryFindPendingAttachment<T, TKey>(TEntity e, TKey key, int since, out int item)
        where T : struct, IAttachment<TKey>
        where TKey : notnull
    {
        var id = TypeId<T>.Value;
        if (id < _attachments.Length && _attachments[id] is AttachmentColumn<T, TKey> column)
        {
            var log = CollectionsMarshal.AsSpan(column.Log);
            for (var i = log.Length - 1; i >= 0 && log[i].Seq >= since; i--)
            {
                if (log[i].Entity == e && EqualityComparer<TKey>.Default.Equals(log[i].Key, key))
                {
                    item = log[i].Item;
                    return true;
                }
            }
        }

        item = -1;
        return false;
    }

    internal ref T PendingAttachment<T, TKey>(int item)
        where T : struct, IAttachment<TKey>
        where TKey : notnull =>
        ref CollectionsMarshal.AsSpan(((AttachmentColumn<T, TKey>)_attachments[TypeId<T>.Value]!).Items)[item];

    private void RequireAlive(TEntity e)
    {
        if (!_registry.IsAlive(e))
        {
            Throw.NotAlive(e);
        }
    }

    private void RequireAttachable(TEntity e)
    {
        if (!_registry.CanAttachTo(e))
        {
            Throw.NotAlive(e);
        }
    }

    private void Record(CommandOp op, CommandColumn? column, TEntity e, int index) =>
        _commands.Add(new Command(op, column, e, index));

    private TColumn Column<TColumn>(ref CommandColumn?[] columns, int id)
        where TColumn : CommandColumn, new()
    {
        if (id >= columns.Length)
        {
            Array.Resize(ref columns, Math.Max(id + 1, columns.Length * 2));
        }

        if (columns[id] is not { } column)
        {
            column = new TColumn();
            columns[id] = column;
            _columns.Add(column);
        }

        return (TColumn)column;
    }

    private readonly record struct Command(CommandOp Op, CommandColumn? Column, TEntity Entity, int Index);

    // Applies through the registry call, so a command behaves exactly as the call would.
    private abstract class CommandColumn
    {
        public abstract void Apply(Registry<TEntity> registry, CommandOp op, TEntity e, int index);
        public abstract void Clear();
    }

    // Value is -1 for a Remove; Seq is the command number.
    private readonly record struct Logged(TEntity Entity, int Value, int Seq);

    private sealed class ComponentColumn<T> : CommandColumn
        where T : IComponent
    {
        public readonly List<T> Values = [];
        public readonly List<RefAction<T>> Edits = [];
        public readonly List<Logged> Log = [];

        public override void Apply(Registry<TEntity> registry, CommandOp op, TEntity e, int index)
        {
            switch (op)
            {
                case CommandOp.Add:
                    registry.Add(e, Values[index]);
                    break;
                case CommandOp.AddOrReplace:
                    registry.AddOrReplace(e, Values[index]);
                    break;
                case CommandOp.Replace:
                    registry.Replace(e, Values[index]);
                    break;
                case CommandOp.Patch when index < 0:
                    registry.Patch<T>(e);
                    break;
                case CommandOp.Patch:
                    registry.Patch(e, Edits[index]);
                    break;
                default:
                    registry.Remove<T>(e);
                    break;
            }
        }

        public override void Clear()
        {
            Values.Clear();
            Edits.Clear();
            Log.Clear();
        }
    }

    private sealed class TagColumn<T> : CommandColumn
        where T : struct, ITag
    {
        public readonly record struct Entry(TEntity Entity, bool Tagged, int Seq);

        public readonly List<Entry> Log = [];

        public override void Apply(Registry<TEntity> registry, CommandOp op, TEntity e, int index)
        {
            if (op == CommandOp.Tag)
            {
                registry.Tag<T>(e);
            }
            else
            {
                registry.Untag<T>(e);
            }
        }

        public override void Clear() => Log.Clear();
    }

    private sealed class RelationColumn<TRelation> : CommandColumn
        where TRelation : struct, IRelation
    {
        public readonly List<TEntity> Targets = [];

        // Target is Null for an Unlink.
        public readonly record struct Entry(TEntity Source, TEntity Target, int Seq);

        public readonly List<Entry> Log = [];

        public override void Apply(Registry<TEntity> registry, CommandOp op, TEntity e, int index)
        {
            if (op == CommandOp.Unlink)
            {
                registry.Unrelate<TRelation>(e);
            }
            else if (registry.IsAlive(Targets[index]))
            {
                registry.Relate<TRelation>(e, Targets[index]);
            }
        }

        public override void Clear()
        {
            Targets.Clear();
            Log.Clear();
        }
    }

    private sealed class LinkColumn<T> : CommandColumn
        where T : struct, ILink<TEntity>
    {
        public readonly List<T> Links = [];
        public readonly List<TEntity> Targets = [];

        // Edit is null for a Patch that only fires OnLinkUpdate.
        public readonly record struct PatchEntry(TEntity Target, RefAction<T>? Edit);

        public readonly List<PatchEntry> Patches = [];

        // Link is -1 for an Unlink.
        public readonly record struct Entry(TEntity Source, TEntity Target, int Link, int Seq);

        public readonly List<Entry> Log = [];

        public override void Apply(Registry<TEntity> registry, CommandOp op, TEntity e, int index)
        {
            if (op == CommandOp.Unlink)
            {
                registry.Unlink<T>(e, Targets[index]);
            }
            else if (op == CommandOp.Patch)
            {
                var patch = Patches[index];
                if (!registry.IsAlive(patch.Target))
                {
                    return;
                }

                if (patch.Edit is null)
                {
                    registry.Patch<T>(e, patch.Target);
                }
                else
                {
                    registry.Patch(e, patch.Target, patch.Edit);
                }
            }
            else if (registry.IsAlive(Links[index].Target))
            {
                registry.Link(e, Links[index]);
            }
        }

        public override void Clear()
        {
            Links.Clear();
            Targets.Clear();
            Patches.Clear();
            Log.Clear();
        }
    }

    private sealed class AttachmentColumn<T, TKey> : CommandColumn
        where T : struct, IAttachment<TKey>
        where TKey : notnull
    {
        public readonly List<T> Items = [];
        public readonly List<TKey> Keys = [];
        public readonly List<MergeEntry> Merges = [];
        public readonly List<PatchEntry> Patches = [];
        public readonly List<Entry> Log = [];

        // Item is -1 for a Detach.
        public readonly record struct Entry(TEntity Entity, TKey Key, int Item, int Seq);

        public readonly record struct MergeEntry(T Item, MergeAction<T> Merge);

        // Edit is null for a Patch that only fires OnAttachmentUpdate.
        public readonly record struct PatchEntry(TKey Key, RefAction<T>? Edit);

        public override void Apply(Registry<TEntity> registry, CommandOp op, TEntity e, int index)
        {
            switch (op)
            {
                case CommandOp.Attach:
                    registry.Attach<T, TKey>(e, Items[index]);
                    break;
                case CommandOp.AttachOrPatch:
                    registry.AttachOrPatch<T, TKey>(e, Merges[index].Item, Merges[index].Merge);
                    break;
                case CommandOp.Patch when Patches[index].Edit is { } edit:
                    registry.Patch<T, TKey>(e, Patches[index].Key, edit);
                    break;
                case CommandOp.Patch:
                    registry.Patch<T, TKey>(e, Patches[index].Key);
                    break;
                default:
                    registry.Detach<T, TKey>(e, Keys[index]);
                    break;
            }
        }

        public override void Clear()
        {
            Items.Clear();
            Keys.Clear();
            Merges.Clear();
            Patches.Clear();
            Log.Clear();
        }
    }

    private sealed class InboxColumn<T> : CommandColumn
        where T : struct, IMessage
    {
        public readonly List<T> Messages = [];

        public override void Apply(Registry<TEntity> registry, CommandOp op, TEntity e, int index) =>
            registry.Post(e, Messages[index]);

        public override void Clear() => Messages.Clear();
    }
}

internal enum CommandOp : byte
{
    Add,
    AddOrReplace,
    Replace,
    Patch,
    Remove,
    Destroy,
    Tag,
    Untag,
    Link,
    Unlink,
    Attach,
    AttachOrPatch,
    Detach,
    Post,
}
