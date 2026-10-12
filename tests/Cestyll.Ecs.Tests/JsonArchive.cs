using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Cestyll.Ecs;
using Cestyll.Ecs.Snapshots;

namespace Cestyll.Ecs.Tests;

// Self-describing JSON archive: one array per section key, entities as [index, version].
// Sections read in any order; a missing or partly read section throws.
internal sealed class JsonArchive<TEntity> : ISnapshotWriter<TEntity>, ISnapshotReader<TEntity>
    where TEntity : unmanaged, IEntity<TEntity>
{
    private static readonly JsonSerializerOptions _options = new()
    {
        IncludeFields = true,
        Converters = { new EntityConverter() },
    };

    private readonly JsonObject _sections;
    private readonly bool _reading;
    private readonly HashSet<string> _read = [];
    private string? _key;
    private JsonArray? _items;
    private int _next;

    public JsonArchive() : this([], reading: false) { }

    private JsonArchive(JsonObject sections, bool reading) => (_sections, _reading) = (sections, reading);

    public bool AllRead => _read.Count == _sections.Count;

    public string ToJson() => _sections.ToJsonString();

    public static JsonArchive<TEntity> Parse(string json) => new(JsonNode.Parse(json)!.AsObject(), reading: true);

    public JsonArchive<TEntity> Reopen() => Parse(ToJson());

    // Splices in sections from another registry's archive.
    public JsonArchive<TEntity> Append(JsonArchive<TEntity> other)
    {
        foreach (var (key, items) in other._sections)
        {
            _sections.Add(key, items?.DeepClone());
        }

        return this;
    }

    public void BeginSection(SnapshotSection kind, Type type, string? name)
    {
        _key = kind == SnapshotSection.Entities ? "entities"
            : name is null ? $"{kind}:{type.Name}"
            : $"{kind}:{type.Name}/{name}";
        _next = 0;
        if (!_reading)
        {
            _items = [];
        }
        else if (_sections[_key] is JsonArray items)
        {
            _items = items;
            _read.Add(_key);
        }
        else
        {
            throw new InvalidDataException($"Archive has no section {_key}");
        }
    }

    // A writer adds the section it built; a reader checks it read all of it.
    public void EndSection()
    {
        if (!_reading)
        {
            _sections.Add(_key!, Items);
        }
        else if (_next != Items.Count)
        {
            throw new InvalidDataException($"Section {_key} has {Items.Count - _next} items left");
        }

        _items = null;
    }

    public void WriteCount(int count) => Items.Add(count);

    public void WriteEntity(TEntity entity) => Items.Add(new JsonArray(entity.Index, entity.Version));

    public void Write<T>(in T value) => Items.Add(JsonSerializer.SerializeToNode(value, _options));

    public void WriteComponents<T>(ReadOnlySpan<T> values)
    {
        foreach (var value in values)
        {
            Write(value);
        }
    }

    public int ReadCount() => Next().GetValue<int>();

    public TEntity ReadEntity()
    {
        var pair = Next().AsArray();
        return TEntity.Make(pair[0]!.GetValue<int>(), pair[1]!.GetValue<uint>());
    }

    public T Read<T>() => Next().Deserialize<T>(_options)!;

    public void ReadComponents<T>(Span<T> values)
    {
        for (var i = 0; i < values.Length; i++)
        {
            values[i] = Read<T>();
        }
    }

    private JsonArray Items => _items ?? throw new InvalidOperationException("No section begun");

    private JsonNode Next() =>
        _next < Items.Count
            ? Items[_next++]!
            : throw new InvalidDataException($"Section {_key} ended");

    private sealed class EntityConverter : JsonConverter<TEntity>
    {
        public override TEntity Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var pair = JsonSerializer.Deserialize<uint[]>(ref reader)!;
            return TEntity.Make((int)pair[0], pair[1]);
        }

        public override void Write(Utf8JsonWriter writer, TEntity value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(value.Index);
            writer.WriteNumberValue(value.Version);
            writer.WriteEndArray();
        }
    }
}
