# Research: Modal "Liberado" na aba "Reprovados"

Nenhum `NEEDS CLARIFICATION` em aberto.

## R1. Comportamento: igual ao handler antigo

- **Decision**: `IServiceOrderCommandHandler.MarkRejectedEstimateAsReadyForPickup(long id)`
  reproduz `old-handlers/MarkEstimateRejectedAsReadyToPickUpHandler.cs`, na mesma ordem:
  1. `id == 0` → `BaseResponse(null, 400, "O número da ordem de serviço não pode ser 0")`;
  2. `GetByIdForUpdateAsync(id)`; se `!IsSuccess || Data == null` →
     `BaseResponse(null, 404, result.Message)` (repassa a mensagem do repositório);
  3. `serviceOrder.MarkEstimateRejectedAsReadyToPickUp()`. O Domain lança `EntityException`
     ("O orçamento precisa estar rejeitado…") quando não está reprovado; o `ExceptionMiddleware`
     devolve **HTTP 400** com essa mensagem (corrigido na 002);
  4. `UpdateAsync`; se `!IsSuccess` → `BaseResponse(null, update.Code, update.Message)`, sem WhatsApp;
  5. `Enqueue(new SendRejectionReadyForPickupMessageRequest(Customer.Name.CustomerName, Customer.Phone.CustomerPhone))`, para todas as empresas;
  6. `BaseResponse(dto, 200, "Ordem de serviço marcada como pronta para entrega")`.
- Sem notificação interna, igual ao antigo.
- **Note**: o antigo usava `GetById`, que não existe mais; `GetByIdForUpdateAsync` é o
  equivalente (mesma escolha das features anteriores). O retorno usa `BaseResponse<ServiceOrderDto?>`
  para manter a interface uniforme (o antigo usava `ServiceOrderDto` não anulável; o JSON é o mesmo).
- **Exceção do domínio**: o antigo deixava a exceção subir. Isso é mantido: não há pré-checagem
  que retorne 409, ao contrário da 002, porque aqui é para seguir o fluxo antigo à risca.

## R2. Nome do método e da rota

- **Decision**: rota `PUT /service-orders/{id:long}/ready-for-pickup`; método
  `MarkRejectedEstimateAsReadyForPickup`. Ambos usam o termo canônico `ReadyForPickup` (feature
  001). O método de domínio `MarkEstimateRejectedAsReadyToPickUp` não é renomeado (fora do escopo).
- Remover `PUT /mark-estimate-rejected-as-ready-to-pick-up` e `MarkEstimateRejectedAsReadyToPickUpCommand`.

## R3. Frontend

- **Decision**: `modals/rejected-release/` (`RejectedReleaseModalComponent` +
  `RejectedReleaseModalService`), copiando `modals/part-purchase/` (o mais recente e mais simples):
  título "Liberação", resumo "Orçamento reprovado" (Solução, Valor total, Garantia, "Reprovado
  em" = `responseDate`), rodapé "O aparelho já está pronto para retirada?" com Cancelar e
  **Liberado**.
- Mensagem local de sucesso: "Ordem de serviço liberada para retirada.".

## R4. Testes (Princípio IV)

- `ServiceOrderCommandHandlerTests`, um caso por saída do fluxo:
  - `id == 0` → 400 e repositório não consultado;
  - não encontrada → 404 com a mensagem do fake;
  - orçamento não reprovado (OS aguardando resposta) → lança `EntityException<ServiceOrder>`, sem
    `UpdateAsync` e sem WhatsApp;
  - falha no `UpdateAsync` → devolve o código e a mensagem do update, sem WhatsApp;
  - sucesso (Particular e PhilcoBritania) → `ServiceOrderStatus == ReadyToPickUp`,
    `RepairStatus` continua `Disapproved`, 1 `SendRejectionReadyForPickupMessageRequest("JOÃO DA SILVA", "41999999999")`,
    0 notificações, mensagem "Ordem de serviço marcada como pronta para entrega".
- Fakes: `FakeServiceOrderCommandRepository` ganha `UpdateResponse` configurável (padrão =
  sucesso) para simular a falha; `FakeServiceOrderQueryRepository` registra se
  `GetByIdForUpdateAsync` foi chamado.
- Domain (`ServiceOrderRejectedReleaseTests`): `MarkEstimateRejectedAsReadyToPickUp()` numa OS
  reprovada → `ReadyToPickUp`; numa OS não reprovada → `EntityException`. O método existe, mas
  não tinha teste.
