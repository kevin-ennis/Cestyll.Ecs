namespace Cestyll.Ecs.Relations;

// Relation and link-set signal: the source and target of the pair.
public delegate void PairHandler<TEntity>(TEntity source, TEntity target);
