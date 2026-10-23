# Plano de Implementação — Módulo de Refino (MVP, 5 recursos)

Escopo: Fiber/Cloth, Hide/Leather, Ore/MetalBar, Rock/StoneBlock, Wood/Planks, T2–T8.
Crafting fica fora do escopo deste plano.

Decisões tomadas em 2026-10-23, que **alteram** o que está em `requirements.md` e `design.md`
(ambos já atualizados; este arquivo é o plano de execução):

1. `RefiningSimulationRepository` fica em `Repository/` (o design.md sugeria `Data/`).
2. A receita real por tier entra no MVP, **incluindo o material refinado do tier anterior** — o design.md
   listava isso como gap fora de escopo.
3. Modo de aquisição (coletado vs comprado) e a cidade de compra do material T-1 são **parâmetros de
   entrada**, não premissas fixas do cálculo.
4. Projeto de testes xUnit + FsCheck criado agora, cobrindo a lógica pura.

## Dados oficiais confirmados no ao-bin-dumps (`items.xml`)

Primeira `craftingrequirements` de cada refinado, `amountcrafted=1`:

| Tier | Bruto por craft | Refinado T-1 por craft | `craftingfocus` (oficial) |
|---|---|---|---|
| T2 | 1 | — | 18 |
| T3 | 2 | 1 | 31 |
| T4 | 2 | 1 | 54 |
| T5 | 3 | 1 | 94 |
| T6 | 4 | 1 | 164 |
| T7 | 5 | 1 | 287 |
| T8 | 5 | 1 | 503 |

Igual para os 5 recursos. Esses valores de focus são do próprio jogo e **substituem a tabela não oficial
do Edison-Cabrera** citada no design.md (T4=48 vs 54 real, T5=89 vs 94 real).

Item IDs confirmados em T2–T8, padrão `T{tier}_{BASE}`:

| Recurso | Bruto | Refinado |
|---|---|---|
| Fiber | `FIBER` | `CLOTH` |
| Hide | `HIDE` | `LEATHER` |
| Ore | `ORE` | `METALBAR` |
| Stone | `ROCK` | `STONEBLOCK` |
| Wood | `WOOD` | `PLANKS` |

Atenção: `RefinedResourceEnum.Stone` → bruto `ROCK` (o nome do enum não bate com o ID do item).

## Modelo de cálculo

### Quantidades

```
craftCount    = RawQuantity / RawPerCraft(tier)      // divisão inteira
rawUsed       = craftCount * RawPerCraft(tier)
leftoverRaw   = RawQuantity - rawUsed                // sobra que não dá pra refinar
prevUsed      = craftCount * PreviousRefinedPerCraft(tier)   // 0 em T2
returnedRaw   = rawUsed  * RRR
returnedPrev  = prevUsed * RRR
refinedProduced = craftCount
```

`craftCount = 0` (quantidade menor que a receita) é entrada válida: simulação sem produção, não erro.

### Focus

```
focusCost = BaseFocusPerCraft(tier) * craftCount * Math.Pow(0.5, MasteryLevel * 0.04)
```

A base é oficial. O multiplicador vem do albion-domain-context 2.6 ("cada 10.000 pontos de eficiência reduz o custo à
metade", teto de 40.000), mapeando `MasteryLevel` 0–100 → eficiência 0–40.000. Dá 1.0 em M=0 e 0.0625 em
M=100, igual ao teto da wiki, e é fiel à curva no meio (M=50 → 25%, enquanto a aproximação linear do
design.md dava 53%). Continua monotônica não crescente, então a Property 6 segue valendo por construção.

Calcular a potência em `double` e converter para `decimal` só no final, ou pré-calcular os 101 fatores.

### Modos de aquisição (parâmetros novos)

`AcquisitionModeEnum { Gathered = 1, Purchased = 2 }`, aplicado em dois campos independentes:
`RawAcquisitionMode` e `PreviousRefinedAcquisitionMode` (ambos default `Purchased`).

- `Purchased`: o material entra no custo. O que a RRR devolve abate esse custo.
  `custoBruto = (rawUsed − returnedRaw) × precoBruto`
  `custoPrev  = (prevUsed − returnedPrev) × precoRefinadoAnterior`
- `Gathered`: o material já é do jogador, custo zero. O que sobra e o que a RRR devolve viram
  receita (valor de mercado), porque o jogador pode vender. O preço de mercado do material continua
  sendo necessário, para valorizar esse devolvido.

**Premissa não verificada**: a RRR devolve também o material T-1 consumido. O albion-domain-context 2.1 só fala do
recurso bruto e a wiki oficial está atrás de Cloudflare — confirmar no jogo antes de tratar como certo.

### Custo, receita e lucro

```
custoAquisicao     = custoBruto + custoPrev          // conforme o modo de cada um
stationFee         = fixo, OU percentual sobre custoAquisicao
custoTotalRefino   = custoAquisicao + stationFee

receitaBrutaRefinado = refinedProduced * precoRefinado(DestinationCity)
receitaLiquidaRefino = receitaBrutaRefinado * (1 - MarketTax/100)

sobras = 0
  + (RawAcquisitionMode == Gathered  ? (returnedRaw + leftoverRaw) * precoBruto * (1 - tax/100) : 0)
  + (PrevAcquisitionMode == Gathered ?  returnedPrev * precoPrev   * (1 - tax/100) : 0)

lucroRefinar = receitaLiquidaRefino + sobras - custoTotalRefino
```

Baseline de comparação (Requisito 4.9), agora dependente do modo:

- `RawAcquisitionMode == Gathered`: `lucroVenderBruto = RawQuantity × precoBruto(OriginCity) × (1 − tax/100)`.
  É a comparação que o jogador realmente faz: tenho o material, refino ou vendo?
- `RawAcquisitionMode == Purchased`: `lucroVenderBruto = 0` — comprar e revender o bruto na mesma cidade
  não gera lucro. Arbitragem entre cidades (comprar na origem, vender no destino) exigiria o preço do
  bruto também na Destination_City; fica como extensão futura, não entra no MVP.

Isso resolve a inconsistência original entre Req 4.1 (que cobrava o bruto) e Req 4.7 (que assumia o bruto
já em mãos), que fazia a recomendação sair "vender bruto" em quase todo cenário.

### Cidades

Quatro parâmetros opcionais sobre a cidade base, todos resolvidos pela mesma regra do Requisito 5
(ausente = cidade base): `OriginCity` (bruto), `PreviousRefinedCity` (material T-1), `RefiningCity`,
`DestinationCity`. Só a `RefiningCity` influencia o Production Bonus.

### Consulta de mercado

Uma única chamada à AODP com até 3 item IDs e até 3 cidades:

```
GET {Host}/api/v2/stats/prices/{T{n}_RAW},{T{n-1}_REFINED},{T{n}_REFINED}.json
    ?locations={OriginCity},{PreviousRefinedCity},{DestinationCity}&qualities=1
```

Em T2 são só 2 IDs. De T3 em diante o preço do T-1 é **sempre** necessário, inclusive no modo `Gathered`
(é ele que valoriza o material devolvido). Cada item é filtrado pela cidade que interessa a ele. Se qualquer preço necessário estiver indisponível
(nulo, zero ou com mais de 24h), vale o Requisito 3.5: resposta 200 parcial, sem persistir.

## Pendências e escolhas a revisar

Decisões tomadas no detalhamento, que o usuário não escolheu explicitamente:

1. **Station_Fee percentual** incide sobre o custo de aquisição. Em modo `Gathered` nos dois materiais
   esse custo é zero e a taxa some — no jogo a taxa da estação não depende de o material ter sido
   comprado. Alternativa: aplicar o percentual sobre o **valor de mercado** dos materiais consumidos,
   independentemente do modo. Decidir antes do passo 4.
2. O modo de aquisição foi criado **separado** para o bruto e para o T-1. Poderia ser um só.
3. No modo `Purchased`, o baseline "vender bruto" é zero; arbitragem entre cidades ficou fora.
4. O material devolvido é valorizado **líquido de imposto** no modo `Gathered` (vira receita) e **a preço
   cheio** no modo `Purchased` (abate custo). Trocar de modo muda mais do que só o custo.
5. No modo `Gathered` o preço de mercado do material continua sendo obrigatório.
6. A premissa de que a RRR devolve o material T-1 não foi confirmada em fonte oficial.

## Passo 0 — specs atualizadas

`requirements.md`, `design.md` e `.claude/rules/refining-module.md` já foram atualizados com as decisões
acima. O rules file carrega automaticamente em qualquer arquivo `*efin*`; sem essa atualização ele
reintroduziria a fórmula antiga de Focus e o "T4+ não modelado" como gap.

## Ordem de implementação

### 1. Enums (`AlbionWebApp/Enums/`)
- `RefinedResourceEnum`: Fiber=1, Hide=2, Ore=3, Stone=4, Wood=5.
- `RefiningRecommendationEnum`: Refine=1, SellRaw=2, Equal=3.
- `AcquisitionModeEnum`: Gathered=1, Purchased=2.

Não precisam de seed nem migration: ao contrário de `AlbionCityEnum`/`ItemQualityEnum`, não espelham
tabela do banco.

### 2. Tabelas estáticas puras (`AlbionWebApp/Services/`)
- `CityResourceSpecializationMap` — `IReadOnlyDictionary<AlbionCityEnum, RefinedResourceEnum?>`:
  Martlock=Hide, Bridgewatch=Stone, Lymhurst=Fiber, FortSterling=Wood, Thetford=Ore,
  Caerleon=null, Brecilien=null.
- `ItemIdResolver.Resolve(RefinedResourceEnum resource, int tier, bool refined)` → `T{tier}_{BASE}`.
- `RefiningRecipeTable` — por tier: `(RawPerCraft, PreviousRefinedPerCraft, BaseFocusPerCraft)`.

### 3. `Options/RefiningOptions.cs`
Seção `Refining` do appsettings, com o focus base por tier (defaults = os 7 valores oficiais), para
permitir ajuste sem recompilar, como o design.md pede.

### 4. `Services/RefiningCalculatorService.cs` + `IRefiningCalculatorService`
Puro, sem I/O. Não recebe `IHttpClientFactory` nem `AppDbContext`.

- `ResolveCities(baseCity, origin?, previousRefined?, refining?, destination?)`
- `CalculateProductionBonus(refiningCity, resource, useFocus, focusPointsAvailable, focusCost)` — 18 ou 58 pp, +59 se o focus for suficiente (igual conta como suficiente)
- `CalculateFocusCost(tier, rawQuantity, masteryLevel)`
- `CalculateRRR(productionBonusPercentagePoints)` — `1 − 1/(1 + bonus/100)`
- `CalculateMaterials(tier, rawQuantity, rrr)` — todas as quantidades da seção "Quantidades"
- `CalculateCostAndProfit(CostAndProfitInput)` — com os dois modos de aquisição e o preço do T-1
- `Validate(RefiningSimulationRequestDTO)` — regras cross-field (XOR entre `StationFeeFixed` e
  `StationFeePercentage`, tier, recurso, cidades)

Tudo em `decimal`, nunca `double`/`float` (exceto a potência do focus, convertida no fim).
XML doc no `CalculateFocusCost` registrando que o multiplicador de maestria é aproximação derivada do
albion-domain-context, não fórmula oficial do jogo.

### 5. `Services/MarketPriceAvailabilityJudge.cs`
`public static bool IsAvailable(decimal? sellPriceMin, DateTime? lastUpdated, DateTime now)` — indisponível
se nulo, zero, ou `now − lastUpdated > 24h`. Puro, testável sem rede.

### 6. `Services/MarketPriceProviderService.cs` + `IMarketPriceProviderService`
- Uma chamada só, no formato da seção "Consulta de mercado".
- **DTO de resposta próprio** (`AodpRefiningPriceDTO`) com `city` como `string`, normalizado por
  `Enum.TryParse` tolerante a espaço e caixa. Não reusar `AodpItemPriceResponseDTO`: ele quebra ao
  desserializar `"Fort Sterling"` — justamente a cidade especializada em Wood. Não mexer no DTO do Search.
- Checar se o corpo é o texto puro de throttle da AODP antes de desserializar.
- Timeout por `CancellationTokenSource(TimeSpan.FromSeconds(AodpOptions.TimeoutSeconds))` linkado ao token
  externo; nunca mexer em `HttpClient.Timeout`.
- `DateTime.SpecifyKind(..., DateTimeKind.Utc)` nas datas antes de comparar com `DateTime.UtcNow`.
- Timeout ou status não 2xx → `CommunicationFailed = true` + `FailureReason`. Nunca exceção para o Controller.

### 7. Entidade e mapeamento
`Models/RefiningSimulationRecord.cs` conforme o design.md, mais os campos novos: `PreviousRefinedCity`,
`RawAcquisitionMode`, `PreviousRefinedAcquisitionMode`, `RawUsed`, `PreviousRefinedUsed`,
`RefinedProduced`, `ReturnedRawQuantity`, `ReturnedPreviousRefinedQuantity`, `RawItemPrice`,
`PreviousRefinedPrice`, `RefinedItemPrice`. Sem `FocusCostRequested` (o DTO não tem esse campo; só o
calculado é persistido).

No `AppDbContext`: `DbSet<RefiningSimulationRecord>`,
`HasIndex(r => new { r.PlayerId, r.CreatedAtUtc }).IsDescending(false, true)`, `decimal(18,4)` nos
monetários, `CreatedAtUtc` gerado na aplicação em UTC.

### 8. Migration
`dotnet ef migrations add AddRefiningSimulationRecords --project AlbionWebApp`

### 9. `Repository/RefiningSimulationRepository.cs` + `IRefiningSimulationRepository`
`SaveAsync` e `GetHistoryAsync(playerId, page, pageSize = 50)` com `OrderByDescending(CreatedAtUtc)`,
`Skip((page − 1) * pageSize).Take(pageSize)` e `AsNoTracking()` na leitura.

### 10. DTOs
`RefiningSimulationRequestDTO` (com os campos novos de modo e `PreviousRefinedCity`),
`RefiningSimulationResponseDTO` (com as quantidades de material e o preço do T-1),
`RefiningHistoryQueryDTO`.

### 11. `Controllers/RefiningController.cs`
`POST api/refining/simulate` e `GET api/refining/history`. Controller fino, na ordem do design.md.
Erros de domínio via `ModelState.AddModelError(campo, mensagem)` + `return ValidationProblem(ModelState)`,
para o 400 ter a mesma forma do automático do `[ApiController]`.
Falha de comunicação ou preço indisponível → 200 parcial, **sem** chamar o repository.

### 12. `Program.cs`
Registrar por interface (`AddScoped<IRefiningCalculatorService, RefiningCalculatorService>()` etc.) e
`builder.Services.Configure<RefiningOptions>(...)`. **Não** adicionar `JsonStringEnumConverter` global —
mudaria a serialização do módulo de Search.

### 13. Testes — `AlbionWebApp.Tests` (xUnit + FsCheck.Xunit, adicionar ao `.slnx`)
Properties 1–18 do design.md sobre a lógica pura, cada teste com o comentário
`// Feature: refining-module, Property {n}: {texto}` logo acima.
Properties 19–20 (persistência e paginação) ficam para quando houver banco de teste.

**Atenção**: as Properties 9, 10 e 11 têm nota de revisão no design.md e **não valem no enunciado
original** com os modos de aquisição — implemente o enunciado revisado, senão o teste falha contra o
código correto. As demais seguem válidas como estão.

Casos novos que as Properties originais não cobrem:
- A RRR devolve também o material T-1 consumido.
- `RawQuantity < RawPerCraft` → `craftCount = 0`, simulação sem produção.
- `Gathered` zera o custo do material e joga o devolvido + a sobra na receita.
- `Purchased` abate o devolvido do custo e usa baseline de venda bruta igual a zero.

## Verificação

```powershell
dotnet build AlbionWebApp.slnx
dotnet ef migrations add AddRefiningSimulationRecords --project AlbionWebApp   # não precisa de banco
dotnet test
docker compose up -d; dotnet ef database update --project AlbionWebApp          # opcional
```
