using System.Text.Json.Serialization;
using RVM.TcgDex.Serialization;
using TcgSet = RVM.TcgDex.Set;

namespace RVM.TcgDex;

/// <summary>
/// A complete card. Pokémon, Trainer and Energy share one type, as in the API: fields that do not
/// apply to the <see cref="Category"/> are <c>null</c>.
/// </summary>
public sealed class Card : CardResume
{
    /// <summary><c>Pokemon</c>, <c>Trainer</c> or <c>Energy</c>.</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Artist.</summary>
    public string? Illustrator { get; set; }

    /// <summary>Rarity, e.g. <c>Common</c>, <c>Rare Holo</c>, <c>Illustration rare</c>.</summary>
    public string? Rarity { get; set; }

    /// <summary>The set the card belongs to. <see cref="GetSetAsync"/> fetches it in full.</summary>
    public SetResume Set { get; set; } = new();

    /// <summary>Printings available (first generation of the variants system).</summary>
    public CardVariants? Variants { get; set; }

    /// <summary>Printings with details and per-variant pricing (second generation, replaces <see cref="Variants"/> in API v3).</summary>
    [JsonPropertyName("variants_detailed")]
    public IReadOnlyList<CardVariantDetail>? VariantsDetailed { get; set; }

    /// <summary>Boosters that contain the card; <c>null</c> when it is in every booster of the set.</summary>
    public IReadOnlyList<Booster>? Boosters { get; set; }

    /// <summary>Market prices; <c>null</c> when no marketplace lists the card.</summary>
    public CardPricing? Pricing { get; set; }

    /// <summary>Ids of the card on third-party marketplaces.</summary>
    public ThirdPartyIds? ThirdParty { get; set; }

    /// <summary>Last change to the card data (prices excluded).</summary>
    public DateTimeOffset? Updated { get; set; }

    /// <summary>Tournament legality. Every card is legal in Unlimited.</summary>
    public Legality Legal { get; set; } = new();

    /// <summary>Pokémon: National Pokédex numbers.</summary>
    public IReadOnlyList<int>? DexId { get; set; }

    /// <summary>Trainer/Energy: Pokédex numbers of the Pokémon pictured on the card.</summary>
    public IReadOnlyList<int>? CameoDexIds { get; set; }

    /// <summary>Pokémon: hit points.</summary>
    public int? Hp { get; set; }

    /// <summary>Pokémon: types (<c>Fire</c>, <c>Water</c>…).</summary>
    public IReadOnlyList<string>? Types { get; set; }

    /// <summary>Pokémon: name of the previous evolution.</summary>
    public string? EvolveFrom { get; set; }

    /// <summary>Pokémon: flavor text.</summary>
    public string? Description { get; set; }

    /// <summary>Pokémon: level; <c>X</c> on LV.X cards.</summary>
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Level { get; set; }

    /// <summary>Pokémon: stage (<c>Basic</c>, <c>Stage1</c>, <c>VMAX</c>…).</summary>
    public string? Stage { get; set; }

    /// <summary>Pokémon: suffix (<c>EX</c>, <c>GX</c>, <c>V</c>…).</summary>
    public string? Suffix { get; set; }

    /// <summary>Pokémon: held item.</summary>
    public CardItem? Item { get; set; }

    /// <summary>Pokémon: abilities.</summary>
    public IReadOnlyList<CardAbility>? Abilities { get; set; }

    /// <summary>Pokémon: attacks.</summary>
    public IReadOnlyList<CardAttack>? Attacks { get; set; }

    /// <summary>Pokémon: weaknesses.</summary>
    public IReadOnlyList<CardWeakRes>? Weaknesses { get; set; }

    /// <summary>Pokémon: resistances.</summary>
    public IReadOnlyList<CardWeakRes>? Resistances { get; set; }

    /// <summary>Pokémon: retreat cost.</summary>
    public int? Retreat { get; set; }

    /// <summary>Regulation mark (Sword &amp; Shield onwards), e.g. <c>D</c>.</summary>
    public string? RegulationMark { get; set; }

    /// <summary>Trainer/Energy: card text.</summary>
    public string? Effect { get; set; }

    /// <summary>Trainer: kind (<c>Item</c>, <c>Supporter</c>, <c>Stadium</c>…).</summary>
    public string? TrainerType { get; set; }

    /// <summary>Energy: kind (<c>Normal</c>, <c>Special</c>).</summary>
    public string? EnergyType { get; set; }

    /// <summary>Already complete: returns this instance.</summary>
    public override Task<Card> GetFullAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(this);

    /// <summary>Fetches the complete set of this card.</summary>
    /// <exception cref="TcgDexNotFoundException">The API no longer has the set.</exception>
    public Task<TcgSet> GetSetAsync(CancellationToken cancellationToken = default) =>
        Sdk.Set.GetRequiredAsync(Set.Id, cancellationToken);

    internal override void Attach(TCGdex sdk)
    {
        base.Attach(sdk);
        Set.Attach(sdk);
    }
}
