# Feature Specification: Padronização de nomenclatura das filas da tela Home

**Feature Branch**: `001-home-naming-consistency`

**Created**: 2026-11-05

**Status**: Draft

**Input**: User description: "Na tela home do angular temos os tabs e tables corretamente, gostaria de refatorar o nome de funcoes, interfaces, classes, endpoints, corrigir para deixar o ingles melhor tanto no angular quanto na api .NET e deixar tudo igual nos dois"

## Contexto

A tela Home mostra seis filas de ordens de serviço (OS), uma por aba: **Orçamentos pendentes**,
**Aguardando resposta**, **Aprovados**, **Reprovados**, **Aguardando peça** e **Prontos**.
A tela funciona corretamente. O problema é a nomenclatura técnica por trás dela: a mesma fila
recebe nomes diferentes em cada camada e alguns nomes estão em inglês incorreto. Exemplos
encontrados hoje:

| Aba (UI) | Rota atual | Handler / repositório | Frontend |
|----------|------------|-----------------------|----------|
| Orçamentos pendentes | `get-pending-estimates-service-orders` | `GetPendingEstimateServiceOrders` / `GetPendingEstimatesAsync` | `pending-estimate-service-ordens.interface.ts` (erro de digitação), `pendingServiceOrdersPagination` |
| Aguardando resposta | `get-waiting-response-service-orders` | `GetWaitingResponseServiceOrders` / `GetWaitingResponseAsync` | campo `inspectionIn` |
| Aprovados | `get-pending-purchase-service-orders` | `GetApprovedServiceOrders` / `GetApprovedAsync` | campo `approvedIn` |
| Reprovados | `get-rejected-estimate-service-orders` | `GetRefusedServiceOrders` / `GetRefusedAsync` | `RefusedServiceOrder`, `isLoadingReprovedServiceOrders` |
| Aguardando peça | `get-waiting-parts-service-orders` | `GetWaitingPartServiceOrders` / `GetWaitingPartAsync` | campo `purchasedIn` |
| Prontos | `get-waiting-pickup-service-orders` | `GetReadyServiceOrders` / `GetReadyAsync` | campos `situation`, `response` |

Há ainda nomes pouco claros no componente, como `myTabSelectedTabChange`.

O modal de orçamento aberto pela Home também mistura os termos: o frontend usa `Quote`
(`AddServiceOrderQuoteModalComponent`, `AddServiceOrderQuote`, `sendQuote`, pasta `modals/quote`),
enquanto a API usa `Estimate` (`add-service-order-estimate`, `AddServiceOrderEstimateCommand`,
`EstimateMessage`).

Esta é uma refatoração técnica. Os "usuários" desta feature são as pessoas que desenvolvem e
mantêm o sistema. Para quem usa a tela, nada pode mudar.

## Clarifications

### Session 2026-11-05

- Q: O modal de orçamento aberto pela Home entra no escopo? → A: Sim. Padronizar em `Estimate`
  no frontend e na API.
- Q: Prefixo das filas de espera: `Awaiting` ou `Waiting`? → A: `Awaiting` em todos os
  identificadores do escopo (`AwaitingResponse`, `AwaitingPart`). `Waiting` não pode mais ser
  usado.
- Q: Renomear o campo `Guarantee` do orçamento para `Warranty`? → A: Sim, mas só no contrato
  (command da API + modelo e formulário do frontend). A entidade, a coluna no banco, o
  WhatsApp e o PDF continuam com `Guarantee`, e o handler faz o mapeamento. Sem migration.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Vocabulário único por fila, de ponta a ponta (Priority: P1)

Ao trabalhar em uma fila da Home, a pessoa desenvolvedora encontra **o mesmo termo** para essa
fila na aba, na rota da API, no handler, no repositório, no tipo de resposta e no serviço,
modelo e estado do frontend. Assim consegue buscar um único nome e encontrar todo o código
daquela fila.

**Why this priority**: é o objetivo central do pedido ("deixar tudo igual nos dois").
Sozinha, já elimina a principal fonte de confusão.

**Independent Test**: para cada fila, buscar o termo canônico (ver Glossário) no repositório e
confirmar que ele aparece em todas as camadas, e que nenhum dos termos antigos
(`Refused`, `Reproved`, `PendingPurchase`, `WaitingPickup`, `WaitingParts`, `ordens` etc.)
continua sendo usado para essa fila.

**Acceptance Scenarios**:

1. **Given** a fila "Reprovados", **When** se busca o termo canônico dela, **Then** a rota,
   o método do handler, o método do repositório, o tipo de resposta, o modelo do frontend, o
   método do serviço do frontend e o estado do componente usam todos esse mesmo termo.
2. **Given** a fila "Aprovados", **When** se compara o nome da rota com o nome do método que
   ela chama, **Then** os dois descrevem a mesma fila (sem rota "pending-purchase" chamando
   método "Approved").
3. **Given** qualquer um dos seis tipos de resposta, **When** se compara o contrato da API
   com o modelo do frontend, **Then** os nomes dos campos são idênticos (respeitando apenas a
   convenção de maiúsculas e minúsculas de cada linguagem).

---

### User Story 2 - Inglês correto e convencional nos identificadores (Priority: P2)

Os identificadores da Home (funções, classes, interfaces, records, campos, arquivos e rotas)
usam inglês correto e seguem convenções consistentes. Datas têm nomes com o mesmo padrão
(ex.: `ApprovedAt` em vez de `ApprovedIn`), não há erros de digitação em nomes de arquivos, e
os handlers de evento têm nomes descritivos.

**Why this priority**: aumenta a legibilidade, mas depende do vocabulário da US1 já estar
definido.

**Independent Test**: revisar a lista de identificadores da Home contra as regras de nomenclatura
(FR-004 a FR-007) e confirmar que nenhuma regra é violada.

**Acceptance Scenarios**:

1. **Given** os campos de data dos tipos de resposta, **When** são revisados, **Then** todos
   seguem o mesmo padrão `<Evento>At` (ex.: `EnteredAt`, `ApprovedAt`, `RejectedAt`).
2. **Given** os arquivos de modelo do frontend, **When** são listados, **Then** nenhum tem
   erro de digitação e todos seguem o mesmo padrão de nome.
3. **Given** o modal de orçamento, **When** se lê o componente, o serviço, o modelo, a pasta e
   o método de envio no frontend, **Then** todos usam `Estimate`, e nenhum identificador usa
   `Quote`.
4. **Given** o handler de troca de aba e os handlers de paginação, **When** são lidos,
   **Then** os nomes descrevem a ação (ex.: `onTabChange`) sem prefixos como `my`.

---

### User Story 3 - Comportamento da tela inalterado (Priority: P1)

Quem usa o sistema abre a Home e encontra as mesmas abas, colunas, dados, paginação, botão
"ORÇAMENTO" e mensagens de erro de antes da refatoração.

**Why this priority**: a refatoração só tem valor se não quebrar nada. Tem prioridade
equivalente à US1 e é verificada junto com ela.

**Independent Test**: com a mesma base de dados, abrir cada aba antes e depois da mudança,
navegar entre páginas e abrir o modal de orçamento, e comparar o resultado.

**Acceptance Scenarios**:

1. **Given** a mesma base de dados, **When** cada uma das seis abas é aberta, **Then** são
   exibidas as mesmas OS, nas mesmas colunas, com os mesmos valores e a mesma ordenação de
   antes.
2. **Given** uma fila com mais de 25 OS, **When** se troca de página ou de tamanho de página,
   **Then** a paginação se comporta como antes.
3. **Given** uma OS em "Orçamentos pendentes", **When** se envia o orçamento pelo modal,
   **Then** a lista é recarregada e a OS sai da fila, como antes.
4. **Given** uma falha da API, **When** a aba é carregada, **Then** aparece a mesma mensagem
   de erro em português.

---

### Edge Cases

- **Frontend e API publicados em momentos diferentes**: como as rotas e os campos JSON mudam,
  uma versão nova do frontend com uma API antiga (ou o contrário) quebra a Home. Os dois
  precisam ser publicados juntos (ver Assumptions).
- **Outros consumidores das rotas antigas**: hoje só a Home consome essas seis rotas. Se
  aparecer outro consumidor (script, integração, coleção do Postman), ele precisa ser atualizado
  no mesmo PR.
- **Identificadores compartilhados fora da Home**: enums de domínio (`ERepairStatus.Disapproved`,
  `EServiceOrderStatus.OrderPart` etc.) e outras rotas de OS também têm nomes questionáveis,
  mas ficam fora do escopo (ver Assumptions) para não afetar dados persistidos nem outras telas.
- **Cliente enviando `guarantee`**: depois da mudança, o command só aceita `warranty`. Um corpo
  com o nome antigo chega com a garantia vazia, então a validação existente precisa rejeitar
  ou tratar isso da mesma forma que um campo vazio. Por isso frontend e API são publicados
  juntos.
- **Campos com valor nulo**: os campos de data e texto que hoje aceitam nulo continuam aceitando
  nulo depois da renomeação.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Cada uma das seis filas da Home MUST ter um único termo canônico em inglês,
  definido no Glossário abaixo.
- **FR-002**: Para cada fila, a rota HTTP, o método do handler da Application, o método do
  repositório, o tipo de resposta, o modelo e o arquivo do frontend, o método do serviço do
  frontend e o estado do componente (lista, carregamento, paginação e handler de paginação) MUST
  usar o termo canônico daquela fila.
- **FR-003**: Os nomes dos campos de cada tipo de resposta MUST ser idênticos entre a API e o
  frontend, variando só na caixa (PascalCase no .NET, camelCase no TypeScript).
- **FR-004**: Campos que representam o momento de um evento MUST seguir o padrão `<Evento>At`
  (ex.: `EnteredAt`, `EvaluatedAt`, `ApprovedAt`, `RejectedAt`, `PartPurchasedAt`, `RepairedAt`).
- **FR-004a**: Estados de espera MUST usar o prefixo `Awaiting` (nunca `Waiting`) em todos os
  identificadores do escopo: rotas, métodos, tipos, arquivos e estado do frontend.
- **FR-005**: Campos com nomes ambíguos MUST ser renomeados para o conceito de domínio que
  representam (ex.: `Situation` → `RepairResult`, `Response` → `RepairStatus`), alinhados aos
  enums que já existem.
- **FR-006**: As seis rotas MUST seguir um padrão único de nome, consistente com o estilo das
  demais rotas da API (`get-<termo-canônico>-service-orders/{pageNumber}/{pageSize}`), com o
  termo no singular e a mesma ordem de palavras em todas.
- **FR-007**: Os identificadores do componente Home MUST ser descritivos e sem
  inconsistências (ex.: `myTabSelectedTabChange` → `onTabChange`, `isLoadingReproved…` alinhado
  ao termo canônico), e os arquivos de modelo MUST estar sem erros de digitação.
- **FR-007a**: O modal de orçamento aberto pela Home MUST usar o termo `Estimate` em todo o
  frontend (pasta, arquivos, componente, serviço, modelo, métodos como `sendQuote`). O nome do
  modelo do frontend MUST ser igual ao do command da API (`AddServiceOrderEstimateCommand` →
  `AddServiceOrderEstimate`), com os mesmos campos. O campo `Guarantee` do command e do modelo
  do frontend MUST ser renomeado para `Warranty` (incluindo o form control e métodos como
  `setGuarantee` → `setWarranty`). O handler MUST mapear `Warranty` para a propriedade
  `Guarantee` da entidade, que não muda. Do lado da API, a rota
  `add-service-order-estimate` e o `AddServiceOrderEstimateCommand` já usam o termo canônico e
  ficam como estão.
- **FR-008**: As rotas antigas MUST ser removidas, sem manter aliases, porque o único consumidor é
  a Home, que é atualizada na mesma entrega.
- **FR-009**: O comportamento visível na Home MUST continuar idêntico: rótulos das abas, títulos
  das colunas, dados exibidos, ordenação, paginação, modal de orçamento e mensagens de erro em
  português.
- **FR-010**: Os testes existentes em `TVSApp.Tests` que fazem referência aos identificadores
  renomeados MUST ser atualizados e continuar passando, e o build do frontend MUST passar sem erros.
- **FR-011**: A refatoração MUST NOT alterar o schema do banco de dados nem exigir migration.

### Key Entities *(include if feature involves data)*

**Glossário canônico das filas** (termo canônico → onde aparece):

| Aba (UI, pt-BR) | Termo canônico | Tipo de resposta | Rota |
|-----------------|----------------|------------------|------|
| Orçamentos pendentes | `PendingEstimate` | `PendingEstimateServiceOrder` | `get-pending-estimate-service-orders` |
| Aguardando resposta | `AwaitingResponse` | `AwaitingResponseServiceOrder` | `get-awaiting-response-service-orders` |
| Aprovados | `Approved` | `ApprovedServiceOrder` | `get-approved-service-orders` |
| Reprovados | `Rejected` | `RejectedServiceOrder` | `get-rejected-service-orders` |
| Aguardando peça | `AwaitingPart` | `AwaitingPartServiceOrder` | `get-awaiting-part-service-orders` |
| Prontos | `ReadyForPickup` | `ReadyForPickupServiceOrder` | `get-ready-for-pickup-service-orders` |

**Orçamento**: termo canônico `Estimate` (substitui `Quote`). Modal:
`AddServiceOrderEstimateModalComponent` / `AddServiceOrderEstimateModalService`, modelo
`AddServiceOrderEstimate`, pasta `modals/estimate`. Rota inalterada: `add-service-order-estimate`. Campo do contrato `Guarantee` → `Warranty`
(apenas no command e no modelo do frontend; a entidade e o banco continuam com `Guarantee`).

**Campos dos tipos de resposta** (nome atual → nome canônico):

- `EntryDate` → `EnteredAt`
- `InspectionIn` → `EvaluatedAt`
- `ApprovedIn` → `ApprovedAt`
- `RefusedIn` → `RejectedAt`
- `PurchasedIn` → `PartPurchasedAt`
- `RepairDate` → `RepairedAt`
- `Situation` → `RepairResult`
- `Response` → `RepairStatus`
- `Number`, `Customer`, `Company`, `Model`, `Defect`, `Solution`, `Amount` → mantidos

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Para 6 de 6 filas, buscar o termo canônico encontra a fila em todas as camadas
  (rota, handler, repositório, tipo de resposta, serviço, modelo e componente), e o modal de
  orçamento usa `Estimate` em 100% dos identificadores.
- **SC-002**: Os termos antigos usados para essas filas (`Refused`, `Reproved`, `PendingPurchase`,
  `WaitingPickup`, `WaitingParts`, `WaitingPart`, `WaitingResponse`, `Quote`, sufixos `In` em
  datas e `ordens`) aparecem 0 vezes no código da Home, nas seis rotas e nos tipos e métodos associados.
- **SC-003**: 100% dos campos dos seis tipos de resposta têm o mesmo nome na API e no frontend.
- **SC-004**: Com a mesma base de dados, as seis abas mostram exatamente os mesmos registros e
  valores antes e depois da refatoração, sem regressão relatada pelos usuários.
- **SC-005**: O build do backend, o build do frontend e a suíte de testes passam sem falhas.

## Assumptions

- **Escopo**: limitado às seis filas da Home e ao que elas usam: rotas, métodos do handler e do
  repositório, records de resposta, serviço, modelos e componente Angular. O modal de orçamento aberto
  pela Home também entra no escopo (ver Clarifications). As demais rotas de OS e os enums de domínio ficam fora do escopo e
  podem ser tratados em uma feature futura.
- **Enums de domínio inalterados**: `ERepairStatus`, `ERepairResult` e `EServiceOrderStatus` não
  são renomeados, para evitar impacto em dados persistidos e em outras telas (Princípio III da
  constituição).
- **`Guarantee` na entidade**: a propriedade da entidade, a coluna no banco e os usos em
  WhatsApp e PDF ficam como `Guarantee`. Só o contrato do orçamento usa `Warranty`. A
  divergência é intencional e fica isolada no mapeamento do handler.
- **Estilo de rota**: mantém o padrão `get-…/{pageNumber}/{pageSize}` já usado em toda a API, em
  vez de adotar REST com recursos, para não deixar só estas rotas fora do padrão.
- **Publicação conjunta**: frontend e API são publicados juntos, por isso não são necessárias
  rotas de compatibilidade.
- **Sem regra de negócio nova**: a feature não cria nem altera regra de negócio, então o
  Princípio IV pede apenas que os testes existentes continuem passando, sem exigir testes novos.
- **Termos canônicos**: "Awaiting" foi confirmado (ver Clarifications). "Rejected" foi escolhido
  em vez de "Refused/Reproved/Disapproved" por ser o termo padrão para orçamento recusado.
