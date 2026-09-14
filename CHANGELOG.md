# Changelog

Format: [Keep a Changelog](https://keepachangelog.com/en/1.1.0/). Versioning:
[SemVer](https://semver.org/) — the version is the git tag (`v1.2.3` publishes `1.2.3`).

## [1.0.0] — unreleased

First release, covering the TCGdex REST API v2.

### Added

- `TCGdex` client with the shape of the official SDKs: `Card`, `Set`, `Serie` (`GetAsync`,
  `ListAsync`) and `Random`
- Navigation: `card.GetSetAsync()`, `set.GetSerieAsync()`, `set.GetCardAsync(localId)`,
  `GetFullAsync()` from any brief model
- `Query`: `Contains`, `Equal`, `Not.*`, `GreaterThan`/`GreaterOrEqualThan`,
  `LesserThan`/`LesserOrEqualThan`, `IsNull`/`NotNull`, `Sort`, `Paginate`
- 13 catalogs: `Types`, `Hp`, `Illustrators`, `Rarities`, `Categories`, `EnergyTypes`, `Retreats`,
  `Stages`, `Suffixes`, `TrainerTypes`, `DexIds`, `RegulationMarks`, `Variants`
- Typed prices: Cardmarket (EUR) and TCGplayer (USD, per printing), also per detailed variant
- Image helpers: `GetImageUrl(Quality, Extension)`, `GetLogoUrl`, `GetSymbolUrl`
- The 18 languages of the API, `SetLanguage`, `SetEndpoint` (self-hosted TCGdex)
- In-memory cache with TTL (1 hour by default), `SetCacheTtl`, pluggable `IDistributedCache`
- `services.AddTcgDex()` over `IHttpClientFactory`
- `TcgDexException` / `TcgDexNotFoundException`
- `net8.0` and `net10.0`

[1.0.0]: https://github.com/Elfarrar/RVM.TcgDex/releases/tag/v1.0.0
