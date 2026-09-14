namespace RVM.TcgDex;

/// <summary>Serie as it appears in lists and inside a set (<c>SerieBrief</c> in the API reference).</summary>
public class SerieResume : ISdkBound
{
    private TCGdex? _sdk;

    /// <summary>Unique id, e.g. <c>swsh</c>.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Localized name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Logo URL without extension. Prefer <see cref="GetLogoUrl"/>.</summary>
    public string? Logo { get; set; }

    /// <summary>Full logo URL; <c>null</c> when the serie has no logo.</summary>
    public string? GetLogoUrl(Extension extension = Extension.Png) => AssetUrl.Logo(Logo, extension);

    /// <summary>Fetches the complete serie, with its sets.</summary>
    /// <exception cref="TcgDexNotFoundException">The API no longer has the serie.</exception>
    public virtual Task<Serie> GetFullAsync(CancellationToken cancellationToken = default) =>
        SdkBinding.Require(_sdk).Serie.GetRequiredAsync(Id, cancellationToken);

    void ISdkBound.Attach(TCGdex sdk) => Attach(sdk);

    internal virtual void Attach(TCGdex sdk) => _sdk = sdk;
}

/// <summary>A complete serie, with its sets.</summary>
public sealed class Serie : SerieResume
{
    /// <summary>Release date of the first set.</summary>
    public DateOnly? ReleaseDate { get; set; }

    /// <summary>First set of the serie.</summary>
    public SetResume? FirstSet { get; set; }

    /// <summary>Latest set of the serie.</summary>
    public SetResume? LastSet { get; set; }

    /// <summary>Sets of the serie.</summary>
    public IReadOnlyList<SetResume> Sets { get; set; } = [];

    /// <summary>Already complete: returns this instance.</summary>
    public override Task<Serie> GetFullAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(this);

    internal override void Attach(TCGdex sdk)
    {
        base.Attach(sdk);
        FirstSet?.Attach(sdk);
        LastSet?.Attach(sdk);
        foreach (var set in Sets)
            set.Attach(sdk);
    }
}
