namespace Cestyll.Ecs.Storage;

// A store whose reads look at pending records while a defer scope is open.
internal interface IPendingReads
{
    bool PendingReads { get; set; }
}
