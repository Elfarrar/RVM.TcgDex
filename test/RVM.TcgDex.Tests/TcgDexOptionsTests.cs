namespace RVM.TcgDex.Tests;

public sealed class TcgDexOptionsTests
{
    [Fact]
    public void Default_PointsToPublicApiInEnglish()
    {
        Assert.Equal("https://api.tcgdex.net/v2/en/", new TcgDexOptions().BaseUri.ToString());
    }

    [Fact]
    public void BaseUri_KeepsLanguageSegment_WhenResolvingRelativePath()
    {
        // The reason for the trailing slash: without it, "cards/..." would replace "pt-br".
        var options = new TcgDexOptions { Language = Language.PtBr };

        var card = new Uri(options.BaseUri, "cards/swsh3-136");

        Assert.Equal("https://api.tcgdex.net/v2/pt-br/cards/swsh3-136", card.ToString());
    }

    [Theory]
    [InlineData("https://tcgdex.example.com/v2/")]
    [InlineData("https://tcgdex.example.com/v2")]
    [InlineData(" https://tcgdex.example.com/v2 ")]
    public void SelfHostedEndpoint_WithOrWithoutTrailingSlash(string endpoint)
    {
        var options = new TcgDexOptions { Endpoint = endpoint, Language = Language.Fr };

        Assert.Equal("https://tcgdex.example.com/v2/fr/", options.BaseUri.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void MissingEndpoint_Throws(string endpoint)
    {
        var options = new TcgDexOptions { Endpoint = endpoint };

        Assert.Throws<InvalidOperationException>(() => options.BaseUri);
    }
}
