using System.Text.Json.Serialization;

namespace RVM.TcgDex;

/// <summary>Market prices of a card. A marketplace that does not list the card is <c>null</c>.</summary>
public sealed class CardPricing
{
    /// <summary>Cardmarket (Europe), in EUR.</summary>
    public CardmarketPricing? Cardmarket { get; set; }

    /// <summary>TCGplayer (North America), in USD, per printing.</summary>
    public TcgplayerPricing? Tcgplayer { get; set; }
}

/// <summary>Cardmarket prices, in <see cref="Unit"/> (EUR). Non-foil and foil (<c>…Holo</c>).</summary>
public sealed class CardmarketPricing
{
    /// <summary>When TCGdex fetched the prices.</summary>
    public DateTimeOffset? Updated { get; set; }

    /// <summary>Currency, <c>EUR</c>.</summary>
    public string? Unit { get; set; }

    /// <summary>Cardmarket product id.</summary>
    public long? IdProduct { get; set; }

    /// <summary>Average selling price (non-foil).</summary>
    public decimal? Avg { get; set; }

    /// <summary>Lowest price (non-foil).</summary>
    public decimal? Low { get; set; }

    /// <summary>Trend price (non-foil).</summary>
    public decimal? Trend { get; set; }

    /// <summary>Average of the last day (non-foil).</summary>
    public decimal? Avg1 { get; set; }

    /// <summary>Average of the last 7 days (non-foil).</summary>
    public decimal? Avg7 { get; set; }

    /// <summary>Average of the last 30 days (non-foil).</summary>
    public decimal? Avg30 { get; set; }

    /// <summary>Average selling price (foil).</summary>
    [JsonPropertyName("avg-holo")]
    public decimal? AvgHolo { get; set; }

    /// <summary>Lowest price (foil).</summary>
    [JsonPropertyName("low-holo")]
    public decimal? LowHolo { get; set; }

    /// <summary>Trend price (foil).</summary>
    [JsonPropertyName("trend-holo")]
    public decimal? TrendHolo { get; set; }

    /// <summary>Average of the last day (foil).</summary>
    [JsonPropertyName("avg1-holo")]
    public decimal? Avg1Holo { get; set; }

    /// <summary>Average of the last 7 days (foil).</summary>
    [JsonPropertyName("avg7-holo")]
    public decimal? Avg7Holo { get; set; }

    /// <summary>Average of the last 30 days (foil).</summary>
    [JsonPropertyName("avg30-holo")]
    public decimal? Avg30Holo { get; set; }
}

/// <summary>TCGplayer prices, in <see cref="Unit"/> (USD). Each printing has its own prices.</summary>
public sealed class TcgplayerPricing
{
    /// <summary>When TCGdex fetched the prices.</summary>
    public DateTimeOffset? Updated { get; set; }

    /// <summary>Currency, <c>USD</c>.</summary>
    public string? Unit { get; set; }

    /// <summary>Non-foil.</summary>
    public TcgplayerPrice? Normal { get; set; }

    /// <summary>Holofoil.</summary>
    public TcgplayerPrice? Holofoil { get; set; }

    /// <summary>Reverse holofoil.</summary>
    [JsonPropertyName("reverse-holofoil")]
    public TcgplayerPrice? ReverseHolofoil { get; set; }

    /// <summary>1st Edition.</summary>
    [JsonPropertyName("1st-edition")]
    public TcgplayerPrice? FirstEdition { get; set; }

    /// <summary>1st Edition holofoil.</summary>
    [JsonPropertyName("1st-edition-holofoil")]
    public TcgplayerPrice? FirstEditionHolofoil { get; set; }

    /// <summary>Unlimited.</summary>
    public TcgplayerPrice? Unlimited { get; set; }

    /// <summary>Unlimited holofoil.</summary>
    [JsonPropertyName("unlimited-holofoil")]
    public TcgplayerPrice? UnlimitedHolofoil { get; set; }
}

/// <summary>TCGplayer prices of one printing.</summary>
public sealed class TcgplayerPrice
{
    /// <summary>TCGplayer product id.</summary>
    public long? ProductId { get; set; }

    /// <summary>Lowest listing.</summary>
    public decimal? LowPrice { get; set; }

    /// <summary>Median listing.</summary>
    public decimal? MidPrice { get; set; }

    /// <summary>Highest listing.</summary>
    public decimal? HighPrice { get; set; }

    /// <summary>Market price (recent sales).</summary>
    public decimal? MarketPrice { get; set; }

    /// <summary>Lowest TCGplayer Direct listing.</summary>
    public decimal? DirectLowPrice { get; set; }
}
