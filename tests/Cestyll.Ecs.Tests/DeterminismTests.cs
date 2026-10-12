using Cestyll.Ecs;
using Cestyll.Ecs.Relations;
using Cestyll.Ecs.Snapshots;
using Cestyll.Ecs.Tags;

namespace Cestyll.Ecs.Tests;

// Same calls walk alike, and a pool loads in saved order. Replays and lockstep rely on it.
public class DeterminismTests
{
    private struct Position : IComponent { public int X; }
    private struct Velocity : IComponent { public int X; }
    private struct Health : IComponent { public int Hp; }
    private struct Dead : ITag;
    private struct Enemy : ITag;

    private struct Follows : IRelation
    {
        public static OnTargetDestroyed Policy => OnTargetDestroyed.Unrelate;
    }

    private static readonly SnapshotSchema<Entity> _schema = new SnapshotSchema<Entity>()
        .Component<Position>().Component<Velocity>().Component<Health>()
        .Tag<Dead>().Tag<Enemy>().Relation<Follows>();

    // Seeded churn, so indices recycle and pools swap-and-pop.
    private static Registry Churned(int seed, bool group)
    {
        var registry = new Registry();
        if (group)
        {
            registry.Group().Own<Position>().Own<Velocity>().Build();
        }

        var random = new Random(seed);
        var live = new List<Entity>();
        for (var step = 0; step < 2000; step++)
        {
            var roll = random.Next(10);
            if (roll < 4 || live.Count == 0)
            {
                Entity e = registry.Create(new Position { X = random.Next(1000) });
                if (random.Next(2) == 0)
                {
                    registry.Add(e, new Velocity { X = random.Next(10) });
                }

                if (random.Next(3) == 0)
                {
                    registry.Tag<Enemy>(e);
                }

                live.Add(e);
                continue;
            }

            var target = live[random.Next(live.Count)];
            switch (roll)
            {
                case 4:
                    registry.Destroy(target);
                    live.Remove(target);
                    break;
                case 5:
                    registry.Remove<Position>(target);
                    break;
                case 6:
                    registry.AddOrReplace(target, new Health { Hp = random.Next(100) });
                    break;
                case 7:
                    registry.Tag<Dead>(target);
                    break;
                case 8:
                    registry.Relate<Follows>(target, live[random.Next(live.Count)]);
                    break;
                default:
                    registry.GetOrAdd<Position>(target).X++;
                    break;
            }
        }

        registry.Sort<Health>((a, b) => a.Hp - b.Hp);
        return registry;
    }

    // Every walk the guarantee covers, flattened into one list.
    private static List<string> Walks(Registry registry)
    {
        var order = new List<string>();
        foreach (var row in registry.View().Rows<Position>())
        {
            order.Add($"P {row.Entity} {row.C1.X}");
        }

        registry.View().WithoutTag<Dead>().Each((Entity e, ref Position p, ref Velocity _) =>
            order.Add($"PV {e} {p.X}"));
        registry.View().Each((Entity e, ref Health h) => order.Add($"H {e} {h.Hp}"));
        registry.View().WithTag<Enemy>().WithoutTag<Dead>().Each((Entity e, ref Position _) =>
            order.Add($"PE {e}"));
        foreach (var e in registry.View().WithTag<Enemy>().Rows())
        {
            order.Add($"T {e}");
        }

        foreach (var e in registry.Entities)
        {
            order.Add($"E {e}");
        }

        return order;
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    public void SameCalls_SameIterationOrder(int seed, bool group)
    {
        var a = Churned(seed, group);
        var b = Churned(seed, group);

        Assert.Equal(Walks(a), Walks(b));
    }

    [Fact]
    public void SameSnapshot_LoadedTwice_WalksAlike_AndAsSaved()
    {
        var source = Churned(7, group: false);
        var archive = new Archive();
        _schema.Save(source, ref archive);

        var first = new Registry();
        var reader = archive.Reopen();
        _schema.Load(first, ref reader);
        var second = new Registry();
        reader = archive.Reopen();
        _schema.Load(second, ref reader);

        Assert.Equal(Walks(first), Walks(second));
        Assert.Equal(Walks(source), Walks(first));
    }
}
