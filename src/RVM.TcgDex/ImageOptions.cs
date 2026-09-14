namespace RVM.TcgDex;

/// <summary>Image quality. Only card images come in two qualities.</summary>
public enum Quality
{
    /// <summary>High resolution (<c>high</c>).</summary>
    High,
    /// <summary>Low resolution (<c>low</c>).</summary>
    Low,
}

/// <summary>Image file format.</summary>
public enum Extension
{
    /// <summary>PNG, transparent background.</summary>
    Png,
    /// <summary>JPG, solid background.</summary>
    Jpg,
    /// <summary>WebP, transparent background and smaller files — recommended by TCGdex.</summary>
    Webp,
}

internal static class AssetUrl
{
    /// <summary>
    /// Asset URLs come from the API without extension (<c>https://assets.tcgdex.net/en/swsh/swsh3/136</c>);
    /// the format is appended by the client. <c>null</c> when the asset does not exist.
    /// </summary>
    public static string? Card(string? baseUrl, Quality quality, Extension extension) =>
        baseUrl is null ? null : $"{baseUrl}/{Code(quality)}.{Code(extension)}";

    public static string? Logo(string? baseUrl, Extension extension) =>
        baseUrl is null ? null : $"{baseUrl}.{Code(extension)}";

    private static string Code(Quality quality) => quality == Quality.Low ? "low" : "high";

    private static string Code(Extension extension) => extension switch
    {
        Extension.Jpg => "jpg",
        Extension.Webp => "webp",
        _ => "png",
    };
}
