namespace RVM.TcgDex.Tests;

/// <summary>Recorded cards from four eras, the three categories, with and without prices.</summary>
public sealed class CardTests
{
    private static Task<Card?> Get(string id, Language language = Language.En, string? fixture = null)
    {
        var code = language.ToCode();
        var api = new FakeApi().Returns($"/v2/{code}/cards/{id}", fixture ?? $"card-{id}.json");
        return api.Client(language).Card.GetAsync(id);
    }

    [Fact]
    public async Task Pokemon_WithEveryCommonField()
    {
        var card = (await Get("swsh3-136"))!;

        Assert.Equal("swsh3-136", card.Id);
        Assert.Equal("136", card.LocalId);
        Assert.Equal("Furret", card.Name);
        Assert.Equal("Pokemon", card.Category);
        Assert.Equal("tetsuya koizumi", card.Illustrator);
        Assert.Equal("Uncommon", card.Rarity);
        Assert.Equal(new DateTimeOffset(2026, 7, 1, 21, 27, 44, TimeSpan.FromHours(1)), card.Updated);

        Assert.Equal("swsh3", card.Set.Id);
        Assert.Equal("Darkness Ablaze", card.Set.Name);
        Assert.Equal(189, card.Set.CardCount.Official);
        Assert.Equal(201, card.Set.CardCount.Total);

        Assert.Equal([162], card.DexId);
        Assert.Equal(110, card.Hp);
        Assert.Equal(["Colorless"], card.Types);
        Assert.Equal("Sentret", card.EvolveFrom);
        Assert.Equal("Stage1", card.Stage);
        Assert.StartsWith("It makes a nest", card.Description);
        Assert.Equal(1, card.Retreat);
        Assert.Equal("D", card.RegulationMark);
        Assert.False(card.Legal.Standard);
        Assert.True(card.Legal.Expanded);

        var weakness = Assert.Single(card.Weaknesses!);
        Assert.Equal("Fighting", weakness.Type);
        Assert.Equal("×2", weakness.Value);

        Assert.Null(card.Effect);
        Assert.Null(card.TrainerType);
    }

    [Fact]
    public async Task Attacks_DamageComesAsNumberOrString_AndIsAlwaysText()
    {
        var furret = (await Get("swsh3-136"))!;
        var bulbasaur = (await Get("A1-001"))!;

        Assert.Equal(["Colorless"], furret.Attacks![0].Cost);
        Assert.Equal("Feelin' Fine", furret.Attacks[0].Name);
        Assert.Equal("Draw 3 cards.", furret.Attacks[0].Effect);
        Assert.Null(furret.Attacks[0].Damage);
        Assert.Equal("90", furret.Attacks[1].Damage);   // JSON number
        Assert.Equal("40", bulbasaur.Attacks![0].Damage); // JSON string
    }

    [Fact]
    public async Task Pricing_IsTyped_ForBothMarketplaces()
    {
        var pricing = (await Get("swsh3-136"))!.Pricing!;

        var cardmarket = pricing.Cardmarket!;
        Assert.Equal("EUR", cardmarket.Unit);
        Assert.Equal(483559, cardmarket.IdProduct);
        Assert.Equal(new DateTimeOffset(2026, 9, 13, 15, 6, 51, 129, TimeSpan.Zero), cardmarket.Updated);
        Assert.Equal(
            [0.08m, 0.02m, 0.1m, 0.22m, 0.1m, 0.08m],
            [cardmarket.Avg, cardmarket.Low, cardmarket.Trend, cardmarket.Avg1, cardmarket.Avg7, cardmarket.Avg30]);
        Assert.Equal(
            [0.26m, 0.04m, 0.19m, 0.24m, 0.27m, 0.3m],
            [cardmarket.AvgHolo, cardmarket.LowHolo, cardmarket.TrendHolo, cardmarket.Avg1Holo, cardmarket.Avg7Holo, cardmarket.Avg30Holo]);

        var tcgplayer = pricing.Tcgplayer!;
        Assert.Equal("USD", tcgplayer.Unit);
        Assert.Equal(219333, tcgplayer.Normal!.ProductId);
        Assert.Equal(
            [0.02m, 0.24m, 209.71m, 0.22m, null],
            [tcgplayer.Normal.LowPrice, tcgplayer.Normal.MidPrice, tcgplayer.Normal.HighPrice, tcgplayer.Normal.MarketPrice, tcgplayer.Normal.DirectLowPrice]);
        Assert.Equal(0.46m, tcgplayer.ReverseHolofoil!.MarketPrice);
        Assert.Null(tcgplayer.Holofoil);
    }

    [Fact]
    public async Task Pricing_HolofoilOnly_AndNullFoilAverages()
    {
        var card = (await Get("dp1-121"))!;

        Assert.Equal(131.66m, card.Pricing!.Tcgplayer!.Holofoil!.MarketPrice);
        Assert.Equal(529.68m, card.Pricing.Tcgplayer.Holofoil.DirectLowPrice);
        Assert.Null(card.Pricing.Cardmarket!.AvgHolo);
        Assert.Equal(22.62m, card.Pricing.Cardmarket.TrendHolo);
        Assert.Equal(277620, card.ThirdParty!.Cardmarket);
        Assert.Equal(86281, card.ThirdParty.Tcgplayer);
    }

    [Fact]
    public async Task Pricing_MarketplacesNull_WhenTheCardIsNotListed()
    {
        var card = (await Get("A1-001"))!;

        Assert.NotNull(card.Pricing);
        Assert.Null(card.Pricing.Cardmarket);
        Assert.Null(card.Pricing.Tcgplayer);
    }

    [Fact]
    public async Task Variants_BothGenerations_WithPerVariantPricing()
    {
        var card = (await Get("swsh3-136"))!;

        Assert.True(card.Variants!.Normal);
        Assert.True(card.Variants.Reverse);
        Assert.False(card.Variants.Holo);
        Assert.False(card.Variants.FirstEdition);
        Assert.False(card.Variants.WPromo);

        Assert.Equal(2, card.VariantsDetailed!.Count);
        var normal = card.VariantsDetailed[0];
        Assert.Equal("normal", normal.Type);
        Assert.Equal("standard", normal.Size);
        Assert.Equal("endfynwn4n10gzq", normal.VariantId);
        Assert.Equal(483559, normal.ThirdParty!.Cardmarket);
        Assert.Equal(0.22m, normal.Pricing!.Tcgplayer!.Normal!.MarketPrice);
        Assert.Equal("reverse", card.VariantsDetailed[1].Type);
    }

    [Fact]
    public async Task OldCard_FirstEditionHolo_WithAbilityAndResistance()
    {
        var card = (await Get("base1-4"))!;

        Assert.True(card.Variants!.FirstEdition);
        Assert.True(card.Variants.Holo);
        Assert.False(card.Variants.Normal);
        var firstEditionShadowless = card.VariantsDetailed!.Single(v => v.Stamp is not null);
        Assert.Equal(["1st-edition"], firstEditionShadowless.Stamp);
        Assert.Equal("shadowless", firstEditionShadowless.Subtype);
        Assert.Null(firstEditionShadowless.Pricing!.Tcgplayer);
        Assert.Equal("Pokemon Power", card.Abilities![0].Type);
        Assert.Equal("Energy Burn", card.Abilities[0].Name);
        var resistance = Assert.Single(card.Resistances!);
        Assert.Equal(("Fighting", "-30"), (resistance.Type, resistance.Value));
        Assert.Equal(3, card.Retreat);
        Assert.Null(card.Set.Symbol);
        Assert.Null(card.Set.GetSymbolUrl());
    }

    [Fact]
    public async Task LevelUpCard()
    {
        var card = (await Get("dp1-121"))!;

        Assert.Equal("LEVEL-UP", card.Stage);
        Assert.Equal("Rare Holo LV.X", card.Rarity);
        Assert.Equal("Poke-POWER", card.Abilities![0].Type);
        Assert.Equal("150", card.Attacks![0].Damage);
    }

    [Fact]
    public async Task Trainer()
    {
        var card = (await Get("swsh1-181"))!;

        Assert.Equal("Trainer", card.Category);
        Assert.Equal("Item", card.TrainerType);
        Assert.StartsWith("Draw cards until you have 6", card.Effect);
        Assert.Equal([479], card.CameoDexIds);
        Assert.Null(card.Hp);
        Assert.Null(card.Attacks);
        Assert.Null(card.DexId);
    }

    [Fact]
    public async Task Energy_WithoutTcgplayerNorIllustrator()
    {
        var card = (await Get("sm1-164"))!;

        Assert.Equal("Energy", card.Category);
        Assert.Equal("Normal", card.EnergyType);
        Assert.Null(card.Illustrator);
        Assert.Null(card.Pricing!.Tcgplayer);
        Assert.Equal(295672, card.ThirdParty!.Cardmarket);
        Assert.Null(card.ThirdParty.Tcgplayer);
    }

    [Fact]
    public async Task PocketCard_WithBoostersAndLocalIdKeepingLeadingZeros()
    {
        var card = (await Get("A1-001"))!;

        Assert.Equal("001", card.LocalId);
        var booster = Assert.Single(card.Boosters!);
        Assert.Equal(("boo_A1-mewtwo", "Mewtwo"), (booster.Id, booster.Name));
        Assert.Null(booster.Logo);
        Assert.Equal("generated", Assert.Single(card.VariantsDetailed!).VariantId);
    }

    [Fact]
    public async Task BrazilianPortuguese()
    {
        var card = (await Get("A1-001", Language.PtBr, "card-A1-001.pt-br.json"))!;

        Assert.Equal("Dominação Genética", card.Set.Name);
        Assert.Equal("https://assets.tcgdex.net/pt-br/tcgp/A1/001/high.png", card.GetImageUrl());
    }

    [Theory]
    [InlineData(Quality.High, Extension.Png, "high.png")]
    [InlineData(Quality.Low, Extension.Webp, "low.webp")]
    [InlineData(Quality.High, Extension.Jpg, "high.jpg")]
    public async Task ImageUrl_AppendsQualityAndExtension(Quality quality, Extension extension, string suffix)
    {
        var card = (await Get("swsh3-136"))!;

        Assert.Equal($"https://assets.tcgdex.net/en/swsh/swsh3/136/{suffix}", card.GetImageUrl(quality, extension));
    }

    [Theory]
    [InlineData("\"X\"", "X")]
    [InlineData("76", "76")]
    public async Task Level_AcceptsTextOrNumber(string json, string expected)
    {
        var api = new FakeApi().ReturnsBody("/v2/en/cards/x-1", $$"""{"id":"x-1","level":{{json}},"localId":7}""");

        var card = (await api.Client().Card.GetAsync("x-1"))!;

        Assert.Equal(expected, card.Level);
        Assert.Equal("7", card.LocalId);
    }

    [Fact]
    public async Task PartialBody_KeepsNonNullableDefaults()
    {
        // With `init` setters the System.Text.Json source generator assigns default (null) to every
        // property missing from the JSON, overriding the initializer — `card.Set` blew up on attach.
        var api = new FakeApi().ReturnsBody("/v2/en/cards/x-1", """{"id":"x-1"}""");

        var card = (await api.Client().Card.GetAsync("x-1"))!;

        Assert.Equal(string.Empty, card.Name);
        Assert.Equal(string.Empty, card.Set.Id);
        Assert.NotNull(card.Legal);
    }

    [Fact]
    public async Task FlexibleField_WithAnotherJsonType_IsAnInvalidBody()
    {
        var api = new FakeApi().ReturnsBody("/v2/en/cards/x-1", """{"id":"x-1","attacks":[{"name":"a","damage":true}]}""");

        var error = await Assert.ThrowsAsync<TcgDexException>(() => api.Client().Card.GetAsync("x-1"));

        Assert.Contains("not the expected JSON", error.Message);
    }

    [Fact]
    public void ImageUrl_IsNull_WithoutImage()
    {
        Assert.Null(new CardResume().GetImageUrl());
    }
}
