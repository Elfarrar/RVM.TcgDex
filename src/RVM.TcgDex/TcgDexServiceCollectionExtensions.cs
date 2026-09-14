using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using RVM.TcgDex;
using RVM.TcgDex.Caching;

// Same namespace as AddHttpClient & co., so `services.AddTcgDex()` shows up without an extra using.
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Registers <see cref="TCGdex"/> in the dependency injection container.</summary>
public static class TcgDexServiceCollectionExtensions
{
    /// <summary>Name of the <see cref="HttpClient"/> the client uses, for <c>IHttpClientFactory</c> configuration.</summary>
    public const string HttpClientName = "RVM.TcgDex";

    /// <summary>
    /// Registers <see cref="TCGdex"/> over <c>IHttpClientFactory</c>:
    /// <code>
    /// services.AddTcgDex(o => o.Language = Language.PtBr);
    /// // then inject TCGdex anywhere
    /// </code>
    /// Every resolved instance gets its own copy of the options (so <c>SetLanguage</c> on one does not
    /// change the others) and they all share one cache: the container's <see cref="IDistributedCache"/>
    /// when there is one, otherwise an in-memory cache.
    /// </summary>
    /// <returns>The builder of the underlying <see cref="HttpClient"/>, to add handlers, resilience, timeouts…</returns>
    public static IHttpClientBuilder AddTcgDex(this IServiceCollection services, Action<TcgDexOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = services.AddOptions<TcgDexOptions>();
        if (configure is not null)
            options.Configure(configure);

        services.TryAddSingleton(_ => new InMemoryCache());
        services.TryAddTransient(provider => new TCGdex(
            provider.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName),
            provider.GetRequiredService<IOptions<TcgDexOptions>>().Value,
            provider.GetService<IDistributedCache>() ?? provider.GetRequiredService<InMemoryCache>()));

        return services.AddHttpClient(HttpClientName);
    }
}
