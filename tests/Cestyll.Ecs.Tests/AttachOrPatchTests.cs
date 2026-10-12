using Cestyll.Ecs;
using Cestyll.Ecs.Attachments;

namespace Cestyll.Ecs.Tests;

public class AttachOrPatchTests
{
    private struct Position : IComponent;

    private enum StatusKind { Burn, Chill }

    private struct Status : IAttachment<StatusKind>
    {
        public StatusKind Kind;
        public float Remaining;
        public float Strength;
        public float Tick;
        public readonly StatusKind Key => Kind;
    }

    private static Status Burn(float dps) => new() { Kind = StatusKind.Burn, Remaining = 3f, Strength = dps };

    // Static: the incoming burn arrives as an argument, so nothing is captured.
    private static readonly MergeAction<Status> _refresh = static (ref Status s, in Status fresh) =>
    {
        s.Remaining = MathF.Max(s.Remaining, fresh.Remaining);
        s.Strength = MathF.Max(s.Strength, fresh.Strength);
    };

    [Fact]
    public void Missing_Attaches_AndFiresOnAttach()
    {
        var registry = new Registry();
        Entity e = registry.Create(new Position());
        var statuses = registry.Attachments<Status, StatusKind>();
        var attached = 0;
        var updated = 0;
        statuses.OnAttach += (_, _) => attached++;
        statuses.OnUpdate += (_, _) => updated++;

        statuses.AttachOrPatch(e, Burn(2f), _refresh).Tick = 0.5f;

        Assert.Equal((1, 0), (attached, updated));
        Assert.Equal(2f, statuses.Get(e, StatusKind.Burn).Strength);
        Assert.Equal(0.5f, statuses.Get(e, StatusKind.Burn).Tick);
    }

    [Fact]
    public void Present_PatchesInPlace_KeepsOtherFields_AndFiresOnUpdate()
    {
        var registry = new Registry();
        Entity e = registry.Create(new Position());
        var statuses = registry.Attachments<Status, StatusKind>();
        statuses.Attach(e, Burn(2f)).Tick = 0.4f;
        statuses.Get(e, StatusKind.Burn).Remaining = 1f;
        var attached = 0;
        var updated = 0;
        statuses.OnAttach += (_, _) => attached++;
        statuses.OnUpdate += (_, _) => updated++;

        registry.AttachOrPatch<Status, StatusKind>(e, Burn(5f), _refresh);

        var burn = statuses.Get(e, StatusKind.Burn);
        Assert.Equal((0, 1), (attached, updated));
        Assert.Equal(3f, burn.Remaining);
        Assert.Equal(5f, burn.Strength);
        Assert.Equal(0.4f, burn.Tick);
    }

    [Fact]
    public void DeadEntity_Throws()
    {
        var registry = new Registry();
        Entity e = registry.Create(new Position());
        registry.Destroy(e);

        Assert.Throws<InvalidOperationException>(() => registry.AttachOrPatch<Status, StatusKind>(e, Burn(1f), _refresh));
    }

    [Fact]
    public void InsideDefer_PatchRunsNow_AttachWaits()
    {
        var registry = new Registry();
        Entity a = registry.Create(new Position());
        Entity b = registry.Create(new Position());
        registry.Attach<Status, StatusKind>(a, Burn(1f));

        using (registry.Defer())
        {
            registry.AttachOrPatch<Status, StatusKind>(a, Burn(5f), _refresh);
            registry.AttachOrPatch<Status, StatusKind>(b, Burn(1f), _refresh);

            Assert.Equal(5f, registry.Attachments<Status, StatusKind>().Get(a, StatusKind.Burn).Strength);
            Assert.False(registry.Attachments<Status, StatusKind>().Has(b));
        }

        Assert.True(registry.Attachments<Status, StatusKind>().Has(b, StatusKind.Burn));
    }

    [Fact]
    public void Refresh_AllocatesNothing()
    {
        var registry = new Registry();
        Entity e = registry.Create(new Position());
        var statuses = registry.Attachments<Status, StatusKind>();
        statuses.Attach(e, Burn(1f));
        statuses.AttachOrPatch(e, Burn(2f), _refresh); // warm up

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 100; i++)
        {
            statuses.AttachOrPatch(e, Burn(i), _refresh);
        }

        Assert.Equal(before, GC.GetAllocatedBytesForCurrentThread());
        Assert.Equal(99f, statuses.Get(e, StatusKind.Burn).Strength);
    }
}
