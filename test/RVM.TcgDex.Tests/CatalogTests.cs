using System.Net;

namespace RVM.TcgDex.Tests;

public sealed class CatalogTests
{
    public static TheoryData<Func<TCGdex, Task>, string> Catalogs => new()
    {
        { t => t.Types.ListAsync(), "types" },
        { t => t.Hp.ListAsync(), "hp" },
        { t => t.Illustrators.ListAsync(), "illustrators" },
        { t => t.Rarities.ListAsync(), "rarities" },
        { t => t.Categories.ListAsync(), "categories" },
        { t => t.EnergyTypes.ListAsync(), "energy-types" },
        { t => t.Retreats.ListAsync(), "retreats" },
        { t => t.Stages.ListAsync(), "stages" },
        { t => t.Suffixes.ListAsync(), "suffixes" },
        { t => t.TrainerTypes.ListAsync(), "trainer-types" },
        { t => t.DexIds.ListAsync(), "dex-ids" },
        { t => t.RegulationMarks.ListAsync(), "regulation-marks" },
        { t => t.Variants.ListAsync(), "variants" },
    };

    [Theory]
    [MemberData(nameof(Catalogs))]
    public async Task EveryCatalog_HitsItsEndpoint(Func<TCGdex, Task> list, string path)
    {
        var api = new FakeApi().ReturnsBody($"/v2/en/{path}", "[]");

        await list(api.Client());

        Assert.Equal($"/v2/en/{path}", api.Requests.Single().RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task TextCatalog()
    {
        var api = new FakeApi().Returns("/v2/en/types", "catalog-types.json");

        var types = await api.Client().Types.ListAsync();

        Assert.Equal(11, types.Count);
        Assert.Contains("Fire", types);
    }

    [Fact]
    public async Task NumericCatalog()
    {
        var api = new FakeApi().Returns("/v2/en/hp", "catalog-hp.json");

        var hp = await api.Client().Hp.ListAsync();

        Assert.Equal(10, hp[0]);
        Assert.Contains(110, hp);
    }

    [Fact]
    public async Task GetAsync_ReturnsTheCardsWithTheValue_AndTheyAreNavigable()
    {
        // The API sends this "name" as a number (162), other catalogs as text.
        var api = new FakeApi()
            .Returns("/v2/en/dex-ids/162", "catalog-dex-ids-162.json")
            .Returns("/v2/en/cards/swsh3-136", "card-swsh3-136.json");

        var furrets = (await api.Client().DexIds.GetAsync(162))!;
        var card = await furrets.Cards.Single(c => c.Id == "swsh3-136").GetFullAsync();

        Assert.Equal("162", furrets.Name);
        Assert.Equal(12, furrets.Cards.Count);
        Assert.Equal(110, card.Hp);
    }

    [Fact]
    public async Task GetAsync_UnknownValue_IsAnEntryWithoutCards_AsTheApiAnswers()
    {
        var api = new FakeApi().Returns("/v2/en/types/zzz", "catalog-types-zzz.json");

        var entry = await api.Client().Types.GetAsync("zzz");

        Assert.Equal("zzz", entry!.Name);
        Assert.Empty(entry.Cards);
    }

    [Fact]
    public async Task GetAsync_EscapesTheValue_AndReturnsNullOnNotFound()
    {
        var api = new FakeApi().Returns("/v2/en/illustrators/Mitsuhiro%20Arita", "error-not-found.json", HttpStatusCode.NotFound);

        Assert.Null(await api.Client().Illustrators.GetAsync("Mitsuhiro Arita"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task GetAsync_RejectsBlankValue(string value)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => new FakeApi().Client().Types.GetAsync(value));
    }
}
