# Data Model: Padronização de nomenclatura das filas da tela Home

Nenhuma entidade persistida muda. Este documento lista os **tipos de resposta e de comando**
renomeados (contrato entre a API e o Angular). Arquivo .NET: `src/TVSApp.Domain/Repositories/Responses.cs`.

## Tipos de resposta das filas

Os campos aparecem em PascalCase no .NET e em camelCase no TypeScript/JSON.

### PendingEstimateServiceOrder (nome mantido)

| Campo | Tipo .NET | Tipo TS | Antes |
|-------|-----------|---------|-------|
| Number | long | number | = |
| Customer | string | string | = |
| Company | string | keyof EEnterprise | = |
| Model | string | string | = |
| Defect | string? | string \| null | = |
| EnteredAt | DateTime | string (ISO) | `EntryDate` |

### AwaitingResponseServiceOrder (antes `WaitingResponseServiceOrder`)

| Campo | Tipo .NET | Antes |
|-------|-----------|-------|
| Number | long | = |
| Customer | string | = |
| Model | string | = |
| Solution | string? | = |
| Amount | decimal | = |
| EvaluatedAt | DateTime? | `InspectionIn` |

### ApprovedServiceOrder (nome mantido)

| Campo | Tipo .NET | Antes |
|-------|-----------|-------|
| Number, Company, Customer, Model, Solution | — | = |
| ApprovedAt | DateTime? | `ApprovedIn` |

### RejectedServiceOrder (antes `RefusedServiceOrder`)

| Campo | Tipo .NET | Antes |
|-------|-----------|-------|
| Number, Customer, Model, Solution | — | = |
| RejectedAt | DateTime? | `RefusedIn` |

### AwaitingPartServiceOrder (antes `WaitingPartServiceOrder`)

| Campo | Tipo .NET | Antes |
|-------|-----------|-------|
| Number, Customer, Model, Solution | — | = |
| PartPurchasedAt | DateTime? | `PurchasedIn` |

### ReadyForPickupServiceOrder (antes `ReadyServiceOrder`)

| Campo | Tipo .NET | Tipo TS | Antes |
|-------|-----------|---------|-------|
| Number, Customer, Model | — | — | = |
| RepairedAt | DateTime? | string \| null | `RepairDate` |
| RepairResult | string? | keyof ERepairResult | `Situation` |
| RepairStatus | string? | keyof ERepairStatus | `Response` |

## Comando de orçamento

### AddServiceOrderEstimateCommand (.NET) ↔ AddServiceOrderEstimate (TS, antes `AddServiceOrderQuote`)

| Campo | Tipo .NET | Antes | Regra (inalterada) |
|-------|-----------|-------|--------------------|
| ServiceOrderId | long | = | — |
| Warranty | string | `Guarantee` | `Trim().ToUpper()`; mapeado para `ServiceOrder.Guarantee` |
| Solution | string | = | `Trim().ToUpper()`; obrigatório |
| TotalCost | decimal | = | ≥ 0 |
| RepairResult | ERepairResult | = | enum válido |
| EstimateMessage | string | = | obrigatório |
| PartPurchaseCost | decimal | = | ≥ 0, padrão 0 |

Mensagens de validação continuam em português e iguais às atuais.

## Estado (inalterado)

`ServiceOrder.AddEstimate` mantém as transições atuais. Não há mudança de domínio.

- Com conserto → `Evaluated` / `Waiting` (fila **AwaitingResponse**)
- Sem conserto ou sem defeito → `ReadyToPickUp` / `Approved` (fila **ReadyForPickup**)
- PhilcoBritania com conserto → `Evaluated` / `Approved` (fila **Approved**)

## Paginação (inalterada)

`PaginatedResult<T>` { items, pageNumber, pageSize, totalCount } envolvido em `BaseResponse<T>`.
