using System.Net;

namespace RVM.TcgDex.Tests;

/// <summary>card → set → serie and back, always through the client that fetched the first object.</summary>
public sealed class NavigationTests
{
    private static FakeApi Api() => new FakeApi()
        .Returns("/v2/en/cards/swsh3-136", "card-swsh3-136.json")
        .Returns("/v2/en/sets/swsh3", "set-swsh3.json")
        .Returns("/v2/en/sets/swsh3/136", "card-swsh3-136.json")
        .Returns("/v2/en/series/swsh", "serie-swsh.json");

    [Fact]
    public async Task Card_GetSetAsync_FetchesTheCompleteSet()
    {
        var api = Api();
        var card = (await api.Client().Card.GetAsync("swsh3-136"))!;

        var set = await card.GetSetAsync();

        Assert.Equal("Darkness Ablaze", set.Name);
        Assert.Equal(new DateOnly(2020, 8, 14), set.ReleaseDate);
        Assert.Equal("DAA", set.TcgOnline);
        Assert.Equal("DAA", set.Abbreviation!.Official);
        Assert.Equal((201, 189, 138, 157, 69, 0), (set.CardCount.Total, set.CardCount.Official, set.CardCount.Normal, set.CardCount.Reverse, set.CardCount.Holo, set.CardCount.FirstEd));
        Assert.Equal(201, set.Cards.Count);
        Assert.True(set.Legal.Expanded);
        Assert.Null(set.Boosters);
        Assert.Equal("https://assets.tcgdex.net/en/swsh/swsh3/logo.webp", set.GetLogoUrl(Extension.Webp));
        Assert.Equal("https://assets.tcgdex.net/univ/swsh/swsh3/symbol.png", set.GetSymbolUrl());
    }

    [Fact]
    public async Task Set_GetSerieAsync_And_BackDownToASet()
    {
        var api = Api().Returns("/v2/en/sets/swsh1", "set-swsh3.json");
        var set = (await api.Client().Set.GetAsync("swsh3"))!;

        var serie = await set.GetSerieAsync();
        await serie.Sets[1].GetFullAsync();

        Assert.Equal("Sword & Shield", serie.Name);
        Assert.Equal(new DateOnly(2019, 11, 15), serie.ReleaseDate);
        Assert.Equal(26, serie.Sets.Count);
        Assert.Equal("swshp", serie.FirstSet!.Id);
        Assert.Equal("Crown Zenith", serie.LastSet!.Name);
        Assert.Equal("https://assets.tcgdex.net/en/swsh/swsh1/logo.png", serie.GetLogoUrl());
        Assert.Equal("/v2/en/sets/swsh1", api.Requests.Last().RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task Set_GetCardAsync_ByLocalId()
    {
        var api = Api().Returns("/v2/en/sets/swsh3/999", "error-not-found.json", HttpStatusCode.NotFound);
        var set = (await api.Client().Set.GetAsync("swsh3"))!;

        var furret = await set.GetCardAsync("136");
        var missing = await set.GetCardAsync("999");

        Assert.Equal("Furret", furret!.Name);
        Assert.Null(missing);
        await Assert.ThrowsAsync<ArgumentException>(() => set.GetCardAsync(" "));
    }

    [Fact]
    public async Task CardOfASet_GetFullAsync_FetchesTheCard()
    {
        var api = Api();
        var set = (await api.Client().Set.GetAsync("swsh3"))!;

        var card = await set.Cards[135].GetFullAsync();

        Assert.Equal("swsh3-136", card.Id);
        Assert.Equal("/v2/en/cards/swsh3-136", api.Requests.Last().RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task Resumes_FromLists_GetFullAsync()
    {
        var api = Api()
            .Returns("/v2/en/cards?name=furret", "cards-furret.json")
            .Returns("/v2/en/series", "series.json");
        var tcgdex = api.Client();

        var fromList = (await tcgdex.Card.ListAsync(Query.Create().Contains("name", "furret")))
            .Single(c => c.Id == "swsh3-136");
        var card = await fromList.GetFullAsync();
        var serie = await (await tcgdex.Serie.ListAsync()).Single(s => s.Id == "swsh").GetFullAsync();

        Assert.Equal(110, card.Hp);
        Assert.Equal(26, serie.Sets.Count);
    }

    [Fact]
    public async Task CompleteModels_GetFullAsync_ReturnThemselves()
    {
        var api = Api();
        var tcgdex = api.Client();
        var card = (await tcgdex.Card.GetAsync("swsh3-136"))!;
        var serie = (await tcgdex.Serie.GetAsync("swsh"))!;
        var requests = api.Requests.Count;

        Assert.Same(card, await card.GetFullAsync());
        Assert.Same(serie, await serie.GetFullAsync());
        Assert.Equal(requests, api.Requests.Count);
    }

    [Fact]
    public async Task Navigation_UsesTheCurrentLanguageOfTheClient()
    {
        var api = Api().Returns("/v2/pt-br/sets/swsh3", "set-swsh3.json");
        var tcgdex = api.Client();
        var card = (await tcgdex.Card.GetAsync("swsh3-136"))!;

        tcgdex.SetLanguage(Language.PtBr);
        await card.GetSetAsync();

        Assert.Equal("/v2/pt-br/sets/swsh3", api.Requests.Last().RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task Navigation_Throws_WhenTheApiLostTheTarget()
    {
        // The API itself pointed at the set, so a 404 here is an inconsistency, not "no such set".
        var api = new FakeApi()
            .Returns("/v2/en/cards/swsh3-136", "card-swsh3-136.json")
            .Returns("/v2/en/sets/swsh3", "error-not-found.json", HttpStatusCode.NotFound);
        var card = (await api.Client().Card.GetAsync("swsh3-136"))!;

        var error = await Assert.ThrowsAsync<TcgDexNotFoundException>(() => card.GetSetAsync());

        Assert.Equal(HttpStatusCode.NotFound, error.StatusCode);
        Assert.Contains("does not exists", error.Message);
    }

    [Fact]
    public async Task Navigation_OnObjectsNotFromAClient_ExplainsWhatToDo()
    {
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => new Card().GetSetAsync());

        Assert.Contains("tcgdex.Card.GetAsync", error.Message);
        await Assert.ThrowsAsync<InvalidOperationException>(() => new SetResume { Id = "swsh3" }.GetFullAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() => new SerieResume { Id = "swsh" }.GetFullAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() => new Set().GetSerieAsync());
    }

    [Fact]
    public async Task Random_CardSetAndSerie()
    {
        var api = new FakeApi()
            .Returns("/v2/en/random/card", "card-swsh3-136.json")
            .Returns("/v2/en/random/set", "set-swsh3.json")
            .Returns("/v2/en/random/serie", "serie-swsh.json")
            .Returns("/v2/en/series/swsh", "serie-swsh.json");
        var tcgdex = api.Client();

        var card = await tcgdex.Random.GetCardAsync();
        var set = await tcgdex.Random.GetSetAsync();
        var serie = await tcgdex.Random.GetSerieAsync();

        Assert.Equal("swsh3-136", card.Id);
        Assert.Equal("swsh3", set.Id);
        Assert.Equal("swsh", serie.Id);
        Assert.Equal("swsh", (await set.GetSerieAsync()).Id); // random results are navigable too
    }
}
