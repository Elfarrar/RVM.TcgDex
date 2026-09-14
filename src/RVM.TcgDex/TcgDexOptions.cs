namespace RVM.TcgDex;

/// <summary>
/// Where the SDK talks to and in which language. Mirrors <c>SetEndpoint</c> / <c>SetLanguage</c>
/// of the official TCGdex SDKs.
/// </summary>
public sealed class TcgDexOptions
{
    /// <summary>Public TCGdex REST v2 API.</summary>
    public const string DefaultEndpoint = "https://api.tcgdex.net/v2";

    /// <summary>API root. Change it to point at a self-hosted TCGdex.</summary>
    public string Endpoint { get; set; } = DefaultEndpoint;

    /// <summary>Language of the returned data. English by default, as in the official SDKs.</summary>
    public Language Language { get; set; } = Language.En;

    /// <summary>
    /// Base address every request is relative to: <c>{Endpoint}/{language code}/</c>. The trailing
    /// slash matters — without it, <see cref="Uri"/> drops the language segment when resolving
    /// <c>cards/swsh3-136</c>.
    /// </summary>
    public Uri BaseUri
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Endpoint))
                throw new InvalidOperationException("TcgDexOptions.Endpoint must be set.");

            return new Uri($"{Endpoint.Trim().TrimEnd('/')}/{Language.ToCode()}/");
        }
    }

    internal TcgDexOptions Clone() => new() { Endpoint = Endpoint, Language = Language };
}
