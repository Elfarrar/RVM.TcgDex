namespace RVM.TcgDex;

/// <summary>The 18 languages served by the TCGdex API.</summary>
public enum Language
{
    /// <summary>English (<c>en</c>).</summary>
    En,
    /// <summary>French (<c>fr</c>).</summary>
    Fr,
    /// <summary>Spanish (<c>es</c>).</summary>
    Es,
    /// <summary>Latin American Spanish (<c>es-mx</c>).</summary>
    EsMx,
    /// <summary>Italian (<c>it</c>).</summary>
    It,
    /// <summary>Portuguese (<c>pt</c>).</summary>
    Pt,
    /// <summary>Brazilian Portuguese (<c>pt-br</c>).</summary>
    PtBr,
    /// <summary>European Portuguese (<c>pt-pt</c>).</summary>
    PtPt,
    /// <summary>German (<c>de</c>).</summary>
    De,
    /// <summary>Dutch (<c>nl</c>).</summary>
    Nl,
    /// <summary>Polish (<c>pl</c>).</summary>
    Pl,
    /// <summary>Russian (<c>ru</c>).</summary>
    Ru,
    /// <summary>Japanese (<c>ja</c>).</summary>
    Ja,
    /// <summary>Korean (<c>ko</c>).</summary>
    Ko,
    /// <summary>Traditional Chinese (<c>zh-tw</c>).</summary>
    ZhTw,
    /// <summary>Indonesian (<c>id</c>).</summary>
    Id,
    /// <summary>Thai (<c>th</c>).</summary>
    Th,
    /// <summary>Simplified Chinese (<c>zh-cn</c>).</summary>
    ZhCn,
}

/// <summary>Conversion between <see cref="Language"/> and the code used in the API URL.</summary>
public static class LanguageExtensions
{
    /// <summary>The code the API expects in the URL, e.g. <c>pt-br</c> for <see cref="Language.PtBr"/>.</summary>
    public static string ToCode(this Language language) => language switch
    {
        Language.En => "en",
        Language.Fr => "fr",
        Language.Es => "es",
        Language.EsMx => "es-mx",
        Language.It => "it",
        Language.Pt => "pt",
        Language.PtBr => "pt-br",
        Language.PtPt => "pt-pt",
        Language.De => "de",
        Language.Nl => "nl",
        Language.Pl => "pl",
        Language.Ru => "ru",
        Language.Ja => "ja",
        Language.Ko => "ko",
        Language.ZhTw => "zh-tw",
        Language.Id => "id",
        Language.Th => "th",
        Language.ZhCn => "zh-cn",
        _ => throw new ArgumentOutOfRangeException(nameof(language), language, "Unknown TCGdex language."),
    };
}
