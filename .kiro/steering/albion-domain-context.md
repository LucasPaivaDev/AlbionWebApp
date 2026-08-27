---
inclusion: fileMatch
fileMatchPattern: "*efin*"
---

# Contexto de Dominio: Albion Online Data Project (AODP) e Mecanica de Refino

Este documento consolida conhecimento de dominio pesquisado sobre a AODP (API publica de dados de mercado do Albion Online) e sobre a mecanica de Refino do jogo. Serve como referencia tecnica para qualquer spec, design ou implementacao que envolva precos de mercado ou calculo de refino no projeto AlbionWebApp.

## 1. Albion Online Data Project (AODP)

### 1.1 O que e

A AODP e um projeto comunitario que coleta dados de mercado (ordens de compra/venda, historico de precos, preco do gold) via um client que monitora o trafego de rede do jogo, e distribui esses dados por uma API REST publica.

### 1.2 Hosts regionais

A API e servida por servidor/regiao. Escolha o host conforme o servidor do jogo:

| Regiao | Host |
|---|---|
| Americas (West) | https://west.albion-online-data.com |
| Asia (East) | https://east.albion-online-data.com |
| Europe | https://europe.albion-online-data.com |

### 1.3 Endpoints principais

Formato padrao: .json e o default (nao precisa especificar); .xml tambem e suportado trocando a extensao.

**Precos atuais (current prices):**

GET /api/v2/stats/prices/{item_ids}.json?locations=Caerleon,Bridgewatch&qualities=2

- {item_ids} aceita multiplos IDs separados por virgula (ex.: T4_BAG,T5_BAG)
- locations aceita multiplas cidades separadas por virgula
- qualities filtra por qualidade do item (1 a 5)
- Existe tambem uma versao em tabela HTML: /api/v2/stats/view/{item_ids}?...

**Historico de precos (somente sell orders):**

GET /api/v2/stats/history/{item_ids}.json?date=2-5-2020&end_date=2-12-2020&locations=Caerleon&qualities=2&time-scale=6

- time-scale: 1 = agregacao horaria, 6 = agregacao de 6 horas, 24 = agregacao diaria
- Datas suportadas: YYYY-MM-DD (preferido) ou MM-DD-YYYY
- Existe endpoint de charts equivalente em /api/v2/stats/charts/{item_ids}.json?...

**Preco do Gold:**

GET /api/v2/stats/gold.json?date=2-5-2020&end_date=2-12-2020   (por intervalo de datas)
GET /api/v2/stats/gold.json?count=2                             (os N precos mais recentes)

**Festividades (eventos confirmados):**

GET /api/v2/stats/festivities

- StartTime/EndTime retornados em C# ticks (ver secao 1.5 para conversao)
- Endpoint retorna 404 quando nao ha snapshot confirmado no momento

### 1.4 Identificacao de itens e localidades

- IDs de item (UniqueName, ex.: T4_BAG) vem dos arquivos de metadados do projeto ao-bin-dumps (items.txt/items.json)
- IDs/nomes de localizacao vem de world.txt/world.json
- Existe Swagger publicado para a API (referenciado no site oficial)

### 1.5 Conversao de timestamps (ticks -> epoch)

Timestamps de historico de mercado e de eventos (Bandit Event, Festivities) vem em ticks do .NET/C#, nao em epoch Unix. Formula de conversao:

epoch_seconds = (ticks - 621355968000000000) / 10000000

Exemplo: 638181504000000000 ticks -> 1682553600 epoch -> 2023-04-27 00:00:00 UTC.

Em C#, isso e equivalente a construir um DateTime a partir do valor de ticks (new DateTime(ticks, DateTimeKind.Utc)), ja que a propria formula deriva do epoch interno do tipo DateTime do .NET - nao e necessario reimplementar a formula manualmente, pode-se usar a API nativa.

### 1.6 Rate limits e boas praticas

- 180 requisicoes por minuto
- 300 requisicoes por 5 minutos
- Limite de 4096 caracteres por URL - combine multiplos item_ids e locations numa unica chamada para reduzir o numero de requisicoes
- Servicos que fazem polling continuo devem usar compressao Gzip (recomendacao oficial do projeto, para sustentabilidade da infraestrutura)

### 1.7 Fora do escopo inicial (avancado)

- Existe uma conexao NATS para consumo de dados em tempo real (streaming), com topicos como marketorders.deduped, markethistories.deduped, goldprices.deduped, festivities.deduped. Nao e necessario para o modulo de Refino no MVP - mencionado aqui apenas para referencia futura caso o projeto evolua para atualizacao em tempo real em vez de polling via REST.
- Existem dumps diarios do banco de dados completo (albion-online-data.com/database/ e variantes regionais) - util para analises historicas offline, fora do escopo do modulo de Refino.

## 2. Mecanica de Refino do Albion Online

### 2.1 Visao geral

Refinar converte um recurso bruto (ex.: Fiber) em um recurso refinado (ex.: Cloth) em uma estacao de refino, consumindo o recurso bruto e, exceto para T2, uma quantia em silver. Durante o refino, uma parte do recurso bruto consumido e devolvida ao jogador - essa fracao devolvida e a Resource Return Rate (RRR).

### 2.2 Formula da RRR

RRR = 1 - 1 / (1 + (Production_Bonus / 100))

Onde Production_Bonus e um percentual (nao a RRR em si) determinado pela soma de varios fatores (ver 2.3). A RRR resultante e o percentual efetivo de material devolvido.

**Exemplo de referencia (validado na wiki oficial):** refinar 100 unidades de Fiber em Simple Cloth em Lymhurst (bonus de producao local de 58%, especializada em Fiber) resulta em RRR de 36.7% - ou seja, ao consumir 100 Fiber, cerca de 36-37 unidades retornam ao jogador. Refinando repetidamente esse retorno até esgotar o material, o total de Simple Cloth produzido com as mesmas 100 unidades originais de Fiber seria de aproximadamente 158 unidades.

### 2.3 Componentes do Production Bonus

| Fator | Valor | Observacao |
|---|---|---|
| Baseline em Royal City | +18% | Aplicado em qualquer cidade Royal, independente de especializacao |
| Especializacao de cidade (refino) | +40% adicional | So na cidade especializada no recurso especifico (ver 2.4). Total nessa cidade: 58% |
| Uso de Focus | +59 pontos percentuais (flat) | Adicionado quando o jogador opta por usar Focus Points no refino |
| Bonus diario de atividades | +10% ou +20% | Dois itens por dia recebem esse bonus extra, visivel no menu de Atividades do jogo. Varia diariamente - nao e um valor fixo por recurso |
| Ilha pessoal/Royal Island | 0% (sem bonus) ou ate 40% (com investimento) | Fora do escopo do MVP do modulo de Refino (ver requirements.md) |
| Hideout (Black Zone) | ~15% base, ate 56% com especializacao + Power Cores | Fora do escopo do MVP do modulo de Refino |

**Tabela de RRR resultante para os cenarios relevantes ao MVP (Royal Cities):**

| Cenario | Production Bonus | RRR base | RRR com Focus |
|---|---|---|---|
| Royal City sem especializacao no recurso | 18% | 15.3% | 43.5% |
| Royal City especializada no recurso | 58% | 36.7% | 53.9% |

### 2.4 Mapeamento de especializacao de refino por cidade (Royal Continent)

Cada cidade Royal (exceto Caerleon e Brecilien) tem bonus de especializacao de +40% de refino para UM recurso especifico:

| Cidade | Regiao | Recurso especializado (refino) |
|---|---|---|
| Martlock | Highland | Hide (Couro) |
| Bridgewatch | Steppe | Stone (Pedra) |
| Lymhurst | Forest | Fiber (Fibra) |
| Fort Sterling | Mountain | Wood (Madeira) |
| Thetford | Swamp | Ore (Metal/Minerio) |
| Caerleon | Centro | Nenhum (sem bonus de refino) |
| Brecilien | Mists | Nenhum (sem bonus de refino) |

Essa tabela corresponde ao "mapeamento fixo predefinido de cidade para recurso especializado" referenciado no requirements.md do modulo de Refino.

Cada cidade tambem tem bonus de crafting (nao de refino) para armas/armaduras especificas - fora do escopo do modulo de Refino, mas relevante se o projeto expandir para um modulo de Crafting no futuro.

### 2.5 Focus Points

- Recurso do jogador, consumido para aplicar o bonus de +59 pontos percentuais de Production Bonus durante o refino/crafting
- Nao afeta a RRR diretamente por si so - o que afeta a RRR e o Production Bonus resultante de usar o Focus
- O custo em Focus necessario para refinar uma certa quantidade e reduzido pela maestria/especializacao do jogador (ver 2.6), nao pela cidade
- Detalhes de regeneracao (quanto acumula por dia, teto maximo) variam conforme fonte e nao sao fornecidos pela AODP (dados de personagem nao sao expostos pela API) - tratado como fora de escopo no MVP do modulo de Refino; o valor de Focus Points disponivel e informado manualmente pelo jogador a cada simulacao

### 2.6 Maestria/Especializacao (Destiny Board) e custo de Focus

- Maestria e especializacao nao alteram a RRR diretamente - quem afeta a RRR e o Production Bonus (cidade + Focus + bonus diario)
- O que a maestria/especializacao afeta e o custo em Focus Points necessario para refinar:
  - Cada ponto de especializacao da +30 de eficiencia de foco dentro da mesma maestria, e +250 de eficiencia especificamente para o recurso especializado
  - Maximizar as maestrias relevantes para T4-T8 acumula ate 40.000 pontos de eficiencia, reduzindo o custo de Focus para 6.25% do custo base (ou seja, cada 10.000 pontos de eficiencia reduz o custo de Focus a metade)
- Conclusao pratica para o calculo do modulo: Mastery_Level informado pelo jogador deve mapear para um fator de eficiencia que reduz o Focus_Cost - quanto maior a maestria, menor o Focus necessario para a mesma quantidade de recurso bruto (esse comportamento ja esta refletido no requirements.md, Requisito 2, Criterio 2)

### 2.7 Custos e impostos de mercado (para calculo de lucro)

- Refino de T2 nao custa silver; T3+ custa silver na estacao de refino (station fee)
- Vendas no mercado de uma cidade sofrem market tax (imposto de mercado), percentual deduzido do valor de venda
- Esses dois fatores (Station_Fee e Market_Tax) sao tratados como inputs opcionais do jogador no modulo de Refino, ja que variam por contexto (posto usado, premio ou nao, guild taxes, etc.) e nao sao expostos pela AODP

## 3. Como este contexto se aplica ao modulo de Refino

- A AODP fornece exclusivamente dados de mercado (precos). Ela nao expoe RRR, Focus, maestria ou bonus de cidade - esses sao calculos de dominio do jogo que o modulo de Refino precisa implementar internamente, usando as formulas e tabelas acima.
- O fluxo de uma simulacao de refino combina:
  1. Calculo deterministico (formulas de RRR, Focus Cost, Production Bonus) - nao depende de rede, pode ser testado isoladamente com testes de propriedade
  2. Consulta de mercado via AODP (preco do recurso bruto na cidade de origem, preco do recurso refinado na cidade de destino) - depende de rede, sujeita a timeout/falha/dados ausentes
- Separar esses dois aspectos em componentes distintos (Refining_Calculator puro vs. Market_Price_Provider que fala com a AODP) e o desenho ja adotado no requirements.md do modulo - mantenha essa separacao no design e na implementacao para permitir testes deterministicos do calculo sem dependencia de rede.

## Fontes consultadas

- [The Albion Online Data Project - API Info](https://www.albion-online-data.com/api)
- [The Albion Online Data Project - Developer Information](https://www.albion-online-data.com/developer)
- [Albion Online Wiki - Resource Return Rate](https://wiki.albiononline.com/wiki/Resource_Return_Rate)
- [Albion Online Wiki - Focus Points / Crafting Focus](https://wiki.albiononline.com/wiki/Focus_Points)
- [Albion Online Wiki - Local Production Bonus](https://wiki.albiononline.com/wiki/Local_Production_Bonus)
- [Albion Online Wiki - Template:City bonuses](https://wiki.albiononline.com/wiki/Template:City_bonuses)
