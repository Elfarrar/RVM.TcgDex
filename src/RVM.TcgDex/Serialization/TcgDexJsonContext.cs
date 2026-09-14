using System.Text.Json.Serialization;

namespace RVM.TcgDex.Serialization;

/// <summary>Source-generated serializers: no reflection at runtime, trimming-friendly.</summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(Card))]
[JsonSerializable(typeof(Set))]
[JsonSerializable(typeof(Serie))]
[JsonSerializable(typeof(List<CardResume>))]
[JsonSerializable(typeof(List<SetResume>))]
[JsonSerializable(typeof(List<SerieResume>))]
[JsonSerializable(typeof(CatalogEntry))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(List<int>))]
[JsonSerializable(typeof(ProblemDetails))]
internal sealed partial class TcgDexJsonContext : JsonSerializerContext;

/// <summary>Error body of the API (<c>application/problem+json</c>).</summary>
internal sealed class ProblemDetails
{
    public string? Type { get; set; }

    public string? Title { get; set; }

    public string? Details { get; set; }
}
