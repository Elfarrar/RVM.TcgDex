using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Caching.Distributed;
using RVM.TcgDex.Caching;
using RVM.TcgDex.Serialization;
using CardModel = RVM.TcgDex.Card;
using SerieModel = RVM.TcgDex.Serie;
using SetModel = RVM.TcgDex.Set;

namespace RVM.TcgDex;

/// <summary>
/// Client of the TCGdex API. Same shape as the official SDKs:
/// <code>
/// var tcgdex = new TCGdex(Language.En);
/// var card = await tcgdex.Card.GetAsync("swsh3-136");
/// var furrets = await tcgdex.Card.ListAsync(Query.Create().Equal("name", "Furret"));
/// </code>
/// Named <c>TCGdex</c>, like the official SDKs, and not <c>TcgDex</c>: a type named after the last
/// segment of its namespace breaks name lookup for consumers whose namespace starts with <c>RVM.</c>.
/// </summary>
public sealed class TCGdex
{
    private static readonly Lazy<HttpClient> SharedHttpClient = new(() => new HttpClient(new SocketsHttpHandler
    {
        // Long-lived client without stale DNS: the pattern recommended when there is no IHttpClientFactory.
        PooledConnectionLifetime = TimeSpan.FromMinutes(15),
        AutomaticDecompression = DecompressionMethods.All,
    }));

    internal static readonly string UserAgent = BuildUserAgent();

    private readonly HttpClient _http;
    private readonly IDistributedCache _cache;
    private TcgDexOptions _options;

    /// <summary>Client over a shared <see cref="HttpClient"/>, pointing at the public API, with its own in-memory cache.</summary>
    public TCGdex(Language language = Language.En)
        : this(SharedHttpClient.Value, new TcgDexOptions { Language = language })
    {
    }

    /// <summary>Client over your own <see cref="HttpClient"/> (from <c>IHttpClientFactory</c>, a test handler…).</summary>
    /// <param name="httpClient">Sends the requests. Not disposed by the client.</param>
    /// <param name="options">Endpoint, language and cache TTL; copied, so later changes do not affect the client.</param>
    /// <param name="cache">
    /// Where responses are cached (Redis, <c>AddDistributedMemoryCache</c>…). <c>null</c> gives the client
    /// its own in-memory cache.
    /// </param>
    public TCGdex(HttpClient httpClient, TcgDexOptions? options = null, IDistributedCache? cache = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _http = httpClient;
        _cache = cache ?? new InMemoryCache();
        _options = Validated(options?.Clone() ?? new TcgDexOptions());

        var json = TcgDexJsonContext.Default;
        Card = new(this, "cards", json.Card, json.ListCardResume);
        Set = new(this, "sets", json.Set, json.ListSetResume);
        Serie = new(this, "series", json.Serie, json.ListSerieResume);
        Random = new(this);

        Types = new(this, "types", json.ListString);
        Hp = new(this, "hp", json.ListInt32);
        Illustrators = new(this, "illustrators", json.ListString);
        Rarities = new(this, "rarities", json.ListString);
        Categories = new(this, "categories", json.ListString);
        EnergyTypes = new(this, "energy-types", json.ListString);
        Retreats = new(this, "retreats", json.ListInt32);
        Stages = new(this, "stages", json.ListString);
        Suffixes = new(this, "suffixes", json.ListString);
        TrainerTypes = new(this, "trainer-types", json.ListString);
        DexIds = new(this, "dex-ids", json.ListInt32);
        RegulationMarks = new(this, "regulation-marks", json.ListString);
        Variants = new(this, "variants", json.ListString);
    }

    /// <summary>Cards.</summary>
    public ResourceEndpoint<CardModel, CardResume> Card { get; }

    /// <summary>Sets.</summary>
    public ResourceEndpoint<SetModel, SetResume> Set { get; }

    /// <summary>Series.</summary>
    public ResourceEndpoint<SerieModel, SerieResume> Serie { get; }

    /// <summary>Random card, set or serie.</summary>
    public RandomEndpoint Random { get; }

    /// <summary>Pokémon types (<c>Fire</c>, <c>Water</c>…).</summary>
    public CatalogEndpoint<string> Types { get; }

    /// <summary>HP values.</summary>
    public CatalogEndpoint<int> Hp { get; }

    /// <summary>Illustrators.</summary>
    public CatalogEndpoint<string> Illustrators { get; }

    /// <summary>Rarities.</summary>
    public CatalogEndpoint<string> Rarities { get; }

    /// <summary>Card categories (<c>Pokemon</c>, <c>Trainer</c>, <c>Energy</c>).</summary>
    public CatalogEndpoint<string> Categories { get; }

    /// <summary>Energy card kinds (<c>Normal</c>, <c>Special</c>).</summary>
    public CatalogEndpoint<string> EnergyTypes { get; }

    /// <summary>Retreat costs.</summary>
    public CatalogEndpoint<int> Retreats { get; }

    /// <summary>Pokémon stages (<c>Basic</c>, <c>Stage1</c>…).</summary>
    public CatalogEndpoint<string> Stages { get; }

    /// <summary>Name suffixes (<c>EX</c>, <c>GX</c>, <c>V</c>…).</summary>
    public CatalogEndpoint<string> Suffixes { get; }

    /// <summary>Trainer card kinds (<c>Item</c>, <c>Supporter</c>…).</summary>
    public CatalogEndpoint<string> TrainerTypes { get; }

    /// <summary>National Pokédex numbers.</summary>
    public CatalogEndpoint<int> DexIds { get; }

    /// <summary>Regulation marks (<c>D</c>, <c>E</c>…).</summary>
    public CatalogEndpoint<string> RegulationMarks { get; }

    /// <summary>Printing kinds (<c>normal</c>, <c>holo</c>, <c>reverse</c>…).</summary>
    public CatalogEndpoint<string> Variants { get; }

    /// <summary>Language of the returned data.</summary>
    public Language Language => _options.Language;

    /// <summary>API root.</summary>
    public string Endpoint => _options.Endpoint;

    /// <summary>How long responses stay cached; <see cref="TimeSpan.Zero"/> means no cache.</summary>
    public TimeSpan CacheTtl => _options.CacheTtl;

    /// <summary>Changes how long the next responses stay cached. <see cref="TimeSpan.Zero"/> turns the cache off.</summary>
    public void SetCacheTtl(TimeSpan ttl)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(ttl, TimeSpan.Zero);

        var next = _options.Clone();
        next.CacheTtl = ttl;
        _options = Validated(next);
    }

    /// <summary>
    /// Changes the language of the next requests. Requests are thread-safe; changing the language
    /// or the endpoint while other threads use the instance is not — use one instance per language.
    /// </summary>
    public void SetLanguage(Language language)
    {
        var next = _options.Clone();
        next.Language = language;
        _options = Validated(next);
    }

    /// <summary>Points the client at another TCGdex (self-hosted), e.g. <c>https://tcgdex.example.com/v2</c>.</summary>
    /// <exception cref="ArgumentException"><paramref name="endpoint"/> is not an absolute URL.</exception>
    public void SetEndpoint(string endpoint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
        if (!Uri.TryCreate(endpoint.Trim(), UriKind.Absolute, out _))
            throw new ArgumentException($"'{endpoint}' is not an absolute URL.", nameof(endpoint));

        var next = _options.Clone();
        next.Endpoint = endpoint;
        _options = Validated(next);
    }

    internal Task<CardModel?> GetCardOfSetAsync(string setId, string localId, CancellationToken cancellationToken) =>
        FetchAsync(["sets", setId, localId], null, TcgDexJsonContext.Default.Card, nullWhenNotFound: true, cancellationToken);

    internal async Task<T?> FetchAsync<T>(string[] path, Query? query, JsonTypeInfo<T> typeInfo, bool nullWhenNotFound, CancellationToken cancellationToken)
        where T : class
    {
        var options = _options;
        var uri = new Uri(options.BaseUri, string.Join('/', path.Select(EscapeSegment)) + query);
        var cacheKey = uri.AbsoluteUri;
        var caching = options.CacheTtl > TimeSpan.Zero;

        if (caching && await ReadCacheAsync(cacheKey, cancellationToken).ConfigureAwait(false) is { } cached)
            return Materialize(cached, typeInfo, null, uri);

        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var problem = await ReadProblemAsync(response, cancellationToken).ConfigureAwait(false);

            // A 404 can also mean "language not available" (self-hosted without it): that is an error,
            // not a missing card. Only the not-found problem — or a body we cannot read — means absent.
            var notFound = response.StatusCode == HttpStatusCode.NotFound
                && (problem?.Type is null || problem.Type.EndsWith("/not-found", StringComparison.Ordinal));

            if (notFound && nullWhenNotFound)
                return null;
            if (notFound)
                throw new TcgDexNotFoundException(Describe(problem, $"Not found: {uri}"), uri);

            throw new TcgDexException(
                Describe(problem, $"TCGdex API answered {(int)response.StatusCode} {response.ReasonPhrase}."),
                response.StatusCode, uri);
        }

        var body = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        var result = Materialize(body, typeInfo, response.StatusCode, uri);

        // The raw JSON is cached, not the object: every hit gets a fresh model, so a caller that
        // mutates one does not change what the next caller sees. Only valid bodies get here.
        if (caching)
            await WriteCacheAsync(cacheKey, body, options.CacheTtl, cancellationToken).ConfigureAwait(false);

        return result;
    }

    private T Materialize<T>(byte[] body, JsonTypeInfo<T> typeInfo, HttpStatusCode? status, Uri uri)
        where T : class
    {
        T? result;
        try
        {
            result = JsonSerializer.Deserialize(body, typeInfo);
        }
        catch (JsonException ex)
        {
            throw new TcgDexException("TCGdex API returned a body that is not the expected JSON.", status, uri, ex);
        }

        if (result is null)
            throw new TcgDexException("TCGdex API returned an empty body.", status, uri);

        Attach(result);
        return result;
    }

    // A cache outage (Redis down…) must not stop requests: it only costs a trip to the API.
    private async Task<byte[]?> ReadCacheAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            return await _cache.GetAsync(key, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return null;
        }
    }

    private async Task WriteCacheAsync(string key, byte[] body, TimeSpan ttl, CancellationToken cancellationToken)
    {
        try
        {
            await _cache.SetAsync(key, body, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl }, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Same as above: the response is already in hand.
        }
    }

    private static string EscapeSegment(string segment)
    {
        // EscapeDataString keeps dots, and Uri resolves "." and ".." even when escaped: an id ".."
        // would silently hit another endpoint (or leave /v2/{lang}/) instead of returning not found.
        if (segment.Trim() is "." or "..")
            throw new ArgumentException($"'{segment}' is not a valid TCGdex id.", nameof(segment));

        return Uri.EscapeDataString(segment);
    }

    private void Attach(object result)
    {
        if (result is ISdkBound model)
            model.Attach(this);
        else if (result is IEnumerable<ISdkBound> models)
            foreach (var item in models)
                item.Attach(this);
    }

    private static async Task<ProblemDetails?> ReadProblemAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return string.IsNullOrWhiteSpace(body) ? null : JsonSerializer.Deserialize(body, TcgDexJsonContext.Default.ProblemDetails);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string Describe(ProblemDetails? problem, string fallback) => problem?.Title switch
    {
        null => fallback,
        var title when problem.Details is not null => $"{title}. {problem.Details}",
        var title => title,
    };

    private static TcgDexOptions Validated(TcgDexOptions options)
    {
        _ = options.BaseUri; // throws on an unusable endpoint now, not on the first request
        if (options.CacheTtl < TimeSpan.Zero)
            throw new InvalidOperationException("TcgDexOptions.CacheTtl cannot be negative.");
        return options;
    }

    private static string BuildUserAgent()
    {
        var version = typeof(TCGdex).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";
        var plus = version.IndexOf('+', StringComparison.Ordinal);
        return $"RVM.TcgDex/{(plus < 0 ? version : version[..plus])} (+https://github.com/Elfarrar/RVM.TcgDex)";
    }
}
