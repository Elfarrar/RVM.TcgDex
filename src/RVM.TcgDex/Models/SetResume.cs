namespace RVM.TcgDex;

/// <summary>Set as it appears in lists, in a serie and in a card (<c>SetBrief</c> in the API reference).</summary>
public sealed class SetResume : ISdkBound
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

    /// <summary>Number of cards.</summary>
    public CardCountResume CardCount { get; set; } = new();

    /// <summary>Full logo URL; <c>null</c> when the set has no logo.</summary>
    public string? GetLogoUrl(Extension extension = Extension.Png) => AssetUrl.Logo(Logo, extension);

    /// <summary>Full symbol URL; <c>null</c> when the set has no symbol.</summary>
    public string? GetSymbolUrl(Extension extension = Extension.Png) => AssetUrl.Logo(Symbol, extension);

    /// <summary>Fetches the complete set, with its cards.</summary>
    /// <exception cref="TcgDexNotFoundException">The API no longer has the set.</exception>
    public Task<Set> GetFullAsync(CancellationToken cancellationToken = default) =>
        SdkBinding.Require(_sdk).Set.GetRequiredAsync(Id, cancellationToken);

    void ISdkBound.Attach(TCGdex sdk) => Attach(sdk);

    internal void Attach(TCGdex sdk) => _sdk = sdk;
}

/// <summary>Number of cards of a set, as in lists.</summary>
public sealed class CardCountResume
{
    /// <summary>All cards, secret ones included.</summary>
    public int Total { get; set; }

    /// <summary>Printed on the cards (the "/189" of "136/189").</summary>
    public int Official { get; set; }
}
