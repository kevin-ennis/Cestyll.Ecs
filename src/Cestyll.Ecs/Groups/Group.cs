using Cestyll.Ecs.Storage;
using Cestyll.Ecs.Views;

namespace Cestyll.Ecs.Groups;

// Owning (packs its Own pools) or non-owning (keeps a member set, looks components up).
public sealed class Group<TEntity> : IDisposable
    where TEntity : unmanaged, IEntity<TEntity>
{
    private readonly Registry<TEntity> _registry;
    private readonly OwningGroup<TEntity>? _owning;
    private readonly NonOwningGroup<TEntity>? _members;

    internal Group(Registry<TEntity> registry, OwningGroup<TEntity> owning)
    {
        _registry = registry;
        _owning = owning;
    }

    internal Group(Registry<TEntity> registry, NonOwningGroup<TEntity> members)
    {
        _registry = registry;
        _members = members;
    }

    // For View.Use(group).
    internal NonOwningGroup<TEntity>? Members => _members;

    public bool Owning => _owning is not null;
    public int Count => _owning?.Count ?? _members!.Count;
    public ReadOnlySpan<TEntity> Entities => _owning is not null ? _owning.Entities : _members!.Entities;
    public bool Contains(TEntity e) => _owning?.Contains(e) ?? _members!.Contains(e);

    // The pool of an owned or With component.
    public Pool<TEntity, T> Storage<T>() where T : IComponent =>
        _owning is not null ? _owning.Storage<T>() : _members!.Storage<T>();

    public void GetSlots<T>(Span<int> slots) where T : IComponent
    {
        if (_owning is not null)
        {
            _owning.GetSlots<T>(slots);
        }
        else
        {
            _members!.GetSlots<T>(slots);
        }
    }

    public void Sort<T>(Comparison<T> compare) where T : IComponent
    {
        if (_owning is not null)
        {
            _owning.Sort(compare);
        }
        else
        {
            _members!.Sort(compare);
        }
    }

    public void SortEntities(Comparison<TEntity> compare)
    {
        if (_owning is not null)
        {
            _owning.SortEntities(compare);
        }
        else
        {
            _members!.SortEntities(compare);
        }
    }

    // No-op when owning.
    public void ShrinkToFit() => _members?.ShrinkToFit();

    public void Dispose()
    {
        if (_owning is not null)
        {
            _owning.Dispose();
        }
        else
        {
            _members!.Dispose();
        }
    }

    // Owned T, slot for slot with Entities.
    public Span<T> Span<T>() where T : IComponent => Owned().Span<T>();

    private OwningGroup<TEntity> Owned() =>
        _owning ?? throw new InvalidOperationException("This group owns no pools: read it with Each or Storage<T>()");

    private View<TEntity> MemberView() => _registry.View().Use(this);

    // The struct names its types, so no type arguments.
    public void Each<TFn>(ref TFn fn) where TFn : struct, IEach<TFn, TEntity> => TFn.Run(this, ref fn);

    public GroupRows<TEntity, T1> Rows<T1>()
        where T1 : IComponent
        => Owned().Rows<T1>();

    public void Each<T1>(RefAction<TEntity, T1> action)
        where T1 : IComponent
    {
        if (_owning is not null)
        {
            _owning.Each(action);
        }
        else
        {
            // Throws for a type the group doesn't require.
            _members!.Storage<T1>();
            MemberView().Each(action);
        }
    }

    public void Each<TFn, T1>(ref TFn fn)
        where TFn : struct, IEach<TFn, TEntity, T1>
        where T1 : IComponent
    {
        if (_owning is not null)
        {
            _owning.Each<TFn, T1>(ref fn);
        }
        else
        {
            // Throws for a type the group doesn't require.
            _members!.Storage<T1>();
            MemberView().Each<TFn, T1>(ref fn);
        }
    }

    public GroupRows<TEntity, T1, T2> Rows<T1, T2>()
        where T1 : IComponent
        where T2 : IComponent
        => Owned().Rows<T1, T2>();

    public void Each<T1, T2>(RefAction<TEntity, T1, T2> action)
        where T1 : IComponent
        where T2 : IComponent
    {
        if (_owning is not null)
        {
            _owning.Each(action);
        }
        else
        {
            // Throws for a type the group doesn't require.
            _members!.Storage<T1>();
            _members!.Storage<T2>();
            MemberView().Each(action);
        }
    }

    public void Each<TFn, T1, T2>(ref TFn fn)
        where TFn : struct, IEach<TFn, TEntity, T1, T2>
        where T1 : IComponent
        where T2 : IComponent
    {
        if (_owning is not null)
        {
            _owning.Each<TFn, T1, T2>(ref fn);
        }
        else
        {
            // Throws for a type the group doesn't require.
            _members!.Storage<T1>();
            _members!.Storage<T2>();
            MemberView().Each<TFn, T1, T2>(ref fn);
        }
    }

    public GroupRows<TEntity, T1, T2, T3> Rows<T1, T2, T3>()
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        => Owned().Rows<T1, T2, T3>();

    public void Each<T1, T2, T3>(RefAction<TEntity, T1, T2, T3> action)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
    {
        if (_owning is not null)
        {
            _owning.Each(action);
        }
        else
        {
            // Throws for a type the group doesn't require.
            _members!.Storage<T1>();
            _members!.Storage<T2>();
            _members!.Storage<T3>();
            MemberView().Each(action);
        }
    }

    public void Each<TFn, T1, T2, T3>(ref TFn fn)
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3>
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
    {
        if (_owning is not null)
        {
            _owning.Each<TFn, T1, T2, T3>(ref fn);
        }
        else
        {
            // Throws for a type the group doesn't require.
            _members!.Storage<T1>();
            _members!.Storage<T2>();
            _members!.Storage<T3>();
            MemberView().Each<TFn, T1, T2, T3>(ref fn);
        }
    }

    public GroupRows<TEntity, T1, T2, T3, T4> Rows<T1, T2, T3, T4>()
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        => Owned().Rows<T1, T2, T3, T4>();

    public void Each<T1, T2, T3, T4>(RefAction<TEntity, T1, T2, T3, T4> action)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
    {
        if (_owning is not null)
        {
            _owning.Each(action);
        }
        else
        {
            // Throws for a type the group doesn't require.
            _members!.Storage<T1>();
            _members!.Storage<T2>();
            _members!.Storage<T3>();
            _members!.Storage<T4>();
            MemberView().Each(action);
        }
    }

    public void Each<TFn, T1, T2, T3, T4>(ref TFn fn)
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4>
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
    {
        if (_owning is not null)
        {
            _owning.Each<TFn, T1, T2, T3, T4>(ref fn);
        }
        else
        {
            // Throws for a type the group doesn't require.
            _members!.Storage<T1>();
            _members!.Storage<T2>();
            _members!.Storage<T3>();
            _members!.Storage<T4>();
            MemberView().Each<TFn, T1, T2, T3, T4>(ref fn);
        }
    }

    public GroupRows<TEntity, T1, T2, T3, T4, T5> Rows<T1, T2, T3, T4, T5>()
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        => Owned().Rows<T1, T2, T3, T4, T5>();

    public void Each<T1, T2, T3, T4, T5>(RefAction<TEntity, T1, T2, T3, T4, T5> action)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
    {
        if (_owning is not null)
        {
            _owning.Each(action);
        }
        else
        {
            // Throws for a type the group doesn't require.
            _members!.Storage<T1>();
            _members!.Storage<T2>();
            _members!.Storage<T3>();
            _members!.Storage<T4>();
            _members!.Storage<T5>();
            MemberView().Each(action);
        }
    }

    public void Each<TFn, T1, T2, T3, T4, T5>(ref TFn fn)
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5>
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
    {
        if (_owning is not null)
        {
            _owning.Each<TFn, T1, T2, T3, T4, T5>(ref fn);
        }
        else
        {
            // Throws for a type the group doesn't require.
            _members!.Storage<T1>();
            _members!.Storage<T2>();
            _members!.Storage<T3>();
            _members!.Storage<T4>();
            _members!.Storage<T5>();
            MemberView().Each<TFn, T1, T2, T3, T4, T5>(ref fn);
        }
    }

    public GroupRows<TEntity, T1, T2, T3, T4, T5, T6> Rows<T1, T2, T3, T4, T5, T6>()
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        => Owned().Rows<T1, T2, T3, T4, T5, T6>();

    public void Each<T1, T2, T3, T4, T5, T6>(RefAction<TEntity, T1, T2, T3, T4, T5, T6> action)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
    {
        if (_owning is not null)
        {
            _owning.Each(action);
        }
        else
        {
            // Throws for a type the group doesn't require.
            _members!.Storage<T1>();
            _members!.Storage<T2>();
            _members!.Storage<T3>();
            _members!.Storage<T4>();
            _members!.Storage<T5>();
            _members!.Storage<T6>();
            MemberView().Each(action);
        }
    }

    public void Each<TFn, T1, T2, T3, T4, T5, T6>(ref TFn fn)
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6>
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
    {
        if (_owning is not null)
        {
            _owning.Each<TFn, T1, T2, T3, T4, T5, T6>(ref fn);
        }
        else
        {
            // Throws for a type the group doesn't require.
            _members!.Storage<T1>();
            _members!.Storage<T2>();
            _members!.Storage<T3>();
            _members!.Storage<T4>();
            _members!.Storage<T5>();
            _members!.Storage<T6>();
            MemberView().Each<TFn, T1, T2, T3, T4, T5, T6>(ref fn);
        }
    }

    public GroupRows<TEntity, T1, T2, T3, T4, T5, T6, T7> Rows<T1, T2, T3, T4, T5, T6, T7>()
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        => Owned().Rows<T1, T2, T3, T4, T5, T6, T7>();

    public void Each<T1, T2, T3, T4, T5, T6, T7>(RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7> action)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
    {
        if (_owning is not null)
        {
            _owning.Each(action);
        }
        else
        {
            // Throws for a type the group doesn't require.
            _members!.Storage<T1>();
            _members!.Storage<T2>();
            _members!.Storage<T3>();
            _members!.Storage<T4>();
            _members!.Storage<T5>();
            _members!.Storage<T6>();
            _members!.Storage<T7>();
            MemberView().Each(action);
        }
    }

    public void Each<TFn, T1, T2, T3, T4, T5, T6, T7>(ref TFn fn)
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7>
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
    {
        if (_owning is not null)
        {
            _owning.Each<TFn, T1, T2, T3, T4, T5, T6, T7>(ref fn);
        }
        else
        {
            // Throws for a type the group doesn't require.
            _members!.Storage<T1>();
            _members!.Storage<T2>();
            _members!.Storage<T3>();
            _members!.Storage<T4>();
            _members!.Storage<T5>();
            _members!.Storage<T6>();
            _members!.Storage<T7>();
            MemberView().Each<TFn, T1, T2, T3, T4, T5, T6, T7>(ref fn);
        }
    }

    public GroupRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8> Rows<T1, T2, T3, T4, T5, T6, T7, T8>()
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        => Owned().Rows<T1, T2, T3, T4, T5, T6, T7, T8>();

    public void Each<T1, T2, T3, T4, T5, T6, T7, T8>(RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7, T8> action)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
    {
        if (_owning is not null)
        {
            _owning.Each(action);
        }
        else
        {
            // Throws for a type the group doesn't require.
            _members!.Storage<T1>();
            _members!.Storage<T2>();
            _members!.Storage<T3>();
            _members!.Storage<T4>();
            _members!.Storage<T5>();
            _members!.Storage<T6>();
            _members!.Storage<T7>();
            _members!.Storage<T8>();
            MemberView().Each(action);
        }
    }

    public void Each<TFn, T1, T2, T3, T4, T5, T6, T7, T8>(ref TFn fn)
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7, T8>
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
    {
        if (_owning is not null)
        {
            _owning.Each<TFn, T1, T2, T3, T4, T5, T6, T7, T8>(ref fn);
        }
        else
        {
            // Throws for a type the group doesn't require.
            _members!.Storage<T1>();
            _members!.Storage<T2>();
            _members!.Storage<T3>();
            _members!.Storage<T4>();
            _members!.Storage<T5>();
            _members!.Storage<T6>();
            _members!.Storage<T7>();
            _members!.Storage<T8>();
            MemberView().Each<TFn, T1, T2, T3, T4, T5, T6, T7, T8>(ref fn);
        }
    }

    public GroupRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9> Rows<T1, T2, T3, T4, T5, T6, T7, T8, T9>()
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
        where T6 : IComponent
        where T7 : IComponent
        where T8 : IComponent
        where T9 : IComponent
        => Owned().Rows<T1, T2, T3, T4, T5, T6, T7, T8, T9>();

    public void Each<T1, T2, T3, T4, T5, T6, T7, T8, T9>(RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9> action)
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
        if (_owning is not null)
        {
            _owning.Each(action);
        }
        else
        {
            // Throws for a type the group doesn't require.
            _members!.Storage<T1>();
            _members!.Storage<T2>();
            _members!.Storage<T3>();
            _members!.Storage<T4>();
            _members!.Storage<T5>();
            _members!.Storage<T6>();
            _members!.Storage<T7>();
            _members!.Storage<T8>();
            _members!.Storage<T9>();
            MemberView().Each(action);
        }
    }

    public void Each<TFn, T1, T2, T3, T4, T5, T6, T7, T8, T9>(ref TFn fn)
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9>
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
        if (_owning is not null)
        {
            _owning.Each<TFn, T1, T2, T3, T4, T5, T6, T7, T8, T9>(ref fn);
        }
        else
        {
            // Throws for a type the group doesn't require.
            _members!.Storage<T1>();
            _members!.Storage<T2>();
            _members!.Storage<T3>();
            _members!.Storage<T4>();
            _members!.Storage<T5>();
            _members!.Storage<T6>();
            _members!.Storage<T7>();
            _members!.Storage<T8>();
            _members!.Storage<T9>();
            MemberView().Each<TFn, T1, T2, T3, T4, T5, T6, T7, T8, T9>(ref fn);
        }
    }

    public GroupRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Rows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>()
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
        => Owned().Rows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>();

    public void Each<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> action)
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
        if (_owning is not null)
        {
            _owning.Each(action);
        }
        else
        {
            // Throws for a type the group doesn't require.
            _members!.Storage<T1>();
            _members!.Storage<T2>();
            _members!.Storage<T3>();
            _members!.Storage<T4>();
            _members!.Storage<T5>();
            _members!.Storage<T6>();
            _members!.Storage<T7>();
            _members!.Storage<T8>();
            _members!.Storage<T9>();
            _members!.Storage<T10>();
            MemberView().Each(action);
        }
    }

    public void Each<TFn, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(ref TFn fn)
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
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
        if (_owning is not null)
        {
            _owning.Each<TFn, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(ref fn);
        }
        else
        {
            // Throws for a type the group doesn't require.
            _members!.Storage<T1>();
            _members!.Storage<T2>();
            _members!.Storage<T3>();
            _members!.Storage<T4>();
            _members!.Storage<T5>();
            _members!.Storage<T6>();
            _members!.Storage<T7>();
            _members!.Storage<T8>();
            _members!.Storage<T9>();
            _members!.Storage<T10>();
            MemberView().Each<TFn, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(ref fn);
        }
    }

    public GroupRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> Rows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>()
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
        => Owned().Rows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>();

    public void Each<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> action)
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
        if (_owning is not null)
        {
            _owning.Each(action);
        }
        else
        {
            // Throws for a type the group doesn't require.
            _members!.Storage<T1>();
            _members!.Storage<T2>();
            _members!.Storage<T3>();
            _members!.Storage<T4>();
            _members!.Storage<T5>();
            _members!.Storage<T6>();
            _members!.Storage<T7>();
            _members!.Storage<T8>();
            _members!.Storage<T9>();
            _members!.Storage<T10>();
            _members!.Storage<T11>();
            MemberView().Each(action);
        }
    }

    public void Each<TFn, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(ref TFn fn)
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>
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
        if (_owning is not null)
        {
            _owning.Each<TFn, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(ref fn);
        }
        else
        {
            // Throws for a type the group doesn't require.
            _members!.Storage<T1>();
            _members!.Storage<T2>();
            _members!.Storage<T3>();
            _members!.Storage<T4>();
            _members!.Storage<T5>();
            _members!.Storage<T6>();
            _members!.Storage<T7>();
            _members!.Storage<T8>();
            _members!.Storage<T9>();
            _members!.Storage<T10>();
            _members!.Storage<T11>();
            MemberView().Each<TFn, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(ref fn);
        }
    }

    public GroupRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> Rows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>()
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
        => Owned().Rows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>();

    public void Each<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> action)
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
        if (_owning is not null)
        {
            _owning.Each(action);
        }
        else
        {
            // Throws for a type the group doesn't require.
            _members!.Storage<T1>();
            _members!.Storage<T2>();
            _members!.Storage<T3>();
            _members!.Storage<T4>();
            _members!.Storage<T5>();
            _members!.Storage<T6>();
            _members!.Storage<T7>();
            _members!.Storage<T8>();
            _members!.Storage<T9>();
            _members!.Storage<T10>();
            _members!.Storage<T11>();
            _members!.Storage<T12>();
            MemberView().Each(action);
        }
    }

    public void Each<TFn, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(ref TFn fn)
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>
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
        if (_owning is not null)
        {
            _owning.Each<TFn, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(ref fn);
        }
        else
        {
            // Throws for a type the group doesn't require.
            _members!.Storage<T1>();
            _members!.Storage<T2>();
            _members!.Storage<T3>();
            _members!.Storage<T4>();
            _members!.Storage<T5>();
            _members!.Storage<T6>();
            _members!.Storage<T7>();
            _members!.Storage<T8>();
            _members!.Storage<T9>();
            _members!.Storage<T10>();
            _members!.Storage<T11>();
            _members!.Storage<T12>();
            MemberView().Each<TFn, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(ref fn);
        }
    }

    public GroupRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> Rows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>()
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
        => Owned().Rows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>();

    public void Each<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> action)
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
        if (_owning is not null)
        {
            _owning.Each(action);
        }
        else
        {
            // Throws for a type the group doesn't require.
            _members!.Storage<T1>();
            _members!.Storage<T2>();
            _members!.Storage<T3>();
            _members!.Storage<T4>();
            _members!.Storage<T5>();
            _members!.Storage<T6>();
            _members!.Storage<T7>();
            _members!.Storage<T8>();
            _members!.Storage<T9>();
            _members!.Storage<T10>();
            _members!.Storage<T11>();
            _members!.Storage<T12>();
            _members!.Storage<T13>();
            MemberView().Each(action);
        }
    }

    public void Each<TFn, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(ref TFn fn)
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>
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
        if (_owning is not null)
        {
            _owning.Each<TFn, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(ref fn);
        }
        else
        {
            // Throws for a type the group doesn't require.
            _members!.Storage<T1>();
            _members!.Storage<T2>();
            _members!.Storage<T3>();
            _members!.Storage<T4>();
            _members!.Storage<T5>();
            _members!.Storage<T6>();
            _members!.Storage<T7>();
            _members!.Storage<T8>();
            _members!.Storage<T9>();
            _members!.Storage<T10>();
            _members!.Storage<T11>();
            _members!.Storage<T12>();
            _members!.Storage<T13>();
            MemberView().Each<TFn, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(ref fn);
        }
    }

    public GroupRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> Rows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>()
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
        => Owned().Rows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>();

    public void Each<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> action)
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
        if (_owning is not null)
        {
            _owning.Each(action);
        }
        else
        {
            // Throws for a type the group doesn't require.
            _members!.Storage<T1>();
            _members!.Storage<T2>();
            _members!.Storage<T3>();
            _members!.Storage<T4>();
            _members!.Storage<T5>();
            _members!.Storage<T6>();
            _members!.Storage<T7>();
            _members!.Storage<T8>();
            _members!.Storage<T9>();
            _members!.Storage<T10>();
            _members!.Storage<T11>();
            _members!.Storage<T12>();
            _members!.Storage<T13>();
            _members!.Storage<T14>();
            MemberView().Each(action);
        }
    }

    public void Each<TFn, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(ref TFn fn)
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>
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
        if (_owning is not null)
        {
            _owning.Each<TFn, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(ref fn);
        }
        else
        {
            // Throws for a type the group doesn't require.
            _members!.Storage<T1>();
            _members!.Storage<T2>();
            _members!.Storage<T3>();
            _members!.Storage<T4>();
            _members!.Storage<T5>();
            _members!.Storage<T6>();
            _members!.Storage<T7>();
            _members!.Storage<T8>();
            _members!.Storage<T9>();
            _members!.Storage<T10>();
            _members!.Storage<T11>();
            _members!.Storage<T12>();
            _members!.Storage<T13>();
            _members!.Storage<T14>();
            MemberView().Each<TFn, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(ref fn);
        }
    }

    public GroupRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> Rows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>()
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
        => Owned().Rows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>();

    public void Each<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> action)
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
        if (_owning is not null)
        {
            _owning.Each(action);
        }
        else
        {
            // Throws for a type the group doesn't require.
            _members!.Storage<T1>();
            _members!.Storage<T2>();
            _members!.Storage<T3>();
            _members!.Storage<T4>();
            _members!.Storage<T5>();
            _members!.Storage<T6>();
            _members!.Storage<T7>();
            _members!.Storage<T8>();
            _members!.Storage<T9>();
            _members!.Storage<T10>();
            _members!.Storage<T11>();
            _members!.Storage<T12>();
            _members!.Storage<T13>();
            _members!.Storage<T14>();
            _members!.Storage<T15>();
            MemberView().Each(action);
        }
    }

    public void Each<TFn, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(ref TFn fn)
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>
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
        if (_owning is not null)
        {
            _owning.Each<TFn, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(ref fn);
        }
        else
        {
            // Throws for a type the group doesn't require.
            _members!.Storage<T1>();
            _members!.Storage<T2>();
            _members!.Storage<T3>();
            _members!.Storage<T4>();
            _members!.Storage<T5>();
            _members!.Storage<T6>();
            _members!.Storage<T7>();
            _members!.Storage<T8>();
            _members!.Storage<T9>();
            _members!.Storage<T10>();
            _members!.Storage<T11>();
            _members!.Storage<T12>();
            _members!.Storage<T13>();
            _members!.Storage<T14>();
            _members!.Storage<T15>();
            MemberView().Each<TFn, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(ref fn);
        }
    }

    public GroupRows<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> Rows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>()
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
        => Owned().Rows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>();

    public void Each<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(RefAction<TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> action)
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
        if (_owning is not null)
        {
            _owning.Each(action);
        }
        else
        {
            // Throws for a type the group doesn't require.
            _members!.Storage<T1>();
            _members!.Storage<T2>();
            _members!.Storage<T3>();
            _members!.Storage<T4>();
            _members!.Storage<T5>();
            _members!.Storage<T6>();
            _members!.Storage<T7>();
            _members!.Storage<T8>();
            _members!.Storage<T9>();
            _members!.Storage<T10>();
            _members!.Storage<T11>();
            _members!.Storage<T12>();
            _members!.Storage<T13>();
            _members!.Storage<T14>();
            _members!.Storage<T15>();
            _members!.Storage<T16>();
            MemberView().Each(action);
        }
    }

    public void Each<TFn, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(ref TFn fn)
        where TFn : struct, IEach<TFn, TEntity, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>
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
        if (_owning is not null)
        {
            _owning.Each<TFn, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(ref fn);
        }
        else
        {
            // Throws for a type the group doesn't require.
            _members!.Storage<T1>();
            _members!.Storage<T2>();
            _members!.Storage<T3>();
            _members!.Storage<T4>();
            _members!.Storage<T5>();
            _members!.Storage<T6>();
            _members!.Storage<T7>();
            _members!.Storage<T8>();
            _members!.Storage<T9>();
            _members!.Storage<T10>();
            _members!.Storage<T11>();
            _members!.Storage<T12>();
            _members!.Storage<T13>();
            _members!.Storage<T14>();
            _members!.Storage<T15>();
            _members!.Storage<T16>();
            MemberView().Each<TFn, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(ref fn);
        }
    }
}
