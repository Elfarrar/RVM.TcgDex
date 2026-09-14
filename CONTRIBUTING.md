# Contributing

Issues and pull requests are welcome. Code, XML docs and README are in English.

## Build and test

Needs the .NET SDK pinned in `global.json` (10.x; it also builds the `net8.0` target).

```
dotnet build RVM.TcgDex.slnx -c Release     # warnings are errors in Release
dotnet test test/RVM.TcgDex.Tests            # unit tests, no network
```

The CI fails below **80% line coverage**.

## How the tests are organized

- **`test/RVM.TcgDex.Tests`** — unit tests. A fake `HttpMessageHandler` (`FakeApi`) answers with
  real API responses recorded in `Fixtures/`. No network: the pull request CI must not depend on a
  third-party service. When you need a new shape, record it from the API
  (`curl https://api.tcgdex.net/v2/en/cards/<id> > Fixtures/card-<id>.json`) instead of writing
  JSON by hand.
- **`test/RVM.TcgDex.ContractTests`** — runs against the **live** API, daily and on demand
  (`contract.yml`), never on pull requests. It fails when the API adds or removes a field on the
  sample resources, or sends a field the models do not expose. After mapping the change in the
  models, regenerate the baseline and review its diff:

  ```
  UPDATE_CONTRACT_BASELINE=1 dotnet test test/RVM.TcgDex.ContractTests
  ```

## Design rules

- **Same shape as the official TCGdex SDKs** (`tcgdex.Card.GetAsync`, `Query.Create().Equal(...)`,
  `card.GetImageUrl(...)`). New features should look like they belong to that family.
- **Only `Microsoft.Extensions.*` dependencies.**
- The client class is `TCGdex`, not `TcgDex`: a type named after the last segment of its namespace
  breaks name lookup for consumers in an `RVM.*` namespace (CS0118).
- Models use `{ get; set; }`, not `init`: with `init`, the `System.Text.Json` source generator
  assigns `null` to every property missing from the JSON, overriding the initializer.
- `GetAsync` returns `null` on 404 (like the official SDKs); navigation throws
  `TcgDexNotFoundException`.

## Releases

Maintainers only: a tag `vX.Y.Z` on `master` runs `publish-nuget.yml`, which tests, packs with that
version and pushes to nuget.org. Update `CHANGELOG.md` in the same pull request as the change.
