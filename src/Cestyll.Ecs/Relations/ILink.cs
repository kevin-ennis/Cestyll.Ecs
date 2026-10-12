namespace Cestyll.Ecs.Relations;

// One link's data. A source holds many links, at most one per target.
// Target must not change once linked (the reverse index is keyed by it); unlink + link to retarget.
public interface ILink<TEntity>
{
    TEntity Target { get; }
}
