# CLAUDE.md — RVM.TcgDex

SDK C# da API [TCGdex](https://tcgdex.dev). Segundo produto do projeto **TradeBinder**; primeiro
cliente dele é o próprio TradeBinder (pelo nuget.org, nunca por ProjectReference).

Porte: aplicacao (biblioteca publicada — não tem VPS, Docker, banco nem deploy)

## Onde está a spec e as tasks

- **Spec:** `C:\IA\RVM.TradeBinder\11-sdk-rvm-tcgdex.md` (escopo 1.0, técnico, testes, listagem).
- **Tasks:** prefixo **`TBIN-`**, cards em `C:\IA\RVM.TradeBinder\docs\Vault\02_Tasks\` — este repo
  **não** tem Kanban próprio. Branch `tbin-NNN` a partir de `master`.

## Regras que valem aqui

- **Repo PÚBLICO, licença MIT.** Nada de credencial, URL interna, nome de VPS nem regra do
  TradeBinder no código ou no histórico (sem coleção, sem BRL). O SDK tem que servir a qualquer um.
- **CI próprio em `ubuntu-latest`, NÃO o `RVM.Actions`.** Repo público não chama reusable workflow
  de repo privado — o run morre em 0 s, zero jobs, sem mensagem. Mesma decisão do
  `RVM.DesignSystem` (ADR-011 de lá). Não "corrigir" para caller.
- **Versão por TAG** (`v1.2.3` publica `1.2.3` no **nuget.org**), nunca literal no csproj.
  Secret `NUGET_ORG_API_KEY` no repo.
- **Não segue VSA/MediatR** — é biblioteca, não aplicação.
- Alvos `net8.0` + `net10.0`, **sem `netstandard2.0`** (P8, decidido em 13/09). Dependências só
  `Microsoft.Extensions.*`.
- **A classe principal é `TCGdex`, não `TcgDex`** — igual aos SDKs oficiais, e porque um tipo com o
  nome do último segmento do namespace (`RVM.TcgDex.TcgDex`) quebra a resolução de nome em quem
  está num namespace `RVM.*` (o próprio TradeBinder): CS0118. Não "padronizar o casing".
- **Modelos com `{ get; set; }`, não `init`** — o source generator do System.Text.Json põe
  `default` (null) em toda propriedade `init` ausente no JSON, atropelando o inicializador. Teste
  `PartialBody_KeepsNonNullableDefaults` segura isso.
- Código, XML doc e README em **inglês** (público internacional, listagem em `tcgdex.dev/sdks`).
  Commit e card em português, como no resto do ecossistema.
- Teste de contrato contra a API real **não** roda no CI de PR (não depender de rede de terceiro
  para mergear) — agendado (`contract.yml`), e falha vira issue com label `contract`. O projeto
  de contrato está na `.slnx`: por isso `ci.yml` e `publish-nuget.yml` testam **só**
  `test/RVM.TcgDex.Tests` — `dotnet test` da solution faria a publicação depender da TCGdex.
- Ensaio de pacote em feed local deixa o `rvm.tcgdex/<versão>` no cache do NuGet
  (`~/.nuget/packages`) — apagar depois, senão ele se passa pelo do nuget.org na mesma versão.
- Cobertura ≥ 80% (portão no `ci.yml`).
