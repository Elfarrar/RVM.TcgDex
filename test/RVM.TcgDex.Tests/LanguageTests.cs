namespace RVM.TcgDex.Tests;

public sealed class LanguageTests
{
    // The list the API itself returns in the language-invalid error (fixture error-language-invalid.json).
    private static readonly string[] ApiLanguages =
        ["en", "fr", "es", "es-mx", "it", "pt", "pt-br", "pt-pt", "de", "nl", "pl", "ru", "ja", "ko", "zh-tw", "id", "th", "zh-cn"];

    [Fact]
    public void EveryLanguage_MapsToTheCodeTheApiAccepts()
    {
        var codes = Enum.GetValues<Language>().Select(l => l.ToCode()).ToArray();

        Assert.Equal(ApiLanguages, codes);
    }

    [Fact]
    public void ApiErrorMessage_ListsExactlyOurLanguages()
    {
        var error = FakeApi.Fixture("error-language-invalid.json");

        Assert.All(ApiLanguages, code => Assert.Contains(code, error));
    }

    [Fact]
    public void UnknownValue_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((Language)999).ToCode());
    }
}
