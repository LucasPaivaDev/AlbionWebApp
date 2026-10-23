# Requirements Document

## Introduction

O módulo de Refino (Refining Module) ajuda o jogador de Albion Online a decidir a melhor forma de refinar recursos, calculando a Resource Return Rate (RRR) efetiva com base na maestria do jogador, nos Focus Points disponíveis e no bônus de produção da cidade de refino, e cruzando esse cálculo com preços reais de mercado obtidos via Albion Online Data Project (AODP) para estimar o custo e o lucro da operação de refino em comparação com a venda do recurso bruto. O módulo expõe os resultados via endpoint de API REST e persiste um histórico de simulações no PostgreSQL através do Entity Framework Core, seguindo os padrões arquiteturais já estabelecidos no projeto AlbionWebApp (ex.: `SearchItemsController`, `SearchItemsService`).

Este módulo cobre o subconjunto de recursos primários de refino (Fiber/Cloth, Hide/Leather, Ore/Metal Bar, Stone/Stone Block, Wood/Planks) nos tiers T2 a T8.

> **Revisão de 2026-10-23.** A receita real de refino entrou no escopo do MVP: a quantidade de recurso bruto por craft varia por tier e, de T3 em diante, o refino consome também uma unidade de recurso refinado do tier anterior (dados confirmados no `ao-bin-dumps/items.xml` — ver `tasks.md`). Além disso, o modo de aquisição de cada material (coletado pelo jogador ou comprado no mercado) passou a ser parâmetro de entrada, porque ele muda tanto o custo quanto a comparação do Requisito 4. Os requisitos abaixo refletem essa revisão; o plano de execução está em `tasks.md`. Maestria e Focus Points são fornecidos manualmente pelo jogador a cada simulação, já que a AODP não expõe dados de personagem. A regeneração de Focus Points ao longo do tempo está fora do escopo deste módulo.

## Glossary

- **Refining_Module**: O conjunto de componentes (controller, service, DTOs, entidades) responsável por calcular e apresentar simulações de refino.
- **Refining_Calculator**: O componente responsável por calcular a Resource Return Rate (RRR) e os custos/lucros de uma simulação de refino, a partir dos parâmetros informados.
- **Market_Price_Provider**: O componente responsável por consultar preços de mercado na AODP (reaproveitando ou estendendo o padrão do `SearchItemsService`).
- **Simulation_Repository**: O componente responsável por persistir e recuperar o histórico de simulações de refino no PostgreSQL via Entity Framework Core.
- **RRR (Resource Return Rate)**: Percentual de matéria-prima devolvido ao jogador ao final do refino. Fórmula: RRR = 1 - 1/(1 + (Production_Bonus/100)).
- **Production_Bonus**: Percentual total de bônus de produção aplicado ao refino, resultante da soma do bônus base da cidade, do bônus de especialização da cidade (quando aplicável), do bônus de Focus (+59 pontos percentuais quando o Focus é usado) e do bônus diário de atividades (quando informado).
- **Focus_Points**: Quantidade de pontos de Foco que o jogador informa estarem disponíveis para uso na simulação de refino.
- **Focus_Cost**: Quantidade de Focus Points necessária para refinar a quantidade de recurso bruto informada, reduzida pela eficiência de maestria informada pelo jogador.
- **Mastery_Level**: Nível de maestria/especialização de refino do jogador para o recurso selecionado, informado manualmente na simulação.
- **Refined_Resource**: Recurso primário suportado pelo módulo (Fiber, Hide, Ore, Stone, Wood) em um tier de T2 a T8, e seu respectivo produto refinado (Cloth, Leather, Metal Bar, Stone Block, Planks).
- **Resource_Tier**: O tier do recurso, restrito ao intervalo T2 a T8.
- **Origin_City**: A cidade informada pelo jogador onde o recurso bruto é comprado.
- **Refining_City**: A cidade informada pelo jogador onde a operação de refino ocorre (define o Production_Bonus base e de especialização).
- **Destination_City**: A cidade informada pelo jogador onde o recurso refinado é vendido.
- **Station_Fee**: Taxa cobrada pelo posto de refino, informada opcionalmente pelo jogador como percentual ou valor fixo em prata.
- **Market_Tax**: Percentual de imposto de mercado aplicado à venda do recurso refinado ou bruto, informado opcionalmente pelo jogador.
- **Refining_Simulation**: O conjunto de parâmetros de entrada e resultados calculados de uma execução do Refining_Calculator, elegível para persistência.
- **Refining_Simulation_Record**: A representação persistida de um Refining_Simulation no PostgreSQL.
- **Mastery_Level**: (redefinição de faixa) Nível de maestria/especialização de refino do jogador para o recurso selecionado, informado manualmente na simulação como número inteiro entre 0 e 100.
- **Identificador de jogador**: Valor informado pelo jogador como dado simples de entrada da requisição para associar uma simulação e seu histórico a um jogador específico. Não representa um sistema de contas, login ou autenticação — é apenas um dado de entrada da simulação.
- **Menor preço de sell order ativo**: O menor valor entre as ordens de venda (sell orders) atualmente ativas para um item em uma cidade, conforme retornado pela AODP, utilizado como preço de venda de referência nos cálculos do Refining_Calculator.
- **Mapeamento fixo predefinido de cidade para recurso especializado**: Tabela fixa, definida no Refining_Module, que associa cada cidade Royal ao(s) Refined_Resource(s) em que ela é especializada, utilizada para determinar o Production_Bonus base (ver Requisito 1).
- **Refining_Recipe**: Tabela fixa por Resource_Tier, definida no Refining_Module a partir do `ao-bin-dumps`, com a quantidade de recurso bruto consumida por craft, a quantidade de Previous_Refined_Resource consumida por craft e o custo base de Focus por craft.
- **Previous_Refined_Resource**: O produto refinado do tier imediatamente anterior ao Resource_Tier informado, consumido pelo refino a partir de T3 (uma unidade por craft). Não existe em T2.
- **Previous_Refined_City**: A cidade informada pelo jogador onde o Previous_Refined_Resource é comprado, quando o modo de aquisição desse material é "comprado".
- **Acquisition_Mode**: Indicação, por material (recurso bruto e Previous_Refined_Resource, independentemente), de que o material já pertence ao jogador ("coletado") ou será comprado no mercado ("comprado"). Determina se o material entra no custo da operação e qual baseline de comparação é usado no Requisito 4.
- **Craft_Count**: Número de operações de refino possíveis com a quantidade bruta informada, igual à divisão inteira da quantidade bruta pela quantidade de bruto por craft da Refining_Recipe. É também a quantidade de Refined_Resource produzida.

## Requirements

### Requisito 1: Cálculo da Resource Return Rate (RRR)

**User Story:** Como jogador de Albion Online, eu quero informar minha maestria, meus Focus Points disponíveis e a cidade de refino, para que eu saiba qual será a taxa de retorno de material (RRR) efetiva do meu refino.

#### Acceptance Criteria

1. THE Refining_Calculator SHALL aceitar como entrada o Refined_Resource, o Resource_Tier, o Mastery_Level (número inteiro de 0 a 100), os Focus_Points disponíveis (número inteiro maior ou igual a 0), um indicador de uso de Focus, a Refining_City, a quantidade bruta de Refined_Resource a ser refinada (número inteiro de 1 a 999999), e o Acquisition_Mode do recurso bruto e do Previous_Refined_Resource.
1.1. THE Refining_Calculator SHALL calcular o Focus_Cost da operação a partir da Refining_Recipe e do Mastery_Level (Requisito 2), não recebendo esse valor como entrada.
1.2. THE Refining_Calculator SHALL derivar o Craft_Count da quantidade bruta informada e da Refining_Recipe, e SHALL tratar Craft_Count igual a zero (quantidade bruta menor que a quantidade por craft) como simulação válida sem produção, não como erro de validação.
2. WHEN a Refining_City corresponde a uma cidade Royal que, conforme mapeamento fixo predefinido de cidade para recurso especializado, é especializada no Refined_Resource informado, THE Refining_Calculator SHALL aplicar um Production_Bonus base de 58 pontos percentuais (18% baseline mais 40% de especialização).
3. WHEN a Refining_City corresponde a uma cidade Royal que, conforme o mesmo mapeamento fixo predefinido, não é especializada no Refined_Resource informado, THE Refining_Calculator SHALL aplicar um Production_Bonus base de 18 pontos percentuais.
4. WHEN o indicador de uso de Focus é verdadeiro E os Focus_Points disponíveis são maiores ou iguais ao Focus_Cost calculado, THE Refining_Calculator SHALL somar 59 pontos percentuais ao Production_Bonus e subtrair o Focus_Cost dos Focus_Points disponíveis.
5. IF o indicador de uso de Focus é verdadeiro E os Focus_Points disponíveis são menores que o Focus_Cost calculado, THEN THE Refining_Calculator SHALL calcular a RRR sem somar os 59 pontos percentuais de bônus de Focus e SHALL indicar ao jogador que os Focus_Points disponíveis foram insuficientes para aplicar o bônus.
6. THE Refining_Calculator SHALL calcular a RRR aplicando a fórmula RRR = 1 - 1/(1 + (Production_Bonus/100)) sobre o Production_Bonus total resultante.
7. THE Refining_Calculator SHALL calcular a quantidade devolvida de cada material consumido — recurso bruto e Previous_Refined_Resource — multiplicando a quantidade efetivamente consumida daquele material (Craft_Count vezes a quantidade por craft da Refining_Recipe) pela RRR calculada.
8. IF o Resource_Tier informado está fora do intervalo T2 a T8, THEN THE Refining_Calculator SHALL rejeitar a simulação com uma mensagem de erro indicando o intervalo de tiers suportado.
9. IF o Refined_Resource informado não pertence ao conjunto suportado (Fiber, Hide, Ore, Stone, Wood), THEN THE Refining_Calculator SHALL rejeitar a simulação com uma mensagem de erro indicando os recursos suportados.
10. IF a quantidade bruta de Refined_Resource informada é menor ou igual a 0, ou o Mastery_Level informado está fora do intervalo de 0 a 100, THEN THE Refining_Calculator SHALL rejeitar a simulação com uma mensagem de erro indicando o campo inválido e o intervalo aceito.

### Requisito 2: Cálculo do custo em Focus Points

**User Story:** Como jogador de Albion Online, eu quero saber quantos Focus Points serão consumidos ao usar Focus no refino, considerando minha maestria, para que eu possa decidir se tenho Focus suficiente para a operação.

#### Acceptance Criteria

1. IF a quantidade bruta de Refined_Resource informada é menor ou igual a 0, OR o Mastery_Level informado está fora do intervalo de 0 a 100, THEN THE Refining_Calculator SHALL rejeitar o cálculo de Focus_Cost com uma mensagem de erro indicando o campo inválido e o intervalo aceito.
2. WHEN o indicador de uso de Focus é verdadeiro, THE Refining_Calculator SHALL calcular o Focus_Cost da simulação com base na quantidade bruta de Refined_Resource informada, reduzido pela eficiência de foco resultante do Mastery_Level informado, de forma que um Mastery_Level maior resulte em um Focus_Cost igual ou menor.
3. IF os Focus_Points disponíveis não forem informados quando o indicador de uso de Focus é verdadeiro, THEN THE Refining_Calculator SHALL tratar os Focus_Points disponíveis como 0 para efeito do cálculo do Critério 4.
4. IF o Focus_Cost calculado é maior que os Focus_Points disponíveis informados, THEN THE Refining_Calculator SHALL retornar a simulação com o indicador de uso de Focus desativado e recalcular a RRR sem o bônus de Focus, informando ao jogador que o Focus disponível foi insuficiente.
5. WHEN o Focus_Cost calculado é igual aos Focus_Points disponíveis informados, THE Refining_Calculator SHALL considerar o Focus disponível como suficiente e aplicar o bônus de Focus normalmente.
6. WHEN o indicador de uso de Focus é falso, THE Refining_Calculator SHALL calcular a simulação sem consumir Focus_Points e sem aplicar o bônus percentual de Focus ao Production_Bonus.

### Requisito 3: Consulta de preços de mercado via AODP

**User Story:** Como jogador de Albion Online, eu quero que o módulo consulte os preços atuais do recurso bruto e do recurso refinado nas cidades relevantes, para que o cálculo de custo e lucro reflita o mercado real.

#### Acceptance Criteria

1. WHEN uma simulação de refino é solicitada, THE Market_Price_Provider SHALL consultar o endpoint de preços atuais da AODP para obter o menor preço de sell order ativo do recurso bruto na Origin_City, o menor preço de sell order ativo do Refined_Resource na Destination_City e, quando o Resource_Tier é maior que 2, o menor preço de sell order ativo do Previous_Refined_Resource na Previous_Refined_City.
2. THE Market_Price_Provider SHALL combinar a consulta de todos os itens necessários à simulação em uma única chamada à AODP, com todos os item_id e todas as cidades envolvidas na mesma requisição.
3. IF a chamada à AODP não retornar resposta dentro de 10 segundos, OR retornar um código de status HTTP não bem-sucedido, THEN THE Market_Price_Provider SHALL propagar um erro identificável de comunicação para o Refining_Calculator sem interromper o processo de forma silenciosa.
4. IF o preço retornado pela AODP para qualquer um dos itens consultados estiver ausente, for igual a zero, OR tiver um timestamp de atualização mais antigo que 24 horas, THEN THE Refining_Calculator SHALL tratar o preço correspondente como indisponível.
5. IF qualquer preço necessário ao cálculo for tratado como indisponível pelo Critério 4, mesmo que apenas um deles esteja ausente, THEN THE Refining_Calculator SHALL retornar a simulação indicando que o custo ou lucro não pôde ser calculado por ausência de dados de mercado.
6. WHERE o Acquisition_Mode de um material é "coletado", THE Refining_Calculator SHALL continuar exigindo o preço desse material para avaliar o valor do material devolvido e da sobra (Requisito 4), tratando-o pelas mesmas regras de disponibilidade dos Critérios 4 e 5.

### Requisito 4: Cálculo de custo e lucro do refino

**User Story:** Como jogador de Albion Online, eu quero ver o custo total de refinar, o valor de venda do recurso refinado e o lucro estimado, para que eu possa decidir se vale mais refinar ou vender o recurso bruto.

#### Acceptance Criteria

1. WHERE o Acquisition_Mode do recurso bruto é "comprado", THE Refining_Calculator SHALL calcular o custo de aquisição do recurso bruto multiplicando a quantidade líquida necessária (quantidade consumida menos a quantidade devolvida pela RRR) pelo preço de venda do recurso bruto na Origin_City; WHERE o Acquisition_Mode do recurso bruto é "coletado", esse custo é zero.
1.1. WHERE o Resource_Tier é maior que 2 E o Acquisition_Mode do Previous_Refined_Resource é "comprado", THE Refining_Calculator SHALL calcular o custo de aquisição do Previous_Refined_Resource multiplicando a quantidade líquida necessária (quantidade consumida menos a quantidade devolvida pela RRR) pelo preço de venda do Previous_Refined_Resource na Previous_Refined_City; WHERE o Acquisition_Mode é "coletado", esse custo é zero.
2. WHERE um Station_Fee é informado como valor fixo, THE Refining_Calculator SHALL somar o Station_Fee integralmente ao custo total de refino.
3. WHERE um Station_Fee é informado como percentual, THE Refining_Calculator SHALL somar ao custo total de refino o valor resultante da aplicação do percentual sobre a soma dos custos de aquisição calculados nos Critérios 1 e 1.1.
4. THE Refining_Calculator SHALL calcular a receita bruta de venda do Refined_Resource multiplicando a quantidade de Refined_Resource produzida (Craft_Count) pelo preço de venda do Refined_Resource na Destination_City.
5. WHERE um Market_Tax é informado, THE Refining_Calculator SHALL deduzir o Market_Tax da receita bruta de venda do Refined_Resource para obter a receita líquida de refino.
5.1. WHERE o Acquisition_Mode de um material é "coletado", THE Refining_Calculator SHALL somar à receita líquida de refino o valor de mercado, líquido de Market_Tax, da quantidade daquele material devolvida pela RRR e, no caso do recurso bruto, também da sobra que não completou um craft.
6. THE Refining_Calculator SHALL calcular o lucro estimado de refinar subtraindo o custo total de refino (Critérios 1 a 3) da receita líquida de refino (Critérios 5 e 5.1).
7. WHERE o Acquisition_Mode do recurso bruto é "coletado", THE Refining_Calculator SHALL calcular a receita bruta de venda do recurso bruto sem refinar multiplicando a quantidade de recurso bruto informada pelo preço de venda do recurso bruto na Origin_City; WHERE o Acquisition_Mode do recurso bruto é "comprado", THE Refining_Calculator SHALL considerar o lucro estimado de vender o recurso bruto sem refinar como zero, já que comprar e revender o mesmo recurso na mesma cidade não gera lucro.
8. WHERE um Market_Tax é informado E o Acquisition_Mode do recurso bruto é "coletado", THE Refining_Calculator SHALL deduzir o Market_Tax da receita bruta de venda do recurso bruto para obter o lucro estimado de vender o recurso bruto sem refinar.
9. THE Refining_Calculator SHALL indicar na simulação qual das duas opções (refinar ou vender bruto) resulta em maior lucro estimado, ou indicar que os lucros estimados são iguais quando a diferença entre eles for zero.

### Requisito 5: Configuração de cidades independentes por etapa

**User Story:** Como jogador de Albion Online, eu quero informar cidades diferentes para comprar o recurso bruto, refinar e vender o recurso refinado, para que eu possa simular rotas de comércio realistas.

#### Acceptance Criteria

1. THE Refining_Calculator SHALL aceitar Origin_City, Previous_Refined_City, Refining_City e Destination_City como quatro parâmetros de entrada distintos, cada um podendo ser igual ou diferente dos demais, sem restrição de correspondência entre eles. Os critérios seguintes, que falam em "as três cidades", aplicam-se igualmente à Previous_Refined_City.
2. IF Origin_City, Refining_City e Destination_City não são informadas explicitamente, THEN THE Refining_Calculator SHALL assumir a mesma cidade informada pelo jogador para as três etapas.
3. WHEN apenas uma ou duas das três cidades (Origin_City, Refining_City, Destination_City) são informadas explicitamente, THE Refining_Calculator SHALL aplicar a cidade informada às etapas correspondentes e utilizar essa mesma cidade informada como valor padrão para cada etapa cuja cidade não foi informada.
4. IF a Origin_City, a Refining_City ou a Destination_City informada não corresponde a uma cidade Royal válida do Albion Online, THEN THE Refining_Calculator SHALL rejeitar a simulação com uma mensagem de erro indicando o campo de cidade inválido e as cidades suportadas.
5. THE Refining_Calculator SHALL aplicar o Production_Bonus com base exclusivamente na Refining_City informada, sem influência da Origin_City ou da Destination_City sobre esse cálculo.

### Requisito 6: Exposição via API REST

**User Story:** Como desenvolvedor cliente do AlbionWebApp, eu quero um endpoint de API para submeter os parâmetros de uma simulação de refino e receber o resultado calculado, para que eu possa integrar o módulo de Refino a uma interface de usuário.

#### Acceptance Criteria

1. THE Refining_Module SHALL expor um endpoint HTTP que aceite requisições HTTP POST com um corpo em formato JSON contendo os parâmetros de entrada definidos no Requisito 1, Requisito 2 e Requisito 5, e retorne o resultado da simulação em formato JSON.
2. IF os parâmetros de entrada da requisição não passarem nas validações de dados definidas para a simulação, THEN THE Refining_Module SHALL retornar um código de status HTTP 400 com uma lista de erros identificando o nome do campo inválido e o motivo específico da rejeição para cada campo.
3. WHEN a simulação é concluída com sucesso, THE Refining_Module SHALL retornar um código de status HTTP 200 contendo o Production_Bonus, a RRR, o Focus_Cost, o indicador de uso efetivo de Focus, o Craft_Count, as quantidades consumidas e devolvidas de cada material, o custo total, a receita líquida, o lucro estimado de refinar, o lucro estimado de vender bruto e a indicação de qual opção é mais lucrativa.
4. IF a simulação não puder ser concluída por falha de comunicação com a AODP (Requisito 3, Critério 3) OR por ausência de dados de mercado (Requisito 3, Critério 5), THEN THE Refining_Module SHALL retornar um código de status HTTP 200 contendo os campos calculáveis sem dados de mercado (Production_Bonus, RRR, Focus_Cost) e um indicador explícito de que o custo, a receita e o lucro não puderam ser calculados, junto com o motivo.

### Requisito 7: Persistência do histórico de simulações

**User Story:** Como jogador de Albion Online, eu quero que minhas simulações de refino sejam salvas, para que eu possa consultar decisões anteriores.

#### Acceptance Criteria

1. WHEN uma simulação de refino é concluída com sucesso, THE Simulation_Repository SHALL persistir um Refining_Simulation_Record no PostgreSQL associado ao identificador de jogador informado na requisição, contendo os parâmetros de entrada e os resultados calculados da simulação.
2. THE Simulation_Repository SHALL registrar a data e hora de criação de cada Refining_Simulation_Record em UTC.
3. IF a simulação falhar antes de produzir um resultado calculado, THEN THE Simulation_Repository SHALL não persistir um Refining_Simulation_Record para essa tentativa.
4. THE Refining_Module SHALL expor um endpoint HTTP que retorne, para um identificador de jogador informado, até 50 Refining_Simulation_Record por página, ordenados pela data de criação em ordem decrescente, retornando uma lista vazia quando não houver registros para o identificador de jogador informado.

### Requisito 8: Validação dos parâmetros de entrada

**User Story:** Como jogador de Albion Online, eu quero receber mensagens claras quando informar dados inválidos, para que eu possa corrigir a simulação rapidamente.

#### Acceptance Criteria

1. IF a quantidade de recurso bruto informada não é um valor numérico ou não é um valor positivo, THEN THE Refining_Module SHALL rejeitar a simulação com uma mensagem de erro indicando que a quantidade deve ser um número positivo.
2. IF o Mastery_Level informado não é um valor inteiro entre 0 e 100, THEN THE Refining_Module SHALL rejeitar a simulação com uma mensagem de erro indicando que o Mastery_Level deve estar entre 0 e 100.
3. IF os Focus_Points informados são negativos, THEN THE Refining_Module SHALL rejeitar a simulação com uma mensagem de erro indicando que o valor deve ser zero ou positivo.
4. IF o Station_Fee informado é negativo, THEN THE Refining_Module SHALL rejeitar a simulação com uma mensagem de erro indicando que o valor deve ser zero ou positivo.
5. IF o Market_Tax informado é negativo ou maior que 100 por cento, THEN THE Refining_Module SHALL rejeitar a simulação com uma mensagem de erro indicando que o valor deve estar entre 0 e 100 por cento.
6. IF o Station_Fee informado como percentual é maior que 100 por cento, THEN THE Refining_Module SHALL rejeitar a simulação com uma mensagem de erro indicando que o valor percentual deve estar entre 0 e 100 por cento.
</content>
