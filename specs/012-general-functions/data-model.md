# Data Model: Funções gerais

**Sem mudança de schema.**

## EditServiceOrderCommand (reescrito, sem MediatR)

| Campo | Regra | Mensagem (400) |
|-------|-------|----------------|
| Brand | trim/maiúsculas | — |
| Model | obrigatório | "O modelo da ordem de serviço não pode estar vazio" |
| SerialNumber | obrigatório | "O número de série da ordem de serviço não pode estar vazio" |
| Defect, Accessories, Location | trim/maiúsculas; vazio permitido | — |
| Type | enum válido | "Tipo de produto inválido." |
| Enterprise | enum válido | "Empresa inválida." |
| EntryDate | obrigatória | "A data de entrada é obrigatória." |
| InspectionDate, ResponseDate, RepairDate, PurchasePartDate, DeliveryDate | opcionais | — |
| Solution, Guarantee | trim/maiúsculas; vazio → mantém vazio | — |
| PartCost, LaborCost | ≥ 0 | "O custo da peça não pode ser negativo." / "O custo de mão de obra não pode ser negativo." |

## Efeito na ServiceOrder (handler `UpdateServiceOrder`)

- `Product.UpdateProduct(brand, model, serial, defect, accessories, type)` + `Product.AddLocation(location)`;
  se a OS é de venda (`Defect == "VENDA"`): defeito `"VENDA"` e acessórios `""` mantidos.
- `EditEnterprise`, `EditEntryDate`, `EditInspectionDate`, `EditResponseDate`, `EditRepairDate`,
  `EditPurchasePartDate`, `EditDeliveryDate`, `EditPartCost`, `EditLaborCost`.
- `EditSolution(new Solution(x))` e `EditGuarantee(new Guarantee(x))` com o valor (vazio grava vazio).
- **Não mudam**: cliente, situação, status do orçamento, resultado, mensagem do orçamento,
  pagamentos, código de segurança, custo de compra de peça.

## Reimprimir

`RegeneratePdf(id)` → `BaseResponse<byte[]>`: 404 se não existe; 500 se o PDF falhar.

## CustomerDialogData (frontend)

`{ customer: Customer; mode?: 'confirm' | 'edit' }` — padrão `'confirm'` (tela Adicionar OS / Vender).
