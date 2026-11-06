# Contract: GET /customers/search

Login exigido (Bearer). Caminho em produção: `/api/customers/search`.

## Query

| Parâmetro | Tipo | Regra |
|-----------|------|-------|
| `name` | string? | ≥ 3 caracteres; parte do nome, sem maiúsculas/acentos |
| `phone` | string? | só os dígitos contam; ≥ 4 dígitos; telefone principal ou secundário |
| `document` | string? | sem pontuação; ≥ 3 caracteres; parte do CPF/CNPJ |
| `pageNumber` | int | padrão 1 (≥ 1) |
| `pageSize` | int | padrão 20 (1–100) |

Filtros combinados. Sem filtros → todos, mais recentes primeiro.

## Respostas

| Situação | HTTP | `message` | `data` |
|----------|------|-----------|--------|
| Com resultados | 200 | "N clientes encontrados." | `PaginatedResult<CustomerDto>` |
| Sem resultados | 200 | "Nenhum cliente encontrado." | página vazia (`totalCount: 0`) |
| Nome curto | 400 | "Digite pelo menos 3 letras do nome." | null |
| Telefone curto | 400 | "Digite pelo menos 4 números do telefone." | null |
| Documento curto | 400 | "Digite pelo menos 3 caracteres do CPF/CNPJ." | null |
| Paginação inválida | 400 | "Paginação inválida." | null |

Exemplo: `GET /customers/search?phone=(41)%2099999-0000` encontra quem tem `41999990000`,
`(41) 99999-0000` ou `41 99999 0000` em `phone` ou `phone2`.

## Inalterado

`GET /customers?name=` (lista simples para o seletor de cliente) continua igual.

## Tela

- `/clientes`: filtros Nome, Telefone, CPF/CNPJ + Limpar/Buscar; grade (Nome, Telefone, CPF/CNPJ,
  Cidade/UF, E-mail); paginação 20; rodapé "Últimos clientes · N no total" / "N encontrados"; clique na
  linha → edição; tempo real.
- `/ordens` (antes `/buscar`, que redireciona): título e menu "Ordens".
