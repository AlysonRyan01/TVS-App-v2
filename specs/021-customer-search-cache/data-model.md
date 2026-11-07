# Data Model: Busca de clientes em memória

**Feature**: [spec.md](./spec.md) | **Date**: 2026-11-07

Não há mudanças no banco nem migrations. Os modelos abaixo existem só em memória, na
Application, e são derivados da tabela `Customers`, que continua sendo a fonte da verdade.

## CustomerSearchEntry (Application)

Uma entrada por cliente cadastrado. É imutável.

| Campo | Tipo | Origem | Regra |
|-------|------|--------|-------|
| `Id` | `long` | `Customers.Id` | Também define a ordem (maior = mais recente). |
| `Name` | `string` | `Customers.Name` | Exibido como está gravado. |
| `Phone` | `string` | `Customers.Phone` | Telefone principal. Pode ser vazio (a tela mostra "Sem telefone"). |
| `SearchKey` | `string` | calculado | `CustomerNameNormalizer.Normalize(Name)`: sem acento, maiúsculas, espaços colapsados. |

Construção: `CustomerSearchEntry.Create(id, name, phone)` calcula a `SearchKey`. Nome `null`
vira `""`, e uma entrada sem nome nunca corresponde a um termo de 3 letras ou mais.

## Snapshot (conteúdo da entrada do cache)

- `IReadOnlyList<CustomerSearchEntry>` com **todas** as entradas, ordenada por `Id` decrescente.
- Chave no `IMemoryCache`: `customers:search-index`.
- Expiração absoluta: 1 hora após a montagem (FR-009).

### Estados e transições

```text
             primeira busca / expirou / invalidado
   [Ausente] ────────────────────────────────────▶ [Montando] (semáforo, 1 por vez)
       ▲                                             │      │
       │ Invalidate() (geração++)                    │ ok e │ ok, mas geração mudou
       │ ou expiração de 1 h                         │ geração│ durante a montagem
       │                                             ▼ igual  ▼
   [Válido] ◀──────────────── grava no cache ──── [Pronto]  devolve à requisição sem gravar → [Ausente]
                                                     │
                                       falha no banco│→ exceção sobe, nada gravado → [Ausente]
```

## CustomerSearchItemDto (Application → resposta HTTP)

Item da resposta de `GET /customers?name=`. Ver [contracts/customers-api.md](./contracts/customers-api.md).

| Campo | Tipo |
|-------|------|
| `id` | `long` |
| `name` | `string` |
| `phone` | `string` |

## Regra de busca (CustomerQueryHandler.SearchByName)

1. `termo = CustomerNameNormalizer.Normalize(name)`. Se `termo.Length < 3`, retorna 400
   "Digite pelo menos 3 letras.".
2. Percorre o snapshot em ordem e seleciona as entradas com `SearchKey.Contains(termo, Ordinal)`.
3. Para em **21** itens (`SearchLimit`). A tela mostra 20 e usa o 21º só para saber que há mais.
4. Mapeia para `CustomerSearchItemDto`.

## Modelos que não mudam

- `Customer` (Domain), `CustomerDto`, tabela `Customers`: sem alteração.
- `ICustomerQueryRepository`: perde `GetCustomerByName`, que fica sem uso. `GetByIdAsync` passa a
  ser usado também pelo novo `GET /customers/{id}`.
