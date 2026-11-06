# Research: Tela Clientes no padrão da tela de Ordens

Estado atual: `GET /customers?name=` (nome obrigatório, até N resultados, mais recentes primeiro) usado
pelo `CustomerSearchComponent` (lista suspensa) na tela Clientes e pelo seletor de cliente de OS/venda.
Busca de OS: `GET /service-orders/search` → `ServiceOrderSearchFilter` (Domain) →
`IServiceOrderQueryRepository.SearchAsync` → `PaginatedResult` (`items`, `totalCount`, `pageNumber`,
`pageSize`, `totalPages`); tela com filtros, `mat-table`, `mat-paginator` (20), "Últimas ordens · N no
total" / "N encontradas". Dados do cliente: nome (gravado em MAIÚSCULAS), `Phone`/`Phone2` (como digitados,
com ou sem pontuação, inclusive do sistema antigo), `Document` (normalizado: 11 dígitos CPF ou 14 do CNPJ,
que pode ter letras), endereço, e-mail. Banco: SQL Server/Azure SQL com collation padrão
`SQL_Latin1_General_CP1_CI_AS` (sem diferenciar maiúsculas, mas diferenciando acentos).

## R1. Rota nova, sem quebrar a antiga

- **Decision**: `GET /customers/search?name=&phone=&document=&pageNumber=1&pageSize=20` (login exigido),
  retorna `BaseResponse<PaginatedResult<CustomerDto>>`. `GET /customers?name=` fica como está.
- **Rationale**: o seletor de cliente (abrir OS/venda) continua igual; a rota nova espelha
  `/service-orders/search` (mesmos nomes de paginação, mesmo envelope).
- **Alternatives**: estender `GET /customers` com filtros opcionais e paginação (mudaria a resposta de
  lista para página e quebraria o seletor).

## R2. Regras dos filtros (no handler)

- **Decision**:
  - Filtros **combinados** (AND), todos opcionais; sem filtro → todos, mais recentes primeiro.
  - `name`: aparado; se informado, mínimo **3** caracteres ("Digite pelo menos 3 letras do nome.");
    busca por "contém", sem diferenciar maiúsculas e **sem diferenciar acentos**.
  - `phone`: só dígitos; se sobrar algo, mínimo **4** dígitos ("Digite pelo menos 4 números do
    telefone."); procura em `Phone` **ou** `Phone2`.
  - `document`: remove `. - / espaço`, maiúsculas (mesma normalização do `Document`); mínimo **3**
    caracteres; "contém".
  - Paginação: `pageNumber ≥ 1`, `pageSize` 1–100 ("Paginação inválida."), padrão 20.
  - Mensagem: "N clientes encontrados." / "Nenhum cliente encontrado.".
- **Rationale**: atende FR-005..008; mínimos evitam consultas que retornam a base inteira por engano.

## R3. Consulta no banco

- **Decision** (Infrastructure, EF Core):
  - Nome sem acento: `EF.Functions.Collate(c.Name.CustomerName, "Latin1_General_CI_AI").Contains(term)`
    — o termo também em maiúsculas.
  - Telefone só dígitos no banco: `Phone` com `Replace` de `(`, `)`, `-`, ` `, `.`, `+` encadeados
    (traduzido para `REPLACE` no SQL) e `Contains(digits)`; idem `Phone2`.
  - Documento: `c.Document.Number.Contains(term)` (já normalizado ao gravar).
  - `OrderByDescending(c => c.Id)`, `Count` + `Skip/Take`, `AsNoTracking`.
- **Rationale**: telefones antigos têm formatos variados; sem migrar dados nem criar coluna nova
  (Princípio III, sem mudança de schema). Volume de clientes de uma loja é pequeno para `REPLACE` no `WHERE`.
- **Alternatives**: coluna calculada/índice só com dígitos (exigiria migration — desnecessário agora);
  filtrar em memória (não escala, quebra paginação).

## R4. Tela Clientes (Angular)

- **Decision**: refazer `customers-page` no molde de `search-service-orders`:
  formulário reativo com `name`, `phone`, `document`; botões Limpar/Buscar; `mat-table` com colunas
  **Nome**, **Telefone** (principal + secundário em linha menor), **CPF/CNPJ** (formatado no front:
  `000.000.000-00` / `00.000.000/0000-00`), **Cidade/UF**, **E-mail**; `mat-paginator` (20, sem seletor de
  tamanho); rodapé "Últimos clientes · N no total" / "N encontrados"; vazio: "Nenhum cliente
  cadastrado." / "Nenhum cliente encontrado."; linha clicável (clique, Enter, `role="button"`) abre o
  `CustomerDialogComponent` em modo edição; ao fechar com alteração → recarrega a página atual com os
  mesmos filtros; `ServiceOrderHubService` (`Atualizar`) → recarrega a página atual.
  A última requisição vence (`switchMap`), erros mostram snackbar e mantêm a grade.
- O `CustomerSearchComponent` continua existindo (usado no seletor de cliente) mas sai desta tela.
- Celular: `StackedTableDirective` + `@include stacked-table($inset: 0.75rem)` (cartões).

## R5. Estilo comum

- **Decision**: extrair de `search-service-orders.component.scss` o layout de página (filtros no topo,
  folha branca com área rolável, rodapé com total e paginação) para `shared/styles/_list-page.scss`
  (`@mixin list-page`) e usar nas duas telas.
- **Rationale**: SC-004 (mesmo arranjo, lado a lado) sem duplicar CSS; mudanças futuras valem para as duas.

## R6. Renomear "Buscar OS" → "Ordens"

- **Decision**: menu `{ label: 'Ordens', icon: 'receipt_long', route: '/ordens' }`; rota `ordens` com
  `title: 'Ordens'`; `{ path: 'buscar', redirectTo: 'ordens' }`; `app-base-layout title="Ordens"`;
  README atualizado. Componente/arquivos mantêm o nome técnico atual (`search-service-orders`).
- **Rationale**: FR-012 e SC-005 (nenhum "Buscar OS" visível); links antigos continuam funcionando.
