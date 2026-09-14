using System.Net;
using System.Text.Json;

namespace RVM.TcgDex.Tests;

public sealed class ClientTests
{
    private const string NotFound = "error-not-found.json";

    [Fact]
    public async Task GetAsync_RequestsTheCardInTheChosenLanguage()
    {
        var api = new FakeApi().Returns("/v2/pt-br/cards/A1-001", "card-A1-001.pt-br.json");

        var card = await api.Client(Language.PtBr).Card.GetAsync("A1-001");

        Assert.Equal("A1-001", card!.Id);
        Assert.Equal("https://api.tcgdex.net/v2/pt-br/cards/A1-001", api.Requests.Single().RequestUri!.ToString());
    }

    [Fact]
    public async Task Requests_IdentifyTheSdk_AndAskForJson()
    {
        var api = new FakeApi().Returns("/v2/en/cards/swsh3-136", "card-swsh3-136.json");

        await api.Client().Card.GetAsync("swsh3-136");

        var request = api.Requests.Single();
        Assert.StartsWith("RVM.TcgDex/", request.Headers.UserAgent.ToString());
        Assert.Equal("application/json", request.Headers.Accept.Single().MediaType);
    }

    [Fact]
    public async Task SetLanguage_AppliesToTheNextRequests()
    {
        var api = new FakeApi()
            .Returns("/v2/en/cards/A1-001", "card-A1-001.json")
            .Returns("/v2/pt-br/cards/A1-001", "card-A1-001.pt-br.json");
        var tcgdex = api.Client();

        var english = await tcgdex.Card.GetAsync("A1-001");
        tcgdex.SetLanguage(Language.PtBr);
        var portuguese = await tcgdex.Card.GetAsync("A1-001");

        Assert.Equal(Language.PtBr, tcgdex.Language);
        Assert.Equal("Genetic Apex", english!.Set.Name);
        Assert.Equal("Dominação Genética", portuguese!.Set.Name);
    }

    [Fact]
    public async Task SetEndpoint_PointsAtASelfHostedApi()
    {
        var api = new FakeApi().Returns("/v2/en/cards/swsh3-136", "card-swsh3-136.json");
        var tcgdex = api.Client();

        tcgdex.SetEndpoint("https://tcgdex.example.com/v2/");
        await tcgdex.Card.GetAsync("swsh3-136");

        Assert.Equal("https://tcgdex.example.com/v2/", tcgdex.Endpoint);
        Assert.Equal("tcgdex.example.com", api.Requests.Single().RequestUri!.Host);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void SetEndpoint_RejectsBlank(string endpoint)
    {
        Assert.Throws<ArgumentException>(() => new FakeApi().Client().SetEndpoint(endpoint));
    }

    [Fact]
    public void Constructor_RejectsUnusableEndpoint_BeforeAnyRequest()
    {
        Assert.Throws<InvalidOperationException>(() => new TCGdex(new HttpClient(new FakeApi()), new TcgDexOptions { Endpoint = "" }));
        Assert.Throws<ArgumentNullException>(() => new TCGdex(null!));
    }

    [Fact]
    public void DefaultConstructor_PublicApiInEnglish()
    {
        var tcgdex = new TCGdex();

        Assert.Equal(Language.En, tcgdex.Language);
        Assert.Equal(TcgDexOptions.DefaultEndpoint, tcgdex.Endpoint);
        Assert.Equal(Language.Ja, new TCGdex(Language.Ja).Language);
    }

    [Fact]
    public async Task Options_AreCopied_SoLaterChangesDoNotLeakIn()
    {
        var api = new FakeApi().Returns("/v2/en/cards/swsh3-136", "card-swsh3-136.json");
        var options = new TcgDexOptions();
        var tcgdex = new TCGdex(new HttpClient(api), options);

        options.Language = Language.Fr;
        await tcgdex.Card.GetAsync("swsh3-136");

        Assert.Equal(Language.En, tcgdex.Language);
    }

    [Fact]
    public async Task Ids_AreEscaped_InThePath()
    {
        var api = new FakeApi().Returns("/v2/en/sets/Darkness%20Ablaze", "set-swsh3.json");

        var set = await api.Client().Set.GetAsync("Darkness Ablaze");

        Assert.Equal("swsh3", set!.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task GetAsync_RejectsBlankId(string id)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => new FakeApi().Client().Card.GetAsync(id));
    }

    [Fact]
    public async Task GetAsync_ReturnsNull_WhenTheApiSaysNotFound()
    {
        var api = new FakeApi().Returns("/v2/en/cards/nope-1", NotFound, HttpStatusCode.NotFound);

        Assert.Null(await api.Client().Card.GetAsync("nope-1"));
    }

    [Fact]
    public async Task GetAsync_ReturnsNull_OnA404WithoutProblemBody()
    {
        var api = new FakeApi().ReturnsBody("/v2/en/cards/nope-1", "<html>Not Found</html>", HttpStatusCode.NotFound);

        Assert.Null(await api.Client().Card.GetAsync("nope-1"));
    }

    [Fact]
    public async Task LanguageNotAvailable_IsAnError_NotAMissingCard()
    {
        // A self-hosted TCGdex may not serve every language: the API answers 404 language-invalid.
        var api = new FakeApi().Returns("/v2/en/cards/swsh3-136", "error-language-invalid.json", HttpStatusCode.NotFound);

        var error = await Assert.ThrowsAsync<TcgDexException>(() => api.Client().Card.GetAsync("swsh3-136"));

        Assert.IsNotType<TcgDexNotFoundException>(error);
        Assert.Equal(HttpStatusCode.NotFound, error.StatusCode);
        Assert.Contains("The chosen language is not available", error.Message);
        Assert.Contains("You must use one of the following languages", error.Message);
    }

    [Fact]
    public async Task ServerError_Throws_WithStatusAndUrl()
    {
        var api = new FakeApi().ReturnsBody("/v2/en/cards/swsh3-136", """{"title":"Internal error"}""", HttpStatusCode.InternalServerError);

        var error = await Assert.ThrowsAsync<TcgDexException>(() => api.Client().Card.GetAsync("swsh3-136"));

        Assert.Equal(HttpStatusCode.InternalServerError, error.StatusCode);
        Assert.Equal("Internal error", error.Message);
        Assert.Equal("/v2/en/cards/swsh3-136", error.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task ServerError_WithoutBody_DescribesTheStatus()
    {
        var api = new FakeApi().ReturnsBody("/v2/en/cards/swsh3-136", "", HttpStatusCode.BadGateway);

        var error = await Assert.ThrowsAsync<TcgDexException>(() => api.Client().Card.GetAsync("swsh3-136"));

        Assert.StartsWith("TCGdex API answered 502", error.Message);
    }

    [Fact]
    public async Task InvalidJson_Throws_WithTheParserErrorInside()
    {
        var api = new FakeApi().ReturnsBody("/v2/en/cards/swsh3-136", "{ not json");

        var error = await Assert.ThrowsAsync<TcgDexException>(() => api.Client().Card.GetAsync("swsh3-136"));

        Assert.IsAssignableFrom<JsonException>(error.InnerException);
    }

    [Fact]
    public async Task NullBody_Throws()
    {
        var api = new FakeApi().ReturnsBody("/v2/en/cards/swsh3-136", "null");

        var error = await Assert.ThrowsAsync<TcgDexException>(() => api.Client().Card.GetAsync("swsh3-136"));

        Assert.Contains("empty body", error.Message);
    }

    [Fact]
    public async Task Cancellation_IsHonored()
    {
        var api = new FakeApi().Returns("/v2/en/cards/swsh3-136", "card-swsh3-136.json");

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => api.Client().Card.GetAsync("swsh3-136", new CancellationToken(canceled: true)));
    }

    [Fact]
    public async Task ListAsync_SendsTheQuery_AndReturnsBriefCards()
    {
        var api = new FakeApi().Returns("/v2/en/cards?name=eq%3AFurret", "cards-furret.json");

        var cards = await api.Client().Card.ListAsync(Query.Create().Equal("name", "Furret"));

        Assert.Equal(12, cards.Count);
        Assert.All(cards, c => Assert.Equal("Furret", c.Name));
        Assert.Equal("hgss1-21", cards[0].Id);
        Assert.Equal("21", cards[0].LocalId);
    }

    [Fact]
    public async Task ListAsync_WithoutQuery_AndNoMatch_ReturnsEmpty()
    {
        var api = new FakeApi().ReturnsBody("/v2/en/cards", "[]");

        Assert.Empty(await api.Client().Card.ListAsync());
    }

    [Fact]
    public async Task ListAsync_Throws_OnNotFound_BecauseAListAlwaysExists()
    {
        var api = new FakeApi().Returns("/v2/en/cards", NotFound, HttpStatusCode.NotFound);

        await Assert.ThrowsAsync<TcgDexNotFoundException>(() => api.Client().Card.ListAsync());
    }

    [Fact]
    public async Task SetsAndSeries_Lists()
    {
        var api = new FakeApi()
            .Returns("/v2/en/sets?pagination%3Apage=1&pagination%3AitemsPerPage=3", "sets-page.json")
            .Returns("/v2/en/series", "series.json");
        var tcgdex = api.Client();

        var sets = await tcgdex.Set.ListAsync(Query.Create().Paginate(1, 3));
        var series = await tcgdex.Serie.ListAsync();

        Assert.Equal(["miscp", "base1", "base2"], sets.Select(s => s.Id));
        Assert.Null(sets[0].GetLogoUrl());
        Assert.Equal(102, sets[1].CardCount.Total);
        Assert.Equal("https://assets.tcgdex.net/univ/base/base2/symbol.webp", sets[2].GetSymbolUrl(Extension.Webp));
        Assert.Equal(21, series.Count);
        Assert.Null(series[0].GetLogoUrl());
        Assert.Equal("https://assets.tcgdex.net/en/base/base1/logo.png", series[1].GetLogoUrl());
    }
}
