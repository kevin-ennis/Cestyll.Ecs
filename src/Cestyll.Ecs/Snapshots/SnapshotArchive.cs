namespace Cestyll.Ecs.Snapshots;

// What a snapshot section holds; BeginSection also gets its type and, for a named pool, its name.
public enum SnapshotSection
{
    Entities,
    Component,
    Relation,
    Link,
    Attachment,
    Tag,
    Context,
    Inbox,
}

// User-implemented archive a Snapshot writes to; the format is the implementation's.
// Write<T> must handle every non-component type written; entity fields inside values are its job.
// WriteComponents gets a whole span, so reference-free T can be byte-copied (padding included).
public interface ISnapshotWriter<TEntity> where TEntity : unmanaged, IEntity<TEntity>
{
    void BeginSection(SnapshotSection kind, Type type, string? name);
    void EndSection();
    void WriteCount(int count);
    void WriteEntity(TEntity entity);
    void Write<T>(in T value);
    void WriteComponents<T>(ReadOnlySpan<T> values);
}

// Archive a loader reads from, in the writer's call order. BeginSection names the expected section;
// EndSection runs before anything is applied: throw there on leftover data. Results are validated.
public interface ISnapshotReader<TEntity> where TEntity : unmanaged, IEntity<TEntity>
{
    void BeginSection(SnapshotSection kind, Type type, string? name);
    void EndSection();
    int ReadCount();
    TEntity ReadEntity();
    T Read<T>();
    void ReadComponents<T>(Span<T> values);
}
