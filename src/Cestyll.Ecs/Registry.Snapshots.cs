using Cestyll.Ecs.Snapshots;
using Cestyll.Ecs.Storage;
using Cestyll.Ecs.Tags;

namespace Cestyll.Ecs;

public partial class Registry<TEntity>
    where TEntity : unmanaged, IEntity<TEntity>
{
    public Snapshot<TEntity, TWriter> Snapshot<TWriter>()
        where TWriter : ISnapshotWriter<TEntity>, allows ref struct =>
        new(this);

    public SnapshotLoader<TEntity, TReader> SnapshotLoader<TReader>()
        where TReader : ISnapshotReader<TEntity>, allows ref struct =>
        new(this);

    public ContinuousLoader<TEntity, TReader> ContinuousLoader<TReader>()
        where TReader : ISnapshotReader<TEntity>, allows ref struct =>
        new(this);

    // Every store, empty ones included, for SnapshotSchema.Uncovered.
    internal List<SnapshotStore> Stores()
    {
        var stores = new List<SnapshotStore>();
        foreach (var s in CollectionsMarshal.AsSpan(_stores))
        {
            if (s is not SparseSet<TEntity> { Dropped: true })
            {
                stores.Add(new(Section(s.Kind), s.StoredType, s.Name));
            }
        }

        foreach (var type in ContextTypes())
        {
            stores.Add(new(SnapshotSection.Context, type, null));
        }

        return stores;

        static SnapshotSection Section(StoreKind kind) => kind switch
        {
            StoreKind.Component => SnapshotSection.Component,
            StoreKind.Tag => SnapshotSection.Tag,
            StoreKind.Relation => SnapshotSection.Relation,
            StoreKind.Links => SnapshotSection.Link,
            StoreKind.Attachments => SnapshotSection.Attachment,
            _ => SnapshotSection.Inbox,
        };
    }

    internal void WriteTag<T, TWriter>(ref TWriter w)
        where T : struct, ITag
        where TWriter : ISnapshotWriter<TEntity>, allows ref struct
    {
        var tags = FindTags<T>();
        w.WriteCount(tags?.Count ?? 0);
        if (tags is null)
        {
            return;
        }

        foreach (var index in tags.Indices)
        {
            w.WriteEntity(TEntity.Make(index, _versions[index]));
        }
    }

    // Every index handed out (from 1; 0 is Null) with its version, then the free ones.
    internal void WriteEntities<TWriter>(ref TWriter w) where TWriter : ISnapshotWriter<TEntity>, allows ref struct
    {
        w.BeginSection(SnapshotSection.Entities, typeof(TEntity), null);
        w.WriteCount(_created - 1);
        for (var i = 1; i < _created; i++)
        {
            w.WriteEntity(TEntity.Make(i, _versions[i]));
        }

        w.WriteCount(_freeCount);
        foreach (var index in _free)
        {
            w.WriteEntity(TEntity.Make(index, _versions[index]));
        }

        w.EndSection();
    }

    internal void ReadEntities<TReader>(ref TReader r) where TReader : ISnapshotReader<TEntity>, allows ref struct
    {
        if (_created != 1)
        {
            throw new InvalidOperationException(
                "SnapshotLoader needs an empty registry. Use ContinuousLoader to merge.");
        }

        var (created, versions, free) = SnapshotIo<TEntity>.ReadEntitySection(ref r);
        _created = created;
        _versions = new uint[Math.Max(64, created)];
        versions.CopyTo(_versions);
        foreach (var index in free)
        {
            _free.Set(index);
        }

        _freeCount = free.Length;
        _freeHint = 0;
    }
}
