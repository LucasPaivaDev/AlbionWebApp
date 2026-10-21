# AlbionWebApp

API REST em ASP.NET Core que consulta preços de mercado do Albion Online via **Albion Online Data Project (AODP)**, persiste snapshots no PostgreSQL e (em desenvolvimento) simula operações de **refino** com cálculo de RRR, Focus e lucro.

## Stack

- .NET 10 (`net10.0`), ASP.NET Core Web API com Controllers, `Nullable` e `ImplicitUsings` habilitados
- EF Core 10 + Npgsql (PostgreSQL 16 via `docker-compose.yml`)
- Swashbuckle (Swagger UI) + `Microsoft.AspNetCore.OpenApi`, ambos só em Development
- Solução: `AlbionWebApp.slnx` → projeto único `AlbionWebApp/AlbionWebApp.csproj`
- **Não há projeto de testes ainda** (o design do módulo de Refino prevê xUnit + FsCheck)

## Comandos

```powershell
docker compose up -d                                   # sobe o Postgres (albion-db, porta 5432)
dotnet build AlbionWebApp.slnx
dotnet run --project AlbionWebApp                       # http://localhost:5105 | https://localhost:7177
dotnet ef migrations add <Nome> --project AlbionWebApp
dotnet ef database update --project AlbionWebApp
```

Swagger: `/swagger` (Development). A connection string fica em `appsettings.Development.json`; a config da AODP fica na seção `Aodp` do `appsettings.json` (`Host`, `TimeoutSeconds`).

## Estrutura

| Pasta | Conteúdo |
|---|---|
| `Controllers/` | `SearchItemsController` (`POST /api/SearchItems`). `WeatherForecastController` é resto do template. |
| `Services/` | `SearchItemsService` — chama a AODP via `IHttpClientFactory`, desserializa e persiste. |
| `Repository/` | `ItemLabelRepository` — grava `ItemLabel` e `ItemPrice` via `AppDbContext`. |
| `Data/` | `AppDbContext` (DbSets + seed de `City`/`Quality` via `HasData`). |
| `DTOs/` | `SearchItemsFiltersDTO` (entrada, Data Annotations), `AodpItemPriceResponseDTO` (resposta da AODP, `JsonPropertyName` snake_case). |
| `Models/` | Entidades EF: `ItemLabel`, `ItemPrice`, `City`, `Quality`. |
| `Enums/` | `AlbionCityEnum` (1–7), `ItemQualityEnum` (1–5). |
| `Options/` | `AodpOptions` (Options pattern, seção `Aodp`). |
| `Migrations/` | Migrations EF Core. |

## Fluxo atual (Search)

`SearchItemsController` → `SearchItemsService.AsyncGetItems` → `GET {Aodp.Host}/api/v2/stats/prices/{ItemName}.json?locations={ItemBuyCity}` → desserializa em `List<AodpItemPriceResponseDTO>` → para cada item, `ItemLabelRepository` cria um `ItemLabel` e um `ItemPrice` → retorna os DTOs da AODP.

## Convenções do código

- Controller fino e Service com a lógica, injetados via DI (`AddScoped` em `Program.cs`). Os Services/Repositories atuais são registrados como **classes concretas, sem interface**. O design do Refino introduz interfaces (`IRefiningCalculatorService` etc.).
- Validação de entrada com Data Annotations nos DTOs e `[ApiController]` retornando 400 automaticamente.
- Namespaces seguem as pastas (`AlbionWebApp.Services`, `AlbionWebApp.DTOs`...). Os arquivos antigos usam namespace com bloco `{ }` e o `AppDbContext` usa namespace de arquivo. Siga o estilo do arquivo que estiver editando.
- Os enums espelham tabelas do banco: `AlbionCityEnum`/`ItemQualityEnum` têm **Ids fixos iguais ao valor numérico**, com seed em `AppDbContext.OnModelCreating`. `ItemPrice.CityId`/`QualityId` gravam `(int)enum` direto. Ao adicionar um valor a esses enums, atualize o `HasData` e gere uma migration. `City.Id`/`Quality.Id` ainda são colunas identity semeadas com Id explícito, então a sequência do Postgres não avança — latente até alguém inserir uma linha sem Id; `ValueGeneratedNever()` é o que descreve o modelo real.
- Datas persistidas em UTC (`DateTimeKind.Utc`). A AODP usa `0001-01-01T00:00:00` e preço `0` para "sem dado", que o repositório converte para `null`.
- Comentários e mensagens de erro em português.

## Pontos de atenção conhecidos (não corrigidos)

- `ItemLabelRepository.CreateItemLabelByAodpDto` cria um `ItemLabel` **novo a cada requisição** (não há upsert), com `Category`/`Subcategory = "teste"` e `CreatedAt` salvo como `string`.
- `ItemPrice.Tier` e `Enchantment` nunca são preenchidos (ficam 0), apesar de o comentário dizer que são extraídos do `item_id`.
- `SearchItemsService` não usa `AodpOptions.TimeoutSeconds` e só filtra pela `ItemBuyCity`. `ItemTier`, `ItemQuality`, `ItemCraftCity` e `ItemSellCity` do filtro são ignorados. `ItemTier` é `string` com `[Range(1, 8)]`.
- `ItemPrices` **não tem FK nem índice** para `Cities`/`Qualities`/`ItemLabels`. As navegações `City`/`Quality`/`ItemLabel` foram removidas de `ItemPrice.cs`; sem navegação nem `HasOne`, o EF deixa de enxergar o relacionamento, e foi a migration `SeedCitiesAndQualities` que emitiu os `DropForeignKey`/`DropIndex`. Devolver as navegações e gerar uma migration recria tudo.
- Qualquer resposta da AODP que contenha Fort Sterling quebra a desserialização de `AodpItemPriceResponseDTO.City` (`JsonException` → 500) — inferido do código, não reproduzido no endpoint. Atinge também o caso `ItemBuyCity` null, em que `locations` vai vazio. Mecanismo e convenção de nomes em `.claude/rules/aodp-integration.md`. O seed grava `City.Name = "FortSterling"` (via `nameof`), contradizendo o comentário em `City.cs` que diz ser o nome "como a AODP retorna".
- `Program.cs` chama `UseSwaggerUI` duas vezes.
- Há credenciais de dev em texto puro (`docker-compose.yml`, `appsettings.Development.json`). Não reutilize essas credenciais fora do ambiente local.

## Documentação de especificação (Kiro)

A fonte de verdade de requisitos e design está em `.kiro/` e **não deve ser duplicada aqui**. Leia os arquivos antes de trabalhar em funcionalidades relacionadas:

- `.kiro/steering/albion-domain-context.md`: endpoints da AODP, rate limits, conversão de ticks, fórmulas de RRR, Production Bonus e especialização de cidades.
- `.kiro/specs/refining-module/requirements.md`: 8 requisitos em formato EARS (RRR, Focus, preços, custo/lucro, cidades, API, histórico, validação).
- `.kiro/specs/refining-module/design.md`: componentes, interfaces, DTOs, entidade, 20 Correctness Properties (PBT) e estratégia de testes.

Resumo do que vale para qualquer código novo que envolva mercado ou refino está em `.claude/rules/` (carregado conforme os arquivos tocados).
