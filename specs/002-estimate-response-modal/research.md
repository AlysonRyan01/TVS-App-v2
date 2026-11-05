# Research: Modal de resposta do orçamento

Nenhum `NEEDS CLARIFICATION` ficou aberto. Este documento registra as decisões e os achados no
código.

## R1. Handlers sem MediatR

- **Decision**: adicionar `ApproveEstimate(long id)` e `RejectEstimate(long id)` ao
  `IServiceOrderCommandHandler` / `ServiceOrderCommandHandler` criados na feature 001.
- **Rationale**: mesmo padrão já usado no `AddEstimate` (handler comum com interface e DI),
  pedido explicitamente no plano da 001.
- **Alternatives considered**: um handler separado (`IEstimateResponseHandler`). Rejeitado
  porque o command handler do agregado OS já existe e é o lugar natural para isso.

## R2. Achado: rotas de aprovação e reprovação sem handler

- **Finding**: `PUT /add-service-order-approve-estimate` e `/add-service-order-reject-estimate`
  chamam `ISender.Send(...)` para `AddServiceOrderApproveEstimateCommand` /
  `AddServiceOrderRejectEstimateCommand`, mas não existe nenhum `IRequestHandler` para eles.
  Hoje, as duas rotas falham ao serem chamadas. A rota de aprovação também não tem
  `.RequireAuthorization()`.
- **Decision**: remover as duas rotas e os dois commands e criar as rotas REST da FR-013a,
  ambas com `.RequireAuthorization()`.

## R3. Notificação interna sem MediatR

- **Finding**: as rotas atuais criam a notificação com `ISender.Send(CreateNotificationCommand)`.
  O `CreateNotificationHandler` (MediatR) só repassa para
  `INotificationCommandRepository.Create(title, message)`.
- **Decision**: o `ServiceOrderCommandHandler` recebe `INotificationCommandRepository` por
  injeção e chama `Create(...)` diretamente, com os mesmos textos de hoje:
  - Aprovação: `"Orçamento Aprovado, OS: {id}"` / `"Favor comprar peça para OS: {id}"`
  - Reprovação: `"Orçamento Reprovado, OS: {id}"` / `"Favor montar a OS {id} para entrega"`
- **Rationale**: elimina o MediatR do fluxo e coloca o efeito colateral de negócio na Application,
  não no endpoint.

## R4. WhatsApp

- **Finding**: `SendThankYouForEstimateApprovalRequest(ServiceOrder, CustomerName, PhoneNumber,
  Amount)` e `SendEstimateDisapprovalAcknowledgmentRequest(ServiceOrder, CustomerName,
  PhoneNumber)` existem e são tratados pelo `WhatsAppNotificationWorker`, mas nada os enfileira.
- **Decision (revisada)**: reproduzir os handlers antigos em `old-handlers/`. Na aprovação, se
  `Enterprise != PhilcoBritania`, enfileirar `SendThankYouForEstimateApprovalRequest` (com
  `Amount = TotalAmount`) e `NotifyOwnerEstimateDecisionRequest(id, Approved, nome)`. Na
  reprovação, sempre `SendEstimateDisapprovalAcknowledgmentRequest` e
  `NotifyOwnerEstimateDecisionRequest(id, Disapproved, nome)`.

## R5. Regra "só responde quem está aguardando resposta" (FR-009)

- **Decision**: a regra fica no **Domain**:
  - nova propriedade `ServiceOrder.IsAwaitingEstimateResponse` =
    `ServiceOrderStatus == Evaluated && RepairStatus == ERepairStatus.Waiting`;
  - `ApproveEstimate()` e `RejectEstimate()` passam a lançar
    `EntityException<ServiceOrder>("Esse orçamento já foi respondido.")` quando
    `!IsAwaitingEstimateResponse` (além da validação de solução que já existe).
- **Handler**: confere `IsAwaitingEstimateResponse` antes e devolve
  `BaseResponse(null, 409, "Esse orçamento já foi respondido.")`, para não depender de exceção
  (ver R6). A exceção no Domain protege a invariante para qualquer outro chamador.
- **Rationale**: Princípio II (regras centrais no Domain) e Princípio IV (testável sem
  infraestrutura).
- **Concorrência**: duas telas respondendo ao mesmo tempo. A segunda requisição lê a OS já
  atualizada e recebe 409. Não há controle otimista de concorrência no projeto; essa janela
  mínima é aceita (SC-004 cobre cliques duplos, que o frontend bloqueia com FR-011).

## R6. Achado: o middleware de exceções responde HTTP 200

- **Finding**: `src/TVSApp.Api/Middlewares/ExceptionMiddleware.cs` monta
  `BaseResponse(null, 400|500, message)`, mas não define `context.Response.StatusCode`. Erros de
  validação (`CommandException`, `EntityException`) saem como **HTTP 200**, e o Angular trata
  isso como sucesso: o modal fecharia mostrando "Orçamento aprovado.". O mesmo vale hoje para o
  modal de orçamento.
- **Decision**: definir `context.Response.StatusCode = code` no middleware (uma linha).
  Sem isso, os cenários de erro da US3 não funcionam.
- **Risk**: muda o status HTTP de erros em toda a API. No Angular, as telas existentes (home,
  login) já tratam erro pelo callback `error` lendo `error.error.message`, que é o formato que
  o middleware devolve. O impacto é positivo.

## R7. Frontend: reuso do padrão do modal de orçamento

- **Decision**: criar `web/src/app/features/home/modals/estimate-response/` com componente,
  serviço e template no mesmo padrão do `modals/estimate/`: header, `ticket` lateral, estados
  `isLoading`/erro, rodapé com ações e `MatDialogRef.close(true)` no sucesso. O SCSS do painel
  `ticket` e do layout é copiado do modal de orçamento.
- **Alternatives considered**: extrair um componente compartilhado `service-order-ticket` e um
  SCSS parcial. É melhor no longo prazo, mas altera o modal de orçamento (fora do escopo).
  Fica como melhoria futura.
- **Dados**: o modal busca a OS por `GET /get-service-order-by-id/{id}` (o mesmo do modal de
  orçamento) e mostra `repairResult`, `solution`, `totalAmount`, `guarantee` e `estimateMessage`
  do `ServiceOrder` compartilhado.

## R8. Testes (Princípio IV)

- **Domain**: `src/TVSApp.Tests/Domain/ServiceOrderEstimateResponseTests.cs`. Aprovar ou reprovar
  uma OS aguardando resposta muda `RepairStatus` e define `ResponseDate`; aprovar ou reprovar uma
  OS já respondida lança a exceção; `IsAwaitingEstimateResponse` está correto para cada estado.
- **Application**: ampliar `ServiceOrderCommandHandlerTests`. Cobrir: 404; 409 quando já
  respondida; sucesso → `UpdateAsync`, notificação com os textos de R3 e WhatsApp com o tipo e
  os campos de R4; PhilcoBritania → sem WhatsApp. Novo fake: `FakeNotificationCommandRepository`.
