using System.Text.Json.Serialization;
using RVM.TcgDex.Serialization;

namespace RVM.TcgDex;

/// <summary>
/// One value of a catalog and the cards that have it (<c>StringEndpoint</c> in the API reference),
/// e.g. <c>tcgdex.DexIds.GetAsync(162)</c> → every Furret.
/// </summary>
public sealed class CatalogEntry : ISdkBound
{
    /// <summary>The value, as the API normalizes it (often lowercase: <c>trainer</c>, <c>mitsuhiro arita</c>).</summary>
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Name { get; set; } = string.Empty;

    /// <summary>Cards with this value; empty when none (the API answers an unknown value with no cards, not 404).</summary>
    public IReadOnlyList<CardResume> Cards { get; set; } = [];

    void ISdkBound.Attach(TCGdex sdk)
    {
        foreach (var card in Cards)
            card.Attach(sdk);
    }
}
