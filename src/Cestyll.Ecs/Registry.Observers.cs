using Cestyll.Ecs.Observers;

namespace Cestyll.Ecs;

public partial class Registry<TEntity>
    where TEntity : unmanaged, IEntity<TEntity>
{
    private readonly List<Observer<TEntity>> _observerList = []; // walked by Destroy

    public Observer<TEntity> Observer()
    {
        var observer = new Observer<TEntity>(this);
        _observerList.Add(observer);
        return observer;
    }

    public Requirements<TEntity> Requirements() => new(this);

    internal bool Observing(Observer<TEntity> observer) => _observerList.Contains(observer);
    internal void Unobserve(Observer<TEntity> observer) => _observerList.Remove(observer);
}
