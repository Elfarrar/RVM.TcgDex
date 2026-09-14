using TcgSerie = RVM.TcgDex.Serie;

namespace RVM.TcgDex;

/// <summary>A complete set, with its cards.</summary>
public sealed class Set : ISdkBound
{
    private TCGdex? _sdk;

    /// <summary>Unique id, e.g. <c>swsh3</c>.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Localized name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Logo URL without extension. Prefer <see cref="GetLogoUrl"/>.</summary>
    public string? Logo { get; set; }

    /// <summary>Symbol URL without extension. Prefer <see cref="GetSymbolUrl"/>.</summary>
    public string? Symbol { get; set; }

    /// <summary>Number of cards, per printing.</summary>
    public SetCardCount CardCount { get; set; } = new();

    /// <summary>The serie of the set. <see cref="GetSerieAsync"/> fetches it in full.</summary>
    public SerieResume Serie { get; set; } = new();

    /// <summary>Pokémon TCG Online code.</summary>
    public string? TcgOnline { get; set; }

    /// <summary>Release date.</summary>
    public DateOnly? ReleaseDate { get; set; }

    /// <summary>Tournament legality of the set (a banned card does not change it).</summary>
    public Legality Legal { get; set; } = new();

    /// <summary>Abbreviation printed on the cards.</summary>
    public SetAbbreviation? Abbreviation { get; set; }

    /// <summary>Boosters of the set.</summary>
    public IReadOnlyList<Booster>? Boosters { get; set; }

    /// <summary>Cards of the set.</summary>
    public IReadOnlyList<CardResume> Cards { get; set; } = [];

    /// <summary>Full logo URL; <c>null</c> when the set has no logo.</summary>
    public string? GetLogoUrl(Extension extension = Extension.Png) => AssetUrl.Logo(Logo, extension);

    /// <summary>Full symbol URL; <c>null</c> when the set has no symbol.</summary>
    public string? GetSymbolUrl(Extension extension = Extension.Png) => AssetUrl.Logo(Symbol, extension);

    /// <summary>Fetches the complete serie of this set.</summary>
    /// <exception cref="TcgDexNotFoundException">The API no longer has the serie.</exception>
    public Task<TcgSerie> GetSerieAsync(CancellationToken cancellationToken = default) =>
        Sdk.Serie.GetRequiredAsync(Serie.Id, cancellationToken);

    /// <summary>Fetches a card of this set by its local id (the printed number).</summary>
    /// <returns>The card, or <c>null</c> when the set has no card with that local id.</returns>
    public Task<Card?> GetCardAsync(string localId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localId);
        return Sdk.GetCardOfSetAsync(Id, localId, cancellationToken);
    }

    private TCGdex Sdk => SdkBinding.Require(_sdk);

    void ISdkBound.Attach(TCGdex sdk)
    {
        _sdk = sdk;
        Serie.Attach(sdk);
        foreach (var card in Cards)
            card.Attach(sdk);
    }
}

/// <summary>Number of cards of a set, per printing.</summary>
public sealed class SetCardCount
{
    /// <summary>All cards, secret ones included.</summary>
    public int Total { get; set; }

    /// <summary>Printed on the cards (the "/189" of "136/189").</summary>
    public int Official { get; set; }

    /// <summary>Cards with a non-foil printing.</summary>
    public int? Normal { get; set; }

    /// <summary>Cards with a reverse printing.</summary>
    public int? Reverse { get; set; }

    /// <summary>Cards with a holo printing.</summary>
    public int? Holo { get; set; }

    /// <summary>Cards with a 1st Edition printing.</summary>
    public int? FirstEd { get; set; }
}

/// <summary>Set abbreviation.</summary>
public sealed class SetAbbreviation
{
    /// <summary>Official abbreviation printed on English cards, e.g. <c>DAA</c>.</summary>
    public string? Official { get; set; }
}
