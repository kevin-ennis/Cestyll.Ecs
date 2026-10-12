# SIMD

Pools and owning groups can be read as `Vector256` lanes, 32 bytes at a time, in safe code. The lanes are the component's bytes read as the lane type, so a `Position { float X, Y }` gives X, Y, X, Y. `Lane<TLane>`, `Lanes`, `VectorWalks` and `VectorRows` live in `Cestyll.Ecs`. `Vector256` comes from `System.Runtime.Intrinsics`.

The examples below run in order against one registry.

```csharp
using System.Runtime.Intrinsics;
using Cestyll.Ecs;
using Cestyll.Ecs.Groups;

struct Position : IComponent { public float X, Y; }
struct Velocity : IComponent { public float X, Y; }
struct Health : IComponent { public float Value; }

var registry = new Registry();
```

## Lanes

`Vectors256` takes the lane type as a value, so the component types are still inferred. `Lanes` has one per supported type: `Float`, `Double`, `Int`, `UInt`, `Long`, `ULong`, `Short`, `UShort`, `Byte` and `SByte`. Each is an empty `Lane<TLane>`, so `default(Lane<float>)` works too.

## Walking a group

`group.Rows<T1, ...>().Vectors256(lane)` returns the rows of an owning group over 1 to 16 of its owned components. Each row has a `ref Vector256<TLane>` per component, `V1` to `V16`, covering the same entities.

```csharp
var movers = registry.Group().Own<Position>().Own<Velocity>().Build();

foreach (var v in movers.Rows<Position, Velocity>().Vectors256(Lanes.Float))
{
    v.V1 += v.V2; // four Positions += four Velocities
}
```

## Walking a pool

`pool.Vectors256(lane)` returns the rows of one pool, in pool order.

```csharp
var regen = Vector256.Create(1.05f);

foreach (var v in registry.Storage<Health>().Vectors256(Lanes.Float))
{
    v.V1 *= regen;
}
```

## The tail

When the count doesn't fill the last vector, the final row is a buffer holding the leftover components, with the spare lanes zeroed. Writes to it are copied back when the loop ends, on `break` too.

```csharp
for (var i = 0; i < 11; i++)
{
    registry.Create(new Health { Value = 1 });
}

var rows = 0;
foreach (var v in registry.Storage<Health>().Vectors256(Lanes.Float))
{
    v.V1 += Vector256.Create(1f);
    rows++;
}

Console.WriteLine(rows); // 2: eight Healths, then the last three
```

## Walking a span

`span.Vectors256(lane, out tail)` reshapes any span of components as vectors and hands back the components after the last full vector for you to finish. Use it on a slice, or to feed a kernel of your own.

```csharp
var pos = movers.Span<Position>().Vectors256(Lanes.Float, out var posTail);
var vel = movers.Span<Velocity>().Vectors256(Lanes.Float, out var velTail);

for (var i = 0; i < pos.Length; i++)
{
    pos[i] += vel[i];
}

for (var i = 0; i < posTail.Length; i++)
{
    posTail[i].X += velTail[i].X;
    posTail[i].Y += velTail[i].Y;
}
```

## Component size

A component's size must divide 32 bytes or be a multiple of it, and components walked together must all be the same size. `Vectors256` throws `ArgumentException` otherwise.

```csharp
struct Odd : IComponent { public int A, B, C; } // 12 bytes

var odd = registry.Group().Own<Odd>().Build();
odd.Rows<Odd>().Vectors256(Lanes.Int); // throws ArgumentException
```

## See also

- [Groups](groups.md)
- [Storage](storage.md)
- [Views](views.md)
- [Registry](registry.md)
