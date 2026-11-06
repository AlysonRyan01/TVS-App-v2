# Research: Tela "Vender"

Base: `old-handlers/CreateSalesServiceOrderHandler.cs`, `CreateSalesServiceOrderCommand.cs`,
`ServiceOrderEndpoints.cs` (rota quebrada), `ServiceOrderCommandHandler.CreateServiceOrder`
(feature 009) e `features/add-service-order/`.

## R1. Onde fica o fluxo de venda

- **Decision**: novo método `CreateSalesServiceOrder(CreateSalesServiceOrderCommand)` em
  `IServiceOrderCommandHandler`/`ServiceOrderCommandHandler`, devolvendo
  `BaseResponse<CreatedServiceOrderPdf>` (o record da 009, com número e PDF).
- **Rationale**: a venda é uma OS; o handler de comandos de OS já tem repositório, PDF e fila de
  WhatsApp injetados. Mesmo padrão de `CreateServiceOrder`.
- **Alternatives**: handler separado `ISaleHandler` (mais DI para um método só).

## R2. Fidelidade ao handler antigo

- **Decision**: reproduzir na ordem: `Validate()` → `Normalize()` → `new Product(brand, model,
  serial, "VENDA", "", type)` → `new ServiceOrder(Particular, customerId, product)` →
  `EditGuarantee`, `EditPartCost(Amount)`, `EditServiceOrderStatus(Delivered)`,
  `EditRepairResult(Repair)`, `EditRepairStatus(Approved)`, `EditDeliveryDate(UtcNow)` →
  `CreateAsync` (falha → 500 com a mensagem) → `GenerateSaleDocumentAsync` (falha → 500) →
  `SendProductSaleMessageRequest(nome, telefone, PartCost, Guarantee, Product.Type)`.
- **Diferença única**: a resposta inclui o número da OS (FR-004), como na 009.
- **Command**: `CreateSalesServiceOrderCommand` perde o `IRequest` do MediatR; validações e
  mensagens ficam iguais (inclusive `Amount == 0`, sem recusar negativo no servidor).
- **Endpoint antigo** chamava `Normalize` e `Validate` antes do handler; isso sai (o handler já faz).

## R3. Rota

- **Decision**: `POST /service-orders/sales` → 200 `application/pdf` (`venda_{id}.pdf`) com
  cabeçalho `X-Service-Order-Id` (já exposto no CORS); erros pelo `BaseResponse.ToResult()`;
  SignalR `"Atualizar"`. Remove `POST /create-sales-service-order`.
- **Rationale**: REST, subrecurso de OS (é uma OS de venda); reaproveita o cabeçalho da 009.
- **Alternatives**: `POST /sales` (sugere recurso próprio, que foi descartado).

## R4. Frontend: reaproveitar a coluna de cliente

- **Decision**: extrair a coluna ① Cliente da Adicionar OS para `app-customer-picker`
  (`customer = model<Customer | null>()`, `reset()`), movendo busca, formulário, modal, modelos e
  as chamadas de cliente/CEP para `features/customers/` (`CustomersService`). Os estilos comuns das
  duas telas vão para o partial global `shared/styles/_two-column-page.scss` (classe
  `.two-column-page` no host das páginas).
- **Rationale**: as duas telas ficam com o mesmo comportamento sem duplicar ~120 linhas de TS/HTML
  e ~200 de SCSS; cada SCSS de componente fica abaixo de 4 kB.
- **Alternatives**: copiar a tela; importar os subcomponentes de dentro de `add-service-order/`.

## R5. Orçamento do bundle

- **Decision**: `/adicionar` e `/vender` com `loadComponent` (carregadas sob demanda).
- **Rationale**: o pacote inicial está em ~998 kB; com a tela nova passa do erro de 1 MB (medido na
  tentativa anterior: 1,003 MB). Com as duas sob demanda ficou em ~980 kB.

## R6. Campos e validação na tela

- **Decision**: Tipo de Produto (select, padrão TV), Marca | Modelo*, Número de Série*,
  Garantia* (texto, placeholder "Ex.: 90 dias") | Valor* (`type="number"`, prefixo "R$",
  `Validators.min(0.01)`). Snackbar "Venda registrada (OS {id})."; erro mantém os campos.
