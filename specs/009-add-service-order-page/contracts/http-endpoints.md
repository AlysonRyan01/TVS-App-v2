# Contract: Rotas da tela "Adicionar OS"

Todas: `.RequireAuthorization()`, resposta `BaseResponse<T>` via `.ToResult()` (exceto o PDF).

## Clientes

| Método e rota | Corpo | Resposta | Erros |
|---------------|-------|----------|-------|
| `GET /customers?name={texto}` | — | `List<CustomerDto>` (até 21, mais recentes primeiro) | 400 "Digite pelo menos 3 letras." |
| `POST /customers` | `CreateCustomerCommand` | `CustomerDto` | 400 validação (nome, telefone, documento) |
| `PUT /customers/{id}` | `UpdateCustomerCommand` (sem `Id` no corpo; vem da rota) | `CustomerDto`, "Cliente atualizado com sucesso" | 404 "Cliente não encontrado."; 400 validação |

## Endereço

| Método e rota | Resposta | Erros |
|---------------|----------|-------|
| `GET /addresses/{cep}` | `AddressDto { zipCode, street, neighborhood, city, state }` | 404 "CEP não encontrado."; 500 falha no serviço de CEP |

## Ordem de serviço

`POST /service-orders`, corpo `CreateServiceOrderCommand`:

| HTTP | Conteúdo | Quando |
|------|----------|--------|
| 200 | `application/pdf` (termo de entrada) + cabeçalho `X-Service-Order-Id: {id}` | criada |
| 400 | JSON `BaseResponse` | validação (cliente 0, modelo, série, tipo ou empresa inválidos) |
| 404 | JSON | cliente não encontrado |
| 500 | JSON | falha ao criar ou ao gerar o PDF (neste caso a OS fica criada) |

Efeito: sucesso → hub "Atualizar"; WhatsApp de boas-vindas enfileirado (todas as empresas).

## Rotas removidas

`get-customer-by-name`, `create-customer`, `update-customer`, `get-address-by-zip-code`,
`create-service-order`.

## Angular — `AddServiceOrderService`

| Método | Chamada |
|--------|---------|
| `searchCustomers(name)` | `GET /customers` com `params: { name }` |
| `createCustomer(cmd)` | `POST /customers` |
| `updateCustomer(id, cmd)` | `PUT /customers/{id}` |
| `getAddress(cep)` | `GET /addresses/{cep}` |
| `createServiceOrder(cmd)` | `POST /service-orders`, `observe: 'response'`, `responseType: 'blob'` |
