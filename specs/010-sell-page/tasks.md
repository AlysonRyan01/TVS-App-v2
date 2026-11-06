# Tasks: Tela "Vender"

**Input**: Design documents from `/specs/010-sell-page/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/http-endpoints.md, quickstart.md

**Tests**: incluídos — a constituição (IV) e a FR-008 exigem testes das regras de venda.

**Organization**: por user story (US1 registrar venda, US2 correções e erros).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: pode rodar em paralelo (arquivos diferentes, sem dependência pendente)
- Namespaces seguem o padrão existente (`TVS_App.*`; testes em `TVSApp.Tests.*`)

---

## Phase 1: Setup

- [X] T001 Conferir a base: `dotnet build TVSApp.sln`, `dotnet test src/TVSApp.Tests` (82 verdes) e `cd web && npx ng build` (inicial ~998 kB, sem erro)

---

## Phase 2: Foundational (coluna de cliente comum — bloqueia a tela Vender)

**Goal**: extrair a coluna ① Cliente da Adicionar OS sem mudar o comportamento de `/adicionar`.

- [X] T002 Mover para `web/src/app/features/customers/` as pastas `add-service-order/customer-search/`, `customer-form/`, `customer-dialog/` e os arquivos `add-service-order/models/{customer.model,address.model,document}.ts` (para `customers/models/`); `create-service-order.model.ts` fica em `add-service-order/models/`
- [X] T003 Criar `web/src/app/features/customers/customers.service.ts` (`CustomersService`, `providedIn: 'root'`) com `searchCustomers`, `createCustomer`, `updateCustomer`, `getAddress` tirados de `add-service-order.service.ts` (que fica só com `createServiceOrder`); trocar `AddServiceOrderService` por `CustomersService` em `customer-search`, `customer-form` e `customer-dialog`
- [X] T004 Criar `web/src/app/features/customers/customer-picker/customer-picker.component.{ts,html}` (`app-customer-picker`, `host: { style: 'display: block' }`): o bloco `@if (selectedCustomer(); as customer) { cartão } @else { busca + form "Criar cliente" }` de `add-service-order.component.html`, com a lógica `openExisting`, `useSearchTermAsName`, `createCustomer`, `changeCustomer`, `addressSummary`, `isSavingCustomer`, `formatDocument`, `maskPhone`; API `readonly customer = model<Customer | null>(null)` e `reset()` (`customer.set(null)` + `_search()?.clear()`); aria-label do cartão "Cliente escolhido"
- [X] T005 Criar `web/src/app/shared/styles/_two-column-page.scss` com todo o conteúdo de `add-service-order.component.scss` aninhado em `.two-column-page { … }` (o bloco `:host` vira `& { … }`; `@use 'brand' as *`), adicionar `@use 'app/shared/styles/two-column-page';` em `web/src/styles.scss` e apagar `add-service-order.component.scss`
- [X] T006 Atualizar `web/src/app/features/add-service-order/add-service-order.component.{ts,html}`: `host: { class: 'two-column-page' }` (sem `styleUrl`), `<app-customer-picker [(customer)]="selectedCustomer" />` no lugar do bloco da coluna, `_picker = viewChild(CustomerPickerComponent)` e `reset()` chamando `this._picker()?.reset()`; remover o código movido
- [X] T007 Trocar `/adicionar` para `loadComponent: () => import('./features/add-service-order/add-service-order.component').then(m => m.AddServiceOrderComponent)` em `web/src/app/app.routes.ts` (comentário: pacote inicial no limite de 1 MB)

**Checkpoint**: `ng build` sem erro; `/adicionar` igual a antes.

---

## Phase 3: User Story 1 — Registrar uma venda no balcão (P1) 🎯 MVP

**Goal**: `/vender` registra a OS de venda como o handler antigo, abre o comprovante e enfileira o WhatsApp.

**Independent Test**: quickstart §3 itens 1–3.

### Backend

- [X] T008 [P] [US1] Em `src/TVSApp.Tests/Handlers/Fakes/FakeServiceOrderPdfGenerator.cs`, implementar `GenerateSaleDocumentAsync(ServiceOrder)` com `SaleResponse` configurável (padrão `new(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x56 }, 200, "Comprovante de venda gerado")`) e contador `SaleCalls`
- [X] T009 [US1] Escrever testes em `src/TVSApp.Tests/Handlers/ServiceOrderCommandHandlerTests.cs` para `CreateSalesServiceOrder`: (a) cada regra de `Validate` lança `CommandException` com as mensagens de data-model.md ("O CustomerId da ordem de serviço não pode ser 0", "O modelo da ordem de serviço não pode estar vazio", "O número de série da ordem de serviço não pode estar vazio", "A venda precisa ter uma garantia", "O tipo do produto da ordem de serviço não tem um valor válido", "O valor da venda não pode ser vazio ou 0") e não cria OS; (b) a OS criada (`LastCreated`) tem `Enterprise = Particular`, `Product.Defect = "VENDA"`, `Product.Accessories = ""`, marca/modelo/série/garantia em maiúsculas sem espaços, `PartCost = Amount`, `ServiceOrderStatus = Delivered`, `RepairResult = Repair`, `RepairStatus = Approved`, `DeliveryDate` preenchida; (c) falha no `CreateAsync` → 500 com a mensagem, sem PDF e sem WhatsApp; (d) falha no PDF → 500, OS criada, sem WhatsApp; (e) sucesso → 200 com `Id` e bytes do PDF, `SaleCalls == 1`, `CheckInCalls == 0` e um `SendProductSaleMessageRequest` com nome, telefone, valor, garantia e tipo
- [X] T010 [US1] Em `src/TVSApp.Application/Requests/ServiceOrderRequests/ServiceOrderCommands/CreateSalesServiceOrderCommand.cs`, remover `using MediatR` e `: IRequest<BaseResponse<byte[]>>` (e o `using TVS_App.Domain.Shared` se ficar sem uso); validações e `Normalize` inalterados
- [X] T011 [US1] Adicionar `Task<BaseResponse<CreatedServiceOrderPdf>> CreateSalesServiceOrder(CreateSalesServiceOrderCommand command);` em `src/TVSApp.Application/Handlers/IServiceOrderCommandHandler.cs` e implementar em `ServiceOrderCommandHandler.cs` exatamente como research R2 (comentário citando `old-handlers/CreateSalesServiceOrderHandler.cs` e a única diferença: devolve o número da OS); T009 deve passar
- [X] T012 [US1] Em `src/TVSApp.Api/Endpoints/ServiceOrderEndpoints.cs`: substituir `POST /create-sales-service-order` por `POST /service-orders/sales` (`IServiceOrderCommandHandler`, `CreateSalesServiceOrderCommand`, `HttpContext`, `IHubContext<ServiceOrderHub>`); erro → `result.ToResult()`; sucesso → SignalR `"Atualizar"`, cabeçalho `X-Service-Order-Id`, `Results.File(pdf, "application/pdf", $"venda_{id}.pdf")`; `.WithTags("ServiceOrder").RequireAuthorization()`. `grep -rn "create-sales-service-order" src web/src` deve vir vazio

### Frontend

- [X] T013 [P] [US1] Criar `web/src/app/features/sell/models/create-sale.model.ts` (`customerId, productType, productBrand, productModel, productSerialNumber, guarantee, amount`) e `web/src/app/features/sell/sell.service.ts` com `createSale(command): Observable<HttpResponse<Blob>>` (`POST ${environment.apiUrl}/service-orders/sales`, `observe: 'response'`, `responseType: 'blob'`)
- [X] T014 [US1] Criar `web/src/app/features/sell/sell.component.{ts,html}` (`app-sell`, `host: { class: 'two-column-page' }`, título "Vender"): coluna "1 Cliente" com `<app-customer-picker [(customer)]="selectedCustomer" />`; coluna "2 Venda" com `.device-area`/`.device-lock` ("Escolha ou crie o cliente ao lado para registrar a venda.") e `[attr.inert]`; seções "Informações do Cliente" (Nome do cliente* só leitura), "Informações do Produto" (Tipo de Produto select padrão TV; Marca | Modelo*; Número de Série*) e "Detalhes da Venda" (Garantia* placeholder "Ex.: 90 dias" | Valor* com `matTextPrefix` "R$", `type="number"`, `step="0.01"`); botão full-width **Criar venda** ("Registrando…" com spinner). `createSale()`: `openPdfWindow()` no clique, envia, `showPdf(body, win, 'venda_{id}.pdf', 'Comprovante de venda - OS {id}')`, snackbar `Venda registrada (OS {id}).`, `reset()` (picker + formulário); erro → fecha a aba e `readBlobErrorMessage(error, 'Não foi possível registrar a venda. Tente de novo.')`
- [X] T015 [US1] Adicionar a rota `vender` com `loadComponent: () => import('./features/sell/sell.component').then(m => m.SellComponent)` e `canActivate: [authGuard]` em `web/src/app/app.routes.ts`, e `route: '/vender'` no item "Vender" de `web/src/app/shared/layout/base-layout/base-layout.component.ts`

**Checkpoint**: venda registrada de ponta a ponta.

---

## Phase 4: User Story 2 — Correções e erros (P2)

**Goal**: mesmo comportamento da Adicionar OS em validação, troca de cliente e falhas.

**Independent Test**: quickstart §3 itens 4–5.

- [X] T016 [US2] Em `web/src/app/features/sell/sell.component.ts`: `productModel`, `productSerialNumber`, `guarantee` com `Validators.required`; `amount` `FormControl<number | null>(null, [Validators.required, Validators.min(0.01)])`; botão desabilitado sem cliente, com formulário inválido ou enviando (`isSending`); `markAllAsTouched()` ao tentar enviar inválido; em erro não resetar; "Trocar cliente" (picker) não mexe no formulário da venda

---

## Phase 5: Polish & validação

- [X] T017 [P] `dotnet build TVSApp.sln` e `dotnet test src/TVSApp.Tests` verdes (82 + novos)
- [X] T018 [P] `cd web && npx ng build` sem erro (inicial < 1 MB; `add-service-order` e `sell` como chunks sob demanda; nenhum SCSS de componente acima de 4 kB novo)
- [ ] T019 Roteiro manual do quickstart §3 (Vender e Adicionar OS)

---

## Dependencies & Execution Order

- T001 → Foundational (T002 → T003 → T004 → T005 → T006 → T007) → US1 frontend.
- Backend de US1 (T008–T012) não depende da Foundational: pode começar logo após T001.
- Dentro do backend: T008 → T009 → T010 → T011 → T012.
- Frontend de US1: T013 ∥ (Foundational); T014 depende de T004, T005 e T013; T015 depende de T014.
- US2 (T016) depende de T014. Polish por último.

### Parallel Opportunities

- Backend (T008–T012) ∥ Foundational (T002–T007).
- T013 ∥ qualquer tarefa de backend; T017 ∥ T018.

### Parallel Example: User Story 1

```text
T008 fake do PDF de venda  |  T013 sell.service.ts + create-sale.model.ts  |  T002–T007 extração do picker
```

## Implementation Strategy

1. **MVP**: Setup + Foundational + US1 → vender pela tela.
2. **US2**: acabamento de validação e erros (pequeno; pode entrar junto com T014).
3. Publicar API + web juntos (a rota antiga sai e a tela usa a nova).
