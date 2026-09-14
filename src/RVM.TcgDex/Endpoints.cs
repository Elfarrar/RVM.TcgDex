using System.Text.Json.Serialization.Metadata;

namespace RVM.TcgDex;

/// <summary>
/// Cards, sets or series: <c>GetAsync</c> one by id, <c>ListAsync</c> the brief version of all of them.
/// Same shape as the <c>Endpoint</c> of the official SDKs.
/// </summary>
/// <typeparam name="TItem">Complete model (<see cref="Card"/>, <see cref="Set"/>, <see cref="Serie"/>).</typeparam>
/// <typeparam name="TResume">Brief model returned by lists.</typeparam>
public sealed class ResourceEndpoint<TItem, TResume>
    where TItem : class
    where TResume : class
{
    private readonly TCGdex _sdk;
    private readonly string _path;
    private readonly JsonTypeInfo<TItem> _item;
    private readonly JsonTypeInfo<List<TResume>> _list;

    internal ResourceEndpoint(TCGdex sdk, string path, JsonTypeInfo<TItem> item, JsonTypeInfo<List<TResume>> list)
    {
        _sdk = sdk;
        _path = path;
        _item = item;
        _list = list;
    }

    /// <summary>Fetches one item by id (a set or serie also accepts its name).</summary>
    /// <returns>The item, or <c>null</c> when it does not exist.</returns>
    /// <exception cref="TcgDexException">The API failed or returned an unexpected body.</exception>
    public Task<TItem?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return _sdk.FetchAsync([_path, id], null, _item, nullWhenNotFound: true, cancellationToken);
    }

    /// <summary>Lists all items, optionally filtered, sorted and paginated.</summary>
    /// <returns>The items; empty when nothing matches.</returns>
    /// <exception cref="TcgDexException">The API failed or returned an unexpected body.</exception>
    public async Task<IReadOnlyList<TResume>> ListAsync(Query? query = null, CancellationToken cancellationToken = default) =>
        (await _sdk.FetchAsync([_path], query, _list, nullWhenNotFound: false, cancellationToken).ConfigureAwait(false))!;

    internal async Task<TItem> GetRequiredAsync(string id, CancellationToken cancellationToken) =>
        (await _sdk.FetchAsync([_path, id], null, _item, nullWhenNotFound: false, cancellationToken).ConfigureAwait(false))!;
}

/// <summary>A random card, set or serie.</summary>
public sealed class RandomEndpoint
{
    private readonly TCGdex _sdk;

    internal RandomEndpoint(TCGdex sdk) => _sdk = sdk;

    /// <summary>Fetches a random card.</summary>
    public async Task<Card> GetCardAsync(CancellationToken cancellationToken = default) =>
        (await _sdk.FetchAsync(["random", "card"], null, Serialization.TcgDexJsonContext.Default.Card, false, cancellationToken).ConfigureAwait(false))!;

    /// <summary>Fetches a random set.</summary>
    public async Task<Set> GetSetAsync(CancellationToken cancellationToken = default) =>
        (await _sdk.FetchAsync(["random", "set"], null, Serialization.TcgDexJsonContext.Default.Set, false, cancellationToken).ConfigureAwait(false))!;

    /// <summary>Fetches a random serie.</summary>
    public async Task<Serie> GetSerieAsync(CancellationToken cancellationToken = default) =>
        (await _sdk.FetchAsync(["random", "serie"], null, Serialization.TcgDexJsonContext.Default.Serie, false, cancellationToken).ConfigureAwait(false))!;
}
