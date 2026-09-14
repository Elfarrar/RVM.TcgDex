using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
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
    private TcgDexOptions _options;

    /// <summary>Client over a shared <see cref="HttpClient"/>, pointing at the public API.</summary>
    public TCGdex(Language language = Language.En)
        : this(SharedHttpClient.Value, new TcgDexOptions { Language = language })
    {
    }

    /// <summary>Client over your own <see cref="HttpClient"/> (from <c>IHttpClientFactory</c>, a test handler…).</summary>
    public TCGdex(HttpClient httpClient, TcgDexOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _http = httpClient;
        _options = Validated(options?.Clone() ?? new TcgDexOptions());

        Card = new(this, "cards", TcgDexJsonContext.Default.Card, TcgDexJsonContext.Default.ListCardResume);
        Set = new(this, "sets", TcgDexJsonContext.Default.Set, TcgDexJsonContext.Default.ListSetResume);
        Serie = new(this, "series", TcgDexJsonContext.Default.Serie, TcgDexJsonContext.Default.ListSerieResume);
        Random = new(this);
    }

    /// <summary>Cards.</summary>
    public ResourceEndpoint<CardModel, CardResume> Card { get; }

    /// <summary>Sets.</summary>
    public ResourceEndpoint<SetModel, SetResume> Set { get; }

    /// <summary>Series.</summary>
    public ResourceEndpoint<SerieModel, SerieResume> Serie { get; }

    /// <summary>Random card, set or serie.</summary>
    public RandomEndpoint Random { get; }

    /// <summary>Language of the returned data.</summary>
    public Language Language => _options.Language;

    /// <summary>API root.</summary>
    public string Endpoint => _options.Endpoint;

    /// <summary>Changes the language of the next requests.</summary>
    public void SetLanguage(Language language)
    {
        var next = _options.Clone();
        next.Language = language;
        _options = Validated(next);
    }

    /// <summary>Points the client at another TCGdex (self-hosted), e.g. <c>https://tcgdex.example.com/v2</c>.</summary>
    public void SetEndpoint(string endpoint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);

        var next = _options.Clone();
        next.Endpoint = endpoint;
        _options = Validated(next);
    }

    internal Task<CardModel?> GetCardOfSetAsync(string setId, string localId, CancellationToken cancellationToken) =>
        FetchAsync(["sets", setId, localId], null, TcgDexJsonContext.Default.Card, nullWhenNotFound: true, cancellationToken);

    internal async Task<T?> FetchAsync<T>(string[] path, Query? query, JsonTypeInfo<T> typeInfo, bool nullWhenNotFound, CancellationToken cancellationToken)
        where T : class
    {
        var uri = new Uri(_options.BaseUri, string.Join('/', path.Select(segment => Uri.EscapeDataString(segment))) + query);

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

        T? result;
        try
        {
            var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await using (stream.ConfigureAwait(false))
                result = await JsonSerializer.DeserializeAsync(stream, typeInfo, cancellationToken).ConfigureAwait(false);
        }
        catch (JsonException ex)
        {
            throw new TcgDexException("TCGdex API returned a body that is not the expected JSON.", response.StatusCode, uri, ex);
        }

        if (result is null)
            throw new TcgDexException("TCGdex API returned an empty body.", response.StatusCode, uri);

        Attach(result);
        return result;
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
        return options;
    }

    private static string BuildUserAgent()
    {
        var version = typeof(TCGdex).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";
        var plus = version.IndexOf('+', StringComparison.Ordinal);
        return $"RVM.TcgDex/{(plus < 0 ? version : version[..plus])} (+https://github.com/Elfarrar/RVM.TcgDex)";
    }
}
