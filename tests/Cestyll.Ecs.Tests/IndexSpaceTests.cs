using Cestyll.Ecs;

namespace Cestyll.Ecs.Tests;

public class IndexSpaceTests
{
    // Entity32 holds 2^20 - 1 live entities; the next Create says so and names Entity64.
    [Fact]
    public void Create_PastTheLimit_NamesItAndEntity64()
    {
        var registry = new Registry<Entity32>();
        var max = GetMax<Entity32>();
        registry.CreateMany(max);

        var ex = Assert.Throws<InvalidOperationException>(() => registry.Create());

        Assert.Contains(max.ToString("N0"), ex.Message);
        Assert.Contains("Registry<Entity64>", ex.Message);
    }

    // A freed index is reused, so the registry keeps working below the limit.
    [Fact]
    public void Create_AfterExhaustion_ReusesAFreedIndex()
    {
        var registry = new Registry<Entity32>();
        registry.CreateMany(GetMax<Entity32>());
        try
        {
            registry.Create();
        }
        catch (InvalidOperationException)
        {
        }
        registry.Destroy(Entity32.Make(5, 0));

        var index = registry.Create().Entity.Index;

        Assert.Equal(5, index);
    }

    private static int GetMax<TEntity>() where TEntity : unmanaged, IEntity<TEntity> => TEntity.MaxIndex;
}
