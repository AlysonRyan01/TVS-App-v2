# Research: Tela "Buscar OS"

Base: `old-handlers/GetServiceOrderByIdHandler.cs`, `GetServiceOrdersByCustomerNameHandler.cs`,
`GetServiceOrdersByModelHandler.cs`, `GetServiceOrdersBySerialNumberHandler.cs`,
`GetServiceOrdersByDateHandler.cs`, `ServiceOrderQueryRepository` (consultas antigas por campo e as
das abas da Home), `ServiceOrderQueryHandler` (feature 007) e a Home (`mat-table` + `mat-paginator`).

## R1. Uma rota de busca com um filtro por vez

- **Decision**: `GET /service-orders/search?number=|customer=|model=|serialNumber=|from=&to=`
  `&pageNumber=1&pageSize=20`. O handler aceita **exatamente um** filtro (o período `from`/`to`
  conta como um); zero ou mais de um → 400 "Preencha um filtro por vez.".
- **Rationale**: a tela usa uma só chamada e um só formato de linha para todos os filtros; a regra
  "um filtro por vez" (Clarification Q2) fica garantida no servidor e testada.
- **Alternatives**: uma rota por campo, como no sistema antigo (`/service-orders/by-model` etc.):
  cinco endpoints e cinco formatos para a mesma tabela.

## R2. Regras de cada filtro (handler)

- `number`: > 0; busca exata pelo número.
- `customer`, `model`, `serialNumber`: `Trim()`; cliente precisa de ≥ 3 letras ("Digite pelo menos
  3 letras do cliente."); busca "contém" (`LIKE %termo%`, sem diferenciar maiúsculas).
- Período: `from` sem `to` → até hoje; `to` sem `from` → desde o início; `to < from` → 400 "A data
  final não pode ser antes da inicial."; o dia final entra inteiro (`EntryDate < to + 1 dia`).
- Paginação: `pageNumber ≥ 1`; `pageSize` entre 1 e 100 (padrão 20). Ordem: número decrescente.
- Diferenças do antigo, todas decididas na spec: cliente não fica limitado a 5 OS; período passa
  a ser paginado; filtros com espaços/maiúsculas são normalizados.

## R3. Formato da linha

- **Decision**: projeção `ServiceOrderSearchItem(Number, Customer, ProductType, Brand, Model,
  SerialNumber, Enterprise, ServiceOrderStatus, IsSale, EnteredAt)` em
  `Domain/Repositories/Responses.cs`, como as das abas da Home; `IsSale = Defect == "VENDA"` e a
  tela mostra "Venda" no lugar da situação.
- **Rationale**: consulta leve (só as colunas da tabela) e o mesmo padrão das abas.

## R4. Painel de detalhes

- **Decision**: reaproveitar `GET /service-orders/{id}` (já REST, devolve `ServiceOrderDto`
  completo) e mostrar num `MatDialog` só de leitura (`service-order-details-dialog`), em seções:
  Cliente, Aparelho, Empresa e situação, Orçamento e valores, Datas. Fecha com Esc/Fechar; a lista
  fica como estava (estado em signals da página).
- **Alternatives**: mandar tudo na busca (linhas pesadas); página própria `/buscar/{id}` (perde o
  estado da lista).

## R5. Rotas antigas

- **Decision**: remover as quebradas que esta tela substitui: `get-service-orders-by-customer-name`,
  `-by-serial-number`, `-by-model`, `-by-date` e as queries `GetServiceOrdersByCustomerNameQuery`,
  `...BySerialNumberQuery`, `...ByModelQuery`, `...ByDateQuery`. Também saem do
  `IServiceOrderQueryRepository` os métodos sem uso `GetByCustomerNameAsync`,
  `GetBySerialNumberAsync`, `GetByModelAsync`, `GetByDateRangeAsync`.
- **Fica**: `get-service-orders-by-enterprise` e `get-all-service-orders` (fora do escopo).

## R6. Frontend

- **Decision**: `features/search-service-orders/` com a página (barra de filtros no topo, tabela
  `mat-table` + `mat-paginator` de 20, total encontrado), serviço e o diálogo de detalhes. "Um
  filtro por vez": ao digitar num campo, os outros são limpos (`valueChanges`, sem disparar
  loop); De/Até são um grupo. Buscar com botão ou Enter; Limpar apaga filtros e resultados. Rota
  `/buscar` com `loadComponent` (orçamento de 1 MB) e menu "Buscar OS" → `/buscar`.
- Labels de situação/empresa/tipo via `enum-labels.ts` existente; datas `dd/MM/yyyy`.
