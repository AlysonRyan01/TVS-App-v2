# Data Model: Tela Clientes no padrão da tela de Ordens

Sem mudança de schema.

## CustomerSearchFilter (Domain, novo)

| Campo | Tipo | Regra (aplicada no handler antes de montar o filtro) |
|-------|------|------------------------------------------------------|
| `Name` | string? | aparado; se informado, ≥ 3 caracteres; "contém", sem maiúsculas/acentos |
| `PhoneDigits` | string? | só dígitos do texto digitado; se informado, ≥ 4 dígitos; procura em `Phone` ou `Phone2` (comparando só os dígitos gravados) |
| `DocumentDigits` | string? | sem `. - / espaço`, maiúsculas; se informado, ≥ 3 caracteres; "contém" em `Document` |

Filtros combinados (AND). Nenhum filtro → todos os clientes.

## CustomerSearchQuery (Application, query string)

`name?`, `phone?`, `document?`, `pageNumber = 1` (≥ 1), `pageSize = 20` (1–100).

## Resultado

`PaginatedResult<CustomerDto>` (existente): `items`, `totalCount`, `pageNumber`, `pageSize`, `totalPages`.
Ordem: `Id` decrescente (cadastrados por último primeiro).

`CustomerDto` (existente): `id`, `name`, `street`, `neighborhood`, `city`, `number`, `zipCode`, `state`,
`phone`, `phone2`, `email?`, `document?`.

## Grade (Angular)

| Coluna | Conteúdo |
|--------|----------|
| Nome | `name` |
| Telefone | `phone`; abaixo, menor, `phone2` se houver |
| CPF/CNPJ | `document` formatado (11 → CPF, 14 → CNPJ) ou "—" |
| Cidade/UF | `city` / `state` ou "—" |
| E-mail | `email` ou "—" |

Linha → `CustomerDialogComponent` (modo edição).
