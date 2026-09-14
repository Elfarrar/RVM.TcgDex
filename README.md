# RVM.TcgDex

<img src="https://raw.githubusercontent.com/Elfarrar/RVM.TcgDex/master/assets/icon.png" alt="" width="64" align="right">

C# SDK for the [TCGdex](https://tcgdex.dev) API — Pokémon TCG cards, sets, series, market prices
and images, in 18 languages. The API is free and needs no key.

Same shape as the official TCGdex SDKs (JavaScript, Python, Kotlin…): if you know one of them, you
already know this one.

- `net8.0` and `net10.0`, nullable-annotated, `CancellationToken` everywhere
- Typed prices from Cardmarket (EUR) and TCGplayer (USD, per printing)
- `System.Text.Json` source generator — no reflection at runtime
- Built-in cache (1 hour, like the official SDKs), or bring your own `IDistributedCache`
- `services.AddTcgDex()` over `IHttpClientFactory`

## Install

```
dotnet add package RVM.TcgDex
```

## Quick start

```csharp
using RVM.TcgDex;

var tcgdex = new TCGdex(Language.En);

var card = await tcgdex.Card.GetAsync("swsh3-136");
Console.WriteLine($"{card!.Name} — {card.Set.Name}, {card.Hp} HP");
// Furret — Darkness Ablaze, 110 HP
```

## Examples

### 1. Get a card, a set, a serie

```csharp
Card? card   = await tcgdex.Card.GetAsync("swsh3-136");
Set? set     = await tcgdex.Set.GetAsync("swsh3");       // a set name works too: "Darkness Ablaze"
Serie? serie = await tcgdex.Serie.GetAsync("swsh");
```

`GetAsync` returns `null` when the resource does not exist, like the official SDKs.

### 2. Search with a query

```csharp
var furrets = await tcgdex.Card.ListAsync(Query.Create().Equal("name", "Furret"));

var bigOnes = await tcgdex.Card.ListAsync(Query.Create()
    .GreaterOrEqualThan("hp", 300)
    .Not.Equal("category", "Trainer")
    .Sort("hp", SortOrder.Desc)
    .Paginate(page: 1, itemsPerPage: 20));
```

`Contains` (the API default, case-insensitive), `Equal`, `Not.Equal`, `Not.Contains`,
`GreaterThan`, `GreaterOrEqualThan`, `LesserThan`, `LesserOrEqualThan`, `IsNull`, `NotNull`, `Sort`,
`Paginate` — the [filter syntax of the REST API](https://tcgdex.dev/rest/filtering-sorting-pagination).
Lists return the brief version of each item; `GetFullAsync()` fetches the rest.

### 3. Images

```csharp
string? url  = card.GetImageUrl(Quality.High, Extension.Webp); // .../swsh3/136/high.webp
string? logo = set.GetLogoUrl(Extension.Png);
string? icon = set.GetSymbolUrl();
```

Image URLs are `null` when TCGdex has no image for that item.

### 4. Market prices

```csharp
CardmarketPricing? eu = card.Pricing?.Cardmarket;   // EUR
Console.WriteLine($"Cardmarket trend: € {eu?.Trend}, 30-day average: € {eu?.Avg30}, foil: € {eu?.TrendHolo}");

TcgplayerPricing? us = card.Pricing?.Tcgplayer;     // USD, per printing
Console.WriteLine($"TCGplayer market: $ {us?.Normal?.MarketPrice}, reverse holo: $ {us?.ReverseHolofoil?.MarketPrice}");

foreach (var variant in card.VariantsDetailed ?? [])
    Console.WriteLine($"{variant.Type}: $ {variant.Pricing?.Tcgplayer?.Normal?.MarketPrice}");
```

A marketplace that does not list the card is `null`.

### 5. Navigate: card → set → serie

```csharp
Set set       = await card.GetSetAsync();
Serie serie   = await set.GetSerieAsync();
Card? furret  = await set.GetCardAsync("136");         // by the number printed on the card
Card full     = await set.Cards[0].GetFullAsync();     // brief → complete
Set lastSet   = await serie.Sets[^1].GetFullAsync();
```

Navigation uses the client that fetched the object — same language, endpoint and cache.

### 6. Languages

```csharp
var brazilian = new TCGdex(Language.PtBr);
var bulbasaur = await brazilian.Card.GetAsync("A1-001");
Console.WriteLine(bulbasaur!.Set.Name);                // Dominação Genética

tcgdex.SetLanguage(Language.Ja);                       // for the next requests
```

The 18 languages of the API: `En`, `Fr`, `Es`, `EsMx`, `It`, `Pt`, `PtBr`, `PtPt`, `De`, `Nl`,
`Pl`, `Ru`, `Ja`, `Ko`, `ZhTw`, `Id`, `Th`, `ZhCn`. Not every card exists in every language.

### 7. Catalogs

```csharp
IReadOnlyList<string> rarities = await tcgdex.Rarities.ListAsync();
IReadOnlyList<int> hpValues    = await tcgdex.Hp.ListAsync();

CatalogEntry? furrets = await tcgdex.DexIds.GetAsync(162);   // every card of Pokédex #162
Console.WriteLine($"{furrets!.Cards.Count} cards");
```

`Types`, `Hp`, `Illustrators`, `Rarities`, `Categories`, `EnergyTypes`, `Retreats`, `Stages`,
`Suffixes`, `TrainerTypes`, `DexIds`, `RegulationMarks`, `Variants`.

### 8. Random

```csharp
Card card   = await tcgdex.Random.GetCardAsync();
Set set     = await tcgdex.Random.GetSetAsync();
Serie serie = await tcgdex.Random.GetSerieAsync();
```

### 9. Dependency injection

```csharp
builder.Services.AddTcgDex(o =>
{
    o.Language = Language.PtBr;
    o.CacheTtl = TimeSpan.FromHours(6);
});

// anywhere
public sealed class CardService(TCGdex tcgdex) { /* ... */ }
```

`AddTcgDex` returns the `IHttpClientBuilder` of the underlying client. The SDK does not retry on
its own (neither do the official SDKs) and the API does have short outages (`503`), so add a
resilience handler in production — with the `Microsoft.Extensions.Http.Resilience` package:

```csharp
builder.Services.AddTcgDex().AddStandardResilienceHandler();
```

Each injected `TCGdex` has its own copy of the options (`SetLanguage` on one does not affect the
others) and they all share one cache.

### 10. Cache and self-hosted TCGdex

```csharp
tcgdex.SetCacheTtl(TimeSpan.FromMinutes(10));
tcgdex.SetCacheTtl(TimeSpan.Zero);                     // no cache

tcgdex.SetEndpoint("https://tcgdex.example.com/v2");  // your own TCGdex instance

// your own cache: Redis, AddDistributedMemoryCache()…
var shared = new TCGdex(httpClient, new TcgDexOptions { Language = Language.En }, distributedCache);
```

Responses are cached as JSON under keys prefixed with `rvm-tcgdex:`, so every read gets a fresh
object. With `AddTcgDex`, an `IDistributedCache` registered in the container is used automatically.

## Errors

| Situation | Result |
|---|---|
| `GetAsync` of something that does not exist | `null` |
| Navigation (`GetSetAsync`, `GetFullAsync`…) to something the API lost | `TcgDexNotFoundException` |
| API error, outage, or unexpected body | `TcgDexException` (with `StatusCode` and `RequestUri`) |
| Language not served by a self-hosted instance | `TcgDexException` |

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md). Changes are listed in [CHANGELOG.md](CHANGELOG.md).

## License and credits

MIT — see [LICENSE](LICENSE).

Card data, images and prices come from [TCGdex](https://tcgdex.dev). This is an independent,
community SDK, not affiliated with TCGdex. Pokémon and its trademarks are © Nintendo, Creatures
Inc. and GAME FREAK inc.; this project is not affiliated with or endorsed by them.
