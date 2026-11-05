# Research: Padronização de nomenclatura das filas da tela Home

Não havia nenhum `NEEDS CLARIFICATION` no Technical Context. Este documento registra as decisões
de design e os achados no código que definem o plano.

## R1. Padrão de handler (sem MediatR)

- **Decision**: usar handlers comuns, com interface registrada no DI (`AddScoped`), seguindo o
  padrão que já existe em `IServiceOrderQueryHandler` / `ServiceOrderQueryHandler`. As filas
  continuam no `IServiceOrderQueryHandler`. O orçamento vai para um novo
  `IServiceOrderCommandHandler` / `ServiceOrderCommandHandler` com o método `AddEstimate`.
- **Rationale**: é o que foi pedido no `/speckit-plan` ("Não usar mediatr, fazer com handler
  comum com interface para injeção de dependência") e mantém leitura e escrita separadas, como
  nos repositórios (`IServiceOrderQueryRepository` / `IServiceOrderCommandRepository`).
- **Alternatives considered**:
  - Manter `ISender.Send(command)`: rejeitado por pedido explícito.
  - Colocar `AddEstimate` no `IServiceOrderQueryHandler`: rejeitado, porque misturaria leitura
    com escrita.
  - Um handler por caso de uso (`IAddServiceOrderEstimateHandler`): viável, mas criaria muitas
    interfaces pequenas. Um command handler por agregado segue o padrão do query handler.

## R2. Achado: a rota de orçamento está sem handler

- **Finding**: `AddServiceOrderEstimateHandler` (MediatR) foi removido no commit `0f7f7d3`, mas
  a rota `PUT /add-service-order-estimate` ainda chama `ISender.Send(AddServiceOrderEstimateCommand)`.
  O build passa, mas em tempo de execução o MediatR falha por não encontrar handler. Hoje,
  enviar o orçamento pelo modal da Home quebra.
- **Decision**: o novo `ServiceOrderCommandHandler.AddEstimate` reimplementa o comportamento do
  handler removido, sem mudar regras:
  1. `Normalize()` + `Validate()` do command;
  2. busca da OS (404 "Essa ordem de serviço não existe" se não existir);
  3. `serviceOrder.AddEstimate(solution, warranty→guarantee, totalCost, repairResult, estimateMessage)`;
  4. `EditPartPurchaseCost(new PartPurchaseCost(partPurchaseCost))`;
  5. `UpdateAsync`;
  6. enfileira WhatsApp (`SendEstimateMessageRequest`) quando `Enterprise != PhilcoBritania`;
  7. retorna `BaseResponse<ServiceOrderDto?>` 200 "Orçamento adicionado com sucesso!".
- **Note**: o handler removido usava `GetById`, que não existe mais. Usar
  `GetByIdForUpdateAsync`, que é o método de leitura para atualização.
- **Rationale**: atende à US3 (comportamento inalterado) e devolve o modal ao funcionamento que
  tinha antes da remoção.

## R3. Endpoint: `BaseResponse<T>.ToResult()`

- **Decision**: todos os endpoints do escopo seguem o formato
  `async (IHandler handler, ...) => (await handler.Metodo(...)).ToResult()`, com
  `.WithTags("ServiceOrder").RequireAuthorization()`. A extensão está em
  `src/TVSApp.Api/Extensions/BaseResponseExtensions.cs`. Na rota de orçamento, o endpoint
  mantém o `hubContext.Clients.All.SendAsync("Atualizar", ...)` quando a resposta é de sucesso,
  porque o SignalR é responsabilidade da Api, e o `Normalize`/`Validate` passa para o handler.
- **Rationale**: é o padrão pedido e o que já se usa nas filas.

## R4. Command sem dependência de MediatR

- **Decision**: `AddServiceOrderEstimateCommand` deixa de implementar `IRequest<...>`. O campo
  `Guarantee` passa a se chamar `Warranty` (FR-007a). O registro `AddMediatR` continua no
  `BuilderExtensions`, porque outros módulos (Customers, Notifications, Metrics, outras rotas
  de OS) ainda usam MediatR e estão fora do escopo.
- **Alternatives considered**: remover o MediatR do projeto todo. Rejeitado porque está fora do
  escopo da spec.

## R5. Serialização JSON

- **Decision**: os nomes dos campos vêm direto das propriedades dos records (camelCase padrão do
  `System.Text.Json` no Minimal API), sem `[JsonPropertyName]`. Renomear a propriedade
  renomeia o campo JSON.
- **Rationale**: atende ao FR-003 (nome idêntico, mudando só a caixa) sem anotações extras.

## R6. Testes (Princípio IV)

- **Decision**: criar `src/TVSApp.Tests/Handlers/ServiceOrderCommandHandlerTests.cs` (MSTest)
  cobrindo `AddEstimate`: OS inexistente → 404; `Warranty` gravado em `Guarantee`; enfileira
  WhatsApp para empresa diferente de PhilcoBritania e não enfileira para PhilcoBritania;
  `PartPurchaseCost` aplicado. Também criar `ServiceOrderQueryHandlerTests` para garantir que
  cada método de fila chama o método de repositório correspondente.
- **Fakes**: o projeto de testes só tem MSTest. Usar fakes escritos à mão (implementações
  simples de `IServiceOrderQueryRepository`, `IServiceOrderCommandRepository` e
  `IWhatsAppQueue`) em vez de adicionar uma biblioteca de mock.
- **Rationale**: `TVSApp.Tests` hoje não tem testes, e o handler é recriado (R2), então
  testá-lo é obrigatório pela constituição.

## R7. Persistência

- **Decision**: nenhuma mudança de schema e nenhuma migration (FR-011). As projeções do
  `ServiceOrderQueryRepository` só trocam os nomes das propriedades dos records. As colunas de
  origem (`InspectionDate`, `PurchasePartDate`, `RepairDate`, `ResponseDate` etc.) continuam
  iguais.
