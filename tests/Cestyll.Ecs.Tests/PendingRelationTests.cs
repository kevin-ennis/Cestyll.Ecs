using Cestyll.Ecs;
using Cestyll.Ecs.Relations;

namespace Cestyll.Ecs.Tests;

// GetTarget inside a defer scope finds what is recorded for a source created in that scope.
public class PendingRelationTests
{
    private struct Position : IComponent;

    private struct Orbits : IRelation
    {
        public static OnTargetDestroyed Policy => OnTargetDestroyed.Unrelate;
    }

    [Fact]
    public void GetTarget_SeesARecordedLink_ForASourceBornInTheScope()
    {
        var registry = new Registry();
        Entity pilot = registry.Create(new Position());
        Entity blade;
        Entity target;

        using (registry.Defer())
        {
            blade = registry.Create(new Position()).Relate<Orbits>(pilot);
            target = registry.GetTarget<Orbits>(blade);
        }

        Assert.Equal(pilot, target);
        Assert.Equal(pilot, registry.GetTarget<Orbits>(blade));
    }

    [Fact]
    public void GetTarget_SeesARecordedRelink()
    {
        var registry = new Registry();
        Entity pilot = registry.Create(new Position());
        Entity other = registry.Create(new Position());
        Entity target;

        using (registry.Defer())
        {
            Entity blade = registry.Create(new Position()).Relate<Orbits>(pilot);
            registry.Relate<Orbits>(blade, other); // relinking replaces
            target = registry.GetTarget<Orbits>(blade);
        }

        Assert.Equal(other, target);
    }

    [Fact]
    public void Unrelate_AnswersFromTheRecordedLink()
    {
        var registry = new Registry();
        Entity pilot = registry.Create(new Position());
        bool first;
        bool targetIsNull;
        bool second;

        using (registry.Defer())
        {
            Entity blade = registry.Create(new Position()).Relate<Orbits>(pilot);
            first = registry.Unrelate<Orbits>(blade);
            targetIsNull = registry.GetTarget<Orbits>(blade).IsNull;
            second = registry.Unrelate<Orbits>(blade);
        }

        Assert.True(first); // was linked, as recorded
        Assert.True(targetIsNull);
        Assert.False(second);
    }

    [Fact]
    public void SourceCount_AnswersFromAppliedLinks_UntilTheScopeEnds()
    {
        var registry = new Registry();
        Entity pilot = registry.Create(new Position());
        int inScope;

        using (registry.Defer())
        {
            registry.Create(new Position()).Relate<Orbits>(pilot);
            registry.Create(new Position()).Relate<Orbits>(pilot);
            inScope = registry.SourceCount<Orbits>(pilot);
        }

        Assert.Equal(0, inScope);
        Assert.Equal(2, registry.SourceCount<Orbits>(pilot));
    }

    [Fact]
    public void OlderSources_ReadTheRelationAsItIs()
    {
        var registry = new Registry();
        Entity pilot = registry.Create(new Position());
        Entity old = registry.Create(new Position());
        bool targetIsNull;

        using (registry.Defer())
        {
            registry.Create();
            registry.Relate<Orbits>(old, pilot);
            targetIsNull = registry.GetTarget<Orbits>(old).IsNull;
        }

        Assert.True(targetIsNull);
        Assert.Equal(pilot, registry.GetTarget<Orbits>(old));
    }
}
