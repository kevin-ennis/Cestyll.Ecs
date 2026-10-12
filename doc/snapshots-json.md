# JSON snapshots

A snapshot archive over `System.Text.Json` that writes each section as an object in one JSON array. It implements the archive interfaces from [Snapshots](snapshots.md), which covers the section calls and the loaders. Types live in `Cestyll.Ecs.Snapshots`.

```csharp
using Cestyll.Ecs;
using Cestyll.Ecs.Snapshots;
using Cestyll.Ecs.Tags;

struct Position : IComponent { public float X, Y; }
struct Frozen : ITag { }
```

## The archive

Each section becomes an object with its kind, type and name, and the reader checks the kind and type. `JsonSerializer` handles every value, so no codecs are needed, and `IncludeFields` lets it see public fields.

```csharp
using System.Text.Json;

sealed class JsonSnapshotWriter(Utf8JsonWriter json, JsonSerializerOptions options) : ISnapshotWriter<Entity32>
{
    public void BeginSection(SnapshotSection kind, Type type, string? name)
    {
        json.WriteStartObject();
        json.WriteString("kind", kind.ToString());
        json.WriteString("type", type.Name);
        json.WriteString("name", name);
        json.WriteStartArray("data");
    }

    public void EndSection()
    {
        json.WriteEndArray();
        json.WriteEndObject();
    }

    public void WriteCount(int count)
    {
        json.WriteNumberValue(count);
    }

    public void WriteEntity(Entity32 entity)
    {
        json.WriteNumberValue(entity.Raw);
    }

    public void Write<T>(in T value)
    {
        JsonSerializer.Serialize(json, value, options);
    }

    public void WriteComponents<T>(ReadOnlySpan<T> values)
    {
        foreach (var value in values)
        {
            Write(in value);
        }
    }
}

sealed class JsonSnapshotReader(JsonElement root, JsonSerializerOptions options) : ISnapshotReader<Entity32>
{
    private JsonElement.ArrayEnumerator _sections = root.EnumerateArray();
    private JsonElement.ArrayEnumerator _data;

    public void BeginSection(SnapshotSection kind, Type type, string? name)
    {
        if (!_sections.MoveNext()
            || _sections.Current.GetProperty("kind").GetString() != kind.ToString()
            || _sections.Current.GetProperty("type").GetString() != type.Name)
        {
            throw new InvalidDataException($"Expected a {kind} section for {type.Name}");
        }

        _data = _sections.Current.GetProperty("data").EnumerateArray();
    }

    public void EndSection()
    {
        if (_data.MoveNext())
        {
            throw new InvalidDataException("Section has data left over");
        }
    }

    public int ReadCount()
    {
        return Next().GetInt32();
    }

    public Entity32 ReadEntity()
    {
        return new Entity32(Next().GetUInt32());
    }

    public T Read<T>()
    {
        return Next().Deserialize<T>(options)!;
    }

    public void ReadComponents<T>(Span<T> values)
    {
        for (var i = 0; i < values.Length; i++)
        {
            values[i] = Read<T>();
        }
    }

    private JsonElement Next()
    {
        return _data.MoveNext() ? _data.Current : throw new InvalidDataException("Section ran out of data");
    }
}
```

## Saving and loading

Write the array with a `Utf8JsonWriter`, and read it back from a `JsonDocument`.

```csharp
var options = new JsonSerializerOptions { IncludeFields = true };

using (var file = File.Create("save.json"))
using (var json = new Utf8JsonWriter(file, new JsonWriterOptions { Indented = true }))
{
    var w = new JsonSnapshotWriter(json, options);

    json.WriteStartArray();
    registry.Snapshot<JsonSnapshotWriter>()
        .Entities(ref w)
        .Component<Position>(ref w)
        .Tag<Frozen>(ref w);
    json.WriteEndArray();
}

using var document = JsonDocument.Parse(File.ReadAllBytes("save.json"));
var r = new JsonSnapshotReader(document.RootElement, options);

new Registry().SnapshotLoader<JsonSnapshotReader>()
    .Entities(ref r)
    .Component<Position>(ref r)
    .Tag<Frozen>(ref r);
```

## Output

With the registry from [Saving](snapshots.md#saving), `save.json` holds the following. Each `data` array holds the archive calls in order. The entity section is how many ids have been handed out, each id, then how many are free and those ids. A component section is a count, the entities, then their values. Each entity is written as its raw value, which here equals its index because no entity has been reused.

```json
[
  {
    "kind": "Entities",
    "type": "Entity32",
    "name": null,
    "data": [
      2,
      1,
      2,
      0
    ]
  },
  {
    "kind": "Component",
    "type": "Position",
    "name": null,
    "data": [
      2,
      1,
      2,
      {
        "X": 1,
        "Y": 0
      },
      {
        "X": 10,
        "Y": 0
      }
    ]
  },
  {
    "kind": "Tag",
    "type": "Frozen",
    "name": null,
    "data": [
      1,
      2
    ]
  }
]
```

## See also

- [Snapshots](snapshots.md)
- [Registry](registry.md)
