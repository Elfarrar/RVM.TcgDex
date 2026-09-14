namespace RVM.TcgDex;

/// <summary>
/// Where the SDK talks to and in which language. Mirrors <c>SetEndpoint</c> / <c>SetLanguage</c>
/// of the official TCGdex SDKs.
/// </summary>
public sealed class TcgDexOptions
{
    /// <summary>Public TCGdex REST v2 API.</summary>
    public const string DefaultEndpoint = "https://api.tcgdex.net/v2";

    /// <summary>Default language, as in the official SDKs.</summary>
    public const string DefaultLanguage = "en";

    /// <summary>API root. Change it to point at a self-hosted TCGdex.</summary>
    public string Endpoint { get; set; } = DefaultEndpoint;

    /// <summary>TCGdex language code (<c>en</c>, <c>fr</c>, <c>pt-br</c>...).</summary>
    public string Language { get; set; } = DefaultLanguage;

    /// <summary>
    /// Base address every request is relative to: <c>{Endpoint}/{Language}/</c>. The trailing slash
    /// matters — without it, <see cref="Uri"/> drops the language segment when resolving
    /// <c>cards/swsh3-136</c>.
    /// </summary>
    public Uri BaseUri
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Endpoint))
                throw new InvalidOperationException("TcgDexOptions.Endpoint must be set.");
            if (string.IsNullOrWhiteSpace(Language))
                throw new InvalidOperationException("TcgDexOptions.Language must be set.");

            return new Uri($"{Endpoint.TrimEnd('/')}/{Language.Trim().ToLowerInvariant()}/");
        }
    }
}
