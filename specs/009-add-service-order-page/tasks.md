---

description: "Task list for 009-add-service-order-page"
---

# Tasks: Tela "Adicionar OS" (cliente + nova ordem de serviço)

**Input**: Design documents from `/specs/009-add-service-order-page/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/http-endpoints.md, quickstart.md

**Tests**: incluídos, exigidos pelo FR-014 e pelo Princípio IV. MSTest com fakes escritos à mão.

**Organization**: a Fase 2 faz a mudança `Cpf` → `Document` (com a migration), as rotas REST de
cliente e de CEP, e a base da página (rota, menu, serviço, PDF). US1 (cliente existente) e US2
(cliente novo) entregam os dois caminhos até criar a OS; US3 cobre trocas e erros.

## Format: `[ID] [P?] [Story] Description`

## Regras gerais para todas as tarefas

- Sem MediatR nos fluxos novos (handlers comuns com interface, registrados com `AddScoped`).
- Endpoints com `.WithTags(...)`, `.RequireAuthorization()` e `return result.ToResult();` (exceto o PDF).
- Textos visíveis e mensagens em português, exatamente como na spec e no contrato.
- Mudança de schema **só** pela migration de T008, revisada à mão.

---

## Phase 1: Setup

- [X] T001 Rodar `dotnet build TVSApp.sln`, `dotnet test src/TVSApp.Tests` (57 testes) e `cd web && npx ng build` e confirmar que tudo passa
- [X] T002 [P] Criar as pastas `web/src/app/features/add-service-order/{customer-search,customer-dialog,models}` e `web/src/app/shared/pdf/`

---

## Phase 2: Foundational (Blocking Prerequisites)

**⚠️ CRITICAL**: nenhuma user story começa antes desta fase.

### Documento (CPF/CNPJ) — Domain

- [X] T003 [P] Criar `src/TVSApp.Tests/Domain/DocumentTests.cs` (MSTest) para o value object de T004: `FromOptional(null)`, `FromOptional("")` e `FromOptional("   ")` → `null`; `"123.456.789-09"` → `Number == "12345678909"`, `Type == EDocumentType.Cpf`, `ToString() == "123.456.789-09"`; `"11.222.333/0001-81"` → `"11222333000181"`, `Cnpj`, `ToString() == "11.222.333/0001-81"`; `"12.abc.345/01de-35"` → `"12ABC34501DE35"`, `Cnpj`; inválidos `"123"`, `"1234567890A"` (11 com letra), `"12ABC34501DEXX"` (dígitos finais com letra), `"123456789012345"` → `ValueObjectException<Document>` com mensagem "Informe um CPF (11 dígitos) ou um CNPJ (14 caracteres)."
- [X] T004 Criar `src/TVSApp.Domain/Enums/EDocumentType.cs` (`Cpf = 1`, `Cnpj = 2`, com `[Display(Name = "CPF")]` / `"CNPJ"`) e `src/TVSApp.Domain/ValueObjects/Customer/Document.cs` (`ValueObject`, construtor `protected Document()`, `public Document(string number)` que normaliza (remove `.` `-` `/` e espaços, `ToUpperInvariant`) e valida com `^\d{11}$` (CPF) ou `^[A-Z0-9]{12}\d{2}$` (CNPJ); `Number { get; private set; }`; `Type => Number.Length == 11 ? EDocumentType.Cpf : EDocumentType.Cnpj`; `static Document? FromOptional(string? number)`; `ToString()` com máscara `000.000.000-00` ou `XX.XXX.XXX/XXXX-00`). Apagar `src/TVSApp.Domain/ValueObjects/Customer/Cpf.cs` com `git rm`. Rodar T003, que deve passar
- [X] T005 Em `src/TVSApp.Domain/Entities/Customer.cs`: `Cpf Cpf` → `Document? Document`; o construtor recebe `Document? document`; `UpdateCpf(string cpf)` → `UpdateDocument(string? document) => Document = Document.FromOptional(document);` (depende de T004)

### Documento — renomear no restante do backend

- [X] T006 Renomear `Cpf` → `Document` em: `src/TVSApp.Application/Requests/CustomerRequests/CustomerCommands/CreateCustomerCommand.cs` e `UpdateCustomerCommand.cs` (parâmetro `string? Document`; remover a regra `if (Cpf.Length != 11)`; normalização `Document = Document?.Trim()`; remover `: IRequest<...>` e `using MediatR`); `src/TVSApp.Application/DTOs/CustomerDto.cs` e `src/TVSApp.Application/DTOs/ServiceOrder/ServiceOrderDto.cs` (`public string? Document { get; init; }`); `src/TVSApp.Application/Mappers/CustomerMapper.cs` (linhas 24 e 40–41: `Document = customer.Document?.Number` / `Document.FromOptional(customerDto.Document)`); `src/TVSApp.Application/Mappers/ServiceOrderMapper.cs` (linhas 39, 79, 116, mesma troca); `src/TVSApp.Application/Requests/ServiceOrderRequests/ServiceOrderCommands/EditServiceOrderCommand.cs` (linhas 21, 60: `string? Document`, `Document = Document?.ToUpper().Trim()`; remover a regra de obrigatório da linha 87) (depende de T005)
- [X] T007 Em `src/TVSApp.Infrastructure/Data/Mappings/CustomerMapping.cs`, trocar o `OwnsOne(c => c.Cpf, ...)` por `builder.OwnsOne(c => c.Document, document => { document.Property(d => d.Number).HasColumnName("Document").HasMaxLength(20); document.Ignore(d => d.Type); });` (sem `IsRequired`); ajustar o código que ainda usar `Customer.Cpf` (handlers de cliente atuais, `CreateCustomerHandler.cs` linha 39 → `Document.FromOptional(normalized.Document)`) até `dotnet build TVSApp.sln` passar (depende de T006)
- [X] T008 Gerar a migration: `dotnet ef migrations add RenameCustomerCpfToDocument --project src/TVSApp.Infrastructure --startup-project src/TVSApp.Api`. **Revisar à mão** o arquivo gerado para conter só: `Up` → `migrationBuilder.RenameColumn(name: "Cpf", table: "Customers", newName: "Document");` + `migrationBuilder.AlterColumn<string>(name: "Document", table: "Customers", type: "nvarchar(20)", maxLength: 20, nullable: true, oldClrType: typeof(string), oldType: "nvarchar(20)", oldMaxLength: 20);`; `Down` → o `AlterColumn` inverso (`nullable: false`) + `RenameColumn("Document" → "Cpf")`. Se o EF gerou `DropColumn`/`AddColumn`, substituir. Conferir que o `ApplicationDataContextModelSnapshot.cs` tem `Document` anulável (depende de T007)
- [X] T009 Atualizar `src/TVSApp.Tests/Handlers/Fakes/ServiceOrderFactory.cs` (`new Cpf("12345678909")` → `new Document("12345678909")`) e rodar `dotnet test src/TVSApp.Tests` (57 + T003 passando) (depende de T007)

### Rotas REST de cliente e CEP

- [X] T010 [P] Criar fakes `src/TVSApp.Tests/Handlers/Fakes/FakeCustomerQueryRepository.cs` (`ICustomerQueryRepository`: `Customers` configurável; `GetByIdAsync` → 404 "Cliente não encontrado." se não achar; `GetCustomerByName(name, limit)` filtra por `Contains` em maiúsculas e guarda `LastLimit`) e `FakeCustomerCommandRepository.cs` (`ICustomerCommandRepository`: `Created`/`Updated`, `CreateAsync` atribui `Id = 10`) (depende de T009)
- [X] T011 Criar `src/TVSApp.Tests/Handlers/CustomerHandlerTests.cs`: `SearchByName` com `"jo"` → 400 "Digite pelo menos 3 letras." e com `"joão"` → 200 e `LastLimit == 21`; `Create` sem nome → `CommandException` "O nome do cliente não pode estar vazio"; sem telefone → "O telefone do cliente não pode estar vazio"; sem documento → 200 com `Document == null`; com CNPJ alfanumérico → `Document == "12ABC34501DE35"`; com documento inválido → `ValueObjectException<Document>`; `Update` de id inexistente → 404 "Cliente não encontrado."; `Update` válido → 200 "Cliente atualizado com sucesso" e telefone trocado (depende de T010)
- [X] T012 Em `src/TVSApp.Domain/Repositories/Customers/ICustomerQueryRepository.cs` e `src/TVSApp.Infrastructure/Repositories/Customers/CustomerQueryRepository.cs`: `GetCustomerByName(string name, int limit)` com `.OrderByDescending(c => c.Id).Take(limit)` no lugar de `.OrderBy(Name).Take(6)`
- [X] T013 Criar `src/TVSApp.Application/Handlers/Customers/ICustomerQueryHandler.cs` + `CustomerQueryHandler.cs` (`SearchByName(string name)`: `name.Trim().Length < 3` → `BaseResponse<List<CustomerDto>>(null, 400, "Digite pelo menos 3 letras.")`; senão `GetCustomerByName(name, 21)` e mapear com `CustomerMapper`) e `ICustomerCommandHandler.cs` + `CustomerCommandHandler.cs` (`Create(CreateCustomerCommand)` e `Update(long id, UpdateCustomerCommand)`) copiando a lógica de `CreateCustomerHandler.cs` e `UpdateCustomerHandler.cs` (mesmas mensagens; `Update` usa o `id` da rota). Apagar com `git rm` `CreateCustomerHandler.cs`, `UpdateCustomerHandler.cs`, `GetCustomerByNameHandler.cs` e `src/TVSApp.Application/Requests/CustomerRequests/CustomersQueries/GetCustomerByNameQuery.cs`. Rodar T011, que deve passar (depende de T011, T012)
- [X] T014 Em `src/TVSApp.Application/DTOs/AddressDto.cs`: `public record AddressDto(string ZipCode, string Street, string Neighborhood, string City, string State);`. Em `src/TVSApp.Infrastructure/Services/AddressService/AddressService.cs`: o `ApiResponse` ganha `string? Uf` e `bool? Erro`; se `response is null` → 500 (como hoje); se `response.Erro == true` → `BaseResponse<AddressDto>(null, 404, "CEP não encontrado.")`; senão `new AddressDto(response.Cep, response.Logradouro, response.Bairro, response.Localidade, response.Uf ?? "")`
- [X] T015 Em `src/TVSApp.Api/Endpoints/CustomerEndpoints.cs`: remover `create-customer`, `update-customer`, `get-customer-by-name` e `get-address-by-zip-code`; criar `GET /customers` (`[FromQuery] string name` → `ICustomerQueryHandler.SearchByName`), `POST /customers` (`CreateCustomerCommand` → `Create`), `PUT /customers/{id:long}` (`UpdateCustomerCommand` → `Update(id, command)`), `GET /addresses/{cep}` (`IAddressService.GetAddressByCep(cep)` → `ToResult()`), todas com `.WithTags("Customer").RequireAuthorization()` (o de CEP também). Registrar `ICustomerQueryHandler`/`ICustomerCommandHandler` com `AddScoped` em `src/TVSApp.Api/Common/BuilderExtensions.cs` (depende de T013, T014)
- [X] T016 Rodar `dotnet build TVSApp.sln` e `dotnet test src/TVSApp.Tests`

### Frontend — base da página

- [X] T017 [P] Extrair de `web/src/app/features/home/modals/delivery/delivery-modal.component.ts` a lógica de PDF para `web/src/app/shared/pdf/pdf-print.ts`: `openPdfWindow(): Window | null` (chamar no clique), `showPdf(pdf: Blob, pdfWindow: Window | null, fileName: string)` (página com iframe + `print()` automático, ou download) e `readBlobErrorMessage(error: HttpErrorResponse, fallback: string): Promise<string>`. O modal de entrega passa a usar essas funções, sem mudar o comportamento
- [X] T018 [P] Em `web/src/app/shared/models/service-order.interface.ts`, trocar `cpf: string` por `document: string | null`. Criar os modelos em `web/src/app/features/add-service-order/models/`: `customer.model.ts` (`Customer` com `id`, `name`, `street`, `neighborhood`, `city`, `number`, `zipCode`, `state`, `phone`, `phone2`, `email`, `document`), `customer-form.model.ts` (`CustomerForm` = os mesmos campos sem `id`), `address.model.ts` (`Address` com `zipCode`, `street`, `neighborhood`, `city`, `state`), `create-service-order.model.ts` (`CreateServiceOrder` com `customerId`, `productBrand`, `productModel`, `productSerialNumber`, `productDefect`, `accessories`, `productType: EProduct`, `enterprise: EEnterprise`) e `shared/document.ts` (`formatDocument(value)`: máscara de CPF ou CNPJ pelo tamanho; `normalizeDocument(value)`: sem pontuação, em maiúsculas)
- [X] T019 Criar `web/src/app/features/add-service-order/add-service-order.service.ts` (`providedIn: 'root'`) conforme `contracts/http-endpoints.md`: `searchCustomers(name)`, `createCustomer(form)`, `updateCustomer(id, form)`, `getAddress(cep)`, `createServiceOrder(cmd)` (`observe: 'response'`, `responseType: 'blob'`) (depende de T018)
- [X] T020 Em `web/src/app/app.routes.ts`, adicionar `{ path: 'adicionar', component: AddServiceOrderComponent, canActivate: [authGuard] }` antes do `**`; em `web/src/app/shared/layout/base-layout/base-layout.component.ts`, o item `{ label: 'Adicionar OS', icon: 'add' }` ganha `route: '/adicionar'`. Criar o esqueleto `add-service-order.component.{ts,html,scss}` (selector `app-add-service-order`, dentro de `<app-base-layout>` como a Home) com as duas colunas (`section.customer-column` | `section.device-column`) e `footer.actions-bar` fixo; abaixo de `56rem`, colunas empilhadas. Estilos com `@use '../../shared/styles/brand'` e os mesmos overrides de form-field e botão do modal de orçamento (depende de T019)
- [X] T021 Rodar `cd web && npx ng build`

**Checkpoint**: documento opcional (com migration), rotas REST de cliente e de CEP, página vazia acessível pelo menu.

---

## Phase 3: User Story 1 - Abrir OS para cliente já cadastrado (Priority: P1) 🎯 MVP

**Goal**: buscar → confirmar ou atualizar no modal → cliente selecionado → criar OS com termo impresso.

**Independent Test**: quickstart.md §3, passos 1 a 6.

### Tests for User Story 1

- [X] T022 [US1] Em `src/TVSApp.Tests/Handlers/Fakes/FakeServiceOrderCommandRepository.cs`, adicionar `CreateResponse` configurável (padrão: devolve a OS recebida com `Id = 1` e `UpdateCustomer(ServiceOrderFactory customer)`) e `LastCreated`; em `FakeServiceOrderPdfGenerator.cs`, `CheckInResponse` + `CheckInCalls`. Em `ServiceOrderCommandHandlerTests.cs`, adicionar testes de `CreateServiceOrder`: `CustomerId = 0` → `CommandException` "O CustomerId da ordem de serviço não pode ser 0"; modelo vazio → "O modelo da ordem de serviço não pode estar vazio"; série vazia → "O número de série da ordem de serviço não pode estar vazio"; sucesso → `Code` 200, `Data.Id == 1`, `Data.Pdf` = bytes do fake, `LastCreated.Product.Model == "42LB5600"` (entrada `" 42lb5600 "`), `ServiceOrderStatus == Entered`, 1 `SendWelcomeMessageRequest("1", "JOÃO DA SILVA", "41999999999")`; `CreateResponse` com erro → 500 com a mensagem e sem PDF; `CheckInResponse` com erro → 500 com a mensagem, `LastCreated` preenchido e sem WhatsApp

### Implementation for User Story 1

- [X] T023 [US1] Em `src/TVSApp.Application/Requests/ServiceOrderRequests/ServiceOrderCommands/CreateServiceOrderCommand.cs`, remover `: IRequest<...>`; em `IServiceOrderCommandHandler.cs`/`ServiceOrderCommandHandler.cs`, criar o record `src/TVSApp.Application/DTOs/ServiceOrder/CreatedServiceOrderPdf.cs` (`record CreatedServiceOrderPdf(long Id, byte[] Pdf)`) e adicionar `Task<BaseResponse<CreatedServiceOrderPdf>> CreateServiceOrder(CreateServiceOrderCommand command)` exatamente como research.md R5 (no sucesso, `new BaseResponse<CreatedServiceOrderPdf>(new(serviceOrder.Id, pdf.Data!), pdf.Code, pdf.Message)`) (comentário apontando para `old-handlers/CreateServiceOrderAndReturnPdfHandler.cs`). Rodar T022, que deve passar (depende de T022)
- [X] T024 [US1] Em `src/TVSApp.Api/Endpoints/ServiceOrderEndpoints.cs`, remover `POST /create-service-order` e criar `POST /service-orders` (`CreateServiceOrderCommand` no corpo): erro → `result.ToResult()`; sucesso → hub "Atualizar", cabeçalho `X-Service-Order-Id: {result.Data.Id}` e `Results.File(result.Data.Pdf, "application/pdf", $"ordem_servico_{result.Data.Id}.pdf")`, com `.WithTags("ServiceOrder").RequireAuthorization()`. Em `src/TVSApp.Api/Common/BuilderExtensions.cs`, adicionar `.WithExposedHeaders("X-Service-Order-Id")` à política de CORS (depende de T023)
- [X] T025 [P] [US1] Criar `web/src/app/features/add-service-order/customer-search/customer-search.component.{ts,html,scss}` (selector `app-customer-search`): campo "Buscar cliente" com `debounceTime(300)` e `distinctUntilChanged`; com menos de 3 letras, mostra "Digite pelo menos 3 letras."; lista os primeiros 20 resultados (nome, telefone, CPF/CNPJ formatado); com 21, mostra "Refine a busca para ver mais."; sem resultado, "Nenhum cliente encontrado." + botão **Cadastrar novo cliente**; outputs `selected = output<Customer>()` e `createNew = output<string>()` (texto digitado) (depende de T019)
- [X] T026 [P] [US1] Criar `web/src/app/features/add-service-order/customer-dialog/customer-dialog.component.{ts,html,scss}` (`MatDialog`, centralizado, `width: '44rem'`, `maxWidth: '95vw'`): data `{ mode: 'existing' | 'new', customer?: Customer, name?: string }`; formulário reativo com nome (obrigatório), CPF/CNPJ, telefone (obrigatório), telefone 2, e-mail, CEP, rua, número, bairro, cidade, UF (máx. 2); CEP com 8 dígitos chama `getAddress` e preenche rua, bairro, cidade e UF (404 → aviso "CEP não encontrado."); modo `existing`: **Cancelar**, **Confirmar** (fecha com o cliente sem salvar) e **Salvar e usar** (habilitado só com `form.dirty`; `updateCustomer` → snackbar "Cliente atualizado." → fecha com o cliente devolvido); modo `new`: **Cancelar** e **Cadastrar e usar** (`createCustomer` → "Cliente cadastrado."); botões travados durante o envio; erro → snackbar com a mensagem e o modal continua aberto (depende de T019)
- [X] T027 [US1] Em `add-service-order.component.{ts,html}`: coluna da esquerda com `<app-customer-search>` (quando não há cliente) ou o cartão do cliente selecionado (nome, telefone, CPF/CNPJ formatado, endereço resumido e **Trocar cliente**); escolher um resultado abre `CustomerDialogComponent` em modo `existing`, e o retorno vira `selectedCustomer`. Coluna da direita: formulário do aparelho com Empresa (`mat-select` com `enumOptions('enterprise')`, padrão Particular), Tipo (`enumOptions('product')`, padrão TV), Marca, Modelo (obrigatório), Nº de série (obrigatório), Defeito (textarea) e Acessórios. Barra do rodapé: **Criar OS** (desabilitado sem cliente ou com o formulário inválido), que abre a aba do PDF no clique (`openPdfWindow()`), chama `createServiceOrder`, em sucesso mostra o PDF (`showPdf`), mostra o snackbar "OS {X-Service-Order-Id} criada." e reseta a página (cliente nulo, busca vazia, formulário com os padrões); em erro fecha a aba e mostra `readBlobErrorMessage` (depende de T024, T025, T026)
- [X] T028 [US1] Rodar `dotnet build TVSApp.sln`, `dotnet test src/TVSApp.Tests` e `cd web && npx ng build`

**Checkpoint**: OS para cliente existente de ponta a ponta.

---

## Phase 4: User Story 2 - Abrir OS para cliente novo (Priority: P1)

**Goal**: busca sem resultado → cadastro na mesma página → OS.

**Independent Test**: quickstart.md §3, passos 7 e 8.

- [X] T029 [US2] Em `add-service-order.component.ts`, tratar `(createNew)` do `app-customer-search`: abrir `CustomerDialogComponent` em modo `new` com `name` = texto digitado; o retorno vira `selectedCustomer` (depende de T027)
- [X] T030 [US2] Conferir no `customer-dialog` (modo `new`) que o CPF/CNPJ é opcional, que o valor enviado vai normalizado (`normalizeDocument`) e que a validação local aceita vazio, 11 dígitos ou 14 caracteres (`^[A-Z0-9]{12}\d{2}$`), mostrando "Informe um CPF (11 dígitos) ou um CNPJ (14 caracteres)." nos outros casos
- [X] T031 [US2] Rodar `cd web && npx ng build`

---

## Phase 5: User Story 3 - Correções e erros (Priority: P2)

**Goal**: trocar cliente sem perder o aparelho; erros com mensagem; sem clique duplo.

**Independent Test**: quickstart.md §3, passos 2, 4 e 9.

- [X] T032 [US3] Conferir em `add-service-order.component.ts`: **Trocar cliente** limpa só o cliente; **Criar OS** ignora cliques enquanto `isSending()` e mostra o spinner "Criando…"; mensagens de campo obrigatório ("Informe o modelo.", "Informe o número de série."); falha mantém tudo o que foi digitado
- [X] T033 [US3] Rodar `cd web && npx ng build`

---

## Phase 6: Polish & Cross-Cutting Concerns

- [ ] T034 [P] Executar quickstart.md §1 (inclusive `database update` numa cópia do banco, conferindo que os CPFs continuam em `Customers.Document`) e §2 (rotas antigas sem referência)
- [ ] T035 Executar quickstart.md §3 manualmente (API + Angular, usuário logado)
- [X] T036 [P] Remover imports e `using` não usados (MediatR nos commands, `Cpf` antigo) e rodar os builds de novo

---

## Dependencies & Execution Order

- Setup → Foundational (Domain T003–T005 → rename T006–T007 → migration T008 → T009; clientes T010–T016; frontend T017–T021) → US1 → US2 → US3 → Polish
- O frontend da Fase 2 (T017–T020) pode andar em paralelo com o backend depois de T002.
- US2 e US3 dependem da página da US1.

### Parallel Opportunities

- T003 ∥ T017 ∥ T018 (domínio, PDF e modelos do frontend)
- T010 ∥ T012 ∥ T014
- T025 ∥ T026 (busca e modal)
- T034 ∥ T036

## Parallel Example: User Story 1

```bash
Task: "T022–T024 backend da criação de OS"
Task: "T025 customer-search"
Task: "T026 customer-dialog"
```

## Implementation Strategy

1. Fase 2 inteira (o documento opcional e a migration são a base de tudo)
2. US1 → **STOP and VALIDATE** (quickstart §3, passos 1–6) → MVP
3. US2 → cliente novo
4. US3 → robustez
5. Polish; publicar API (com a migration) e frontend juntos
