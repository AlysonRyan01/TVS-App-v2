# Contract: endpoints de clientes afetados

**Feature**: [spec.md](../spec.md) | **Date**: 2026-11-07

Todos os endpoints exigem autenticação (`RequireAuthorization`) e usam o envelope
`BaseResponse` existente: `{ data, code, message }`.

## GET /customers?name={termo} (alterado)

Busca da barra "Buscar cliente", respondida pela lista em memória.

| Item | Antes | Depois |
|------|-------|--------|
| Origem | `LIKE '%TERMO%'` no banco a cada chamada | snapshot em memória |
| Item da lista | `CustomerDto` completo | `CustomerSearchItemDto { id, name, phone }` |
| Comparação | maiúsculas, sensível a acento | sem distinção de maiúsculas e de acento |
| Ordem / limite | `Id` desc / 21 | sem mudança |

**200**

```json
{
  "data": [
    { "id": 30412, "name": "JOSÉ DA SILVA", "phone": "41999990000" },
    { "id": 28877, "name": "JOSE SILVA SANTOS", "phone": "" }
  ],
  "code": 200,
  "message": "Clientes obtidos com sucesso!"
}
```

- `data` tem no máximo 21 itens. O cliente mostra 20 e, se receber 21, exibe "Refine a busca
  para ver mais".
- Nenhum resultado: `200` com `data: []`.

**400**: termo normalizado com menos de 3 letras.

```json
{ "data": null, "code": 400, "message": "Digite pelo menos 3 letras." }
```

**500**: falha ao montar a lista (banco indisponível). A resposta vem do `ExceptionMiddleware`
existente, com mensagem em português. Nada fica em cache e a chamada seguinte tenta de novo.

## GET /customers/{id} (novo)

Ficha completa, buscada quando o operador seleciona um resultado (FR-004).

**200**: `data` é o `CustomerDto` existente, no mesmo formato de `POST /customers` e
`PUT /customers/{id}`.

```json
{
  "data": {
    "id": 30412, "name": "JOSÉ DA SILVA",
    "street": "RUA A", "neighborhood": "CENTRO", "city": "CURITIBA", "number": "10",
    "zipCode": "80000000", "state": "PR",
    "phone": "41999990000", "phone2": "", "email": null, "document": null
  },
  "code": 200,
  "message": "Cliente recuperado com sucesso!"
}
```

**404**

```json
{ "data": null, "code": 404, "message": "O cliente com id:999 não existe" }
```

## POST /customers e PUT /customers/{id} (contrato sem mudança)

Request e response continuam iguais. A mudança é interna: quando a gravação termina com sucesso,
a lista em memória é invalidada antes de a resposta ser enviada. Uma busca feita depois da
resposta sempre enxerga o cliente criado ou alterado (FR-005).

## Frontend (`web/src/app/features/customers`)

- Novo tipo `CustomerSearchItem { id: number; name: string; phone: string }` em `models/`.
- `CustomersService.searchCustomers(name)` → `Observable<BaseResponse<CustomerSearchItem[]>>`.
- Novo `CustomersService.getCustomer(id)` → `Observable<BaseResponse<Customer>>`.
- `CustomerSearchComponent.selected` passa a emitir `CustomerSearchItem`.
- `CustomerPickerComponent.openExisting(item)` chama `getCustomer(item.id)` e abre o
  `CustomerDialogComponent` com o `Customer` completo. Em caso de erro, mostra snackbar
  "Não foi possível abrir o cliente. Tente de novo.".
