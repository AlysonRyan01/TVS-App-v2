# Data Model: Tela "Vender"

**Sem mudança de schema** (nenhuma migration). A venda é uma `ServiceOrder` existente.

## OS de venda (ServiceOrder) — valores gravados

| Campo | Valor |
|-------|-------|
| `Enterprise` | `Particular` |
| `CustomerId` | do command |
| `Product.Brand / Model / SerialNumber` | do command, `Trim().ToUpper()` |
| `Product.Defect` | `"VENDA"` |
| `Product.Accessories` | `""` |
| `Product.Type` | do command (padrão `Tv`) |
| `Guarantee` | do command, `Trim().ToUpper()` |
| `PartCost` | `Amount` |
| `ServiceOrderStatus` | `Delivered` |
| `RepairResult` | `Repair` |
| `RepairStatus` | `Approved` |
| `EntryDate` / `DeliveryDate` | agora (UTC) |
| `SecurityCode` | gerado pelo construtor, como em toda OS |

## CreateSalesServiceOrderCommand (existente; sai o `IRequest`)

`(long CustomerId, string ProductBrand, string ProductModel, string ProductSerialNumber,
string Guarantee, decimal Amount, EProduct ProductType = Tv)`

| Regra (`Validate`) | Mensagem |
|--------------------|----------|
| `CustomerId == 0` | "O CustomerId da ordem de serviço não pode ser 0" |
| modelo vazio | "O modelo da ordem de serviço não pode estar vazio" |
| série vazia | "O número de série da ordem de serviço não pode estar vazio" |
| garantia vazia | "A venda precisa ter uma garantia" |
| tipo fora do enum | "O tipo do produto da ordem de serviço não tem um valor válido" |
| `Amount == 0` | "O valor da venda não pode ser vazio ou 0" |

## Resposta

`CreatedServiceOrderPdf(long Id, byte[] Pdf)` (record da feature 009).

## Frontend

`CreateSale` (model): `{ customerId, productType, productBrand, productModel, productSerialNumber,
guarantee, amount }`.
