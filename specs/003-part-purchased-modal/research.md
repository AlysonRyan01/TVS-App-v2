# Research: Modal "Comprado" na aba "Aprovados"

Nenhum `NEEDS CLARIFICATION` em aberto.

## R1. Comportamento: igual ao handler antigo

- **Decision**: `IServiceOrderCommandHandler.RegisterPartPurchase(long id)` reproduz
  `old-handlers/AddPurchasedPartHandler.cs`:
  1. busca a OS (`GetByIdForUpdateAsync`); se não existir → `BaseResponse(null, 404, "Essa ordem de serviço não existe")`;
  2. `serviceOrder.AddPurchasedPart()` (Domain, sem alteração: `ServiceOrderStatus = OrderPart`,
     `PurchasePartDate = UtcNow`);
  3. `UpdateAsync`;
  4. retorna `BaseResponse(dto, 200, "Compra de peça adicionada com sucesso!")`.
- Sem validação de situação, sem notificação interna e sem WhatsApp, porque o handler antigo não
  tinha nada disso.
- **Note**: o antigo usava `GetById`, que não existe mais; `GetByIdForUpdateAsync` é o equivalente
  para atualização (mesma escolha das features 001 e 002).
- **Nome do método**: `RegisterPartPurchase` descreve a ação em inglês correto, como pede a
  feature 001. O nome do método de domínio `AddPurchasedPart` não muda (fora do escopo).

## R2. Rota REST

- **Decision**: `PUT /service-orders/{id:long}/part/purchase`, sem corpo,
  `.RequireAuthorization()`, com SignalR "Atualizar" quando dá certo (a rota antiga já fazia isso)
  e `ToResult()`. Remover `PUT /add-service-order-purchased-part` e `AddPurchasedPartCommand`.
- **Rationale**: Clarification da spec; mesmo estilo de `/service-orders/{id}/estimate/approve|reject`.

## R3. Frontend

- **Decision**: novo `modals/part-purchase/` (`PartPurchaseModalComponent` +
  `PartPurchaseModalService`), copiando a estrutura do `modals/estimate-response/`: header
  "Compra da peça", painel `ticket`, resumo somente leitura, rodapé com Cancelar e **Comprado**,
  botões travados e `disableClose` durante o envio.
- Resumo exibido (do `ServiceOrder` compartilhado): `solution`, `totalAmount`, `partPurchaseCost`,
  `guarantee`, `responseDate` (rótulo "Aprovado em").
- Sem tratamento especial de 409 (a API não devolve 409 nesta ação).
- **Alternatives considered**: um componente genérico de "modal de ação" para resposta e compra.
  Rejeitado por enquanto: mexe no modal da 002. Com 3 modais no mesmo padrão, vale uma feature
  própria de refatoração para extrair o `ticket` e os estilos.

## R4. Testes (Princípio IV)

- `ServiceOrderCommandHandlerTests`: 404; sucesso → `ServiceOrderStatus == OrderPart`,
  `PurchasePartDate` não nulo, `UpdateAsync` chamado, mensagem "Compra de peça adicionada com
  sucesso!", nenhuma notificação e nenhum WhatsApp.
- `ServiceOrderPartPurchaseTests` (Domain): `AddPurchasedPart()` muda a situação e define a data.
  O método já existe, mas não tinha teste.
