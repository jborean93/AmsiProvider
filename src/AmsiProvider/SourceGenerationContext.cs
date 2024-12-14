using System.Text.Json.Serialization;

namespace AmsiProvider;

[JsonSerializable(typeof(ConfigJson))]
[JsonSerializable(typeof(ScanEntry))]
internal partial class SourceGenerationContext : JsonSerializerContext
{ }

internal record ConfigJson(
    string? LogPath,
    bool StoreByPid,
    string? ContentEncoding
);

internal record ScanEntry(
    string Action,
    string AppName,
    string ContentName,
    long SessionId,
    string Content
);
