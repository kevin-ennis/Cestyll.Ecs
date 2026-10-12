using Cestyll.Ecs.Storage;
using Cestyll.Ecs.Tags;

namespace Cestyll.Ecs.Views;

// Membership test over one storage: a set, a tag set or a keyed test. None means nobody has T.
internal readonly struct Filter<TEntity>(
    SparseSet<TEntity>? set, TagSet<TEntity>? tags, bool live = false, KeyTest<TEntity>? key = null)
    where TEntity : unmanaged, IEntity<TEntity>
{
    public SparseSet<TEntity>? Set => set;
    public TagSet<TEntity>? Tags => tags;
    public bool Exists => set is not null || tags is not null || (key?.Exists ?? false);

    // Tested per entity: every filter but a tag folded into the mask.
    public bool PerEntity => tags is null || live;

    // Entities tested here come from a live pool, so the index-only tag test is exact.
    public bool Has(TEntity e) =>
        key is not null ? key.Has(e) : tags is not null ? tags.Test(e.Index) : set is not null && set.Has(e);
}
