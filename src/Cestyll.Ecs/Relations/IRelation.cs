namespace Cestyll.Ecs.Relations;

public enum OnTargetDestroyed
{
    Unrelate,       // sources survive, their target is removed (targeting, ownership)
    DestroySources, // sources are destroyed too, recursively (hierarchy, containment)
}

// TRelation.Policy folds to a constant.
public interface IRelation
{
    static abstract OnTargetDestroyed Policy { get; }
}
