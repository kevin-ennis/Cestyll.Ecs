using Cestyll.Ecs;
using Cestyll.Ecs.Tags;

namespace Cestyll.Ecs.Tests;

// Tag reads inside a defer scope find what is recorded for an entity created in that scope.
public class PendingTagTests
{
    private struct Position : IComponent;
    private struct Speed : IComponent { public float Value; }
    private struct Elite : ITag;
    private struct Enraged : ITag;

    // Shaped like a factory step that must apply once: guarded on the tag it sets.
    private static void MakeElite(Registry registry, Entity e)
    {
        if (registry.HasTag<Elite>(e))
        {
            return;
        }

        registry.Tag<Elite>(e);
        registry.Get<Speed>(e).Value *= 2f;
    }

    [Fact]
    public void TagSetContains_ChecksLiveness()
    {
        var registry = new Registry();
        Entity stale = registry.Create();
        registry.Destroy(stale);
        Entity e = registry.Create();
        registry.Tag<Elite>(e);
        var tags = registry.Tags<Elite>();

        var live = tags.Contains(e);
        var old = tags.Contains(stale);

        Assert.Equal(stale.Index, e.Index);
        Assert.True(live);
        Assert.False(old); // same index, older version
    }

    [Fact]
    public void TagSetContains_SeesAppliedTagsOnly()
    {
        var registry = new Registry();
        var tags = registry.Tags<Elite>();
        bool hasTag;
        bool contains;

        using (registry.Defer())
        {
            Entity born = registry.Create();
            registry.Tag<Elite>(born);
            hasTag = registry.HasTag<Elite>(born);
            contains = tags.Contains(born);
        }

        Assert.True(hasTag);
        Assert.False(contains);
    }

    [Fact]
    public void HasTag_SeesARecordedTag_SoAGuardAppliesOnce()
    {
        var registry = new Registry();
        Entity e;
        bool hasTag;
        bool handleHasTag;
        bool inSet;

        using (registry.Defer())
        {
            e = registry.Create(new Speed { Value = 1f });
            MakeElite(registry, e);
            MakeElite(registry, e);
            hasTag = registry.HasTag<Elite>(e);
            handleHasTag = registry.Handle(e).HasTag<Elite>();
            inSet = registry.Tags<Elite>().Test(e.Index);
        }

        Assert.True(hasTag);
        Assert.True(handleHasTag);
        Assert.False(inSet); // still only recorded
        Assert.Equal(2f, registry.Get<Speed>(e).Value);
        Assert.True(registry.HasTag<Elite>(e));
    }

    [Fact]
    public void LatestRecordWins()
    {
        var registry = new Registry();
        bool hasTag;

        using (registry.Defer())
        {
            Entity e = registry.Create();
            registry.Tag<Enraged>(e);
            registry.Untag<Enraged>(e);
            hasTag = registry.HasTag<Enraged>(e);
        }

        Assert.False(hasTag);
    }

    [Fact]
    public void SeveralTagsReadTogether()
    {
        var registry = new Registry();
        bool allSame;
        bool any;
        bool all;

        using (registry.Defer())
        {
            Entity e = registry.Create();
            registry.Tag<Enraged>(e);
            registry.Untag<Enraged>(e);
            registry.Tag<Elite>(e);
            allSame = registry.HasAllTags<Elite, Elite>(e);
            any = registry.HasAnyTags<Enraged, Elite>(e);
            all = registry.HasAllTags<Enraged, Elite>(e);
        }

        Assert.True(allSame);
        Assert.True(any);
        Assert.False(all);
    }

    [Fact]
    public void BoolResults_AnswerAsAReadJustBeforeTheCall()
    {
        var registry = new Registry();
        bool firstTag;
        bool secondTag;
        bool firstUntag;
        bool secondUntag;
        bool firstRemove;
        bool secondRemove;

        using (registry.Defer())
        {
            Entity e = registry.Create(new Speed());
            firstTag = registry.Tag<Elite>(e);
            secondTag = registry.Tag<Elite>(e);
            firstUntag = registry.Untag<Elite>(e);
            secondUntag = registry.Untag<Elite>(e);
            firstRemove = registry.Remove<Speed>(e);
            secondRemove = registry.Remove<Speed>(e);
        }

        Assert.True(firstTag);    // lacked it
        Assert.False(secondTag);  // the first is recorded
        Assert.True(firstUntag);  // had it, as recorded
        Assert.False(secondUntag);
        Assert.True(firstRemove); // had it, as recorded
        Assert.False(secondRemove);
    }

    [Fact]
    public void OlderEntities_ReadTheTagSetAsItIs()
    {
        var registry = new Registry();
        Entity old = registry.Create(new Position());
        bool hasTag;

        using (registry.Defer())
        {
            registry.Create();
            registry.Tag<Elite>(old);
            hasTag = registry.HasTag<Elite>(old);
        }

        Assert.False(hasTag);
        Assert.True(registry.HasTag<Elite>(old));
    }
}
