using Cestyll.Ecs.Views;

namespace Cestyll.Ecs;

public partial class Registry<TEntity>
    where TEntity : unmanaged, IEntity<TEntity>
{
    public View<TEntity> View() => new(new ViewQuery<TEntity>(this));
}
