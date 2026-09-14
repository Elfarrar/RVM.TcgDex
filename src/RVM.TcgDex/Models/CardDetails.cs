using System.Text.Json.Serialization;
using RVM.TcgDex.Serialization;

namespace RVM.TcgDex;

/// <summary>Printings of a card (first generation of the variants system).</summary>
public sealed class CardVariants
{
    /// <summary>Non-foil printing.</summary>
    public bool Normal { get; set; }

    /// <summary>Everything but the illustration is foil.</summary>
    public bool Reverse { get; set; }

    /// <summary>The illustration is foil.</summary>
    public bool Holo { get; set; }

    /// <summary>1st Edition stamp (early series).</summary>
    public bool FirstEdition { get; set; }

    /// <summary>W Promo stamp.</summary>
    [JsonPropertyName("wPromo")]
    public bool WPromo { get; set; }
}

/// <summary>One printing of a card, with details and its own prices.</summary>
public sealed class CardVariantDetail
{
    /// <summary><c>normal</c>, <c>holo</c> or <c>reverse</c>.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Older sets only (e.g. Base Set shadowless).</summary>
    public string? Subtype { get; set; }

    /// <summary><c>standard</c> or <c>jumbo</c>.</summary>
    public string? Size { get; set; }

    /// <summary>Stamps (<c>1st-edition</c>, <c>pokemon-center</c>, <c>staff</c>…).</summary>
    public IReadOnlyList<string>? Stamp { get; set; }

    /// <summary>Foil pattern, for holo and reverse.</summary>
    public string? Foil { get; set; }

    /// <summary>Ids of this printing on third-party marketplaces.</summary>
    public ThirdPartyIds? ThirdParty { get; set; }

    /// <summary>Unique id of the printing.</summary>
    public string VariantId { get; set; } = string.Empty;

    /// <summary>Prices of this printing.</summary>
    public CardPricing? Pricing { get; set; }
}

/// <summary>Ids on third-party marketplaces.</summary>
public sealed class ThirdPartyIds
{
    /// <summary>Cardmarket product id.</summary>
    public long? Cardmarket { get; set; }

    /// <summary>TCGplayer product id.</summary>
    public long? Tcgplayer { get; set; }
}

/// <summary>Tournament legality.</summary>
public sealed class Legality
{
    /// <summary>Legal in Standard.</summary>
    public bool Standard { get; set; }

    /// <summary>Legal in Expanded.</summary>
    public bool Expanded { get; set; }
}

/// <summary>A booster pack.</summary>
public sealed class Booster
{
    /// <summary>Unique id.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Localized name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Logo URL.</summary>
    public string? Logo { get; set; }

    /// <summary>Front artwork URL.</summary>
    [JsonPropertyName("artwork_front")]
    public string? ArtworkFront { get; set; }

    /// <summary>Back artwork URL.</summary>
    [JsonPropertyName("artwork_back")]
    public string? ArtworkBack { get; set; }
}

/// <summary>An attack of a Pokémon.</summary>
public sealed class CardAttack
{
    /// <summary>Energy cost, in the printed order.</summary>
    public IReadOnlyList<string>? Cost { get; set; }

    /// <summary>Attack name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Attack text; <c>null</c> when there is none.</summary>
    public string? Effect { get; set; }

    /// <summary>Damage as printed: <c>90</c>, <c>30+</c>, <c>20×</c>.</summary>
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Damage { get; set; }
}

/// <summary>An ability of a Pokémon.</summary>
public sealed class CardAbility
{
    /// <summary>Ability kind (<c>Ability</c>, <c>Poke-POWER</c>, <c>Pokemon Power</c>…), localized.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Ability name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Ability text.</summary>
    public string? Effect { get; set; }
}

/// <summary>Weakness or resistance to a type.</summary>
public sealed class CardWeakRes
{
    /// <summary>The type (<c>Fire</c>, <c>Water</c>…).</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Modifier as printed: <c>×2</c>, <c>+20</c>, <c>-30</c>.</summary>
    public string? Value { get; set; }
}

/// <summary>Item held by a Pokémon.</summary>
public sealed class CardItem
{
    /// <summary>Item name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Item text.</summary>
    public string Effect { get; set; } = string.Empty;
}
