using Cestyll.Ecs;
using Cestyll.Ecs.Relations;

namespace Cestyll.Ecs.Tests;

// IndexOfMany must agree with IndexOf entity for entity.
public class BatchLookupTests
{
    private struct Hp : IComponent { public int Value; }

    public static TheoryData<int> Counts => [0, 1, 7, 8, 9, 64, 1000];

    [Theory]
    [MemberData(nameof(Counts))]
    public void IndexOfMany_MatchesIndexOf(int count)
    {
        var registry = new Registry();
        var pool = registry.Storage<Hp>();
        var rng = new Random(count);
        var live = Enumerable.Range(0, 3000).Select(_ => (Entity)registry.Create()).ToList();
        foreach (var e in live.Where((_, k) => k % 3 != 0))
        {
            pool.Add(e, new Hp { Value = e.Index });
        }

        var dead = live.Where((_, k) => k % 7 == 0).ToList();
        foreach (var e in dead)
        {
            registry.Destroy(e); // stale handles: version mismatch
        }

        var recycled = Enumerable.Range(0, dead.Count).Select(_ => (Entity)registry.Create()).ToList();
        var candidates = live.Concat(recycled).Concat(
        [
            Entity.Null,
            Entity.Make(900_000, 0), // index past the end of the sparse array
            Entity.Make(1, 5),       // live index, wrong version
        ]).ToList();

        var batch = Enumerable.Range(0, count).Select(_ => candidates[rng.Next(candidates.Count)]).ToArray();
        var slots = new int[count];
        pool.IndexOfMany(batch, slots);

        Assert.Equal(batch.Select(pool.IndexOf).ToArray(), slots);
    }

    [Fact]
    public void SourceSpan_MatchesGetSources()
    {
        var registry = new Registry();
        Entity owner = registry.Create();
        var items = Enumerable.Range(0, 100).Select(_ => (Entity)registry.Create()).ToList();
        foreach (var item in items)
        {
            registry.Relate<Owns>(item, owner);
        }

        foreach (var item in items.Where((_, k) => k % 3 == 0))
        {
            registry.Unrelate<Owns>(item);
        }

        var relation = registry.Relations<Owns>();
        var expected = new HashSet<Entity>();
        foreach (var s in relation.GetSources(owner))
        {
            expected.Add(s);
        }

        Assert.Equal(expected, relation.SourceSpan(owner).ToArray().ToHashSet());
        Assert.True(relation.SourceSpan(items[0]).IsEmpty);
    }

    private struct Owns : IRelation
    {
        public static OnTargetDestroyed Policy => OnTargetDestroyed.Unrelate;
    }
}
