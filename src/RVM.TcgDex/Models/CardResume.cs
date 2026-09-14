using System.Text.Json.Serialization;
using RVM.TcgDex.Serialization;

namespace RVM.TcgDex;

/// <summary>Card as it appears in lists and inside a set (<c>CardBrief</c> in the API reference).</summary>
public class CardResume : ISdkBound
{
    private TCGdex? _sdk;

    /// <summary>Globally unique id: set id + local id, e.g. <c>swsh3-136</c>.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Id inside the set, usually the printed number (<c>136</c>, <c>TG01</c>).</summary>
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string LocalId { get; set; } = string.Empty;

    /// <summary>Card name, including the suffix when printed next to it.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Image URL without quality and extension. Prefer <see cref="GetImageUrl"/>.</summary>
    public string? Image { get; set; }

    /// <summary>Full image URL; <c>null</c> when TCGdex has no image for this card.</summary>
    public string? GetImageUrl(Quality quality = Quality.High, Extension extension = Extension.Png) =>
        AssetUrl.Card(Image, quality, extension);

    /// <summary>Fetches the complete card.</summary>
    /// <exception cref="TcgDexNotFoundException">The API no longer has the card.</exception>
    public virtual Task<Card> GetFullAsync(CancellationToken cancellationToken = default) =>
        Sdk.Card.GetRequiredAsync(Id, cancellationToken);

    private protected TCGdex Sdk => SdkBinding.Require(_sdk);

    void ISdkBound.Attach(TCGdex sdk) => Attach(sdk);

    internal virtual void Attach(TCGdex sdk) => _sdk = sdk;
}
