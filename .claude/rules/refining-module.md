---
paths:
  - "**/*efin*"
  - "**/*Focus*"
  - "**/*ItemIdResolver*"
  - "**/*CityResourceSpecialization*"
---

# Módulo de Refino

**Status:** especificado e planejado (`requirements.md`, `design.md` e `tasks.md` em `.claude/specs/refining-module/`), **ainda não implementado**. Antes de implementar, leia os três inteiros — o `tasks.md` é o plano de execução e contém as decisões de 2026-10-23 que alteram pontos do design original. Este arquivo é só um resumo das decisões que não podem ser quebradas.

## Arquitetura obrigatória

- `RefiningCalculatorService`: **puro, sem I/O**. Não recebe `IHttpClientFactory` nem `AppDbContext`. Concentra as fórmulas, a resolução de cidades e a validação de domínio.
- `MarketPriceProviderService`: fala com a AODP em uma única chamada combinando bruto, refinado do tier anterior e refinado, com timeout de 10s. A regra de disponibilidade fica em `MarketPriceAvailabilityJudge.IsAvailable`, método estático puro (preço nulo, zero ou com mais de 24h significa indisponível). Usa **DTO de resposta próprio** com `city` como `string` normalizada — reusar `AodpItemPriceResponseDTO` quebra em `"Fort Sterling"`, que é a cidade especializada em Wood.
- `RefiningSimulationRepository`: persistência EF Core em `Repository/`, junto do `ItemLabelRepository` (decidido em 2026-10-23; o design.md sugeria `Data/`).
- `RefiningController` em `api/refining`: `POST simulate` e `GET history?playerId=&page=`.

## Fórmulas e constantes (MVP, apenas Royal Cities)

- `RRR = 1 - 1/(1 + ProductionBonus/100)`, sempre em `decimal`, nunca `double`/`float`.
- Bônus base: **58** pp se a cidade de refino é especializada no recurso, senão **18** pp.
- Focus: **+59** pp somente se `UseFocus && FocusPoints >= FocusCost`. Com Focus insuficiente, o bônus não é aplicado e o aviso é sinalizado. Se forem iguais, o Focus é suficiente.
- Especialização: Martlock=Hide, Bridgewatch=Stone, Lymhurst=Fiber, FortSterling=Wood, Thetford=Ore, Caerleon/Brecilien=nenhum. É uma tabela estática `IReadOnlyDictionary`, não um enum novo.
- Receita por craft, confirmada no `ao-bin-dumps/items.xml` e igual para os 5 recursos — bruto / refinado T-1 / `craftingfocus`: T2 `1/—/18`, T3 `2/1/31`, T4 `2/1/54`, T5 `3/1/94`, T6 `4/1/164`, T7 `5/1/287`, T8 `5/1/503`. Esses valores de focus são oficiais e **substituem** a tabela não oficial do Edison-Cabrera citada no design.md. A tabela de focus base fica configurável em `RefiningOptions`.
- Focus Cost: `BaseFocusPerCraft(tier) × craftCount × Math.Pow(0.5, MasteryLevel × 0.04)`. A curva vem do albion-domain-context 2.6 ("cada 10.000 de eficiência reduz à metade", teto 40.000) e **substitui a aproximação linear** do design.md. Continua sendo aproximação — documente em XML doc.
- **Premissa não verificada** (adotada em 2026-10-23, pendente de confirmação em fonte oficial): a RRR devolve todos os materiais consumidos, inclusive o refinado T-1. O albion-domain-context 2.1 só menciona o recurso bruto, e a wiki oficial está atrás de Cloudflare. Se a premissa cair, só a devolução do T-1 muda.
- Item IDs via `ItemIdResolver` (função pura), no formato `T{tier}_{ID}`, confirmados em T2–T8 no ao-bin-dumps. Brutos: `FIBER`, `HIDE`, `ORE`, `ROCK` (Stone), `WOOD`. Refinados: `CLOTH`, `LEATHER`, `METALBAR`, `STONEBLOCK`, `PLANKS`.
- Modo de aquisição (`AcquisitionModeEnum { Gathered, Purchased }`) é parâmetro de entrada, separado para o bruto e para o refinado T-1. `Purchased` entra no custo e tem o devolvido abatido; `Gathered` tem custo zero e o devolvido + a sobra viram receita. O baseline "vender bruto" só existe no modo `Gathered`; em `Purchased` é zero (arbitragem entre cidades ficou fora do MVP).

## Regras de resposta e persistência

- DTO inválido retorna 400 (`ValidationProblemDetails`, com um erro por campo). A regra cross-field `StationFeeFixed` XOR `StationFeePercentage` fica em `Validate`.
- Falha da AODP ou preço indisponível retorna **200 parcial** (`MarketDataAvailable=false` + motivo, custo e lucro `null`) e **não persiste** histórico.
- Sucesso retorna 200 completo e persiste `RefiningSimulationRecord` (`CreatedAtUtc` gerado na aplicação, `decimal(18,4)`, índice `(PlayerId, CreatedAtUtc DESC)`), mais a migration `AddRefiningSimulationRecords`.
- Histórico: no máximo 50 por página, em ordem decrescente de data. Sem registros, retorna lista vazia com 200.

## Testes

xUnit + FsCheck (`[Property(MaxTest = 100)]`), com o comentário `// Feature: refining-module, Property {n}: {texto}` acima de cada teste. As Properties 1–20 do `design.md` cobrem a lógica pura. A integração com a AODP usa `HttpMessageHandler` fake. O projeto de testes ainda precisa ser criado e adicionado ao `.slnx`.

## Gaps conhecidos (fora do MVP)

- Arbitragem de recurso bruto entre cidades (comprar na origem, revender no destino) como baseline do modo `Purchased`.
- Qualidade/encantamento, bônus diário, ilhas e hideouts estão fora de escopo.
