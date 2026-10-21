---
paths:
  - "**/*efin*"
  - "**/*Focus*"
  - "**/*ItemIdResolver*"
  - "**/*CityResourceSpecialization*"
---

# Módulo de Refino

**Status:** especificado (requirements + design em `.kiro/specs/refining-module/`), **ainda não implementado**. Não existe `tasks.md`. Antes de implementar, leia `requirements.md` e `design.md` inteiros. Este arquivo é só um resumo das decisões que não podem ser quebradas.

## Arquitetura obrigatória

- `RefiningCalculatorService`: **puro, sem I/O**. Não recebe `IHttpClientFactory` nem `AppDbContext`. Concentra as fórmulas, a resolução de cidades e a validação de domínio.
- `MarketPriceProviderService`: fala com a AODP em uma única chamada combinando bruto e refinado, com timeout de 10s. A regra de disponibilidade fica em `MarketPriceAvailabilityJudge.IsAvailable`, método estático puro (preço nulo, zero ou com mais de 24h significa indisponível).
- `RefiningSimulationRepository`: persistência EF Core. O design sugere `Data/`, mas o projeto já tem a pasta `Repository/`, com `ItemLabelRepository`. Confirme com o usuário onde criar.
- `RefiningController` em `api/refining`: `POST simulate` e `GET history?playerId=&page=`.

## Fórmulas e constantes (MVP, apenas Royal Cities)

- `RRR = 1 - 1/(1 + ProductionBonus/100)`, sempre em `decimal`, nunca `double`/`float`.
- Bônus base: **58** pp se a cidade de refino é especializada no recurso, senão **18** pp.
- Focus: **+59** pp somente se `UseFocus && FocusPoints >= FocusCost`. Com Focus insuficiente, o bônus não é aplicado e o aviso é sinalizado. Se forem iguais, o Focus é suficiente.
- Especialização: Martlock=Hide, Bridgewatch=Stone, Lymhurst=Fiber, FortSterling=Wood, Thetford=Ore, Caerleon/Brecilien=nenhum. É uma tabela estática `IReadOnlyDictionary`, não um enum novo.
- Focus Cost (**aproximação não oficial**, documente em XML doc): `BaseFocusCostPerUnit × RawQuantity × (1 - MasteryLevel/100 × 0.9375)`. O `BaseFocusCostPerUnit` deve ser configurável.
- Item IDs via `ItemIdResolver` (função pura), no formato `T{tier}_{ID}`. Brutos: `FIBER`, `HIDE`, `ORE`, `ROCK` (Stone), `WOOD`. Refinados: `CLOTH`, `LEATHER`, `METALBAR`, `STONEBLOCK`, `PLANKS`. O design não fixa os IDs brutos, então confirme no ao-bin-dumps (`items.txt`) antes de hardcodar.

## Regras de resposta e persistência

- DTO inválido retorna 400 (`ValidationProblemDetails`, com um erro por campo). A regra cross-field `StationFeeFixed` XOR `StationFeePercentage` fica em `Validate`.
- Falha da AODP ou preço indisponível retorna **200 parcial** (`MarketDataAvailable=false` + motivo, custo e lucro `null`) e **não persiste** histórico.
- Sucesso retorna 200 completo e persiste `RefiningSimulationRecord` (`CreatedAtUtc` gerado na aplicação, `decimal(18,4)`, índice `(PlayerId, CreatedAtUtc DESC)`), mais a migration `AddRefiningSimulationRecords`.
- Histórico: no máximo 50 por página, em ordem decrescente de data. Sem registros, retorna lista vazia com 200.

## Testes

xUnit + FsCheck (`[Property(MaxTest = 100)]`), com o comentário `// Feature: refining-module, Property {n}: {texto}` acima de cada teste. As Properties 1–20 do `design.md` cobrem a lógica pura. A integração com a AODP usa `HttpMessageHandler` fake. O projeto de testes ainda precisa ser criado e adicionado ao `.slnx`.

## Gaps conhecidos (fora do MVP)

- Refino de T4+ consome também material refinado do tier anterior, e isso não está modelado.
- Qualidade/encantamento, bônus diário, ilhas e hideouts estão fora de escopo.
