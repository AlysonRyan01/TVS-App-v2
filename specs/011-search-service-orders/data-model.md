# Data Model: Buscar OS

**Sem mudança de schema.**

## ServiceOrderSearchFilter (Domain, novo record)

| Campo | Tipo | Regra |
|-------|------|-------|
| Number | long? | > 0 |
| Customer | string? | trim; ≥ 3 letras |
| Model | string? | trim |
| SerialNumber | string? | trim |
| From | DateTime? | data (sem hora) |
| To | DateTime? | data; ≥ From |

Exatamente **um** entre: Number, Customer, Model, SerialNumber, período (From e/ou To).

## ServiceOrderSearchItem (Domain/Repositories/Responses.cs, novo record)

| Campo | Origem |
|-------|--------|
| Number | `Id` |
| Customer | `Customer.Name.CustomerName` |
| ProductType | `Product.Type` |
| Brand / Model / SerialNumber | `Product.*` |
| Enterprise | `Enterprise` |
| ServiceOrderStatus | `ServiceOrderStatus` |
| IsSale | `Product.Defect == "VENDA"` |
| EnteredAt | `EntryDate` |

## Repositório

`IServiceOrderQueryRepository.SearchAsync(ServiceOrderSearchFilter filter, int pageNumber, int pageSize)`
→ `BaseResponse<PaginatedResult<ServiceOrderSearchItem>>`, ordenado por `Id` decrescente; período
como `EntryDate >= From` e `EntryDate < To + 1 dia`.

## Handler

`IServiceOrderQueryHandler.Search(ServiceOrderSearchQuery query)` com
`ServiceOrderSearchQuery(long? Number, string? Customer, string? Model, string? SerialNumber,
DateTime? From, DateTime? To, int PageNumber = 1, int PageSize = 20)`.

| Regra | Resposta |
|-------|----------|
| nenhum filtro ou mais de um | 400 "Preencha um filtro por vez." |
| `Number <= 0` | 400 "Informe um número de OS válido." |
| cliente < 3 letras | 400 "Digite pelo menos 3 letras do cliente." |
| `To < From` | 400 "A data final não pode ser antes da inicial." |
| `PageNumber < 1` ou `PageSize` fora de 1–100 | 400 "Paginação inválida." |
| ok | 200 com a página (lista vazia se não achar) |

## Frontend

- `ServiceOrderSearchItem` (interface) e `ServiceOrderSearchFilter` (form): `number`, `customer`,
  `model`, `serialNumber`, `from`, `to`.
- Detalhes: `ServiceOrder` (interface existente em `shared/models/service-order.interface.ts`).
