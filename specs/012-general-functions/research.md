# Research: Funções gerais

Base: `old-handlers/EditServiceOrderHandler.cs` (+ `EditServiceOrderCommand`,
`ServiceOrderMapper.ApplyChanges`), `old-handlers/RegenerateAndReturnPdfHandler.cs`
(+ `RegenerateDocument`), a rota de localização (`PUT /service-orders/{id}/product/location`), o
painel da Buscar OS (feature 011) e os componentes de `features/customers/` (feature 010).

## R1. Alterar OS — o que muda em relação ao antigo

- **Decision**: reescrever `EditServiceOrderCommand` (sem MediatR) só com os dados da OS:
  `Brand, Model, SerialNumber, Defect, Accessories, Type, Location, Enterprise, EntryDate,
  InspectionDate, ResponseDate, RepairDate, PurchasePartDate, DeliveryDate, Solution, Guarantee,
  PartCost, LaborCost`. Handler `UpdateServiceOrder(id, command)` em `ServiceOrderCommandHandler`:
  `Validate` → `Normalize` (maiúsculas/trim, como o antigo) → `GetByIdForUpdateAsync` (404) →
  aplicar → `UpdateAsync` → `ServiceOrderDto`.
- **Diferenças do antigo, todas de propósito**:
  - **Sem cliente** (Clarification/Edge Case da spec): o antigo criava um `Customer` novo com o
    mesmo Id e o gravava junto; aqui o cliente é editado em `/clientes`.
  - **Sem situação/orçamento/resultado** (`ServiceOrderStatus`, `RepairStatus`, `RepairResult`,
    `EstimateMessage`): mudam só pelos fluxos da Home; editá-los à mão quebraria as abas.
  - **Localização preservada**: o antigo fazia `new Product(...)` e apagava a localização sem
    querer; aqui usa `Product.UpdateProduct(...)` + `AddLocation(command.Location)`.
  - **OS de venda**: defeito continua `"VENDA"` e acessórios vazios, mesmo que venham outros
    valores (senão a OS deixaria de contar como venda nas métricas).
  - Erros como `CommandException` (400 pelo middleware), não `ArgumentException` (virava 500).
- **Validações**: modelo e série obrigatórios; tipo e empresa válidos; peça e mão de obra ≥ 0;
  data de entrada obrigatória.

## R2. Reimprimir

- **Decision**: `RegeneratePdf(long id)` em `ServiceOrderCommandHandler` (já tem o gerador de PDF):
  `GetByIdForUpdateAsync` (404 "Essa ordem de serviço não existe") → `RegeneratePdfAsync` (falha →
  500) → bytes. Rota `GET /service-orders/{id:long}/pdf` → `application/pdf`
  (`ordem_servico_{id}.pdf`). O documento (entrada/entrega/venda) continua sendo escolhido pelo
  `RegenerateDocument` existente, como no antigo.
- **Alternatives**: `POST` como o antigo (não altera nada → `GET` é o verbo certo).

## R3. Localização rápida

- **Decision**: reaproveitar `PUT /service-orders/{id}/product/location` (já existe e já avisa as
  telas). No painel, o campo Localização é editável mesmo fora do modo Alterar, com botão de salvar
  que aparece quando o valor muda.

## R4. Painel: modo leitura × modo edição

- **Decision**: o painel (`service-order-details-dialog`) ganha um `FormGroup` com os campos da OS;
  em leitura os inputs ficam `readonly` (selects desabilitados); **Alterar** liga a edição,
  **Salvar** chama `PUT /service-orders/{id}` e **Cancelar**/Esc volta aos valores gravados. O
  rodapé mostra **Reimprimir · Alterar · Fechar** em leitura e **Cancelar · Salvar** em edição.
  Datas em `input type="date"` (mesmo motivo da 011). Ao salvar, o diálogo devolve a OS nova e a
  tabela da busca atualiza a linha.
- **Esc em edição**: cancela a edição em vez de fechar o painel (`disableClose` enquanto edita).

## R5. Clientes

- **Decision**: tela `/clientes` (`features/customers/customers-page`) com a busca de cliente
  (`app-customer-search`) e, ao escolher, o `CustomerDialogComponent` em modo **editar** (novo
  campo `mode: 'edit'` nos dados do diálogo: título "Editar cliente", texto e botão "Salvar";
  sem alterações, só fecha). Snackbar "Cliente atualizado.". Rota sob demanda.
- **Menu**: sai "Geral"; entra "Clientes" (ícone `group`) depois de "Buscar OS".

## R6. Rotas antigas

- Removidas: `PUT /edit-service-order`, `POST /regenerate-service-order-pdf`,
  `RegenerateAndReturnPdfCommand`, `ServiceOrderMapper.ApplyChanges`,
  `ServiceOrderMapper.MapToUpdateResponseDto` e `UpdateServiceOrderResponseDto` (ficam sem uso).
