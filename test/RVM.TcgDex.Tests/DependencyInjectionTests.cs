using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;

namespace RVM.TcgDex.Tests;

public sealed class DependencyInjectionTests
{
    private static ServiceProvider Build(FakeApi api, Action<IServiceCollection>? extra = null, Action<TcgDexOptions>? configure = null)
    {
        var services = new ServiceCollection();
        extra?.Invoke(services);
        services.AddTcgDex(configure).ConfigurePrimaryHttpMessageHandler(() => api);
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Resolves_WithTheConfiguredOptions_OverTheFactoryClient()
    {
        var api = new FakeApi().Returns("/v2/pt-br/cards/A1-001", "card-A1-001.pt-br.json");
        using var provider = Build(api, configure: o => { o.Language = Language.PtBr; o.CacheTtl = TimeSpan.FromMinutes(10); });

        var tcgdex = provider.GetRequiredService<TCGdex>();
        var card = await tcgdex.Card.GetAsync("A1-001");

        Assert.Equal(Language.PtBr, tcgdex.Language);
        Assert.Equal(TimeSpan.FromMinutes(10), tcgdex.CacheTtl);
        Assert.Equal("Dominação Genética", card!.Set.Name);
    }

    [Fact]
    public async Task Instances_AreIndependent_ButShareOneCache()
    {
        var api = new FakeApi().Returns("/v2/en/cards/swsh3-136", "card-swsh3-136.json");
        using var provider = Build(api);

        var first = provider.GetRequiredService<TCGdex>();
        var second = provider.GetRequiredService<TCGdex>();
        await first.Card.GetAsync("swsh3-136");
        await second.Card.GetAsync("swsh3-136");
        first.SetLanguage(Language.Fr);

        Assert.NotSame(first, second);
        Assert.Single(api.Requests);
        Assert.Equal(Language.En, second.Language);
    }

    [Fact]
    public async Task UsesTheContainersDistributedCache_WhenThereIsOne()
    {
        var api = new FakeApi().Returns("/v2/en/cards/swsh3-136", "card-swsh3-136.json");
        using var provider = Build(api, services => services.AddDistributedMemoryCache());

        await provider.GetRequiredService<TCGdex>().Card.GetAsync("swsh3-136");

        var cache = provider.GetRequiredService<IDistributedCache>();
        Assert.NotNull(await cache.GetAsync("https://api.tcgdex.net/v2/en/cards/swsh3-136"));
    }

    [Fact]
    public void Defaults_WithoutConfigure()
    {
        using var provider = Build(new FakeApi());

        var tcgdex = provider.GetRequiredService<TCGdex>();

        Assert.Equal(Language.En, tcgdex.Language);
        Assert.Equal(TcgDexOptions.DefaultEndpoint, tcgdex.Endpoint);
    }

    [Fact]
    public void CallingTwice_DoesNotRegisterTwice()
    {
        var services = new ServiceCollection();

        services.AddTcgDex();
        services.AddTcgDex(o => o.Language = Language.De);
        using var provider = services.BuildServiceProvider();

        Assert.Single(services, d => d.ServiceType == typeof(TCGdex));
        Assert.Equal(Language.De, provider.GetRequiredService<TCGdex>().Language);
    }

    [Fact]
    public void NullServices_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddTcgDex());
    }
}
