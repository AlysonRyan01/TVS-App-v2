# Data Model: Tela "Adicionar OS"

## Document (novo value object, substitui `Cpf`)

| Campo | Tipo | Persistido | Regra |
|-------|------|------------|-------|
| `Number` | string (até 20) | sim, coluna `Customers.Document` (antes `Cpf`), **anulável** | normalizado: sem `.` `-` `/` e espaços, em maiúsculas |
| `Type` | `EDocumentType` (`Cpf` = 1, `Cnpj` = 2) | **não** | 11 caracteres → Cpf; 14 → Cnpj |

Validação (após normalizar):
- CPF: `^\d{11}$`
- CNPJ: `^[A-Z0-9]{12}\d{2}$` (alfanumérico)
- qualquer outro valor não vazio → "Informe um CPF (11 dígitos) ou um CNPJ (14 caracteres)."
- vazio/nulo → o cliente fica sem documento.

## Customer (alterado)

| Antes | Depois |
|-------|--------|
| `Cpf Cpf` (obrigatório) | `Document? Document` (opcional) |
| `UpdateCpf(string)` | `UpdateDocument(string?)` |

Os outros campos não mudam: nome (obrigatório), telefone (obrigatório), telefone 2, e-mail e endereço.

## Migration `RenameCustomerCpfToDocument`

```text
Up:   RenameColumn  Customers.Cpf → Customers.Document
      AlterColumn   Customers.Document  nvarchar(20) NULL
Down: RenameColumn  Customers.Document → Customers.Cpf
      AlterColumn   Customers.Cpf  nvarchar(20) NOT NULL DEFAULT ''   (documentos nulos viram '')
```

## ServiceOrder (sem mudança de schema)

Criada com `ServiceOrderStatus = Entered`, `RepairStatus = Entered`, `EntryDate = agora`,
`SecurityCode` gerado (construtor existente) → aparece em "Orçamentos pendentes".

## Contratos de dados

- `CreateCustomerCommand` / `UpdateCustomerCommand`: `Name`, `Street`, `Neighborhood`, `City`,
  `Number`, `ZipCode`, `State`, `Phone`, `Phone2`, `Email`, **`Document`** (antes `Cpf`), opcional.
- `CustomerDto`, `ServiceOrderDto`: **`Document`** (`string?`, antes `Cpf`).
- `CreateServiceOrderCommand` (sem mudança de campos): `CustomerId`, `ProductBrand`, `ProductModel`,
  `ProductSerialNumber`, `ProductDefect`, `Accessories`, `ProductType` (padrão Tv), `Enterprise`
  (padrão Particular).
- `AddressDto`: `ZipCode`, `Street`, `Neighborhood`, `City`, **`State`** (antes `Cep`, `Rua`,
  `Bairro`, `Cidade`, sem UF).
