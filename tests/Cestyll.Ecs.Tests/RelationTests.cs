using Cestyll.Ecs;
using Cestyll.Ecs.Relations;

namespace Cestyll.Ecs.Tests;

public class RelationTests
{
    private struct Owns : IRelation
    {
        public static OnTargetDestroyed Policy => OnTargetDestroyed.Unrelate;
    }

    private struct Parent : IRelation
    {
        public static OnTargetDestroyed Policy => OnTargetDestroyed.DestroySources;
    }

    private static List<Entity> Sources<T>(Registry registry, Entity target) where T : struct, IRelation
    {
        var list = new List<Entity>();
        foreach (var s in registry.GetSources<T>(target))
        {
            list.Add(s);
        }

        return list;
    }

    [Fact]
    public void Relate_SetsTargetAndReverseIndex()
    {
        var registry = new Registry();
        Entity owner = registry.Create();
        Entity a = registry.Create();
        Entity b = registry.Create();

        registry.Relate<Owns>(a, owner);
        registry.Relate<Owns>(b, owner);

        Assert.Equal(owner, registry.GetTarget<Owns>(a));
        Assert.Equal(owner, registry.GetTarget<Owns>(b));
        Assert.Equal(2, registry.SourceCount<Owns>(owner));
        Assert.Equal(new HashSet<Entity> { a, b }, Sources<Owns>(registry, owner).ToHashSet());
    }

    [Fact]
    public void Relate_Again_ReplacesTarget()
    {
        var registry = new Registry();
        Entity first = registry.Create();
        Entity second = registry.Create();
        Entity item = registry.Create();

        registry.Relate<Owns>(item, first);
        registry.Relate<Owns>(item, second);

        Assert.Equal(second, registry.GetTarget<Owns>(item));
        Assert.Equal(0, registry.SourceCount<Owns>(first));
        Assert.Empty(Sources<Owns>(registry, first));
        Assert.Equal([item], Sources<Owns>(registry, second));
    }

    [Fact]
    public void Relate_SameTargetTwice_IsNoOp()
    {
        var registry = new Registry();
        Entity owner = registry.Create();
        Entity item = registry.Create();

        registry.Relate<Owns>(item, owner);
        registry.Relate<Owns>(item, owner);

        Assert.Equal(1, registry.SourceCount<Owns>(owner));
    }

    [Fact]
    public void Relate_DeadEntity_Throws()
    {
        var registry = new Registry();
        Entity owner = registry.Create();
        Entity item = registry.Create();
        registry.Destroy(owner);

        Assert.Throws<InvalidOperationException>(() => registry.Relate<Owns>(item, owner));
        Assert.Throws<InvalidOperationException>(() => registry.Relate<Owns>(owner, item));
    }

    [Fact]
    public void Unrelate_RemovesTarget()
    {
        var registry = new Registry();
        Entity owner = registry.Create();
        Entity item = registry.Create();
        registry.Relate<Owns>(item, owner);

        var first = registry.Unrelate<Owns>(item);
        var second = registry.Unrelate<Owns>(item);

        Assert.True(first);
        Assert.False(second);
        Assert.True(registry.GetTarget<Owns>(item).IsNull);
        Assert.Equal(0, registry.SourceCount<Owns>(owner));
    }

    [Fact]
    public void Kinds_AreIndependent()
    {
        var registry = new Registry();
        Entity owner = registry.Create();
        Entity parent = registry.Create();
        Entity item = registry.Create();

        registry.Relate<Owns>(item, owner);
        registry.Relate<Parent>(item, parent);

        Assert.Equal(owner, registry.GetTarget<Owns>(item));
        Assert.Equal(parent, registry.GetTarget<Parent>(item));
        Assert.Equal(0, registry.SourceCount<Parent>(owner));
    }

    [Fact]
    public void GetSources_UnrelatingCurrent_VisitsEverySource()
    {
        var registry = new Registry();
        Entity owner = registry.Create();
        var items = new HashSet<Entity>();
        for (var i = 0; i < 100; i++)
        {
            Entity item = registry.Create();
            registry.Relate<Owns>(item, owner);
            items.Add(item);
        }

        var visits = new List<Entity>();
        foreach (var s in registry.GetSources<Owns>(owner))
        {
            visits.Add(s);
            registry.Unrelate<Owns>(s);
        }

        Assert.Equal(visits.Count, visits.Distinct().Count());
        Assert.Equal(items, visits.ToHashSet());
        Assert.Equal(0, registry.SourceCount<Owns>(owner));
    }

    [Fact]
    public void GetSources_UnrelatingCurrentAcrossShrinksAndCompaction_VisitsEverySource()
    {
        var registry = new Registry();
        Entity owner = registry.Create();
        Entity other = registry.Create();
        var items = new HashSet<Entity>();
        for (var i = 0; i < 5000; i++)
        {
            Entity item = registry.Create();
            registry.Relate<Owns>(item, owner);
            items.Add(item);
        }

        Entity bystander = registry.Create();
        registry.Relate<Owns>(bystander, other);

        var visits = new List<Entity>();
        foreach (var s in registry.GetSources<Owns>(owner))
        {
            visits.Add(s);
            registry.Unrelate<Owns>(s);
        }

        Assert.Equal(visits.Count, visits.Distinct().Count());
        Assert.Equal(items, visits.ToHashSet());
        Assert.Equal([bystander], Sources<Owns>(registry, other));
    }

    [Fact]
    public void Unrelate_WideTarget_KeepsCapacity()
    {
        var registry = new Registry();
        Entity owner = registry.Create();
        var items = Enumerable.Range(0, 10_000).Select(_ => (Entity)registry.Create()).ToList();
        foreach (var item in items)
        {
            registry.Relate<Owns>(item, owner);
        }

        var relation = registry.Relations<Owns>();

        foreach (var item in items.Skip(5))
        {
            registry.Unrelate<Owns>(item);
        }

        Assert.True(relation.SlabCapacity >= 10_000, "unlink gave memory back");
        Assert.True(relation.SourceCapacity >= 10_000);
    }

    [Fact]
    public void ShrinkToFit_TrimsWideTargetThatEmptiedOut()
    {
        var registry = new Registry();
        Entity owner = registry.Create();
        var items = Enumerable.Range(0, 10_000).Select(_ => (Entity)registry.Create()).ToList();
        foreach (var item in items)
        {
            registry.Relate<Owns>(item, owner);
        }

        foreach (var item in items.Skip(5))
        {
            registry.Unrelate<Owns>(item);
        }

        var relation = registry.Relations<Owns>();

        relation.ShrinkToFit();

        Assert.Equal(64, relation.SlabCapacity);
        Assert.Equal(64, relation.SourceCapacity);
        Assert.Equal(5, relation.Sources.Length);
        Assert.Equal(items.Take(5).ToHashSet(), Sources<Owns>(registry, owner).ToHashSet());
    }

    // Ten sources on each of 1000 targets.
    private static List<Entity> RelateManyTargets(Registry registry)
    {
        var targets = Enumerable.Range(0, 1000).Select(_ => (Entity)registry.Create()).ToList();
        var items = new List<Entity>();
        foreach (var target in targets)
        {
            for (var i = 0; i < 10; i++)
            {
                Entity item = registry.Create();
                registry.Relate<Owns>(item, target);
                items.Add(item);
            }
        }

        return items;
    }

    // Leaves one source on every tenth target.
    private static void UnrelateMostSources(Registry registry, List<Entity> items)
    {
        foreach (var item in items.Where((_, k) => k % 100 != 0))
        {
            registry.Unrelate<Owns>(item);
        }
    }

    [Fact]
    public void Unrelate_KeepsSlabCapacity()
    {
        var registry = new Registry();
        var items = RelateManyTargets(registry);
        var relation = registry.Relations<Owns>();
        var full = relation.SlabCapacity;

        UnrelateMostSources(registry, items);

        Assert.Equal(full, relation.SlabCapacity);
    }

    [Fact]
    public void Compact_KeepsSlabCapacity()
    {
        var registry = new Registry();
        var items = RelateManyTargets(registry);
        var relation = registry.Relations<Owns>();
        var full = relation.SlabCapacity;
        UnrelateMostSources(registry, items);

        relation.Compact();

        Assert.Equal(full, relation.SlabCapacity);
    }

    [Fact]
    public void ShrinkToFit_TrimsSlabAcrossManyTargets()
    {
        var registry = new Registry();
        var items = RelateManyTargets(registry);
        var relation = registry.Relations<Owns>();
        UnrelateMostSources(registry, items);
        relation.Compact();

        registry.ShrinkToFit();

        Assert.Equal(100, relation.SlabCapacity); // one source on each of 100 targets
        Assert.Equal(100, relation.Sources.Length);
        foreach (var source in relation.Sources)
        {
            Assert.Equal([source], Sources<Owns>(registry, relation.GetTarget(source)));
        }
    }

    [Fact]
    public void DestroyTarget_UnrelatePolicy_KeepsSources()
    {
        var registry = new Registry();
        Entity owner = registry.Create();
        var items = Enumerable.Range(0, 300).Select(_ => (Entity)registry.Create()).ToList();
        foreach (var item in items)
        {
            registry.Relate<Owns>(item, owner);
        }

        registry.Destroy(owner);

        Assert.All(items, item =>
        {
            Assert.True(registry.IsAlive(item));
            Assert.True(registry.GetTarget<Owns>(item).IsNull);
        });
        Assert.Empty(registry.Relations<Owns>().Sources.ToArray());
    }

    // Forty items split between doomed and kept, plus doomed itself on kept; returns kept's items.
    private static List<Entity> RelateAlternating(Registry registry, Entity doomed, Entity kept)
    {
        var keptItems = new List<Entity>();
        for (var i = 0; i < 40; i++)
        {
            Entity item = registry.Create();
            registry.Relate<Owns>(item, i % 2 == 0 ? doomed : kept);
            if (i % 2 != 0)
            {
                keptItems.Add(item);
            }
        }

        registry.Relate<Owns>(doomed, kept);
        return keptItems;
    }

    [Fact]
    public void DestroyTarget_UnrelatePolicy_LeavesOtherTargetsIntact()
    {
        var registry = new Registry();
        Entity doomed = registry.Create();
        Entity kept = registry.Create();
        var keptItems = RelateAlternating(registry, doomed, kept);

        registry.Destroy(doomed);

        Assert.Equal(keptItems.ToHashSet(), Sources<Owns>(registry, kept).ToHashSet());
        Assert.Equal(keptItems.ToHashSet(), registry.Relations<Owns>().Sources.ToArray().ToHashSet());
    }

    [Fact]
    public void DestroyTarget_UnrelatePolicy_SurvivingEdgesStillUnrelate()
    {
        var registry = new Registry();
        Entity doomed = registry.Create();
        Entity kept = registry.Create();
        var keptItems = RelateAlternating(registry, doomed, kept);
        registry.Destroy(doomed);

        var results = new List<bool>();
        foreach (var item in keptItems)
        {
            results.Add(registry.Unrelate<Owns>(item));
        }

        Assert.All(results, r => Assert.True(r)); // edge slots still pair with the in-list
        Assert.Equal(0, registry.SourceCount<Owns>(kept));
        Assert.Equal(0, registry.Relations<Owns>().SlabUsed);
    }

    [Fact]
    public void DestroyTarget_DestroySourcesPolicy_Cascades()
    {
        var registry = new Registry();
        Entity root = registry.Create();
        Entity child = registry.Create();
        Entity grandchild = registry.Create();
        Entity bystander = registry.Create();
        registry.Relate<Parent>(child, root);
        registry.Relate<Parent>(grandchild, child);

        registry.Destroy(root);

        Assert.False(registry.IsAlive(child));
        Assert.False(registry.IsAlive(grandchild));
        Assert.True(registry.IsAlive(bystander));
        Assert.Empty(registry.Relations<Parent>().Sources.ToArray());
    }

    [Fact]
    public void DestroyTarget_DestroySourcesPolicy_CascadesWideTree()
    {
        var registry = new Registry();
        Entity root = registry.Create();
        var tree = new List<Entity>();
        for (var i = 0; i < 100; i++)
        {
            Entity child = registry.Create();
            registry.Relate<Parent>(child, root);
            tree.Add(child);
            for (var j = 0; j < 3; j++)
            {
                Entity grandchild = registry.Create();
                registry.Relate<Parent>(grandchild, child);
                tree.Add(grandchild);
            }
        }

        Entity otherRoot = registry.Create();
        Entity survivor = registry.Create();
        registry.Relate<Parent>(survivor, otherRoot);

        registry.Destroy(root);

        Assert.All(tree, e => Assert.False(registry.IsAlive(e)));
        Assert.Equal([survivor], Sources<Parent>(registry, otherRoot));
        Assert.Equal(1, registry.Relations<Parent>().Sources.Length);
    }

    [Fact]
    public void DestroySource_UnrelatesFromTarget()
    {
        var registry = new Registry();
        Entity owner = registry.Create();
        Entity a = registry.Create();
        Entity b = registry.Create();
        registry.Relate<Owns>(a, owner);
        registry.Relate<Owns>(b, owner);

        registry.Destroy(a);

        Assert.Equal(1, registry.SourceCount<Owns>(owner));
        Assert.Equal([b], Sources<Owns>(registry, owner));
    }

    [Fact]
    public void StaleHandles_Miss()
    {
        var registry = new Registry();
        Entity owner = registry.Create();
        Entity item = registry.Create();
        registry.Relate<Owns>(item, owner);
        registry.Destroy(owner);
        registry.Destroy(item);
        Entity newOwner = registry.Create(); // may recycle an index
        Entity newItem = registry.Create();

        Assert.True(registry.GetTarget<Owns>(item).IsNull);
        Assert.Equal(0, registry.SourceCount<Owns>(owner));
        Assert.True(registry.GetTarget<Owns>(newItem).IsNull);
        Assert.Equal(0, registry.SourceCount<Owns>(newOwner));
    }

    [Fact]
    public void TryGetTarget_Unrelated_Misses()
    {
        var registry = new Registry();
        registry.Create();
        var item = registry.Create();

        var found = item.TryGetTarget<Owns>(out var none);

        Assert.False(found);
        Assert.True(none.IsNull);
    }

    [Fact]
    public void TryGetTarget_Related_ReportsTarget()
    {
        var registry = new Registry();
        Entity owner = registry.Create();
        var item = registry.Create();
        item.Relate<Owns>(owner);

        var found = registry.TryGetTarget<Owns>(item, out var target);
        var foundInSet = registry.Relations<Owns>().TryGetTarget(item, out var setTarget);

        Assert.True(found);
        Assert.Equal(owner, target);
        Assert.True(foundInSet);
        Assert.Equal(owner, setTarget);
    }

    [Fact]
    public void TryGetTarget_AfterTargetDestroyed_Misses()
    {
        var registry = new Registry();
        Entity owner = registry.Create();
        var item = registry.Create();
        item.Relate<Owns>(owner);
        registry.Destroy(owner);

        var found = registry.TryGetTarget<Owns>(item, out var target);
        var foundInSet = registry.Relations<Owns>().TryGetTarget(item, out _);

        Assert.False(found);
        Assert.True(target.IsNull);
        Assert.False(foundInSet);
    }

    // Random link / unlink / relink / destroy against a dictionary model.
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void RandomOperations_MatchModel(int seed)
    {
        var rng = new Random(seed);
        var registry = new Registry();
        var live = Enumerable.Range(0, 200).Select(_ => (Entity)registry.Create()).ToList();
        var model = new Dictionary<Entity, Entity>(); // source -> target

        for (var step = 0; step < 20_000; step++)
        {
            var source = live[rng.Next(live.Count)];
            // Few targets, so some get hundreds of sources and blocks grow and shrink.
            var target = live[rng.Next(Math.Min(8, live.Count))];
            switch (rng.Next(10))
            {
                case < 6:
                    registry.Relate<Owns>(source, target);
                    model[source] = target;
                    break;
                case < 9:
                    Assert.Equal(model.Remove(source), registry.Unrelate<Owns>(source));
                    break;
                default:
                    registry.Destroy(source);
                    live.Remove(source);
                    model.Remove(source);
                    foreach (var s in model.Where(kv => kv.Value == source).Select(kv => kv.Key).ToList())
                    {
                        model.Remove(s);
                    }

                    live.Add(registry.Create());
                    break;
            }

            if (step % 500 == 0)
            {
                AssertMatches(registry, live, model);
            }
        }

        AssertMatches(registry, live, model);
    }

    private static void AssertMatches(Registry registry, List<Entity> live, Dictionary<Entity, Entity> model)
    {
        foreach (var e in live)
        {
            Assert.Equal(model.TryGetValue(e, out var t) ? t : Entity.Null, registry.GetTarget<Owns>(e));
            var expected = model.Where(kv => kv.Value == e).Select(kv => kv.Key).ToHashSet();
            Assert.Equal(expected.Count, registry.SourceCount<Owns>(e));
            Assert.Equal(expected, Sources<Owns>(registry, e).ToHashSet());
        }

        Assert.Equal(model.Count, registry.Relations<Owns>().Sources.Length);
    }
}
