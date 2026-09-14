namespace RVM.TcgDex;

/// <summary>
/// Models keep a reference to the client that fetched them, so navigation
/// (<c>card.GetSetAsync()</c>) uses the same language, endpoint and HTTP client.
/// </summary>
internal interface ISdkBound
{
    void Attach(TCGdex sdk);
}

internal static class SdkBinding
{
    public static TCGdex Require(TCGdex? sdk) => sdk ?? throw new InvalidOperationException(
        "This object was not returned by a TCGdex client, so it cannot fetch related data. " +
        "Get it through TCGdex (e.g. tcgdex.Card.GetAsync) to use navigation methods.");
}
