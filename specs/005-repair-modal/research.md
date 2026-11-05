# Research: Modal "Consertado" na aba "Aguardando peça"

Nenhum `NEEDS CLARIFICATION` em aberto.

## R1. Comportamento: igual ao handler antigo

- **Decision**: `IServiceOrderCommandHandler.RegisterRepair(long id)` reproduz
  `old-handlers/AddServiceOrderRepairHandler.cs`, na mesma ordem:
  1. `GetByIdForUpdateAsync(id)`; se `Data == null` → `BaseResponse(null, 404, "Essa ordem de serviço não existe")`;
  2. `serviceOrder.ExecuteRepair()`. O Domain lança `EntityException` ("Não podemos executar o
     conserto pois a ordem de serviço não tem o status de aprovado") quando `RepairStatus` é
     `Entered` ou `Waiting`; o `ExceptionMiddleware` responde **HTTP 400**;
  3. `await UpdateAsync(serviceOrder)`, **sem checar o resultado** (igual ao antigo);
  4. `Enqueue(new SendDeviceReadyMessageRequest(Id.ToString(), Customer.Name.CustomerName, Customer.Phone.CustomerPhone))`, para todas as empresas;
  5. `BaseResponse(dto, 200, "Conserto adicionado com sucesso")`.
- Sem notificação interna, igual ao antigo.
- **Note**: `GetById` → `GetByIdForUpdateAsync` (mesma escolha das features anteriores).

## R2. Nome do método e da rota

- **Decision**: rota `PUT /service-orders/{id:long}/repair`; método `RegisterRepair` (mesmo padrão
  de `RegisterPartPurchase`). O método de domínio `ExecuteRepair` não muda.
- Remover `PUT /add-service-order-repair` e `AddServiceOrderRepairCommand`.

## R3. Frontend

- **Decision**: `modals/repair/` (`RepairModalComponent` + `RepairModalService`), copiando
  `modals/rejected-release/`: título "Conserto", seção "Resumo do conserto" (Solução, Valor
  total, Garantia, "Peça comprada em" = `purchasePartDate`), rodapé "O conserto já foi concluído?"
  com Cancelar e **Consertado**. Mensagem de sucesso: "Conserto registrado.". Fallback de erro:
  "Não foi possível registrar o conserto. Tente de novo.".
- Já são 5 modais com o mesmo painel lateral e os mesmos estilos. A extração para um componente
  compartilhado continua recomendada como feature de refatoração.

## R4. Testes (Princípio IV)

- `ServiceOrderCommandHandlerTests`:
  - não encontrada → 404 "Essa ordem de serviço não existe";
  - orçamento aguardando resposta → `EntityException<ServiceOrder>`, sem `UpdateAsync` e sem WhatsApp;
  - sucesso (Particular e PhilcoBritania), a partir de uma OS aprovada com peça comprada →
    `ServiceOrderStatus == Repaired`, `RepairDate` não nulo, 1
    `SendDeviceReadyMessageRequest("1", "JOÃO DA SILVA", "41999999999")`, 0 notificações,
    mensagem "Conserto adicionado com sucesso";
  - falha no `UpdateAsync` → **mesmo assim** 200 e WhatsApp enviado (documenta o comportamento
    antigo, que não checa o salvamento).
- Domain (`ServiceOrderRepairTests`): `ExecuteRepair()` numa OS aprovada → `Repaired` + data;
  numa OS aguardando resposta → exceção com a mensagem exata. O método existe, mas não tinha
  teste.
