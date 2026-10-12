using System.Buffers;
using Cestyll.Ecs.Tags;

namespace Cestyll.Ecs;

// Bulk operations; each matches the single-entity call, signals included.
public partial class Registry<TEntity>
    where TEntity : unmanaged, IEntity<TEntity>
{
    // Not Create: Create<T1>(T1) would bind an array argument as a component.
    public void CreateMany(Span<TEntity> entities)
    {
        for (var i = 0; i < entities.Length; i++)
        {
            entities[i] = CreateEntity();
        }
    }

    public TEntity[] CreateMany(int count)
    {
        var entities = new TEntity[count];
        CreateMany(entities);
        return entities;
    }

    // Grows versions once; returns one past the highest index count new entities can take.
    private int ReserveCreates(int count)
    {
        var fresh = Math.Max(0, count - _freeCount);
        var bound = _created + fresh;
        if (bound > _versions.Length)
        {
            Array.Resize(ref _versions, (int)Math.Min(Array.MaxLength, Math.Max(bound, _versions.Length * 2L)));
        }

        return bound;
    }

    // Versions and every pool grow once up front, so one pass, no mid-batch reallocation.
    public void CreateMany<T1>(Span<TEntity> entities, T1 c1)
        where T1 : IComponent
    {
        if (_deferDepth > 0)
        {
            for (var i = 0; i < entities.Length; i++)
            {
                entities[i] = Create(c1);
            }

            return;
        }

        var bound = ReserveCreates(entities.Length);
        var p1 = Storage<T1>();
        p1.ReserveBatch(entities.Length, bound);
        for (var i = 0; i < entities.Length; i++)
        {
            var e = CreateEntity();
            entities[i] = e;
            p1.Add(e, c1);
        }
    }

    public TEntity[] CreateMany<T1>(int count, T1 c1)
        where T1 : IComponent
    {
        var entities = new TEntity[count];
        CreateMany(entities, c1);
        return entities;
    }

    public void CreateMany<T1, T2>(Span<TEntity> entities, T1 c1, T2 c2)
        where T1 : IComponent
        where T2 : IComponent
    {
        if (_deferDepth > 0)
        {
            for (var i = 0; i < entities.Length; i++)
            {
                entities[i] = Create(c1, c2);
            }

            return;
        }

        var bound = ReserveCreates(entities.Length);
        var p1 = Storage<T1>();
        var p2 = Storage<T2>();
        p1.ReserveBatch(entities.Length, bound);
        p2.ReserveBatch(entities.Length, bound);
        for (var i = 0; i < entities.Length; i++)
        {
            var e = CreateEntity();
            entities[i] = e;
            p1.Add(e, c1);
            p2.Add(e, c2);
        }
    }

    public TEntity[] CreateMany<T1, T2>(int count, T1 c1, T2 c2)
        where T1 : IComponent
        where T2 : IComponent
    {
        var entities = new TEntity[count];
        CreateMany(entities, c1, c2);
        return entities;
    }

    public void CreateMany<T1, T2, T3>(Span<TEntity> entities, T1 c1, T2 c2, T3 c3)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
    {
        if (_deferDepth > 0)
        {
            for (var i = 0; i < entities.Length; i++)
            {
                entities[i] = Create(c1, c2, c3);
            }

            return;
        }

        var bound = ReserveCreates(entities.Length);
        var p1 = Storage<T1>();
        var p2 = Storage<T2>();
        var p3 = Storage<T3>();
        p1.ReserveBatch(entities.Length, bound);
        p2.ReserveBatch(entities.Length, bound);
        p3.ReserveBatch(entities.Length, bound);
        for (var i = 0; i < entities.Length; i++)
        {
            var e = CreateEntity();
            entities[i] = e;
            p1.Add(e, c1);
            p2.Add(e, c2);
            p3.Add(e, c3);
        }
    }

    public TEntity[] CreateMany<T1, T2, T3>(int count, T1 c1, T2 c2, T3 c3)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
    {
        var entities = new TEntity[count];
        CreateMany(entities, c1, c2, c3);
        return entities;
    }

    public void CreateMany<T1, T2, T3, T4>(Span<TEntity> entities, T1 c1, T2 c2, T3 c3, T4 c4)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
    {
        if (_deferDepth > 0)
        {
            for (var i = 0; i < entities.Length; i++)
            {
                entities[i] = Create(c1, c2, c3, c4);
            }

            return;
        }

        var bound = ReserveCreates(entities.Length);
        var p1 = Storage<T1>();
        var p2 = Storage<T2>();
        var p3 = Storage<T3>();
        var p4 = Storage<T4>();
        p1.ReserveBatch(entities.Length, bound);
        p2.ReserveBatch(entities.Length, bound);
        p3.ReserveBatch(entities.Length, bound);
        p4.ReserveBatch(entities.Length, bound);
        for (var i = 0; i < entities.Length; i++)
        {
            var e = CreateEntity();
            entities[i] = e;
            p1.Add(e, c1);
            p2.Add(e, c2);
            p3.Add(e, c3);
            p4.Add(e, c4);
        }
    }

    public TEntity[] CreateMany<T1, T2, T3, T4>(int count, T1 c1, T2 c2, T3 c3, T4 c4)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
    {
        var entities = new TEntity[count];
        CreateMany(entities, c1, c2, c3, c4);
        return entities;
    }

    public void CreateMany<T1, T2, T3, T4, T5>(Span<TEntity> entities, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
    {
        if (_deferDepth > 0)
        {
            for (var i = 0; i < entities.Length; i++)
            {
                entities[i] = Create(c1, c2, c3, c4, c5);
            }

            return;
        }

        var bound = ReserveCreates(entities.Length);
        var p1 = Storage<T1>();
        var p2 = Storage<T2>();
        var p3 = Storage<T3>();
        var p4 = Storage<T4>();
        var p5 = Storage<T5>();
        p1.ReserveBatch(entities.Length, bound);
        p2.ReserveBatch(entities.Length, bound);
        p3.ReserveBatch(entities.Length, bound);
        p4.ReserveBatch(entities.Length, bound);
        p5.ReserveBatch(entities.Length, bound);
        for (var i = 0; i < entities.Length; i++)
        {
            var e = CreateEntity();
            entities[i] = e;
            p1.Add(e, c1);
            p2.Add(e, c2);
            p3.Add(e, c3);
            p4.Add(e, c4);
            p5.Add(e, c5);
        }
    }

    public TEntity[] CreateMany<T1, T2, T3, T4, T5>(int count, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
    {
        var entities = new TEntity[count];
        CreateMany(entities, c1, c2, c3, c4, c5);
        return entities;
    }

    public void CreateMany<T1, T2, T3, T4, T5, T6>(Span<TEntity> entities, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
    {
        if (_deferDepth > 0)
        {
            for (var i = 0; i < entities.Length; i++)
            {
                entities[i] = Create(c1, c2, c3, c4, c5, c6);
            }

            return;
        }

        var bound = ReserveCreates(entities.Length);
        var p1 = Storage<T1>();
        var p2 = Storage<T2>();
        var p3 = Storage<T3>();
        var p4 = Storage<T4>();
        var p5 = Storage<T5>();
        var p6 = Storage<T6>();
        p1.ReserveBatch(entities.Length, bound);
        p2.ReserveBatch(entities.Length, bound);
        p3.ReserveBatch(entities.Length, bound);
        p4.ReserveBatch(entities.Length, bound);
        p5.ReserveBatch(entities.Length, bound);
        p6.ReserveBatch(entities.Length, bound);
        for (var i = 0; i < entities.Length; i++)
        {
            var e = CreateEntity();
            entities[i] = e;
            p1.Add(e, c1);
            p2.Add(e, c2);
            p3.Add(e, c3);
            p4.Add(e, c4);
            p5.Add(e, c5);
            p6.Add(e, c6);
        }
    }

    public TEntity[] CreateMany<T1, T2, T3, T4, T5, T6>(int count, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
    {
        var entities = new TEntity[count];
        CreateMany(entities, c1, c2, c3, c4, c5, c6);
        return entities;
    }

    public void CreateMany<T1, T2, T3, T4, T5, T6, T7>(Span<TEntity> entities, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
    {
        if (_deferDepth > 0)
        {
            for (var i = 0; i < entities.Length; i++)
            {
                entities[i] = Create(c1, c2, c3, c4, c5, c6, c7);
            }

            return;
        }

        var bound = ReserveCreates(entities.Length);
        var p1 = Storage<T1>();
        var p2 = Storage<T2>();
        var p3 = Storage<T3>();
        var p4 = Storage<T4>();
        var p5 = Storage<T5>();
        var p6 = Storage<T6>();
        var p7 = Storage<T7>();
        p1.ReserveBatch(entities.Length, bound);
        p2.ReserveBatch(entities.Length, bound);
        p3.ReserveBatch(entities.Length, bound);
        p4.ReserveBatch(entities.Length, bound);
        p5.ReserveBatch(entities.Length, bound);
        p6.ReserveBatch(entities.Length, bound);
        p7.ReserveBatch(entities.Length, bound);
        for (var i = 0; i < entities.Length; i++)
        {
            var e = CreateEntity();
            entities[i] = e;
            p1.Add(e, c1);
            p2.Add(e, c2);
            p3.Add(e, c3);
            p4.Add(e, c4);
            p5.Add(e, c5);
            p6.Add(e, c6);
            p7.Add(e, c7);
        }
    }

    public TEntity[] CreateMany<T1, T2, T3, T4, T5, T6, T7>(int count, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
    {
        var entities = new TEntity[count];
        CreateMany(entities, c1, c2, c3, c4, c5, c6, c7);
        return entities;
    }

    public void CreateMany<T1, T2, T3, T4, T5, T6, T7, T8>(Span<TEntity> entities, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
    {
        if (_deferDepth > 0)
        {
            for (var i = 0; i < entities.Length; i++)
            {
                entities[i] = Create(c1, c2, c3, c4, c5, c6, c7, c8);
            }

            return;
        }

        var bound = ReserveCreates(entities.Length);
        var p1 = Storage<T1>();
        var p2 = Storage<T2>();
        var p3 = Storage<T3>();
        var p4 = Storage<T4>();
        var p5 = Storage<T5>();
        var p6 = Storage<T6>();
        var p7 = Storage<T7>();
        var p8 = Storage<T8>();
        p1.ReserveBatch(entities.Length, bound);
        p2.ReserveBatch(entities.Length, bound);
        p3.ReserveBatch(entities.Length, bound);
        p4.ReserveBatch(entities.Length, bound);
        p5.ReserveBatch(entities.Length, bound);
        p6.ReserveBatch(entities.Length, bound);
        p7.ReserveBatch(entities.Length, bound);
        p8.ReserveBatch(entities.Length, bound);
        for (var i = 0; i < entities.Length; i++)
        {
            var e = CreateEntity();
            entities[i] = e;
            p1.Add(e, c1);
            p2.Add(e, c2);
            p3.Add(e, c3);
            p4.Add(e, c4);
            p5.Add(e, c5);
            p6.Add(e, c6);
            p7.Add(e, c7);
            p8.Add(e, c8);
        }
    }

    public TEntity[] CreateMany<T1, T2, T3, T4, T5, T6, T7, T8>(int count, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
    {
        var entities = new TEntity[count];
        CreateMany(entities, c1, c2, c3, c4, c5, c6, c7, c8);
        return entities;
    }

    public void CreateMany<T1, T2, T3, T4, T5, T6, T7, T8, T9>(Span<TEntity> entities, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
    {
        if (_deferDepth > 0)
        {
            for (var i = 0; i < entities.Length; i++)
            {
                entities[i] = Create(c1, c2, c3, c4, c5, c6, c7, c8, c9);
            }

            return;
        }

        var bound = ReserveCreates(entities.Length);
        var p1 = Storage<T1>();
        var p2 = Storage<T2>();
        var p3 = Storage<T3>();
        var p4 = Storage<T4>();
        var p5 = Storage<T5>();
        var p6 = Storage<T6>();
        var p7 = Storage<T7>();
        var p8 = Storage<T8>();
        var p9 = Storage<T9>();
        p1.ReserveBatch(entities.Length, bound);
        p2.ReserveBatch(entities.Length, bound);
        p3.ReserveBatch(entities.Length, bound);
        p4.ReserveBatch(entities.Length, bound);
        p5.ReserveBatch(entities.Length, bound);
        p6.ReserveBatch(entities.Length, bound);
        p7.ReserveBatch(entities.Length, bound);
        p8.ReserveBatch(entities.Length, bound);
        p9.ReserveBatch(entities.Length, bound);
        for (var i = 0; i < entities.Length; i++)
        {
            var e = CreateEntity();
            entities[i] = e;
            p1.Add(e, c1);
            p2.Add(e, c2);
            p3.Add(e, c3);
            p4.Add(e, c4);
            p5.Add(e, c5);
            p6.Add(e, c6);
            p7.Add(e, c7);
            p8.Add(e, c8);
            p9.Add(e, c9);
        }
    }

    public TEntity[] CreateMany<T1, T2, T3, T4, T5, T6, T7, T8, T9>(int count, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
    {
        var entities = new TEntity[count];
        CreateMany(entities, c1, c2, c3, c4, c5, c6, c7, c8, c9);
        return entities;
    }

    public void CreateMany<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Span<TEntity> entities, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
    {
        if (_deferDepth > 0)
        {
            for (var i = 0; i < entities.Length; i++)
            {
                entities[i] = Create(c1, c2, c3, c4, c5, c6, c7, c8, c9, c10);
            }

            return;
        }

        var bound = ReserveCreates(entities.Length);
        var p1 = Storage<T1>();
        var p2 = Storage<T2>();
        var p3 = Storage<T3>();
        var p4 = Storage<T4>();
        var p5 = Storage<T5>();
        var p6 = Storage<T6>();
        var p7 = Storage<T7>();
        var p8 = Storage<T8>();
        var p9 = Storage<T9>();
        var p10 = Storage<T10>();
        p1.ReserveBatch(entities.Length, bound);
        p2.ReserveBatch(entities.Length, bound);
        p3.ReserveBatch(entities.Length, bound);
        p4.ReserveBatch(entities.Length, bound);
        p5.ReserveBatch(entities.Length, bound);
        p6.ReserveBatch(entities.Length, bound);
        p7.ReserveBatch(entities.Length, bound);
        p8.ReserveBatch(entities.Length, bound);
        p9.ReserveBatch(entities.Length, bound);
        p10.ReserveBatch(entities.Length, bound);
        for (var i = 0; i < entities.Length; i++)
        {
            var e = CreateEntity();
            entities[i] = e;
            p1.Add(e, c1);
            p2.Add(e, c2);
            p3.Add(e, c3);
            p4.Add(e, c4);
            p5.Add(e, c5);
            p6.Add(e, c6);
            p7.Add(e, c7);
            p8.Add(e, c8);
            p9.Add(e, c9);
            p10.Add(e, c10);
        }
    }

    public TEntity[] CreateMany<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(int count, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
    {
        var entities = new TEntity[count];
        CreateMany(entities, c1, c2, c3, c4, c5, c6, c7, c8, c9, c10);
        return entities;
    }

    public void CreateMany<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(Span<TEntity> entities, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10, T11 c11)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
    {
        if (_deferDepth > 0)
        {
            for (var i = 0; i < entities.Length; i++)
            {
                entities[i] = Create(c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11);
            }

            return;
        }

        var bound = ReserveCreates(entities.Length);
        var p1 = Storage<T1>();
        var p2 = Storage<T2>();
        var p3 = Storage<T3>();
        var p4 = Storage<T4>();
        var p5 = Storage<T5>();
        var p6 = Storage<T6>();
        var p7 = Storage<T7>();
        var p8 = Storage<T8>();
        var p9 = Storage<T9>();
        var p10 = Storage<T10>();
        var p11 = Storage<T11>();
        p1.ReserveBatch(entities.Length, bound);
        p2.ReserveBatch(entities.Length, bound);
        p3.ReserveBatch(entities.Length, bound);
        p4.ReserveBatch(entities.Length, bound);
        p5.ReserveBatch(entities.Length, bound);
        p6.ReserveBatch(entities.Length, bound);
        p7.ReserveBatch(entities.Length, bound);
        p8.ReserveBatch(entities.Length, bound);
        p9.ReserveBatch(entities.Length, bound);
        p10.ReserveBatch(entities.Length, bound);
        p11.ReserveBatch(entities.Length, bound);
        for (var i = 0; i < entities.Length; i++)
        {
            var e = CreateEntity();
            entities[i] = e;
            p1.Add(e, c1);
            p2.Add(e, c2);
            p3.Add(e, c3);
            p4.Add(e, c4);
            p5.Add(e, c5);
            p6.Add(e, c6);
            p7.Add(e, c7);
            p8.Add(e, c8);
            p9.Add(e, c9);
            p10.Add(e, c10);
            p11.Add(e, c11);
        }
    }

    public TEntity[] CreateMany<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(int count, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10, T11 c11)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
    {
        var entities = new TEntity[count];
        CreateMany(entities, c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11);
        return entities;
    }

    public void CreateMany<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(Span<TEntity> entities, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10, T11 c11, T12 c12)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent
    {
        if (_deferDepth > 0)
        {
            for (var i = 0; i < entities.Length; i++)
            {
                entities[i] = Create(c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12);
            }

            return;
        }

        var bound = ReserveCreates(entities.Length);
        var p1 = Storage<T1>();
        var p2 = Storage<T2>();
        var p3 = Storage<T3>();
        var p4 = Storage<T4>();
        var p5 = Storage<T5>();
        var p6 = Storage<T6>();
        var p7 = Storage<T7>();
        var p8 = Storage<T8>();
        var p9 = Storage<T9>();
        var p10 = Storage<T10>();
        var p11 = Storage<T11>();
        var p12 = Storage<T12>();
        p1.ReserveBatch(entities.Length, bound);
        p2.ReserveBatch(entities.Length, bound);
        p3.ReserveBatch(entities.Length, bound);
        p4.ReserveBatch(entities.Length, bound);
        p5.ReserveBatch(entities.Length, bound);
        p6.ReserveBatch(entities.Length, bound);
        p7.ReserveBatch(entities.Length, bound);
        p8.ReserveBatch(entities.Length, bound);
        p9.ReserveBatch(entities.Length, bound);
        p10.ReserveBatch(entities.Length, bound);
        p11.ReserveBatch(entities.Length, bound);
        p12.ReserveBatch(entities.Length, bound);
        for (var i = 0; i < entities.Length; i++)
        {
            var e = CreateEntity();
            entities[i] = e;
            p1.Add(e, c1);
            p2.Add(e, c2);
            p3.Add(e, c3);
            p4.Add(e, c4);
            p5.Add(e, c5);
            p6.Add(e, c6);
            p7.Add(e, c7);
            p8.Add(e, c8);
            p9.Add(e, c9);
            p10.Add(e, c10);
            p11.Add(e, c11);
            p12.Add(e, c12);
        }
    }

    public TEntity[] CreateMany<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(int count, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10, T11 c11, T12 c12)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent
    {
        var entities = new TEntity[count];
        CreateMany(entities, c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12);
        return entities;
    }

    public void CreateMany<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(Span<TEntity> entities, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10, T11 c11, T12 c12, T13 c13)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent
        where T13 : IComponent
    {
        if (_deferDepth > 0)
        {
            for (var i = 0; i < entities.Length; i++)
            {
                entities[i] = Create(c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12, c13);
            }

            return;
        }

        var bound = ReserveCreates(entities.Length);
        var p1 = Storage<T1>();
        var p2 = Storage<T2>();
        var p3 = Storage<T3>();
        var p4 = Storage<T4>();
        var p5 = Storage<T5>();
        var p6 = Storage<T6>();
        var p7 = Storage<T7>();
        var p8 = Storage<T8>();
        var p9 = Storage<T9>();
        var p10 = Storage<T10>();
        var p11 = Storage<T11>();
        var p12 = Storage<T12>();
        var p13 = Storage<T13>();
        p1.ReserveBatch(entities.Length, bound);
        p2.ReserveBatch(entities.Length, bound);
        p3.ReserveBatch(entities.Length, bound);
        p4.ReserveBatch(entities.Length, bound);
        p5.ReserveBatch(entities.Length, bound);
        p6.ReserveBatch(entities.Length, bound);
        p7.ReserveBatch(entities.Length, bound);
        p8.ReserveBatch(entities.Length, bound);
        p9.ReserveBatch(entities.Length, bound);
        p10.ReserveBatch(entities.Length, bound);
        p11.ReserveBatch(entities.Length, bound);
        p12.ReserveBatch(entities.Length, bound);
        p13.ReserveBatch(entities.Length, bound);
        for (var i = 0; i < entities.Length; i++)
        {
            var e = CreateEntity();
            entities[i] = e;
            p1.Add(e, c1);
            p2.Add(e, c2);
            p3.Add(e, c3);
            p4.Add(e, c4);
            p5.Add(e, c5);
            p6.Add(e, c6);
            p7.Add(e, c7);
            p8.Add(e, c8);
            p9.Add(e, c9);
            p10.Add(e, c10);
            p11.Add(e, c11);
            p12.Add(e, c12);
            p13.Add(e, c13);
        }
    }

    public TEntity[] CreateMany<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(int count, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10, T11 c11, T12 c12, T13 c13)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent
        where T13 : IComponent
    {
        var entities = new TEntity[count];
        CreateMany(entities, c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12, c13);
        return entities;
    }

    public void CreateMany<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(Span<TEntity> entities, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10, T11 c11, T12 c12, T13 c13, T14 c14)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent
        where T13 : IComponent
        where T14 : IComponent
    {
        if (_deferDepth > 0)
        {
            for (var i = 0; i < entities.Length; i++)
            {
                entities[i] = Create(c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12, c13, c14);
            }

            return;
        }

        var bound = ReserveCreates(entities.Length);
        var p1 = Storage<T1>();
        var p2 = Storage<T2>();
        var p3 = Storage<T3>();
        var p4 = Storage<T4>();
        var p5 = Storage<T5>();
        var p6 = Storage<T6>();
        var p7 = Storage<T7>();
        var p8 = Storage<T8>();
        var p9 = Storage<T9>();
        var p10 = Storage<T10>();
        var p11 = Storage<T11>();
        var p12 = Storage<T12>();
        var p13 = Storage<T13>();
        var p14 = Storage<T14>();
        p1.ReserveBatch(entities.Length, bound);
        p2.ReserveBatch(entities.Length, bound);
        p3.ReserveBatch(entities.Length, bound);
        p4.ReserveBatch(entities.Length, bound);
        p5.ReserveBatch(entities.Length, bound);
        p6.ReserveBatch(entities.Length, bound);
        p7.ReserveBatch(entities.Length, bound);
        p8.ReserveBatch(entities.Length, bound);
        p9.ReserveBatch(entities.Length, bound);
        p10.ReserveBatch(entities.Length, bound);
        p11.ReserveBatch(entities.Length, bound);
        p12.ReserveBatch(entities.Length, bound);
        p13.ReserveBatch(entities.Length, bound);
        p14.ReserveBatch(entities.Length, bound);
        for (var i = 0; i < entities.Length; i++)
        {
            var e = CreateEntity();
            entities[i] = e;
            p1.Add(e, c1);
            p2.Add(e, c2);
            p3.Add(e, c3);
            p4.Add(e, c4);
            p5.Add(e, c5);
            p6.Add(e, c6);
            p7.Add(e, c7);
            p8.Add(e, c8);
            p9.Add(e, c9);
            p10.Add(e, c10);
            p11.Add(e, c11);
            p12.Add(e, c12);
            p13.Add(e, c13);
            p14.Add(e, c14);
        }
    }

    public TEntity[] CreateMany<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(int count, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10, T11 c11, T12 c12, T13 c13, T14 c14)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent
        where T13 : IComponent
        where T14 : IComponent
    {
        var entities = new TEntity[count];
        CreateMany(entities, c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12, c13, c14);
        return entities;
    }

    public void CreateMany<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(Span<TEntity> entities, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10, T11 c11, T12 c12, T13 c13, T14 c14, T15 c15)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent
        where T13 : IComponent
        where T14 : IComponent
        where T15 : IComponent
    {
        if (_deferDepth > 0)
        {
            for (var i = 0; i < entities.Length; i++)
            {
                entities[i] = Create(c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12, c13, c14, c15);
            }

            return;
        }

        var bound = ReserveCreates(entities.Length);
        var p1 = Storage<T1>();
        var p2 = Storage<T2>();
        var p3 = Storage<T3>();
        var p4 = Storage<T4>();
        var p5 = Storage<T5>();
        var p6 = Storage<T6>();
        var p7 = Storage<T7>();
        var p8 = Storage<T8>();
        var p9 = Storage<T9>();
        var p10 = Storage<T10>();
        var p11 = Storage<T11>();
        var p12 = Storage<T12>();
        var p13 = Storage<T13>();
        var p14 = Storage<T14>();
        var p15 = Storage<T15>();
        p1.ReserveBatch(entities.Length, bound);
        p2.ReserveBatch(entities.Length, bound);
        p3.ReserveBatch(entities.Length, bound);
        p4.ReserveBatch(entities.Length, bound);
        p5.ReserveBatch(entities.Length, bound);
        p6.ReserveBatch(entities.Length, bound);
        p7.ReserveBatch(entities.Length, bound);
        p8.ReserveBatch(entities.Length, bound);
        p9.ReserveBatch(entities.Length, bound);
        p10.ReserveBatch(entities.Length, bound);
        p11.ReserveBatch(entities.Length, bound);
        p12.ReserveBatch(entities.Length, bound);
        p13.ReserveBatch(entities.Length, bound);
        p14.ReserveBatch(entities.Length, bound);
        p15.ReserveBatch(entities.Length, bound);
        for (var i = 0; i < entities.Length; i++)
        {
            var e = CreateEntity();
            entities[i] = e;
            p1.Add(e, c1);
            p2.Add(e, c2);
            p3.Add(e, c3);
            p4.Add(e, c4);
            p5.Add(e, c5);
            p6.Add(e, c6);
            p7.Add(e, c7);
            p8.Add(e, c8);
            p9.Add(e, c9);
            p10.Add(e, c10);
            p11.Add(e, c11);
            p12.Add(e, c12);
            p13.Add(e, c13);
            p14.Add(e, c14);
            p15.Add(e, c15);
        }
    }

    public TEntity[] CreateMany<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(int count, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10, T11 c11, T12 c12, T13 c13, T14 c14, T15 c15)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent
        where T13 : IComponent
        where T14 : IComponent
        where T15 : IComponent
    {
        var entities = new TEntity[count];
        CreateMany(entities, c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12, c13, c14, c15);
        return entities;
    }

    public void CreateMany<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(Span<TEntity> entities, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10, T11 c11, T12 c12, T13 c13, T14 c14, T15 c15, T16 c16)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent
        where T13 : IComponent
        where T14 : IComponent
        where T15 : IComponent
        where T16 : IComponent
    {
        if (_deferDepth > 0)
        {
            for (var i = 0; i < entities.Length; i++)
            {
                entities[i] = Create(c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12, c13, c14, c15, c16);
            }

            return;
        }

        var bound = ReserveCreates(entities.Length);
        var p1 = Storage<T1>();
        var p2 = Storage<T2>();
        var p3 = Storage<T3>();
        var p4 = Storage<T4>();
        var p5 = Storage<T5>();
        var p6 = Storage<T6>();
        var p7 = Storage<T7>();
        var p8 = Storage<T8>();
        var p9 = Storage<T9>();
        var p10 = Storage<T10>();
        var p11 = Storage<T11>();
        var p12 = Storage<T12>();
        var p13 = Storage<T13>();
        var p14 = Storage<T14>();
        var p15 = Storage<T15>();
        var p16 = Storage<T16>();
        p1.ReserveBatch(entities.Length, bound);
        p2.ReserveBatch(entities.Length, bound);
        p3.ReserveBatch(entities.Length, bound);
        p4.ReserveBatch(entities.Length, bound);
        p5.ReserveBatch(entities.Length, bound);
        p6.ReserveBatch(entities.Length, bound);
        p7.ReserveBatch(entities.Length, bound);
        p8.ReserveBatch(entities.Length, bound);
        p9.ReserveBatch(entities.Length, bound);
        p10.ReserveBatch(entities.Length, bound);
        p11.ReserveBatch(entities.Length, bound);
        p12.ReserveBatch(entities.Length, bound);
        p13.ReserveBatch(entities.Length, bound);
        p14.ReserveBatch(entities.Length, bound);
        p15.ReserveBatch(entities.Length, bound);
        p16.ReserveBatch(entities.Length, bound);
        for (var i = 0; i < entities.Length; i++)
        {
            var e = CreateEntity();
            entities[i] = e;
            p1.Add(e, c1);
            p2.Add(e, c2);
            p3.Add(e, c3);
            p4.Add(e, c4);
            p5.Add(e, c5);
            p6.Add(e, c6);
            p7.Add(e, c7);
            p8.Add(e, c8);
            p9.Add(e, c9);
            p10.Add(e, c10);
            p11.Add(e, c11);
            p12.Add(e, c12);
            p13.Add(e, c13);
            p14.Add(e, c14);
            p15.Add(e, c15);
            p16.Add(e, c16);
        }
    }

    public TEntity[] CreateMany<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(int count, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10, T11 c11, T12 c12, T13 c13, T14 c14, T15 c15, T16 c16)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        where T10 : IComponent
        where T11 : IComponent
        where T12 : IComponent
        where T13 : IComponent
        where T14 : IComponent
        where T15 : IComponent
        where T16 : IComponent
    {
        var entities = new TEntity[count];
        CreateMany(entities, c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12, c13, c14, c15, c16);
        return entities;
    }

    // Not atomic. AddValues is separate because Add(entities, array) would bind T to the array.
    public void Add<T>(ReadOnlySpan<TEntity> entities, T value)
        where T : IComponent =>
        Storage<T>().Add(entities, value);

    public void AddValues<T>(ReadOnlySpan<TEntity> entities, ReadOnlySpan<T> values) where T : IComponent =>
        Storage<T>().AddValues(entities, values);

    // Walks a copy, so entities may be a store's own Entities, which the destroys reorder.
    public void Destroy(ReadOnlySpan<TEntity> entities)
    {
        var buffer = ArrayPool<TEntity>.Shared.Rent(entities.Length);
        try
        {
            entities.CopyTo(buffer);
            foreach (var e in buffer.AsSpan(0, entities.Length))
            {
                Destroy(e);
            }
        }
        finally
        {
            ArrayPool<TEntity>.Shared.Return(buffer);
        }
    }

    // Returns how many had T. Safe on T's own Entities.
    public int Remove<T>(ReadOnlySpan<TEntity> entities) where T : IComponent => FindPool<T>()?.Remove(entities) ?? 0;

    public void Clear<T>() where T : IComponent => FindPool<T>()?.Clear();

    public void ClearTags<T>() where T : struct, ITag
    {
        if (FindTags<T>() is not { } tags || tags.TryClearQuietly())
        {
            return;
        }

        // Tag sets hold only live entities, so the current version gives the handle.
        foreach (var index in tags.Indices)
        {
            tags.Remove(TEntity.Make(index, _versions[index]));
        }
    }

    public void DestroyTagged<T>() where T : struct, ITag
    {
        if (FindTags<T>() is not { Count: > 0 } tags)
        {
            return;
        }

        // A handler may destroy or untag entities the walk hasn't reached yet.
        foreach (var index in tags.Indices)
        {
            if (tags.Test(index))
            {
                Destroy(TEntity.Make(index, _versions[index]));
            }
        }
    }

    // Destroys every live entity; ones a handler creates meanwhile survive if below the walk's index.
    public void Clear()
    {
        foreach (var e in Entities)
        {
            Destroy(e);
        }
    }
}
