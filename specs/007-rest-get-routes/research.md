# Research: Rotas GET das tabelas no padrão REST

Nenhum `NEEDS CLARIFICATION` em aberto.

## R1. Formato das rotas

- **Decision**:
  - `GET /service-orders/{fila}`, com `{fila}` ∈ `pending-estimate`, `awaiting-response`,
    `approved`, `rejected`, `awaiting-part`, `ready-for-pickup` (6 rotas literais, uma por fila);
  - `GET /service-orders/{id:long}` para a busca por número.
- **Paginação**: parâmetros de consulta com padrão no próprio endpoint:
  `(IServiceOrderQueryHandler handler, int pageNumber = 1, int pageSize = 25)`. No Minimal API,
  parâmetros simples fora da rota já vêm da query string, e o valor padrão os torna opcionais.
  Um valor não numérico gera 400 automático (falha de binding).
- **Conflitos**: segmentos literais (`/service-orders/approved`) têm prioridade sobre o parâmetro,
  e `{id:long}` só aceita números, então não há ambiguidade. As rotas de ação são `PUT`, então
  também não conflitam.
- **Alternatives considered**:
  - `GET /service-orders?status=approved`: rejeitado porque cada fila tem filtro e formato de
    resposta próprios (records diferentes), e um único endpoint com `status` misturaria tipos de
    resposta.
  - Paginação no caminho: é o que se está eliminando.

## R2. Handler e repositório sem mudança

- **Decision**: os métodos de `IServiceOrderQueryHandler` (`GetPendingEstimateServiceOrders` etc.
  e `GetServiceOrderById`) e o repositório não mudam. Só os endpoints e o frontend mudam.
- **Rationale**: a feature é de roteamento; as respostas MUST ser idênticas (FR-004).

## R3. Testes (FR-007)

- **Finding**: `TVSApp.Tests` referencia só Domain e Application. Testar as rotas HTTP (padrões
  de paginação) exigiria referenciar a Api e adicionar `Microsoft.AspNetCore.Mvc.Testing`, com
  autenticação e banco simulados. Isso é infraestrutura nova, maior que a feature.
- **Decision**:
  - a correspondência fila → consulta já está coberta por `ServiceOrderQueryHandlerTests` (6
    testes, feature 001), que continuam valendo;
  - os padrões de paginação (1 e 25) ficam como valores padrão dos parâmetros do endpoint e são
    verificados no quickstart §3 (chamada sem parâmetros);
  - não há regra de negócio nova, então o Princípio IV não pede teste novo.
- **Alternatives considered**: criar o projeto de testes de integração da Api. Recomendado como
  feature própria, porque serviria a todas as rotas (inclusive as de ação das features 002 a 006).

## R4. Frontend

- **Decision**:
  - `HomeService`: as 6 chamadas passam a `GET ${apiUrl}/service-orders/{fila}` com
    `params: { pageNumber, pageSize }` (objeto de params do `HttpClient`);
  - os 6 serviços de modal (`estimate`, `estimate-response`, `part-purchase`, `rejected-release`,
    `repair`, `delivery`): `getServiceOrderById(id)` → `GET ${apiUrl}/service-orders/${id}`.
- Os nomes dos métodos não mudam; só as URLs.
