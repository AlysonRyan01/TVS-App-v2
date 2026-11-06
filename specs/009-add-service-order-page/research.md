# Research: Tela "Adicionar OS"

Nenhum `NEEDS CLARIFICATION` em aberto (layout, busca, pós-criação e documento resolvidos na spec).

## R1. Documento (CPF/CNPJ) no Domain

- **Decision**: o value object `Cpf` é substituído por `Document` (`src/TVSApp.Domain/ValueObjects/Customer/Document.cs`):
  - `Number` (string normalizada) e `Type` (`EDocumentType.Cpf` | `EDocumentType.Cnpj`, calculado
    pelo tamanho, **não persistido**);
  - normalização: remove `.`, `-`, `/` e espaços, e converte para maiúsculas;
  - validação: CPF `^\d{11}$` ou CNPJ `^[A-Z0-9]{12}\d{2}$` (alfanumérico desde 07/2026); se não
    bater → `ValueObjectException<Document>("Informe um CPF (11 dígitos) ou um CNPJ (14 caracteres).")`;
  - `ToString()` com máscara: `000.000.000-00` ou `AA.AAA.AAA/AAAA-00`;
  - fábrica `Document.FromOptional(string?)` → `null` quando vazio.
- `Customer.Cpf` → `Customer.Document` (tipo `Document?`); `UpdateCpf` → `UpdateDocument(string?)`.
- **Alternatives considered**: coluna `Cnpj` separada, ou `Document` + `DocumentType` persistido.
  Ambas foram rejeitadas na spec (o tipo sai do tamanho).
- **Sem dígitos verificadores** (Assumption da spec).

## R2. Migration

- **Decision**: `dotnet ef migrations add RenameCustomerCpfToDocument` e conferir/ajustar à mão para
  conter **apenas**:
  - `Up`: `RenameColumn("Cpf" → "Document", table: "Customers")` + `AlterColumn` para
    `nullable: true` (mantém `maxLength: 20`);
  - `Down`: `RenameColumn("Document" → "Cpf")` + `AlterColumn` de volta para `nullable: false`
    com `defaultValue: ""` (gerado pelo EF; clientes sem documento ficam com `""` ao reverter).
- **Rationale**: o EF pode gerar drop + add para propriedades owned renomeadas, o que **apagaria os
  CPFs**; por isso a migration é revisada à mão (Princípio III).
- O mapping (`CustomerMapping.cs`) passa a `OwnsOne(c => c.Document, d => d.Property(x => x.Number).HasColumnName("Document").HasMaxLength(20))`,
  sem `IsRequired`, e com `d.Ignore(x => x.Type)`.

## R3. Renomear Cpf → Document no restante

| Onde | Mudança |
|------|---------|
| `CreateCustomerCommand` / `UpdateCustomerCommand` | `Cpf` → `Document` (string, opcional); `Validate` sem a regra de 11 caracteres (passa a ser do value object) |
| `CustomerDto`, `ServiceOrderDto` | `Cpf` → `Document` (`string?`) |
| `CustomerMapper`, `ServiceOrderMapper` | `Document.FromOptional(...)` / `customer.Document?.Number` |
| `EditServiceOrderCommand` | `Cpf` → `Document`, sem a regra de obrigatório |
| `web/src/app/shared/models/service-order.interface.ts` | `cpf: string` → `document: string \| null` |

## R4. Handlers de cliente sem MediatR

- **Decision**: `ICustomerQueryHandler.SearchByName(string name)` e `ICustomerCommandHandler.Create(CreateCustomerCommand)` /
  `Update(long id, UpdateCustomerCommand)` em `src/TVSApp.Application/Handlers/Customers/`,
  portando a lógica atual de `CreateCustomerHandler`, `UpdateCustomerHandler` e
  `GetCustomerByNameHandler` (mesmas mensagens e normalizações). Os 3 handlers MediatR e o
  `GetCustomerByNameQuery` são removidos; os commands perdem `IRequest`. O registro `AddMediatR`
  continua (outros handlers de cliente e de notificação ainda usam).
- **Busca**: o repositório `GetCustomerByName` passa a ter `limit` (o handler pede 21) e ordenar por
  `Id` decrescente (mais recentes primeiro); o handler devolve até 21 itens e a tela mostra 20 + o
  aviso "Refine a busca para ver mais." quando vier o 21º. Mínimo de 3 letras: validado no handler
  (400 "Digite pelo menos 3 letras.") e na tela.

## R5. Criar OS (igual ao handler antigo)

- **Decision**: `IServiceOrderCommandHandler.CreateServiceOrder(CreateServiceOrderCommand)` →
  `BaseResponse<CreatedServiceOrderPdf>` (`record CreatedServiceOrderPdf(long Id, byte[] Pdf)`: o
  antigo devolvia só o PDF; o número da OS é acrescentado para a tela), reproduzindo `old-handlers/CreateServiceOrderAndReturnPdfHandler.cs` na
  ordem: `Validate()`; `Normalize()`; `new Product(...)`; `new ServiceOrder(enterprise, customerId, product)`;
  `CreateAsync` (se falhar → 500 com a mensagem); usar `Data` retornado (com `Customer` carregado
  pelo repositório); `GenerateCheckInDocumentAsync` (se falhar → 500 com a mensagem; a OS fica
  criada); `Enqueue(SendWelcomeMessageRequest(Id, Nome, Telefone))` para todas as empresas;
  retornar o número da OS e o PDF. O `CreateServiceOrderCommand` perde `IRequest`.
- **Nota**: o repositório responde 404 "Cliente não encontrado" se o `CustomerId` não existir; o
  antigo transformava qualquer falha em 500. Mantido (fidelidade).

## R6. Rotas REST

| Nova | Removida | Handler |
|------|----------|---------|
| `GET /customers?name=` | `GET /get-customer-by-name` | `ICustomerQueryHandler.SearchByName` |
| `POST /customers` | `POST /create-customer` | `ICustomerCommandHandler.Create` |
| `PUT /customers/{id:long}` | `PUT /update-customer` | `ICustomerCommandHandler.Update` |
| `GET /addresses/{cep}` | `GET /get-address-by-zip-code` | `IAddressService.GetAddressByCep` |
| `POST /service-orders` | `POST /create-service-order` | `IServiceOrderCommandHandler.CreateServiceOrder` |

- `POST /service-orders`: igual à entrega (006): erro → `ToResult()`; sucesso → hub "Atualizar" +
  `Results.File(data.Pdf, "application/pdf", $"ordem_servico_{data.Id}.pdf")`. O número da OS vai
  no cabeçalho `X-Service-Order-Id` (exposto via CORS `WithExposedHeaders`) para a tela mostrar
  "OS {número} criada.".
- `GET /addresses/{cep}`: `AddressDto` passa para nomes em inglês e ganha UF:
  `AddressDto(ZipCode, Street, Neighborhood, City, State)`. O ViaCEP responde `{"erro": true}`
  para CEP inexistente → 404 "CEP não encontrado.".

## R7. Frontend

- **Rota**: `{ path: 'adicionar', component: AddServiceOrderComponent, canActivate: [authGuard] }`;
  item "Adicionar OS" do menu com `route: '/adicionar'`.
- **Estrutura** (`web/src/app/features/add-service-order/`):
  - `add-service-order.component` (página, duas colunas + rodapé fixo);
  - `customer-search` (campo com `debounceTime(300)`, mínimo de 3 letras, lista);
  - `customer-dialog` (modal central do `MatDialog`, `width: 44rem`; modos `existing` — Confirmar /
    Salvar e usar — e `new` — Cadastrar e usar; CEP preenche o endereço);
  - `add-service-order.service.ts` (customers, addresses e service-orders).
- **PDF**: a lógica de abrir em nova aba + imprimir + baixar como alternativa (hoje dentro do
  `delivery-modal.component.ts`) é extraída para `web/src/app/shared/pdf/pdf-print.ts` e usada pela
  entrega e pela criação de OS.
- **Visual**: mesmas cores, tipografia e overrides de campos e botões dos modais (`_brand`,
  form-field outline, botão vermelho). Em telas abaixo de 56rem, as colunas se empilham.
- **Documento na tela**: campo "CPF/CNPJ" com máscara aplicada só na exibição; o valor enviado
  vai sem pontuação.

## R8. Testes (Princípio IV)

- Domain: `DocumentTests` (vazio → null, CPF, CPF com máscara, CNPJ numérico, CNPJ alfanumérico,
  minúsculas → maiúsculas, inválidos, `Type`, `ToString`).
- Application: `CustomerHandlerTests` (search: menos de 3 letras → 400, até 21 itens; create: nome
  e telefone obrigatórios, documento opcional e inválido; update: 404, atualiza), com
  `FakeCustomerQueryRepository` / `FakeCustomerCommandRepository`;
  `ServiceOrderCommandHandlerTests` + `CreateServiceOrder` (validação, normalização, falha ao
  criar → 500, falha no PDF → 500 com a OS criada, sucesso → PDF + WhatsApp de boas-vindas),
  com `FakeServiceOrderCommandRepository.CreateResponse` configurável e
  `FakeServiceOrderPdfGenerator.CheckInResponse`.
