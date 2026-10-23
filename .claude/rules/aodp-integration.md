---
paths:
  - "AlbionWebApp/Services/**"
  - "AlbionWebApp/DTOs/Aodp*"
  - "AlbionWebApp/Options/**"
  - "**/*Market*"
  - "**/*Price*"
---

# Integração com a AODP

Referência completa: `.claude/rules/albion-domain-context.md` (seção 1).

- Host regional vem de `AodpOptions.Host` (`appsettings.json` → `Aodp:Host`, padrão `https://west.albion-online-data.com`). Nunca fixe o host no código.
- Preços atuais: `GET /api/v2/stats/prices/{ids}.json?locations=A,B&qualities=N`. Vários `item_id` e várias cidades cabem em **uma única chamada**, então agrupe sempre que puder.
- Rate limit: 180 req/min e 300 req/5min, com URL de no máximo 4096 caracteres.
- Quando a AODP aplica throttle, o corpo da resposta vem em **texto puro**, não JSON (`Throttled, be nice to the server, use compression (gzip, deflate, etc)! Throttle resets at <data>`). `JsonSerializer.Deserialize` quebra nesse corpo, então cheque o conteúdo antes de desserializar. O status code dessa resposta ainda não foi verificado.
- Nomes de cidade **não batem entre ida e volta**: na resposta a AODP usa o nome de exibição com espaço (`"city":"Fort Sterling"`), enquanto `locations=FortSterling` sem espaço foi aceito em teste. `AlbionCityEnum` + `JsonStringEnumConverter` só cobre a forma sem espaço, então a desserialização precisa aceitar o nome com espaço.
- "Sem dado" vem como preço `0` e data `0001-01-01T00:00:00`. Trate esses casos como ausência, nunca como preço real.
- Timestamps em ticks .NET (history, festivities) viram data com `new DateTime(ticks, DateTimeKind.Utc)`.
- Timeout: use um `CancellationTokenSource(TimeSpan.FromSeconds(AodpOptions.TimeoutSeconds))` ligado ao token recebido. **Não** altere `HttpClient.Timeout` global.
- Falhas de rede e status diferente de 2xx em fluxos de negócio esperados (ex.: Refino) viram **resultado tipado** (`CommunicationFailed`, `FailureReason`), não exceção propagada ao Controller.
- O `SearchItemsService` atual está acoplado a uma única cidade e não tem timeout. Serviços novos seguem o mesmo padrão (`IHttpClientFactory` + `AodpOptions`), mas **não herdam** dele.
