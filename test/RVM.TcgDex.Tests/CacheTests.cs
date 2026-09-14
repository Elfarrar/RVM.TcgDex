using System.Net;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using RVM.TcgDex.Caching;

namespace RVM.TcgDex.Tests;

public sealed class CacheTests
{
    private const string CardPath = "/v2/en/cards/swsh3-136";

    private static FakeApi Api() => new FakeApi().Returns(CardPath, "card-swsh3-136.json");

    [Fact]
    public async Task SecondRequest_IsServedFromCache_AsAFreshObject()
    {
        var api = Api();
        var tcgdex = api.Client();

        var first = (await tcgdex.Card.GetAsync("swsh3-136"))!;
        first.Name = "changed by the first caller";
        var second = (await tcgdex.Card.GetAsync("swsh3-136"))!;

        Assert.Single(api.Requests);
        Assert.NotSame(first, second);
        Assert.Equal("Furret", second.Name);
    }

    [Fact]
    public async Task CachedObjects_AreStillNavigable()
    {
        var api = Api().Returns("/v2/en/sets/swsh3", "set-swsh3.json");
        var tcgdex = api.Client();

        await tcgdex.Card.GetAsync("swsh3-136");
        var fromCache = (await tcgdex.Card.GetAsync("swsh3-136"))!;

        Assert.Equal("swsh3", (await fromCache.GetSetAsync()).Id);
    }

    [Fact]
    public async Task Entry_Expires_AfterTheTtl()
    {
        var api = Api();
        var time = new FakeTime();
        var tcgdex = new TCGdex(new HttpClient(api), new TcgDexOptions { CacheTtl = TimeSpan.FromMinutes(5) }, new InMemoryCache(time));

        await tcgdex.Card.GetAsync("swsh3-136");
        time.Advance(TimeSpan.FromMinutes(4));
        await tcgdex.Card.GetAsync("swsh3-136");
        time.Advance(TimeSpan.FromMinutes(2));
        await tcgdex.Card.GetAsync("swsh3-136");

        Assert.Equal(2, api.Requests.Count);
    }

    [Fact]
    public async Task Errors_AndNotFound_AreNotCached()
    {
        var api = new FakeApi().ReturnsBody(CardPath, "", HttpStatusCode.ServiceUnavailable);
        var tcgdex = api.Client();

        await Assert.ThrowsAsync<TcgDexException>(() => tcgdex.Card.GetAsync("swsh3-136"));
        api.Returns(CardPath, "error-not-found.json", HttpStatusCode.NotFound);
        Assert.Null(await tcgdex.Card.GetAsync("swsh3-136"));
        api.Returns(CardPath, "card-swsh3-136.json");
        Assert.NotNull(await tcgdex.Card.GetAsync("swsh3-136"));

        Assert.Equal(3, api.Requests.Count);
    }

    [Fact]
    public async Task ZeroTtl_TurnsTheCacheOff()
    {
        var api = Api();
        var tcgdex = api.Client();

        tcgdex.SetCacheTtl(TimeSpan.Zero);
        await tcgdex.Card.GetAsync("swsh3-136");
        await tcgdex.Card.GetAsync("swsh3-136");

        Assert.Equal(TimeSpan.Zero, tcgdex.CacheTtl);
        Assert.Equal(2, api.Requests.Count);
    }

    [Fact]
    public async Task Key_IsTheFullUrl_SoLanguagesAndQueriesDoNotMix()
    {
        var api = new FakeApi()
            .Returns("/v2/en/cards/A1-001", "card-A1-001.json")
            .Returns("/v2/pt-br/cards/A1-001", "card-A1-001.pt-br.json")
            .ReturnsBody("/v2/en/cards?name=a", "[]")
            .ReturnsBody("/v2/en/cards?name=b", "[]");
        var tcgdex = api.Client();

        await tcgdex.Card.GetAsync("A1-001");
        tcgdex.SetLanguage(Language.PtBr);
        var portuguese = await tcgdex.Card.GetAsync("A1-001");
        tcgdex.SetLanguage(Language.En);
        await tcgdex.Card.GetAsync("A1-001");                        // cached
        await tcgdex.Card.ListAsync(Query.Create().Contains("name", "a"));
        await tcgdex.Card.ListAsync(Query.Create().Contains("name", "b"));
        await tcgdex.Card.ListAsync(Query.Create().Contains("name", "a")); // cached

        Assert.Equal("Dominação Genética", portuguese!.Set.Name);
        Assert.Equal(4, api.Requests.Count);
    }

    [Fact]
    public void Ttl_DefaultsToOneHour_AndCannotBeNegative()
    {
        var tcgdex = new FakeApi().Client();

        Assert.Equal(TimeSpan.FromHours(1), tcgdex.CacheTtl);
        Assert.Throws<ArgumentOutOfRangeException>(() => tcgdex.SetCacheTtl(TimeSpan.FromSeconds(-1)));
        Assert.Throws<InvalidOperationException>(
            () => new TCGdex(new HttpClient(new FakeApi()), new TcgDexOptions { CacheTtl = TimeSpan.FromSeconds(-1) }));
    }

    [Fact]
    public async Task ExternalDistributedCache_ReceivesTheResponses()
    {
        var api = Api();
        var cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var tcgdex = new TCGdex(new HttpClient(api), cache: cache);

        await tcgdex.Card.GetAsync("swsh3-136");

        Assert.NotNull(await cache.GetAsync("rvm-tcgdex:https://api.tcgdex.net/v2/en/cards/swsh3-136"));
    }

    [Fact]
    public async Task BrokenCache_DoesNotStopRequests()
    {
        var api = Api();
        var tcgdex = new TCGdex(new HttpClient(api), cache: new ThrowingCache(new InvalidOperationException("redis down")));

        var card = await tcgdex.Card.GetAsync("swsh3-136");

        Assert.Equal("Furret", card!.Name);
    }

    [Fact]
    public async Task CancellationInsideTheCache_StillCancels()
    {
        var tcgdex = new TCGdex(new HttpClient(Api()), cache: new ThrowingCache(new OperationCanceledException()));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => tcgdex.Card.GetAsync("swsh3-136"));
    }

    [Fact]
    public async Task InMemoryCache_SweepsExpiredEntries_OnWrite_AtMostOncePerMinute()
    {
        var time = new FakeTime();
        var cache = new InMemoryCache(time);
        var shortLived = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10) };

        await cache.SetAsync("a", [1], shortLived);
        await cache.SetAsync("b", [2], shortLived);
        time.Advance(TimeSpan.FromSeconds(30));
        await cache.SetAsync("c", [3], shortLived);   // expired, but the last sweep was < 1 min ago
        Assert.Equal(3, cache.Count);

        time.Advance(TimeSpan.FromMinutes(1));
        await cache.SetAsync("d", [4], new DistributedCacheEntryOptions());

        Assert.Equal(1, cache.Count);
        Assert.Equal([4], await cache.GetAsync("d"));
    }

    [Fact]
    public async Task InMemoryCache_HonorsAbsoluteExpiration_AndRemove()
    {
        var time = new FakeTime();
        var cache = new InMemoryCache(time);

        cache.Set("a", [1], new DistributedCacheEntryOptions { AbsoluteExpiration = time.GetUtcNow().AddSeconds(5) });
        cache.Set("b", [2], new DistributedCacheEntryOptions());
        cache.Refresh("a");
        await cache.RefreshAsync("a");
        Assert.Equal([1], cache.Get("a"));

        time.Advance(TimeSpan.FromSeconds(6));
        await cache.RemoveAsync("b");

        Assert.Null(cache.Get("a"));
        Assert.Null(cache.Get("b"));
        Assert.Equal(0, cache.Count);
    }

    private sealed class ThrowingCache(Exception error) : IDistributedCache
    {
        public byte[]? Get(string key) => throw error;
        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => throw error;
        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => throw error;
        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) => throw error;
        public void Refresh(string key) => throw error;
        public Task RefreshAsync(string key, CancellationToken token = default) => throw error;
        public void Remove(string key) => throw error;
        public Task RemoveAsync(string key, CancellationToken token = default) => throw error;
    }
}

internal sealed class FakeTime : TimeProvider
{
    private DateTimeOffset _now = new(2026, 9, 13, 12, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now += by;
}
